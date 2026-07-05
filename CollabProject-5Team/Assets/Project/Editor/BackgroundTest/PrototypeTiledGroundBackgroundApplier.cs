using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace GameDevTycoon.EditorTools
{
    public static class PrototypeTiledGroundBackgroundApplier
    {
        private const string TexturePath = "Assets/Project/Art/Texture/OfficeGroundCityBackground.png";
        private const string MaterialPath = "Assets/Project/Art/Materials/PrototypeExteriorGroundTile.mat";
        private const string GroundObjectName = "Prototype_TiledExteriorGround";
        private const string LegacyImageBackgroundName = "Prototype_RooftopBackground";

        [InitializeOnLoadMethod]
        private static void ApplyWhenEditorLoads()
        {
            EditorApplication.delayCall += ApplyToActiveSceneIfNeeded;
        }

        [MenuItem("Tools/Prototype/Apply Tiled Ground Background")]
        public static void ApplyToActiveSceneIfNeeded()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded)
                return;

            RemoveLegacyImageBackground();

            Texture2D texture = GetBackgroundTexture();
            if (texture == null)
            {
                Debug.LogWarning($"[{nameof(PrototypeTiledGroundBackgroundApplier)}] 배경 이미지 텍스처를 찾지 못했습니다: {TexturePath}");
                return;
            }

            Material material = GetOrCreateTileMaterial(texture);

            Bounds officeBounds = CalculateOfficeBounds();
            GameObject ground = GameObject.Find(GroundObjectName);
            if (ground == null)
            {
                ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = GroundObjectName;
            }

            Collider collider = ground.GetComponent<Collider>();
            if (collider != null)
                Object.DestroyImmediate(collider);

            ApplyGroundTransform(ground.transform, officeBounds);

            Renderer renderer = ground.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;

            if (!Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
                Debug.Log($"[{nameof(PrototypeTiledGroundBackgroundApplier)}] 이미지 텍스처 바닥 배경을 적용했습니다: center={ground.transform.position}, scale={ground.transform.localScale}");
            }
        }

        private static void RemoveLegacyImageBackground()
        {
            GameObject legacy = GameObject.Find(LegacyImageBackgroundName);
            if (legacy != null)
                Object.DestroyImmediate(legacy);
        }

        private static Texture2D GetBackgroundTexture()
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (texture == null)
            {
                AssetDatabase.Refresh();
                texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            }

            if (texture != null)
            {
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;
                EditorUtility.SetDirty(texture);
            }

            return texture;
        }

        private static Material GetOrCreateTileMaterial(Texture2D texture)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");

            if (shader == null)
                shader = Shader.Find("Standard");

            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "PrototypeExteriorGroundTile" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else if (shader != null && material.shader != shader)
            {
                material.shader = shader;
            }

            AssignTexture(material, texture);
            material.renderQueue = 2000;

            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0.18f);

            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", 0f);

            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static void AssignTexture(Material material, Texture2D texture)
        {
            Vector2 tiling = Vector2.one;

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
                material.SetTextureScale("_BaseMap", tiling);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
                material.SetTextureScale("_MainTex", tiling);
            }

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", Color.white);

            if (material.HasProperty("_Color"))
                material.SetColor("_Color", Color.white);
        }

        private static void ApplyGroundTransform(Transform ground, Bounds officeBounds)
        {
            Vector3 center = officeBounds.center;
            float width = Mathf.Max(officeBounds.size.x * 4.5f, 48f);
            float depth = Mathf.Max(officeBounds.size.z * 5.8f, 62f);

            ground.position = new Vector3(center.x, officeBounds.min.y - 0.08f, center.z);
            ground.rotation = Quaternion.identity;
            ground.localScale = new Vector3(width / 10f, 1f, depth / 10f);
        }

        private static Bounds CalculateOfficeBounds()
        {
            Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            bool hasBounds = false;

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                if (!renderer.gameObject.scene.IsValid())
                    continue;

                if (renderer.gameObject.name == GroundObjectName || renderer.gameObject.name == LegacyImageBackgroundName)
                    continue;

                if (renderer.GetComponent<CanvasRenderer>() != null || renderer is ParticleSystemRenderer)
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
