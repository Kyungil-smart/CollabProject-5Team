using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameDevTycoon.UI.Ingame;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GameDevTycoon.EditorQA
{
    public sealed class QAPlayFlowValidator : IQAValidator
    {
        private const string MainGameScenePath = "Assets/Project/Scenes/GameScene.unity";

        private static readonly Type[] RequiredManagers =
        {
            typeof(GameManager),
            typeof(Company),
            typeof(_EmployeeManager),
            typeof(DateTimeManager),
            typeof(QuestManager),
            typeof(ReportManager)
        };

        public string Name => "Play Flow";

        public IEnumerable<QAResult> Run()
        {
            List<QAResult> results = new();

            Scene scene = default;
            try
            {
                scene = EditorSceneManager.OpenScene(MainGameScenePath, OpenSceneMode.Additive);

                results.AddRange(ValidateManagers(scene));
                results.AddRange(ValidateDateTimeContract());
                results.AddRange(ValidateDayWorkFlow(scene));
                results.AddRange(ValidateQuestFlow(scene));
                results.AddRange(ValidateNightReportFlow(scene));
            }
            finally
            {
                if (scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, removeScene: true);
            }

            foreach (QAResult result in results)
                yield return result;

            yield return new QAResult(
                QASeverity.Info,
                "Play Flow",
                "낮 업무 시작/완료, 금요일 밤 보고서, 밤 퇴근 흐름 검사를 완료했습니다.",
                MainGameScenePath);
        }

        private static IEnumerable<QAResult> ValidateManagers(Scene scene)
        {
            foreach (Type managerType in RequiredManagers)
            {
                Component[] components = FindComponents(scene, managerType);
                if (components.Length == 0)
                {
                    yield return new QAResult(
                        QASeverity.Error,
                        "Play Flow Manager",
                        $"GameScene에 플레이 루프 필수 매니저 {managerType.Name}가 없습니다.",
                        MainGameScenePath);
                    continue;
                }

                if (components.Length > 1)
                {
                    yield return new QAResult(
                        QASeverity.Warning,
                        "Play Flow Manager",
                        $"GameScene에 {managerType.Name}가 {components.Length}개 있습니다. 싱글톤이면 중복 생성/파괴 흐름을 확인하세요.",
                        MainGameScenePath);
                }
            }
        }

        private static IEnumerable<QAResult> ValidateDateTimeContract()
        {
            Type type = typeof(DateTimeManager);
            const BindingFlags instanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            const BindingFlags staticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (string methodName in new[]
            {
                "OnClickEndDayButton",
                "ProgressDay",
                "ProgressNight",
                "CompleteDayWork",
                "GetDialogueState"
            })
            {
                if (type.GetMethod(methodName, instanceFlags) == null)
                {
                    yield return new QAResult(
                        QASeverity.Error,
                        "Play Flow Contract",
                        $"DateTimeManager.{methodName} 메서드를 찾을 수 없습니다. 낮/밤 전환 계약이 깨졌을 수 있습니다.",
                        MainGameScenePath);
                }
            }

            foreach (string eventOrActionName in new[]
            {
                "OnDay",
                "OnWorkCompleted",
                "OnNightLoading",
                "OnNight",
                "OnReportEnd"
            })
            {
                bool hasEvent = type.GetEvent(eventOrActionName, staticFlags) != null;
                bool hasField = type.GetField(eventOrActionName, staticFlags) != null;
                if (!hasEvent && !hasField)
                {
                    yield return new QAResult(
                        QASeverity.Error,
                        "Play Flow Contract",
                        $"DateTimeManager.{eventOrActionName} 이벤트/액션을 찾을 수 없습니다. UI와 보고서 흐름 구독이 끊길 수 있습니다.",
                        MainGameScenePath);
                }
            }
        }

        private static IEnumerable<QAResult> ValidateDayWorkFlow(Scene scene)
        {
            HUDPresenter hudPresenter = FindFirst<HUDPresenter>(scene);
            HUDView hudView = FindFirst<HUDView>(scene);
            DeskInteract deskInteract = FindFirst<DeskInteract>(scene);

            if (hudPresenter == null)
            {
                yield return new QAResult(
                    QASeverity.Error,
                    "Play Flow Day",
                    "HUDPresenter가 없어 업무 시작 버튼과 낮/밤 UI 전환을 연결할 수 없습니다.",
                    MainGameScenePath);
                yield break;
            }

            if (!hudPresenter.enabled)
            {
                yield return new QAResult(
                    QASeverity.Warning,
                    "Play Flow Day",
                    "HUDPresenter 컴포넌트가 비활성화되어 있습니다. Play Mode에서 버튼 구독이 실행되지 않을 수 있습니다.",
                    MainGameScenePath);
            }

            foreach (QAResult result in RequireReference(hudPresenter, "_view", "HUDPresenter._view가 비어 있어 HUD 버튼 입력을 받을 수 없습니다."))
                yield return result;

            foreach (QAResult result in RequireReference(hudPresenter, "_desk", "HUDPresenter._desk가 비어 있어 업무 시작 버튼을 눌러도 책상 업무 위치로 이동할 수 없습니다."))
                yield return result;

            if (hudView == null)
            {
                yield return new QAResult(
                    QASeverity.Error,
                    "Play Flow Day",
                    "HUDView가 없어 업무 시작/퇴근/밤 하단 버튼 이벤트를 발행할 수 없습니다.",
                    MainGameScenePath);
            }
            else
            {
                foreach (QAResult result in ValidateHUDViewButtons(hudView))
                    yield return result;
            }

            if (deskInteract == null)
            {
                yield return new QAResult(
                    QASeverity.Error,
                    "Play Flow Day",
                    "DeskInteract가 없어 업무 시작 버튼이 플레이어 책상 이동으로 이어질 수 없습니다.",
                    MainGameScenePath);
            }
            else
            {
                foreach (QAResult result in ValidateDeskInteract(deskInteract))
                    yield return result;
            }

            if (FindFirst<HRPresenter>(scene) == null)
            {
                yield return new QAResult(
                    QASeverity.Error,
                    "Play Flow Night UI",
                    "HRPresenter가 없어 밤 하단 인사관리 버튼을 누를 때 팝업 흐름이 끊길 수 있습니다.",
                    MainGameScenePath);
            }

            if (FindFirst<ProjectPresenter>(scene) == null)
            {
                yield return new QAResult(
                    QASeverity.Error,
                    "Play Flow Night UI",
                    "ProjectPresenter가 없어 밤 하단 프로젝트 버튼을 누를 때 팝업 흐름이 끊길 수 있습니다.",
                    MainGameScenePath);
            }
        }

        private static IEnumerable<QAResult> ValidateQuestFlow(Scene scene)
        {
            QuestManager questManager = FindFirst<QuestManager>(scene);
            if (questManager == null)
                yield break;

            if (questManager.dailyQuests == null || questManager.dailyQuests.Count == 0)
            {
                yield return new QAResult(
                    QASeverity.Error,
                    "Play Flow Quest",
                    "QuestManager.dailyQuests가 비어 있습니다. 하루 시작 시 일일 업무가 생성되지 않아 업무 완료/퇴근 흐름이 막힐 수 있습니다.",
                    MainGameScenePath);
                yield break;
            }

            for (int i = 0; i < questManager.dailyQuests.Count; i++)
            {
                QuestSO quest = questManager.dailyQuests[i];
                if (quest == null)
                {
                    yield return new QAResult(
                        QASeverity.Error,
                        "Play Flow Quest",
                        $"QuestManager.dailyQuests[{i}]가 비어 있습니다.",
                        MainGameScenePath);
                }
            }

            Transform questObjectsRoot = GetObjectReference<Transform>(questManager, "questObjectsRoot");
            RectTransform questCanvas = GetObjectReference<RectTransform>(questManager, "questCanvas");
            GameObject speechBubblePrefab = GetObjectReference<GameObject>(questManager, "speechBubblePrefab");

            if (questObjectsRoot == null)
            {
                yield return new QAResult(
                    QASeverity.Error,
                    "Play Flow Quest",
                    "QuestManager.questObjectsRoot가 비어 있습니다. 퀘스트 activeObjects를 켜고 끌 수 없습니다.",
                    MainGameScenePath);
            }
            else
            {
                foreach (QAResult result in ValidateQuestObjectNames(questManager, questObjectsRoot))
                    yield return result;
            }

            if (questCanvas == null)
            {
                yield return new QAResult(
                    QASeverity.Error,
                    "Play Flow Quest",
                    "QuestManager.questCanvas가 비어 있습니다. 퀘스트 완료 말풍선/연출이 표시되지 않습니다.",
                    MainGameScenePath);
            }

            if (speechBubblePrefab == null)
            {
                yield return new QAResult(
                    QASeverity.Warning,
                    "Play Flow Quest",
                    "QuestManager.speechBubblePrefab이 비어 있습니다. 퀘스트 완료 자체는 가능하지만 완료 말풍선은 나오지 않습니다.",
                    MainGameScenePath);
            }
        }

        private static IEnumerable<QAResult> ValidateNightReportFlow(Scene scene)
        {
            ReportPresenter reportPresenter = FindFirst<ReportPresenter>(scene);
            ReportView reportView = FindFirst<ReportView>(scene);
            HUDPresenter hudPresenter = FindFirst<HUDPresenter>(scene);

            if (reportPresenter == null)
            {
                yield return new QAResult(
                    QASeverity.Error,
                    "Play Flow Report",
                    "ReportPresenter가 없어 금요일 밤 OnNight 이후 주간 보고서 팝업을 시작할 수 없습니다.",
                    MainGameScenePath);
                yield break;
            }

            if (!reportPresenter.enabled)
            {
                yield return new QAResult(
                    QASeverity.Warning,
                    "Play Flow Report",
                    "ReportPresenter 컴포넌트가 비활성화되어 있습니다. DateTimeManager.OnNight 구독이 실행되지 않을 수 있습니다.",
                    MainGameScenePath);
            }

            foreach (QAResult result in RequireReference(reportPresenter, "_view", "ReportPresenter._view가 비어 있어 보고서 UI를 표시할 수 없습니다."))
                yield return result;

            foreach (QAResult result in RequireReference(reportPresenter, "_employeeStatusMiniItemPrefab", "ReportPresenter._employeeStatusMiniItemPrefab이 비어 있어 주간 코멘트/상태 목록을 만들 수 없습니다."))
                yield return result;

            foreach (QAResult result in RequireReference(reportPresenter, "_reportCardViewPrefab", "ReportPresenter._reportCardViewPrefab이 비어 있어 보고서 후보 카드를 만들 수 없습니다."))
                yield return result;

            if (reportView == null)
            {
                yield return new QAResult(
                    QASeverity.Error,
                    "Play Flow Report",
                    "ReportView가 없어 보고서 패널/상세/종료 화면을 제어할 수 없습니다.",
                    MainGameScenePath);
            }

            foreach (QAResult result in ValidateReportNextButtons(reportPresenter))
                yield return result;

            if (hudPresenter == null)
            {
                yield return new QAResult(
                    QASeverity.Error,
                    "Play Flow Report",
                    "HUDPresenter가 없어 보고서 완료 후 DateTimeManager.OnReportEnd를 받아 밤 UI로 전환하기 어렵습니다.",
                    MainGameScenePath);
            }
        }

        private static IEnumerable<QAResult> ValidateHUDViewButtons(HUDView hudView)
        {
            foreach (string path in new[]
            {
                "_settingsButton",
                "_questIconButton",
                "_workStartButton",
                "_hrButton",
                "_projectButton",
                "_companyButton",
                "_saveButton",
                "_nightQuitButton"
            })
            {
                Button button = GetObjectReference<Button>(hudView, path);
                if (button == null)
                {
                    yield return new QAResult(
                        QASeverity.Error,
                        "Play Flow UI",
                        $"HUDView.{path} 버튼 참조가 비어 있습니다.",
                        MainGameScenePath);
                }
            }

            foreach (string path in new[] { "_dayUI", "_nightUI", "_questBanner" })
            {
                GameObject panel = GetObjectReference<GameObject>(hudView, path);
                if (panel == null)
                {
                    yield return new QAResult(
                        QASeverity.Error,
                        "Play Flow UI",
                        $"HUDView.{path} 패널 참조가 비어 있습니다.",
                        MainGameScenePath);
                }
            }
        }

        private static IEnumerable<QAResult> ValidateDeskInteract(DeskInteract deskInteract)
        {
            Transform workPosition = GetObjectReference<Transform>(deskInteract, "_workPosition");
            if (workPosition == null)
            {
                yield return new QAResult(
                    QASeverity.Error,
                    "Play Flow Day",
                    "DeskInteract._workPosition이 비어 있습니다. 업무 시작 시 플레이어 이동 목적지가 없어 버튼이 실패할 수 있습니다.",
                    MainGameScenePath);
            }
        }

        private static IEnumerable<QAResult> ValidateReportNextButtons(ReportPresenter reportPresenter)
        {
            SerializedObject so = new(reportPresenter);
            SerializedProperty nextButtons = so.FindProperty("_nextButtons");

            if (nextButtons == null || !nextButtons.isArray)
            {
                yield return new QAResult(
                    QASeverity.Error,
                    "Play Flow Report",
                    "ReportPresenter._nextButtons 배열을 찾을 수 없습니다.",
                    MainGameScenePath);
                yield break;
            }

            if (nextButtons.arraySize < 3)
            {
                yield return new QAResult(
                    QASeverity.Error,
                    "Play Flow Report",
                    $"ReportPresenter._nextButtons는 기획/아트/개발 3개 이상이어야 합니다. 현재 {nextButtons.arraySize}개입니다.",
                    MainGameScenePath);
            }

            int count = Math.Min(nextButtons.arraySize, 3);
            for (int i = 0; i < count; i++)
            {
                SerializedProperty element = nextButtons.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue == null)
                {
                    yield return new QAResult(
                        QASeverity.Error,
                        "Play Flow Report",
                        $"ReportPresenter._nextButtons[{i}]가 비어 있습니다. 해당 직군 보고서 선택 후 다음 단계로 넘어갈 수 없습니다.",
                        MainGameScenePath);
                }
            }
        }

        private static IEnumerable<QAResult> ValidateQuestObjectNames(QuestManager questManager, Transform questObjectsRoot)
        {
            HashSet<string> sceneObjectNames = questObjectsRoot
                .GetComponentsInChildren<Transform>(includeInactive: true)
                .Select(t => t.name)
                .ToHashSet();

            foreach (QuestSO quest in questManager.dailyQuests.Where(q => q != null))
            {
                foreach (string objectName in SplitObjectNames(quest.activeObjects).Concat(SplitObjectNames(quest.resultObjects)))
                {
                    if (sceneObjectNames.Contains(objectName))
                        continue;

                    yield return new QAResult(
                        QASeverity.Warning,
                        "Play Flow Quest",
                        $"QuestSO {quest.id}({quest.Name})의 오브젝트 '{objectName}'를 questObjectsRoot 하위에서 찾을 수 없습니다.",
                        AssetDatabase.GetAssetPath(quest),
                        quest);
                }
            }
        }

        private static IEnumerable<QAResult> RequireReference(Component component, string propertyPath, string message)
        {
            if (GetObjectReference<Object>(component, propertyPath) != null)
                yield break;

            yield return new QAResult(
                QASeverity.Error,
                "Play Flow Reference",
                message,
                MainGameScenePath);
        }

        private static T FindFirst<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(includeInactive: true))
                .FirstOrDefault();
        }

        private static Component[] FindComponents(Scene scene, Type type)
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren(type, includeInactive: true))
                .ToArray();
        }

        private static T GetObjectReference<T>(Object target, string propertyPath) where T : Object
        {
            SerializedObject so = new(target);
            SerializedProperty property = so.FindProperty(propertyPath);
            return property?.objectReferenceValue as T;
        }

        private static IEnumerable<string> SplitObjectNames(string names)
        {
            if (string.IsNullOrWhiteSpace(names))
                yield break;

            foreach (string raw in names.Split(','))
            {
                string trimmed = raw.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed == "None")
                    continue;

                yield return trimmed;
            }
        }
    }
}
