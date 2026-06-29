#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ToonMaterialFallbackConverter
{
    private const string BrokenToonShaderName = "Toon/Toon";

    private static readonly string[] CharacterPrefabRoots =
    {
        "Assets/Project/Prefabs/Employee",
        "Assets/Project/Prefabs/HBPrefabs"
    };

    [MenuItem("Tools/Rendering/Fix Toon Character Materials To URP Simple Lit")]
    public static void ConvertCharacterPrefabMaterials()
    {
        Shader targetShader = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (targetShader == null)
        {
            targetShader = Shader.Find("Universal Render Pipeline/Lit");
        }

        if (targetShader == null)
        {
            Debug.LogError("[Toon Material Fix] URP Simple Lit/Lit shader를 찾을 수 없습니다. URP 패키지 상태를 확인해주세요.");
            return;
        }

        List<Material> targetMaterials = CollectCharacterPrefabMaterials()
            .Where(material => material != null && material.shader != null && material.shader.name == BrokenToonShaderName)
            .Distinct()
            .OrderBy(AssetDatabase.GetAssetPath)
            .ToList();

        if (targetMaterials.Count == 0)
        {
            Debug.Log("[Toon Material Fix] 캐릭터/플레이어 프리팹에서 Toon/Toon 머티리얼을 찾지 못했습니다.");
            return;
        }

        int convertedCount = 0;
        foreach (Material material in targetMaterials)
        {
            string materialPath = AssetDatabase.GetAssetPath(material);
            Texture baseTexture = ReadTexture(material, "_BaseMap", "_MainTex", "_MainTex_ST");
            Color baseColor = ReadColor(material, Color.white, "_BaseColor", "_Color");

            Undo.RecordObject(material, "Convert Toon Material To URP");
            material.shader = targetShader;
            WriteTexture(material, baseTexture, "_BaseMap", "_MainTex");
            WriteColor(material, baseColor, "_BaseColor", "_Color");
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);

            Debug.Log($"[Toon Material Fix] Converted: {materialPath} -> {targetShader.name}");
            convertedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Toon Material Fix] 완료: {convertedCount}개 캐릭터 머티리얼을 {targetShader.name} 셰이더로 변경했습니다.");
    }

    private static HashSet<Material> CollectCharacterPrefabMaterials()
    {
        var materials = new HashSet<Material>();

        foreach (string root in CharacterPrefabRoots)
        {
            if (!AssetDatabase.IsValidFolder(root))
            {
                continue;
            }

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { root });
            foreach (string guid in prefabGuids)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    continue;
                }

                foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
                {
                    foreach (Material sharedMaterial in renderer.sharedMaterials)
                    {
                        if (sharedMaterial != null)
                        {
                            materials.Add(sharedMaterial);
                        }
                    }
                }
            }
        }

        return materials;
    }

    private static Texture ReadTexture(Material material, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            if (material.HasProperty(propertyName))
            {
                Texture texture = material.GetTexture(propertyName);
                if (texture != null)
                {
                    return texture;
                }
            }
        }

        return null;
    }

    private static Color ReadColor(Material material, Color fallback, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            if (material.HasProperty(propertyName))
            {
                return material.GetColor(propertyName);
            }
        }

        return fallback;
    }

    private static void WriteTexture(Material material, Texture texture, params string[] propertyNames)
    {
        if (texture == null)
        {
            return;
        }

        foreach (string propertyName in propertyNames)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetTexture(propertyName, texture);
            }
        }
    }

    private static void WriteColor(Material material, Color color, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, color);
            }
        }
    }
}
#endif
