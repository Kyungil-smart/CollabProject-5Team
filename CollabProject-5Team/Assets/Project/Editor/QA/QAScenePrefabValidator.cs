using System.Collections.Generic;
using System.Linq;
using GameDevTycoon.UI.Ingame;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameDevTycoon.EditorQA
{
    public sealed class QAScenePrefabValidator : IQAValidator
    {
        private const string SceneRoot = "Assets/Project/Scenes";
        private const string PrefabRoot = "Assets/Project/Prefabs";
        private const string MainGameScenePath = "Assets/Project/Scenes/GameScene.unity";

        private static readonly System.Type[] RequiredGameSceneComponents =
        {
            typeof(GameManager),
            typeof(Company),
            typeof(_EmployeeManager),
            typeof(DateTimeManager),
            typeof(QuestManager),
            typeof(ReportManager)
        };

        private static readonly System.Type[] RequiredReferenceComponentTypes =
        {
            typeof(ReportPresenter),
            typeof(ReportView),
            typeof(ProjectPresenter),
            typeof(ProjectView),
            typeof(HRPresenter),
            typeof(HRView),
            typeof(QuestPresenter),
            typeof(QuestView),
            typeof(HUDPresenter),
            typeof(HUDView)
        };

        private static readonly HashSet<string> OptionalRequiredReferencePaths = new()
        {
            "_slideIconActive",
            "_slideIconInactive"
        };

        public string Name => "Scene / Prefab References";

        public IEnumerable<QAResult> Run()
        {
            List<QAResult> results = new();
            int prefabCount = 0;
            int sceneCount = 0;

            foreach (string prefabPath in FindAssetPaths("t:Prefab", PrefabRoot))
            {
                prefabCount++;
                GameObject prefabRoot = null;

                try
                {
                    prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
                    results.AddRange(ValidateGameObjectTree(prefabRoot, prefabPath, isPrefab: true));
                }
                finally
                {
                    if (prefabRoot != null)
                        PrefabUtility.UnloadPrefabContents(prefabRoot);
                }
            }

            foreach (string scenePath in FindAssetPaths("t:Scene", SceneRoot))
            {
                sceneCount++;
                Scene scene = default;

                try
                {
                    scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

                    if (scenePath == MainGameScenePath)
                        results.AddRange(ValidateRequiredGameSceneComponents(scene));

                    foreach (GameObject root in scene.GetRootGameObjects())
                        results.AddRange(ValidateGameObjectTree(root, scenePath, isPrefab: false));
                }
                finally
                {
                    if (scene.IsValid() && scene.isLoaded)
                        EditorSceneManager.CloseScene(scene, removeScene: true);
                }
            }

            foreach (QAResult result in results)
                yield return result;

            yield return new QAResult(
                QASeverity.Info,
                "Scene/Prefab",
                $"씬 {sceneCount}개, 프리팹 {prefabCount}개 연결 검사를 완료했습니다.",
                SceneRoot);
        }

        private static IEnumerable<QAResult> ValidateGameObjectTree(
            GameObject root,
            string assetPath,
            bool isPrefab)
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                GameObject go = transform.gameObject;
                string hierarchyPath = GetHierarchyPath(go);

                int missingScriptCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                if (missingScriptCount > 0)
                {
                    yield return new QAResult(
                        QASeverity.Error,
                        isPrefab ? "Prefab Missing Script" : "Scene Missing Script",
                        $"{hierarchyPath}에 Missing Script가 {missingScriptCount}개 있습니다.",
                        assetPath,
                        isPrefab ? AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) : null);
                }

                foreach (Component component in go.GetComponents<Component>())
                {
                    if (component == null)
                        continue;

                    foreach (QAResult result in ValidateBrokenObjectReferences(component, assetPath, hierarchyPath, isPrefab))
                        yield return result;

                    foreach (QAResult result in ValidateRequiredObjectReferences(component, assetPath, hierarchyPath, isPrefab))
                        yield return result;

                    if (component is Button button)
                    {
                        foreach (QAResult result in ValidateButton(button, assetPath, hierarchyPath, isPrefab))
                            yield return result;
                    }
                }
            }
        }

        private static IEnumerable<QAResult> ValidateRequiredGameSceneComponents(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();

            foreach (System.Type requiredType in RequiredGameSceneComponents)
            {
                bool exists = roots.Any(root =>
                    root.GetComponentsInChildren(requiredType, includeInactive: true).Length > 0);

                if (exists)
                    continue;

                yield return new QAResult(
                    QASeverity.Error,
                    "Scene Required Component",
                    $"GameScene에 필수 컴포넌트 {requiredType.Name}가 없습니다.",
                    MainGameScenePath);
            }
        }

        private static IEnumerable<QAResult> ValidateBrokenObjectReferences(
            Component component,
            string assetPath,
            string hierarchyPath,
            bool isPrefab)
        {
            SerializedObject serializedObject = new(component);
            SerializedProperty property = serializedObject.GetIterator();
            bool enterChildren = true;

            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (property.propertyType != SerializedPropertyType.ObjectReference)
                    continue;

                bool hasBrokenReference =
                    property.objectReferenceValue == null
                    && property.objectReferenceInstanceIDValue != 0;

                if (!hasBrokenReference)
                    continue;

                yield return new QAResult(
                    QASeverity.Error,
                    isPrefab ? "Prefab Broken Reference" : "Scene Broken Reference",
                    $"{hierarchyPath}/{component.GetType().Name}.{property.displayName}에 깨진 Object Reference가 있습니다.",
                    assetPath,
                    isPrefab ? AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) : null);
            }
        }

        private static IEnumerable<QAResult> ValidateRequiredObjectReferences(
            Component component,
            string assetPath,
            string hierarchyPath,
            bool isPrefab)
        {
            System.Type componentType = component.GetType();
            if (!RequiredReferenceComponentTypes.Contains(componentType))
                yield break;

            SerializedObject serializedObject = new(component);
            SerializedProperty property = serializedObject.GetIterator();
            bool enterChildren = true;

            while (property.NextVisible(enterChildren))
            {
                enterChildren = true;

                if (property.propertyPath == "m_Script")
                    continue;

                if (IsOptionalReferencePath(property.propertyPath))
                    continue;

                if (property.propertyType == SerializedPropertyType.ObjectReference
                    && property.objectReferenceValue == null
                    && property.objectReferenceInstanceIDValue == 0)
                {
                    yield return new QAResult(
                        QASeverity.Error,
                        isPrefab ? "Prefab Required Reference" : "Scene Required Reference",
                        $"{hierarchyPath}/{componentType.Name}.{property.displayName} 필수 참조가 비어 있습니다.",
                        assetPath,
                        isPrefab ? AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) : null);
                }
            }

            foreach (QAResult result in ValidateSpecialRequiredFields(component, assetPath, hierarchyPath, isPrefab))
                yield return result;
        }

        private static IEnumerable<QAResult> ValidateSpecialRequiredFields(
            Component component,
            string assetPath,
            string hierarchyPath,
            bool isPrefab)
        {
            if (component is ReportPresenter)
            {
                SerializedObject so = new(component);
                SerializedProperty nextButtons = so.FindProperty("_nextButtons");
                if (nextButtons != null && nextButtons.isArray && nextButtons.arraySize < 3)
                {
                    yield return RequiredReferenceError(
                        component,
                        assetPath,
                        hierarchyPath,
                        isPrefab,
                        $"ReportPresenter._nextButtons는 기획/아트/개발 3개 이상이어야 합니다. 현재 {nextButtons.arraySize}개입니다.");
                }
            }

            if (component is ReportView)
            {
                SerializedObject so = new(component);
                SerializedProperty reviewPanels = so.FindProperty("_panelReportReviews");
                SerializedProperty reviewContents = so.FindProperty("_ReportReviewContents");

                if (reviewPanels != null && reviewPanels.isArray && reviewPanels.arraySize < 3)
                {
                    yield return RequiredReferenceError(
                        component,
                        assetPath,
                        hierarchyPath,
                        isPrefab,
                        $"ReportView._panelReportReviews는 기획/아트/개발 3개 이상이어야 합니다. 현재 {reviewPanels.arraySize}개입니다.");
                }

                if (reviewContents != null && reviewContents.isArray && reviewContents.arraySize < 3)
                {
                    yield return RequiredReferenceError(
                        component,
                        assetPath,
                        hierarchyPath,
                        isPrefab,
                        $"ReportView._ReportReviewContents는 기획/아트/개발 3개 이상이어야 합니다. 현재 {reviewContents.arraySize}개입니다.");
                }
            }

            if (component is ProjectPresenter)
            {
                SerializedObject so = new(component);
                SerializedProperty staffCardPrefabs = so.FindProperty("_staffCardPrefabs");
                if (staffCardPrefabs == null || !staffCardPrefabs.isArray)
                    yield break;

                var mappedRoles = new HashSet<Role>();
                for (int i = 0; i < staffCardPrefabs.arraySize; i++)
                {
                    SerializedProperty entry = staffCardPrefabs.GetArrayElementAtIndex(i);
                    SerializedProperty role = entry.FindPropertyRelative("role");
                    SerializedProperty prefab = entry.FindPropertyRelative("prefab");

                    if (role != null)
                        mappedRoles.Add((Role)role.enumValueIndex);

                    if (prefab != null && prefab.objectReferenceValue == null)
                    {
                        yield return RequiredReferenceError(
                            component,
                            assetPath,
                            hierarchyPath,
                            isPrefab,
                            $"ProjectPresenter._staffCardPrefabs[{i}] 프리팹 참조가 비어 있습니다.");
                    }
                }

                foreach (Role role in new[] { Role.PLANNER, Role.ARTIST, Role.PROGRAMMER })
                {
                    if (mappedRoles.Contains(role))
                        continue;

                    yield return RequiredReferenceError(
                        component,
                        assetPath,
                        hierarchyPath,
                        isPrefab,
                        $"ProjectPresenter._staffCardPrefabs에 {role} 직군 카드 프리팹 매핑이 없습니다.");
                }
            }
        }

        private static bool IsOptionalReferencePath(string propertyPath)
        {
            string normalized = propertyPath;
            int arrayMarker = normalized.IndexOf(".Array.data", System.StringComparison.Ordinal);
            if (arrayMarker >= 0)
                normalized = normalized.Substring(0, arrayMarker);

            return OptionalRequiredReferencePaths.Contains(normalized);
        }

        private static QAResult RequiredReferenceError(
            Component component,
            string assetPath,
            string hierarchyPath,
            bool isPrefab,
            string message)
        {
            return new QAResult(
                QASeverity.Error,
                isPrefab ? "Prefab Required Reference" : "Scene Required Reference",
                $"{hierarchyPath}/{component.GetType().Name}: {message}",
                assetPath,
                isPrefab ? AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) : null);
        }

        private static IEnumerable<QAResult> ValidateButton(
            Button button,
            string assetPath,
            string hierarchyPath,
            bool isPrefab)
        {
            int eventCount = button.onClick.GetPersistentEventCount();

            if (eventCount == 0)
            {
                yield return new QAResult(
                    QASeverity.Warning,
                    isPrefab ? "Prefab Button" : "Scene Button",
                    $"{hierarchyPath} 버튼에 Inspector OnClick 연결이 없습니다. 코드에서 동적으로 연결하는 버튼이면 무시해도 됩니다.",
                    assetPath,
                    isPrefab ? AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) : null);
                yield break;
            }

            for (int i = 0; i < eventCount; i++)
            {
                Object target = button.onClick.GetPersistentTarget(i);
                string methodName = button.onClick.GetPersistentMethodName(i);
                UnityEventCallState state = button.onClick.GetPersistentListenerState(i);

                if (state == UnityEventCallState.Off)
                    continue;

                if (target == null)
                {
                    yield return new QAResult(
                        QASeverity.Error,
                        isPrefab ? "Prefab Button" : "Scene Button",
                        $"{hierarchyPath} 버튼의 OnClick #{i} 대상이 비어 있습니다.",
                        assetPath,
                        isPrefab ? AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) : null);
                }
                else if (string.IsNullOrWhiteSpace(methodName))
                {
                    yield return new QAResult(
                        QASeverity.Error,
                        isPrefab ? "Prefab Button" : "Scene Button",
                        $"{hierarchyPath} 버튼의 OnClick #{i} 메서드명이 비어 있습니다.",
                        assetPath,
                        isPrefab ? AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) : null);
                }
            }
        }

        private static IEnumerable<string> FindAssetPaths(string filter, string root)
        {
            return AssetDatabase
                .FindAssets(filter, new[] { root })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path);
        }

        private static string GetHierarchyPath(GameObject go)
        {
            var names = new Stack<string>();
            Transform current = go.transform;

            while (current != null)
            {
                names.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", names);
        }
    }
}
