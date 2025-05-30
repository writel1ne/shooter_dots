using System.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace shooter_game.scripts.DOTS.Collisions
{
    [RequireComponent(typeof(MeshFilter))]
    [BurstCompile]
    public class WorldCollisionBaker : MonoBehaviour
    {
        [SerializeField] private float _collisionSendRate = 1;
        [SerializeField] private bool _sendEachFixedUpdate;
        [SerializeField] private bool _parentBaker = false;
        private WaitForSeconds _cooldown;
        private Coroutine _coroutine;
        private Entity _entity;
        private EntityManager _entityManager;
        private readonly WaitForFixedUpdate _fixedUpdate = new();

        private bool _isFirstUpdate = true;

        private bool _isStatic;
        private MeshFilter _meshFilter;

        private void Start()
        {
            if (_parentBaker)
            {
                MeshFilter[] objects = FindObjectsByType<MeshFilter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (var obj in objects)
                {
                    obj.gameObject.AddComponent(typeof(WorldCollisionBaker));
                }
            }
            
            // _collider = GetComponent<Collider>();
            // _isStatic = _collider.gameObject.isStatic;
            // _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            // _cooldown = new WaitForSeconds(_collisionSendRate > 0.05f ? _collisionSendRate : 0.05f);
            // _entity = _entityManager.CreateEntity();
        }

        private void OnEnable()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _isStatic = gameObject.isStatic;
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _cooldown = new WaitForSeconds(_collisionSendRate > 0.05f ? _collisionSendRate : 0.05f);
            _entity = _entityManager.CreateEntity();

            if (_coroutine != null) StopCoroutine(_coroutine);

            _coroutine = StartCoroutine(CollisionSender());
        }

        private void OnDisable()
        {
            if (_coroutine != null) StopCoroutine(_coroutine);
        }

        [BurstCompile]
        private IEnumerator CollisionSender()
        {
            do
            {
                SendCollision();
                yield return _sendEachFixedUpdate ? _fixedUpdate : _cooldown;
            } while (!_isStatic);
        }

        [BurstCompile]
        private void SendCollision()
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            ecb.AddComponent(_entity,
                new GameObjectUpdateSyncColliderRequest
                {
                    Bounds = _meshFilter.sharedMesh.bounds, Quaternion = transform.rotation,
                    Scale = transform.lossyScale, Center = _meshFilter.transform.position
                });

            ecb.Playback(_entityManager);
            ecb.Dispose();
        }
    }
}