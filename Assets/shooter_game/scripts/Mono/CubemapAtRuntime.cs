using UnityEngine;

namespace shooter_game.scripts.animation.input_data
{
    public class CubemapAtRuntime : MonoBehaviour
    {
        [Header("Cubemap Settings")]
        public int cubemapSize = 128; // Размер каждой грани кубмапы (должен быть степенью двойки)

        public CubemapFace_ClearFlags
            clearFlags = CubemapFace_ClearFlags.Skybox; // Как очищать буфер перед рендером каждой грани

        public Color backgroundColor = Color.blue; // Цвет фона, если clearFlags = Color
        public LayerMask cullingMask = -1; // Какие слои рендерить в кубмапу
        public float nearClipPlane = 0.1f;
        public float farClipPlane = 1000f;

        [Header("Target Object")] public GameObject objectToApplyTo; // Объект, на который навесим кубмапу (опционально)

        public enum ApplyMode
        {
            Skybox,
            MaterialProperty
        }

        public ApplyMode applyMode = ApplyMode.MaterialProperty;

        public string
            materialPropertyName =
                "_ReflectionTex"; // Имя свойства текстуры в шейдере (часто _Cube, _Tex, _ReflectionTex, _EnvMap)

        private Cubemap generatedCubemap;
        private Camera renderCamera;

        void Start()
        {
            // 1. Создаем объект Cubemap
            generatedCubemap = new Cubemap(cubemapSize, TextureFormat.RGBA32, true); // true для мип-уровней

            // 2. Создаем и настраиваем временную камеру для рендеринга
            GameObject camGO = new GameObject("CubemapRenderCam");
            renderCamera = camGO.AddComponent<Camera>();

            // Позиционируем камеру там, откуда хотим снимать окружение
            // Например, в центре этого объекта или в указанной точке
            renderCamera.transform.position = transform.position;
            // Можно и вращение задать, но RenderToCubemap его переопределит для каждой грани
            renderCamera.transform.rotation = Quaternion.identity;

            renderCamera.cullingMask = cullingMask;
            renderCamera.nearClipPlane = nearClipPlane;
            renderCamera.farClipPlane = farClipPlane;

            // Установка флагов очистки для камеры (влияет на RenderToCubemap)
            switch (clearFlags)
            {
                case CubemapFace_ClearFlags.Skybox:
                    renderCamera.clearFlags = CameraClearFlags.Skybox;
                    break;
                case CubemapFace_ClearFlags.Color:
                    renderCamera.clearFlags = CameraClearFlags.SolidColor;
                    renderCamera.backgroundColor = backgroundColor;
                    break;
                case CubemapFace_ClearFlags.Depth:
                    renderCamera.clearFlags = CameraClearFlags.Depth;
                    break;
                case CubemapFace_ClearFlags.Nothing:
                    renderCamera.clearFlags = CameraClearFlags.Nothing;
                    break;
            }

            // 3. Рендерим сцену в кубмапу
            // Этот метод отрендерит все 6 граней
            bool success = renderCamera.RenderToCubemap(generatedCubemap);

            if (!success)
            {
                Debug.LogError("Failed to render to cubemap.");
                Destroy(camGO); // Очистка
                return;
            }

            // Опционально: сглаживание кубмапы, если нужно (может быть медленно)
            // generatedCubemap.SmoothEdges(); 
            generatedCubemap.Apply(); // Применяем изменения (если были SmoothEdges или SetPixel)

            Debug.Log("Cubemap generated successfully!");

            // 4. Применяем кубмапу
            ApplyCubemap();

            // 5. Очищаем временную камеру
            Destroy(camGO);
        }

