using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GameDevTycoon.EditorQA
{
    public static class QAAssetUtility
    {
        public const string ProjectAssetRoot = "Assets/Project";
        public const string DatabaseRoot = "Assets/Project/DB";

        public static List<T> FindAssetsByType<T>(string searchRoot = ProjectAssetRoot)
            where T : Object
        {
            var results = new List<T>();
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { searchRoot });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                    results.Add(asset);
            }

            return results;
        }

        public static List<AssetEntry<T>> FindAssetEntriesByType<T>(string searchRoot = ProjectAssetRoot)
            where T : Object
        {
            var results = new List<AssetEntry<T>>();
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { searchRoot });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset == null) continue;

                results.Add(new AssetEntry<T>(asset, path, guid));
            }

            return results;
        }

        public static string GetPath(Object target)
        {
            return target == null ? string.Empty : AssetDatabase.GetAssetPath(target);
        }
    }

    public readonly struct AssetEntry<T> where T : Object
    {
        public T Asset { get; }
        public string Path { get; }
        public string Guid { get; }

        public AssetEntry(T asset, string path, string guid)
        {
            Asset = asset;
            Path = path;
            Guid = guid;
        }
    }
}
