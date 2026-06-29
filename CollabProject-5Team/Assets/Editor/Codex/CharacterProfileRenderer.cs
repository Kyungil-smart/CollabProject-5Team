using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CodexTools
{
    public static class CharacterProfileRenderer
    {
        private const int Width = 1024;
        private const int Height = 1707;
        private const float TargetHeightRatio = 0.82f;
        private const float TargetWidthRatio = 0.86f;

        private static readonly string OutputDir =
            "/Users/fraise77/Documents/Codex/2026-06-25/google-drive-plugin-google-drive-openai/outputs/unity-profile-renders-3x5/raw";

        private static readonly string IdleClipPath =
            "Assets/Imports/Character/Suriyun/Animations/Pspsps Animations/Character Animations/Anim_Chibi@IdleA.fbx";

        private static readonly (string Name, string PrefabPath)[] Characters =
        {
            ("솜이", "Assets/Imports/Character/Pspsps/Prefab/Characters/Chibi_Cat_00.prefab"),
            ("모카", "Assets/Imports/Character/Pspsps/Prefab/Characters/Chibi_Cat_01.prefab"),
            ("아메", "Assets/Imports/Character/Pspsps/Prefab/Characters/Chibi_Cat_02.prefab"),
            ("쿠키", "Assets/Imports/Character/Pspsps/Prefab/Characters/Chibi_Cat_03.prefab"),
            ("베리", "Assets/Imports/Character/Pspsps/Prefab/Characters/Chibi_Cat_04.prefab"),
            ("라떼", "Assets/Imports/Character/Pspsps/Prefab/Characters/Chibi_Cat_05.prefab"),
            ("우냥", "Assets/Imports/Character/Pspsps/Prefab/Characters/Chibi_Cat_06.prefab"),
            ("네로", "Assets/Imports/Character/Pspsps/Prefab/Characters/Chibi_Cat_07.prefab"),
            ("레오", "Assets/Imports/Character/Pspsps/Prefab/Characters/Chibi_Cat_08.prefab"),
            ("루피", "Assets/Imports/Character/Pspsps/Prefab/Characters/Chibi_Cat_09.prefab"),
            ("슈슈", "Assets/Imports/Character/Pspsps/Prefab/Bear/Chibi_Bear_00.prefab"),
            ("베어", "Assets/Imports/Character/Pspsps/Prefab/Bear/Chibi_Bear_01.prefab"),
            ("뭉치", "Assets/Imports/Character/Pspsps/Prefab/Bear/Chibi_Bear_02.prefab"),
            ("카누", "Assets/Imports/Character/Pspsps/Prefab/Bear/Chibi_Bear_03.prefab"),
            ("초코", "Assets/Imports/Character/Pspsps/Prefab/Bear/Chibi_Bear_04.prefab"),
            ("포키", "Assets/Imports/Character/Pspsps/Prefab/Bear/Chibi_Bear_05.prefab"),
            ("핑키", "Assets/Imports/Character/Pspsps/Prefab/Bear/Chibi_Bear_06.prefab"),
            ("허니", "Assets/Imports/Character/Pspsps/Prefab/Bear/Chibi_Bear_07.prefab"),
            ("멜론", "Assets/Imports/Character/Pspsps/Prefab/Bear/Chibi_Bear_08.prefab"),
            ("민티", "Assets/Imports/Character/Pspsps/Prefab/Bear/Chibi_Bear_09.prefab"),
            ("소다", "Assets/Imports/Character/Pspsps/Prefab/Bear/Chibi_Bear_10.prefab"),
            ("젤리", "Assets/Imports/Character/Pspsps/Prefab/Bear/Chibi_Bear_11.prefab"),
            ("포코", "Assets/Imports/Character/Pspsps/Prefab/Bear/Chibi_Bear_12.prefab"),
            ("쿠냐", "Assets/Imports/Character/Pspsps/Prefab/Bear/Chibi_Bear_13.prefab"),
            ("렉스", "Assets/Imports/Character/Pspsps/Prefab/Bear/Chibi_Bear_14.prefab"),
            ("팡이", "Assets/Imports/Character/Pspsps/Prefab/Bear/Chibi_Bear_15.prefab"),
            ("바드", "Assets/Imports/Character/Pspsps/Prefab/Bear/Chibi_Bear_Show_Equipment.prefab")
        };

        [MenuItem("Tools/Codex/Render Character Profile PNGs")]
        public static void RenderAll()
        {
            Directory.CreateDirectory(OutputDir);

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleClipPath);
            var cameraObject = new GameObject("Codex_Profile_Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0, 0, 0, 0);
            camera.orthographic = true;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;

            var lightObject = new GameObject("Codex_Profile_Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.transform.rotation = Quaternion.Euler(38f, -28f, 0f);

            foreach (var character in Characters)
            {
                RenderCharacter(character.Name, character.PrefabPath, camera, clip);
            }

            UnityEngine.Object.DestroyImmediate(cameraObject);
            UnityEngine.Object.DestroyImmediate(lightObject);
            AssetDatabase.Refresh();
            Debug.Log($"Codex character profile rendering complete: {OutputDir}");
        }

        private static void RenderCharacter(string characterName, string prefabPath, Camera camera, AnimationClip clip)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"Missing prefab for {characterName}: {prefabPath}");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = $"Codex_Render_{characterName}";
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;

            ReplaceMaterialsWithPreviewSafeUnlit(instance);

            if (clip != null)
            {
                clip.SampleAnimation(instance, 0f);
            }

            var bounds = CalculateBounds(instance);
            var center = bounds.center;
            var aspect = (float)Width / Height;
            var sizeForHeight = bounds.size.y / (2f * TargetHeightRatio);
            var sizeForWidth = bounds.size.x / (2f * aspect * TargetWidthRatio);
            camera.orthographicSize = Mathf.Max(sizeForHeight, sizeForWidth);
            camera.transform.position = new Vector3(center.x, center.y, center.z + 8f);
            camera.transform.rotation = Quaternion.identity;
            camera.transform.LookAt(center);

            var renderTexture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            renderTexture.antiAliasing = 8;
            camera.targetTexture = renderTexture;
            RenderTexture.active = renderTexture;
            GL.Clear(true, true, new Color(0, 0, 0, 0));
            camera.Render();

            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            texture.Apply();

            File.WriteAllBytes(Path.Combine(OutputDir, $"{characterName}_unity_profile_raw.png"), texture.EncodeToPNG());

            camera.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(renderTexture);
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(instance);
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(root.transform.position, Vector3.one);
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            return bounds;
        }

        private static void ReplaceMaterialsWithPreviewSafeUnlit(GameObject root)
        {
            var unlitTexture = Shader.Find("Sprites/Default");
            var unlitColor = Shader.Find("Unlit/Color");
            var standard = Shader.Find("Standard");

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var originalMaterials = renderer.sharedMaterials;
                var replacementMaterials = new Material[originalMaterials.Length];
                for (var i = 0; i < originalMaterials.Length; i++)
                {
                    var original = originalMaterials[i];
                    if (original == null)
                    {
                        replacementMaterials[i] = null;
                        continue;
                    }

                    var texture = FindTexture(original);
                    var color = FindColor(original);
                    var shader = texture != null ? unlitTexture : unlitColor;
                    if (shader == null)
                    {
                        shader = standard;
                    }

                    var replacement = new Material(shader);
                    replacement.name = $"{original.name}_CodexPreview";
                    if (texture != null)
                    {
                        SetTexture(replacement, texture);
                        ConfigureTransparentMaterial(replacement);
                    }
                    else
                    {
                        replacement.renderQueue = original.renderQueue;
                    }
                    SetColor(replacement, color);
                    replacementMaterials[i] = replacement;
                }
                renderer.sharedMaterials = replacementMaterials;
            }
        }

        private static Texture FindTexture(Material material)
        {
            var propertyNames = new[] { "_BaseMap", "_MainTex", "_BaseColorMap", "_UnlitColorMap", "_Texture" };
            foreach (var propertyName in propertyNames)
            {
                if (material.HasProperty(propertyName))
                {
                    var texture = material.GetTexture(propertyName);
                    if (texture != null)
                    {
                        return texture;
                    }
                }
            }
            return material.mainTexture;
        }

        private static Color FindColor(Material material)
        {
            var propertyNames = new[] { "_BaseColor", "_Color", "_TintColor" };
            foreach (var propertyName in propertyNames)
            {
                if (material.HasProperty(propertyName))
                {
                    return material.GetColor(propertyName);
                }
            }
            return Color.white;
        }

        private static void SetTexture(Material material, Texture texture)
        {
            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
            }
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }
            material.mainTexture = texture;
        }

        private static void SetColor(Material material, Color color)
        {
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
        }

        private static void ConfigureTransparentMaterial(Material material)
        {
            material.renderQueue = 3000;
            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            }
            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }
            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0f);
            }
            material.EnableKeyword("_ALPHABLEND_ON");
        }
    }
}