        void ApplyCubemap()
        {
            if (generatedCubemap == null)
            {
                Debug.LogError("Cannot apply null cubemap.");
                return;
            }

            switch (applyMode)
            {
                case ApplyMode.Skybox:
                    // Создаем новый материал для скайбокса (или используем существующий)
                    Material skyboxMaterial = new Material(Shader.Find("Skybox/Cubemap"));
                    skyboxMaterial.SetTexture("_Tex", generatedCubemap);
                    RenderSettings.skybox = skyboxMaterial;
                    DynamicGI.UpdateEnvironment(); // Обновить освещение окружения
                    Debug.Log("Cubemap applied as Skybox.");
                    break;

                case ApplyMode.MaterialProperty:
                    if (objectToApplyTo != null)
                    {
                        Renderer rend = objectToApplyTo.GetComponent<Renderer>();
                        if (rend != null && rend.material != null)
                        {
                            // Убедитесь, что у материала есть свойство с таким именем
                            if (rend.material.HasProperty(materialPropertyName))
                            {
                                // Важно: работаем с копией материала, чтобы не изменить ассет
                                Material instanceMaterial = rend.material;
                                instanceMaterial.SetTexture(materialPropertyName, generatedCubemap);
                                Debug.Log(
                                    $"Cubemap applied to material property '{materialPropertyName}' on {objectToApplyTo.name}.");
                                
                                Texture assignedTexture = instanceMaterial.GetTexture(materialPropertyName);
                                if (assignedTexture == generatedCubemap)
                                {
                                    Debug.Log($"VERIFIED: Cubemap '{generatedCubemap.name}' (InstanceID: {generatedCubemap.GetInstanceID()}) successfully assigned and retrieved from material property '{materialPropertyName}'.", generatedCubemap);
                                }
                                else if (assignedTexture != null)
                                {
                                    Debug.LogWarning($"VERIFICATION FAILED: A different texture ('{assignedTexture.name}', InstanceID: {assignedTexture.GetInstanceID()}) is in the slot. Expected '{generatedCubemap.name}' (InstanceID: {generatedCubemap.GetInstanceID()}).", assignedTexture);
                                }
                                else
                                {
                                    Debug.LogWarning($"VERIFICATION FAILED: No texture is in the slot '{materialPropertyName}' after assignment.");
                                }
                            }
                            else
                            {
                                Debug.LogError(
                                    $"Material on {objectToApplyTo.name} does not have property '{materialPropertyName}'. Common names: _Cube, _Tex, _MainTex, _ReflectionTex, _EnvMap");
                            }
                        }
                        else
                        {
                            Debug.LogError("ObjectToApplyTo does not have a Renderer or Material.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("ObjectToApplyTo is not set for MaterialProperty mode.");
                    }

                    break;
            }
        }

        // Для повторного рендеринга, если нужно (например, каждую секунду)
        public void RegenerateCubemap()
        {
            if (renderCamera != null && generatedCubemap != null)
            {
                renderCamera.transform.position = transform.position; // Обновляем позицию
                bool success = renderCamera.RenderToCubemap(generatedCubemap);
                if (success)
                {
                    // generatedCubemap.SmoothEdges();
                    generatedCubemap.Apply();
                    Debug.Log("Cubemap regenerated.");
                    // Повторное применение не всегда нужно, если материал уже ссылается на эту кубмапу.
                    // Но для скайбокса или если вы меняете саму кубмапу (не только ее контент), может понадобиться.
                    // ApplyCubemap(); 
                }
                else
                {
                    Debug.LogError("Failed to regenerate cubemap.");
                }
            }
        }

        void OnDestroy()
        {
            // Очистка при уничтожении объекта
            if (renderCamera != null)
            {
                Destroy(renderCamera.gameObject);
            }

            if (generatedCubemap != null)
            {
                Destroy(generatedCubemap);
            }
            // Если вы создавали материал скайбокса, его тоже можно здесь удалить,
            // если он больше нигде не используется.
            // if (RenderSettings.skybox != null && RenderSettings.skybox.GetTexture("_Tex") == generatedCubemap)
            // {
            //     Destroy(RenderSettings.skybox);
            //     RenderSettings.skybox = null; // Или установить дефолтный
            // }
        }

        // Enum для удобства выбора флагов очистки в инспекторе
        public enum CubemapFace_ClearFlags
        {
            Skybox,
            Color,
            Depth,
            Nothing
        }
    }
}