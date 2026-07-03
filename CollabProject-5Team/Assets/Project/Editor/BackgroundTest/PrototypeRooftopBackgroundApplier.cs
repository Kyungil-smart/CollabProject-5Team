using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

namespace GameDevTycoon.EditorTools
{
    internal static class TransformHierarchyPathExtensions
    {
        public static string GetHierarchyPath(this Transform transform)
        {
            if (transform == null)
                return string.Empty;

            string path = transform.name;
            Transform current = transform.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }
    }

    public static class PrototypeRooftopBackgroundApplier
    {
        private const string TexturePath = "Assets/Project/Art/Texture/OfficeRooftopBackground.png";
        private const string MaterialPath = "Assets/Project/Art/Materials/OfficeRooftopBackground.mat";
        private const string ShaderPath = "Assets/Project/Art/Shaders/PrototypeCameraBackground.shader";
        private const string ObjectName = "Prototype_RooftopBackground";
        private const string ShaderName = "Prototype/Camera Background";

        [InitializeOnLoadMethod]
        private static void ApplyWhenEditorLoads()
        {
            EditorApplication.delayCall += ApplyToActiveSceneIfNeeded;
        }

        [MenuItem("Tools/Prototype/Apply Rooftop Background")]
        public static void ApplyToActiveSceneIfNeeded()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded)
                return;

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (texture == null)
            {
                AssetDatabase.Refresh();
                texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            }

            if (texture == null)
            {
                Debug.LogWarning($"[{nameof(PrototypeRooftopBackgroundApplier)}] 배경 텍스처를 찾지 못했습니다: {TexturePath}");
                return;
            }

            Material material = GetOrCreateMaterial(texture);
            GameObject quad = GameObject.Find(ObjectName);
            if (quad == null)
            {
                quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = ObjectName;
            }

