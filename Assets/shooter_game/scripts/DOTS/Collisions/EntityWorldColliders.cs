using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace shooter_game.scripts.DOTS.Collisions
{
    [BurstCompile]
    public struct EntityWorldColliders : IComponentData
    {
        public NativeHashMap<Entity, OBB> Colliders;
    }

    [BurstCompile]
    public struct GameObjectUpdateSyncColliderRequest : IComponentData
    {
        public Bounds Bounds;
        public quaternion Quaternion;
        public float3 Scale;
        public float3 Center;
    }

    [BurstCompile]
    public struct OBB
    {
        public float3 Center;
        public float3 Extents;
        public quaternion Rotation;
        
        private float3 _axisX;
        private float3 _axisY;
        private float3 _axisZ;
        private bool _axesInitialized;
        
        public float3 AxisX
        {
            get
            {
                if (!_axesInitialized) InitializeAxes();
                return _axisX;
            }
        }

        public float3 AxisY
        {
            get
            {
                if (!_axesInitialized) InitializeAxes();
                return _axisY;
            }
        }

        public float3 AxisZ
        {
            get
            {
                if (!_axesInitialized) InitializeAxes();
                return _axisZ;
            }
        }

        private void InitializeAxes()
        {
            _axisX = math.mul(Rotation, new float3(1, 0, 0));
            _axisY = math.mul(Rotation, new float3(0, 1, 0));
            _axisZ = math.mul(Rotation, new float3(0, 0, 1));
            _axesInitialized = true;
        }

        public OBB(AABB aabb, Quaternion orientation, float3 scale, float3 center)
        {
            Center = center;
            Extents = math.abs(new float3(aabb.Extents.x * scale.x, aabb.Extents.y * scale.y,
                aabb.Extents.z * scale.z));
            Rotation = orientation;
            _axesInitialized = false;
            _axisX = default;
            _axisY = default;
            _axisZ = default;
        }

        public OBB(Bounds aabb, Quaternion orientation, float3 scale, float3 center) : this(aabb.ToAABB(),
            orientation, scale, center)
        {
        }

        public float3[] GetCorners()
        {
            if (!_axesInitialized) InitializeAxes();
            var corners = new float3[8];
            var scaledX = _axisX * Extents.x;
            var scaledY = _axisY * Extents.y;
            var scaledZ = _axisZ * Extents.z;

            corners[0] = Center - scaledX - scaledY - scaledZ;
            corners[1] = Center + scaledX - scaledY - scaledZ;
            corners[2] = Center - scaledX + scaledY - scaledZ;
            corners[3] = Center + scaledX + scaledY - scaledZ;
            corners[4] = Center - scaledX - scaledY + scaledZ;
            corners[5] = Center + scaledX - scaledY + scaledZ;
            corners[6] = Center - scaledX + scaledY + scaledZ;
            corners[7] = Center + scaledX + scaledY + scaledZ;

            return corners;
        }

        public void DrawGizmos(Color color)
        {
            if (approximately(Extents.x, float3.zero.x) &&
                approximately(Extents.y, float3.zero.y) &&
                approximately(Extents.z, float3.zero.z)
               ) return;

            var corners = GetCorners();
            Gizmos.color = color;

            Gizmos.DrawLine(corners[0], corners[1]);
            Gizmos.DrawLine(corners[1], corners[3]);
            Gizmos.DrawLine(corners[3], corners[2]);
            Gizmos.DrawLine(corners[2], corners[0]);

            Gizmos.DrawLine(corners[4], corners[5]);
            Gizmos.DrawLine(corners[5], corners[7]);
            Gizmos.DrawLine(corners[7], corners[6]);
            Gizmos.DrawLine(corners[6], corners[4]);

            Gizmos.DrawLine(corners[0], corners[4]);
            Gizmos.DrawLine(corners[1], corners[5]);
            Gizmos.DrawLine(corners[2], corners[6]);
            Gizmos.DrawLine(corners[3], corners[7]);
        }

        public bool Intersects(ref EntityWorldColliders others)
        {
            foreach (var obb in others.Colliders)
                if (Intersects(obb.Value))
                {
                    return true;
                }

            // valueArray.Dispose();
            return false;
        }

        /// <summary>
        ///     Проверяет пересечение этого OBB с другим OBB с использованием теоремы о разделяющей оси (SAT).
        /// </summary>
        public bool Intersects(OBB other)
        {
            if (!_axesInitialized) InitializeAxes();
            if (!other._axesInitialized) other.InitializeAxes();

            // Оси первого OBB
            //Vector3[] axesA = { this.AxisX, this.AxisY, this.AxisZ };
            // Оси второго OBB
            //Vector3[] axesB = { other.AxisX, other.AxisY, other.AxisZ };

            // Вектор между центрами OBB
            var T = other.Center - Center;

            // Матрица абсолютных значений скалярных произведений осей
            // R[i][j] = | Dot(axesA[i], axesB[j]) |
            //float[,] R = new float[3, 3];
            var R = new float3x3();
            // Матрица абсолютных значений проекций T на оси (для удобства)
            // t[i] = | Dot(T, axesA[i]) |
            // t[j+3] = | Dot(T, axesB[j]) |
            float absTAx, absTAy, absTAz;

            // --- Тесты по осям первого OBB (A) ---
            // Ось A.AxisX
            absTAx = math.abs(math.dot(T, AxisX));
            if (absTAx > Extents.x + (other.Extents.x * math.abs(math.dot(AxisX, other.AxisX)) +
                                      other.Extents.y * math.abs(math.dot(AxisX, other.AxisY)) +
                                      other.Extents.z * math.abs(math.dot(AxisX, other.AxisZ)))) return false;
            // Ось A.AxisY
            absTAy = math.abs(math.dot(T, AxisY));
            if (absTAy > Extents.y + (other.Extents.x * math.abs(math.dot(AxisY, other.AxisX)) +
                                      other.Extents.y * math.abs(math.dot(AxisY, other.AxisY)) +
                                      other.Extents.z * math.abs(math.dot(AxisY, other.AxisZ)))) return false;
            // Ось A.AxisZ
            absTAz = math.abs(math.dot(T, AxisZ));
            if (absTAz > Extents.z + (other.Extents.x * math.abs(math.dot(AxisZ, other.AxisX)) +
                                      other.Extents.y * math.abs(math.dot(AxisZ, other.AxisY)) +
                                      other.Extents.z * math.abs(math.dot(AxisZ, other.AxisZ)))) return false;

            // --- Тесты по осям второго OBB (B) ---
            // Ось B.AxisX
            if (math.abs(math.dot(T, other.AxisX)) > other.Extents.x +
                (Extents.x * math.abs(math.dot(AxisX, other.AxisX)) +
                 Extents.y * math.abs(math.dot(AxisY, other.AxisX)) +
                 Extents.z * math.abs(math.dot(AxisZ, other.AxisX)))) return false;
            // Ось B.AxisY
            if (math.abs(math.dot(T, other.AxisY)) > other.Extents.y +
                (Extents.x * math.abs(math.dot(AxisX, other.AxisY)) +
                 Extents.y * math.abs(math.dot(AxisY, other.AxisY)) +
                 Extents.z * math.abs(math.dot(AxisZ, other.AxisY)))) return false;
            // Ось B.AxisZ
            if (math.abs(math.dot(T, other.AxisZ)) > other.Extents.z +
                (Extents.x * math.abs(math.dot(AxisX, other.AxisZ)) +
                 Extents.y * math.abs(math.dot(AxisY, other.AxisZ)) +
                 Extents.z * math.abs(math.dot(AxisZ, other.AxisZ)))) return false;

            // Заполняем матрицу R для тестов с кросс-произведениями
            // Это уже было сделано неявно выше, но можно сделать явно для R
            for (var i = 0; i < 3; i++)
            for (var j = 0; j < 3; j++)
            {
                //R[i, j] = math.abs(math.dot(axesA[i], axesB[j]));
                float3 thisAxis;
                float3 otherAxis;
                switch (i)
                {
                    case 0:
                        thisAxis = AxisX;
                        break;
                    case 1:
                        thisAxis = AxisY;
                        break;
                    default:
                        thisAxis = AxisZ;
                        break;
                }

                switch (j)
                {
                    case 0:
                        otherAxis = other.AxisX;
                        break;
                    case 1:
                        otherAxis = other.AxisY;
                        break;
                    default:
                        otherAxis = other.AxisZ;
                        break;
                }

                R[i][j] = math.abs(math.dot(thisAxis, otherAxis));
            }

            var epsilon = 1e-5f; // Малое значение для проверки на параллельность/нулевой вектор

            // --- Тесты по осям, полученным из векторных произведений ---
            // L = A.AxisX x B.AxisX
            float ra, rb; // Projected radii
            float3 L;

            L = math.cross(AxisX, other.AxisX);
            if (math.lengthsq(L) > epsilon)
            {
                // Проверяем, что оси не параллельны (L не нулевой вектор)
                // L не обязательно нормализован, но формула работает: | Dot(T,L) | > ra*|L| + rb*|L|
                // ra = ExtentsA.y*R[2,0] + ExtentsA.z*R[1,0] (проекция на ось, перпендикулярную A.AxisX и L)
                // rb = ExtentsB.y*R[0,2] + ExtentsB.z*R[0,1] (проекция на ось, перпендикулярную B.AxisX и L)
                ra = Extents.y * R[2][0] + Extents.z * R[1][0];
                rb = other.Extents.y * R[0][2] + other.Extents.z * R[0][1];
                if (math.abs(math.dot(T, L)) > ra + rb) return false;
            }

            // L = A.AxisX x B.AxisY
            L = math.cross(AxisX, other.AxisY);
            if (math.lengthsq(L) > epsilon)
            {
                ra = Extents.y * R[2][1] + Extents.z * R[1][1];
                rb = other.Extents.x * R[0][2] + other.Extents.z * R[0][0];
                if (math.abs(math.dot(T, L)) > ra + rb) return false;
            }

            // L = A.AxisX x B.AxisZ
            L = math.cross(AxisX, other.AxisZ);
            if (math.lengthsq(L) > epsilon)
            {
                ra = Extents.y * R[2][2] + Extents.z * R[1][2];
                rb = other.Extents.x * R[0][1] + other.Extents.y * R[0][0];
                if (math.abs(math.dot(T, L)) > ra + rb) return false;
            }

            // L = A.AxisY x B.AxisX
            L = math.cross(AxisY, other.AxisX);
            if (math.lengthsq(L) > epsilon)
            {
                ra = Extents.x * R[2][0] + Extents.z * R[0][0];
                rb = other.Extents.y * R[1][2] +
                     other.Extents.z *
                     R[1][1]; // Было: other.Extents.y * R[0,2] + other.Extents.z * R[0,1]; (ошибка в индексах B)
                if (math.abs(math.dot(T, L)) > ra + rb) return false;
            }

            // L = A.AxisY x B.AxisY
            L = math.cross(AxisY, other.AxisY);
            if (math.lengthsq(L) > epsilon)
            {
                ra = Extents.x * R[2][1] + Extents.z * R[0][1];
                rb = other.Extents.x * R[1][2] +
                     other.Extents.z * R[1][0]; // Было: other.Extents.x * R[0,2] + other.Extents.z * R[0,0];
                if (math.abs(math.dot(T, L)) > ra + rb) return false;
            }

            // L = A.AxisY x B.AxisZ
            L = math.cross(AxisY, other.AxisZ);
            if (math.lengthsq(L) > epsilon)
            {
                ra = Extents.x * R[2][2] + Extents.z * R[0][2];
                rb = other.Extents.x * R[1][1] +
                     other.Extents.y * R[1][0]; // Было: other.Extents.x * R[0,1] + other.Extents.y * R[0,0];
                if (math.abs(math.dot(T, L)) > ra + rb) return false;
            }

            // L = A.AxisZ x B.AxisX
            L = math.cross(AxisZ, other.AxisX);
            if (math.lengthsq(L) > epsilon)
            {
                ra = Extents.x * R[1][0] + Extents.y * R[0][0];
                rb = other.Extents.y * R[2][2] +
                     other.Extents.z * R[2][1]; // Было: other.Extents.y * R[0,2] + other.Extents.z * R[0,1];
                if (math.abs(math.dot(T, L)) > ra + rb) return false;
            }

            // L = A.AxisZ x B.AxisY
            L = math.cross(AxisZ, other.AxisY);
            if (math.lengthsq(L) > epsilon)
            {
                ra = Extents.x * R[1][1] + Extents.y * R[0][1];
                rb = other.Extents.x * R[2][2] +
                     other.Extents.z * R[2][0]; // Было: other.Extents.x * R[0,2] + other.Extents.z * R[0,0];
                if (math.abs(math.dot(T, L)) > ra + rb) return false;
            }

            // L = A.AxisZ x B.AxisZ
            L = math.cross(AxisZ, other.AxisZ);
            if (math.lengthsq(L) > epsilon)
            {
                ra = Extents.x * R[1][2] + Extents.y * R[0][2];
                rb = other.Extents.x * R[2][1] +
                     other.Extents.y * R[2][0]; // Было: other.Extents.x * R[0,1] + other.Extents.y * R[0,0];
                if (math.abs(math.dot(T, L)) > ra + rb) return false;
            }

            // Если ни одна из 15 осей не разделяет OBB, они пересекаются
            return true;
        }

        private static bool approximately(float a, float b, float epsilon = 0.00001f) // Выберите эпсилон по умолчанию
        {
            return math.abs(a - b) < float.Epsilon;
        }

        // Вспомогательный метод для проекции OBB на ось (не используется в текущей реализации SAT, но полезен для понимания)
        /*
        private void GetProjectionInterval(Vector3 axis, out float min, out float max)
        {
            // Проекция центра OBB на ось
            float centerProjection = math.dot(Center, axis);

            // "Радиус" проекции OBB на ось
            float projectedRadius =
                Extents.x * math.abs(math.dot(AxisX, axis)) +
                Extents.y * math.abs(math.dot(AxisY, axis)) +
                Extents.z * math.abs(math.dot(AxisZ, axis));

            min = centerProjection - projectedRadius;
            max = centerProjection + projectedRadius;
        }
        */
    }
}