            Bounds officeBounds = CalculateOfficeBounds();
            Camera camera = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                Debug.LogWarning($"[{nameof(PrototypeRooftopBackgroundApplier)}] Main Camera를 찾지 못했습니다.");
                return;
            }

            ApplyCameraFacingTransform(quad.transform, camera, officeBounds);

            Collider collider = quad.GetComponent<Collider>();
            if (collider != null)
                Object.DestroyImmediate(collider);

            Renderer renderer = quad.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            PrototypeCameraFixedBackground fixedBackground = quad.GetComponent<PrototypeCameraFixedBackground>();
            if (fixedBackground == null)
                fixedBackground = quad.AddComponent<PrototypeCameraFixedBackground>();

            fixedBackground.Configure(camera, 1.35f, CalculateBackgroundDistance(camera, officeBounds));

            if (!Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
                Debug.Log($"[{nameof(PrototypeRooftopBackgroundApplier)}] 테스트 배경을 씬에 적용했습니다: {ObjectName}, center={quad.transform.position}, scale={quad.transform.localScale}");
            }
        }

        [MenuItem("Tools/Prototype/Log Background Renderer Candidates")]
        public static void LogBackgroundRendererCandidates()
        {
            Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            int logged = 0;

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !renderer.gameObject.scene.IsValid())
                    continue;

                if (renderer.GetComponent<CanvasRenderer>() != null || renderer is ParticleSystemRenderer)
                    continue;

                Bounds bounds = renderer.bounds;
                float footprint = Mathf.Max(bounds.size.x, 0f) * Mathf.Max(bounds.size.z, 0f);
                if (footprint < 20f)
                    continue;

                Material material = renderer.sharedMaterial;
                string materialName = material != null ? material.name : "(no material)";
                string shaderName = material != null && material.shader != null ? material.shader.name : "(no shader)";

                Debug.Log($"[Background Candidate] {renderer.transform.GetHierarchyPath()} | footprint={footprint:F1}, boundsCenter={bounds.center}, boundsSize={bounds.size}, material={materialName}, shader={shaderName}", renderer.gameObject);
                logged++;
            }

            Debug.Log($"[Background Candidate] 총 {logged}개 후보를 출력했습니다.");
        }

        private static Material GetOrCreateMaterial(Texture2D texture)
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null)
                shader = Shader.Find(ShaderName);

            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");

            if (shader == null)
                shader = Shader.Find("Unlit/Texture");

            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null)
            {
                bool changed = false;
                if (shader != null && material.shader != shader)
                {
                    material.shader = shader;
                    changed = true;
                }

                changed |= AssignTexture(material, texture);

                if (changed && !Application.isPlaying)
                {
                    EditorUtility.SetDirty(material);
                    AssetDatabase.SaveAssets();
                }

                return material;
            }

            material = new Material(shader);
            material.name = "OfficeRooftopBackground";
            AssignTexture(material, texture);
            material.renderQueue = 1000;

            AssetDatabase.CreateAsset(material, MaterialPath);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static bool AssignTexture(Material material, Texture2D texture)
        {
            bool changed = false;

            if (material.HasProperty("_BaseMap"))
            {
                changed |= material.GetTexture("_BaseMap") != texture;
                material.SetTexture("_BaseMap", texture);
            }

            if (material.HasProperty("_MainTex"))
            {
                changed |= material.GetTexture("_MainTex") != texture;
                material.SetTexture("_MainTex", texture);
            }

            if (material.HasProperty("_Cull"))
            {
                changed |= !Mathf.Approximately(material.GetFloat("_Cull"), 0f);
                material.SetFloat("_Cull", 0f);
            }

            if (material.HasProperty("_ZWrite"))
            {
                changed |= !Mathf.Approximately(material.GetFloat("_ZWrite"), 0f);
                material.SetFloat("_ZWrite", 0f);
            }

            if (material.HasProperty("_ZTest"))
            {
                changed |= !Mathf.Approximately(material.GetFloat("_ZTest"), (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                material.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
            }

            changed |= material.renderQueue != 3000;
            material.renderQueue = 3000;

            return changed;
        }

        private static void ApplyCameraFacingTransform(Transform background, Camera camera, Bounds officeBounds)
        {
            Vector3 cameraPosition = camera.transform.position;
            Vector3 cameraForward = camera.transform.forward;
            float distance = CalculateBackgroundDistance(camera, officeBounds);

            background.position = cameraPosition + cameraForward * distance;
            background.rotation = camera.transform.rotation;

            float aspect = Mathf.Max(0.1f, camera.aspect);
            float height;
            float width;

            if (camera.orthographic)
            {
                height = camera.orthographicSize * 2f;
                width = height * aspect;
            }
            else
            {
                height = 2f * distance * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
                width = height * aspect;
            }

            background.localScale = new Vector3(width * 1.35f, height * 1.35f, 1f);
        }

        private static float CalculateBackgroundDistance(Camera camera, Bounds officeBounds)
        {
            Vector3 cameraPosition = camera.transform.position;
            Vector3 cameraForward = camera.transform.forward;
            float officeDistance = Vector3.Dot(officeBounds.center - cameraPosition, cameraForward);
            float officeDepth = Mathf.Max(officeBounds.extents.magnitude, 8f);
            float distance = Mathf.Max(camera.nearClipPlane + 1f, officeDistance + officeDepth);

            if (camera.farClipPlane > camera.nearClipPlane + 10f)
                distance = Mathf.Min(distance, camera.farClipPlane - 5f);

            return distance;
        }

        private static Bounds CalculateOfficeBounds()
        {
            Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            bool hasBounds = false;

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || renderer.gameObject.name == ObjectName)
                    continue;

                if (!renderer.gameObject.scene.IsValid())
                    continue;

                if (renderer.GetComponent<CanvasRenderer>() != null)
                    continue;

                if (renderer is ParticleSystemRenderer)
                    continue;

                if (renderer.transform.root.name.StartsWith("Canvas_"))
                    continue;

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds ? bounds : new Bounds(Vector3.zero, new Vector3(12f, 2f, 12f));
        }
    }
}
