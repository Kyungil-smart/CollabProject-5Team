using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameDevTycoon.EditorQA
{
    public sealed class QAWindow : EditorWindow
    {
        private readonly List<IQAValidator> _validators = new();
        private readonly List<QAResult> _results = new();
        private Vector2 _scrollPosition;
        private bool _showInfo = true;
        private bool _showWarning = true;
        private bool _showError = true;
        private bool _showChecklist;
        private string _selectedCategory = AllCategories;
        private string _selectedTriage = AllTriage;
        private string _selectedOwner = AllOwners;
        private string _searchText = string.Empty;

        private const string AllCategories = "All";
        private const string AllTriage = "All";
        private const string AllOwners = "All";

        [MenuItem("Tools/QA/Prototype QA Window")]
        public static void Open()
        {
            QAWindow window = GetWindow<QAWindow>("Prototype QA");
            window.minSize = new Vector2(560f, 360f);
            window.Show();
        }

        private void OnEnable()
        {
            _validators.Clear();
            _validators.AddRange(QARunner.CreateDefaultValidators());
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawChecklist();
            DrawSummary();
            DrawFilters();
            DrawResults();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("전체 검사 실행", EditorStyles.toolbarButton, GUILayout.Width(110f)))
                {
                    RunAllValidators();
                }

                EditorGUI.BeginDisabledGroup(_results.Count == 0);
                if (GUILayout.Button("Markdown 리포트", EditorStyles.toolbarButton, GUILayout.Width(110f)))
                {
                    QAReportExporter.ExportMarkdown(_results);
                }
                EditorGUI.EndDisabledGroup();

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("결과 지우기", EditorStyles.toolbarButton, GUILayout.Width(90f)))
                {
                    _results.Clear();
                }
            }
        }

        private void DrawChecklist()
        {
            EditorGUILayout.Space(6f);
            _showChecklist = EditorGUILayout.Foldout(_showChecklist, "현재 QA 검사 항목 보기", true);
            if (!_showChecklist)
                return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(
                    "이 창에서 어떤 항목을 검사하는지 빠르게 확인하는 용도입니다.",
                    EditorStyles.miniLabel);

                foreach (IQAValidator validator in _validators)
                {
                    QAValidatorInfo info = QAValidatorInfoRegistry.Get(validator);
                    if (info == null)
                        continue;

                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.LabelField(info.Name, EditorStyles.boldLabel);
                        EditorGUILayout.LabelField(info.Summary, EditorStyles.wordWrappedMiniLabel);

                        foreach (string check in info.Checks)
                            EditorGUILayout.LabelField($"- {check}", EditorStyles.miniLabel);
                    }
                }
            }
        }

        private void DrawSummary()
        {
            int errorCount = _results.Count(r => r.Severity == QASeverity.Error);
            int warningCount = _results.Count(r => r.Severity == QASeverity.Warning);
            int infoCount = _results.Count(r => r.Severity == QASeverity.Info);
            int visibleCount = _results.Count(ShouldShow);
            int fixNowCount = _results.Count(r => GetTriageStatus(r) == "즉시 수정");
            int dataPendingCount = _results.Count(r => GetTriageStatus(r) == "데이터 입력 대기");
            int designReviewCount = _results.Count(r => GetTriageStatus(r) == "기획/정책 확인");
            int checkNeededCount = _results.Count(r => GetTriageStatus(r) == "확인 필요");
            int designOwnerCount = _results.Count(r => GetOwnerStatus(r) == "기획 QA");
            int devOwnerCount = _results.Count(r => GetOwnerStatus(r) == "개발 QA");
            int sharedOwnerCount = _results.Count(r => GetOwnerStatus(r) == "공통 QA");

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("1차 QA 에디터", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                $"결과: Error {errorCount} / Warning {warningCount} / Info {infoCount} / 표시 {visibleCount}");
            EditorGUILayout.LabelField(
                $"처리 기준: 즉시 수정 {fixNowCount} / 데이터 입력 대기 {dataPendingCount} / 기획 확인 {designReviewCount} / 확인 필요 {checkNeededCount}");
            EditorGUILayout.LabelField(
                $"담당 영역: 기획 QA {designOwnerCount} / 개발 QA {devOwnerCount} / 공통 QA {sharedOwnerCount}");
            EditorGUILayout.Space(4f);
        }

        private void DrawFilters()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                _showError = GUILayout.Toggle(_showError, "Error", EditorStyles.miniButtonLeft);
                _showWarning = GUILayout.Toggle(_showWarning, "Warning", EditorStyles.miniButtonMid);
                _showInfo = GUILayout.Toggle(_showInfo, "Info", EditorStyles.miniButtonRight);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                string[] categories = BuildCategoryOptions();
                int currentIndex = System.Array.IndexOf(categories, _selectedCategory);
                if (currentIndex < 0)
                {
                    currentIndex = 0;
                    _selectedCategory = AllCategories;
                }

                EditorGUILayout.LabelField("Category", GUILayout.Width(60f));
                int nextIndex = EditorGUILayout.Popup(currentIndex, categories);
                _selectedCategory = categories[nextIndex];
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                string[] triageOptions = BuildTriageOptions();
                int currentTriageIndex = System.Array.IndexOf(triageOptions, _selectedTriage);
                if (currentTriageIndex < 0)
                {
                    currentTriageIndex = 0;
                    _selectedTriage = AllTriage;
                }

                EditorGUILayout.LabelField("Triage", GUILayout.Width(60f));
                int nextTriageIndex = EditorGUILayout.Popup(currentTriageIndex, triageOptions);
                _selectedTriage = triageOptions[nextTriageIndex];
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                string[] ownerOptions = BuildOwnerOptions();
                int currentOwnerIndex = System.Array.IndexOf(ownerOptions, _selectedOwner);
                if (currentOwnerIndex < 0)
                {
                    currentOwnerIndex = 0;
                    _selectedOwner = AllOwners;
                }

                EditorGUILayout.LabelField("Owner", GUILayout.Width(60f));
                int nextOwnerIndex = EditorGUILayout.Popup(currentOwnerIndex, ownerOptions);
                _selectedOwner = ownerOptions[nextOwnerIndex];

                EditorGUILayout.LabelField("Search", GUILayout.Width(48f));
                _searchText = EditorGUILayout.TextField(_searchText);

                EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_searchText));
                if (GUILayout.Button("Clear", GUILayout.Width(54f)))
                    _searchText = string.Empty;
                EditorGUI.EndDisabledGroup();
            }
        }

        private void DrawResults()
        {
            EditorGUILayout.Space(6f);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            foreach (QAResult result in _results.Where(ShouldShow).OrderBy(GetSeverityOrder).ThenBy(GetTriageOrder))
            {
                DrawResult(result);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawResult(QAResult result)
        {
            string triageStatus = GetTriageStatus(result);
            string triageMemo = GetTriageMemo(result);
            string ownerStatus = GetOwnerStatus(result);
            string ownerMemo = GetOwnerMemo(result);
            QAResultAdvice advice = QAResultAdvisor.GetAdvice(result);
            MessageType messageType = result.Severity switch
            {
                QASeverity.Error => MessageType.Error,
                QASeverity.Warning => MessageType.Warning,
                _ => MessageType.Info
            };

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.HelpBox($"[{result.Category}] {result.Message}", messageType);
                EditorGUILayout.LabelField($"담당 영역: {ownerStatus} - {ownerMemo}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"처리 기준: {triageStatus} - {triageMemo}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"우선순위: {advice.Priority}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"예상 원인: {advice.ExpectedCause}", EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField($"관련 코드/영역: {advice.RelatedCode}", EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField($"해결 힌트: {advice.FixHint}", EditorStyles.wordWrappedMiniLabel);

                if (result.Target != null || !string.IsNullOrEmpty(result.AssetPath))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUI.BeginDisabledGroup(result.Target == null);
                        if (GUILayout.Button("Select", GUILayout.Width(70f)))
                        {
                            Selection.activeObject = result.Target;
                            EditorGUIUtility.PingObject(result.Target);
                        }
                        EditorGUI.EndDisabledGroup();

                        EditorGUILayout.SelectableLabel(result.AssetPath, GUILayout.Height(18f));
                    }
                }
            }
        }

        private void RunAllValidators()
        {
            _results.Clear();
            _results.AddRange(QARunner.RunAll(_validators));
        }

        private bool ShouldShow(QAResult result)
        {
            bool severityVisible = result.Severity switch
            {
                QASeverity.Error => _showError,
                QASeverity.Warning => _showWarning,
                QASeverity.Info => _showInfo,
                _ => true
            };

            if (!severityVisible)
                return false;

            if (_selectedCategory != AllCategories && result.Category != _selectedCategory)
                return false;

            if (_selectedTriage != AllTriage && GetTriageStatus(result) != _selectedTriage)
                return false;

            if (_selectedOwner != AllOwners && GetOwnerStatus(result) != _selectedOwner)
                return false;

            if (!string.IsNullOrWhiteSpace(_searchText) && !MatchesSearch(result, _searchText.Trim()))
                return false;

            return true;
        }

        private static int GetSeverityOrder(QAResult result)
        {
            return result.Severity switch
            {
                QASeverity.Error => 0,
                QASeverity.Warning => 1,
                QASeverity.Info => 2,
                _ => 3
            };
        }

        private static int GetTriageOrder(QAResult result)
        {
            return GetTriageStatus(result) switch
            {
                "즉시 수정" => 0,
                "기획/정책 확인" => 1,
                "데이터 입력 대기" => 2,
                "확인 필요" => 3,
                "정보" => 4,
                _ => 5
            };
        }

        private string[] BuildCategoryOptions()
        {
            return new[] { AllCategories }
                .Concat(_results
                    .Select(r => r.Category)
                    .Where(category => !string.IsNullOrWhiteSpace(category))
                    .Distinct()
                .OrderBy(category => category))
                .ToArray();
        }

        private string[] BuildTriageOptions()
        {
            return new[] { AllTriage }
                .Concat(_results
                    .Select(GetTriageStatus)
                    .Where(status => !string.IsNullOrWhiteSpace(status))
                    .Distinct()
                    .OrderBy(GetTriageOptionOrder))
                .ToArray();
        }

        private string[] BuildOwnerOptions()
        {
            return new[] { AllOwners }
                .Concat(_results
                    .Select(GetOwnerStatus)
                    .Where(owner => !string.IsNullOrWhiteSpace(owner))
                    .Distinct()
                    .OrderBy(GetOwnerOptionOrder))
                .ToArray();
        }

        private static int GetTriageOptionOrder(string status)
        {
            return status switch
            {
                "즉시 수정" => 0,
                "기획/정책 확인" => 1,
                "데이터 입력 대기" => 2,
                "확인 필요" => 3,
                "정보" => 4,
                _ => 5
            };
        }

        private static int GetOwnerOptionOrder(string owner)
        {
            return owner switch
            {
                "기획 QA" => 0,
                "개발 QA" => 1,
                "공통 QA" => 2,
                _ => 3
            };
        }

        private static string GetTriageStatus(QAResult result)
        {
            if (result.Severity == QASeverity.Info)
                return "정보";

            if (IsDataPendingResult(result))
                return "데이터 입력 대기";

            if (IsDesignReviewResult(result))
                return "기획/정책 확인";

            if (result.Severity == QASeverity.Error)
                return "즉시 수정";

            return "확인 필요";
        }

        private static string GetTriageMemo(QAResult result)
        {
            return GetTriageStatus(result) switch
            {
                "즉시 수정" => "플레이/빌드 오류 가능성이 높으므로 우선 수정합니다.",
                "데이터 입력 대기" => "데이터가 아직 덜 들어온 상태라면 추적만 하고, 데이터 입력 후 다시 검사합니다.",
                "기획/정책 확인" => "공식, 기준값, 예외 처리처럼 기획 확정이 필요한 항목입니다.",
                "확인 필요" => "동적 연결/작업 중 상태일 수 있으므로 담당자가 확인합니다.",
                "정보" => "정상 집계 또는 검사 완료 정보입니다.",
                _ => "분류 기준을 확인합니다."
            };
        }

        private static string GetOwnerStatus(QAResult result)
        {
            if (result.Severity == QASeverity.Info)
                return "공통 QA";

            if (IsDevelopmentOwnerResult(result))
                return "개발 QA";

            if (IsDesignOwnerResult(result))
                return "기획 QA";

            return "공통 QA";
        }

        private static string GetOwnerMemo(QAResult result)
        {
            return GetOwnerStatus(result) switch
            {
                "기획 QA" => "데이터, 수치, 공식, 콘텐츠 누락처럼 기획자가 먼저 판단할 항목입니다.",
                "개발 QA" => "씬/프리팹/버튼/참조/빌드처럼 개발자가 연결 상태를 확인할 항목입니다.",
                "공통 QA" => "정상 집계이거나 기획/개발이 함께 확인할 수 있는 항목입니다.",
                _ => "담당 영역을 확인합니다."
            };
        }

        private static bool IsDataPendingResult(QAResult result)
        {
            return Contains(result.Category, "Report Coverage")
                || Contains(result.Category, "Report Simulation")
                || Contains(result.Category, "Report Team Simulation")
                || Contains(result.Message, "보고서 후보")
                || Contains(result.Message, "보고서 구간")
                || Contains(result.Message, "아직 입력")
                || Contains(result.Message, "조합 누락")
                || Contains(result.Message, "coverage");
        }

        private static bool IsDesignReviewResult(QAResult result)
        {
            return Contains(result.Category, "Project Formula")
                || Contains(result.Message, "기획")
                || Contains(result.Message, "정책")
                || Contains(result.Message, "공식")
                || Contains(result.Message, "클램프")
                || Contains(result.Message, "최소값")
                || Contains(result.Message, "극단 케이스");
        }

        private static bool IsDesignOwnerResult(QAResult result)
        {
            return IsDataPendingResult(result)
                || IsDesignReviewResult(result)
                || Contains(result.Category, "Sheet Sync")
                || Contains(result.Category, "Employee")
                || Contains(result.Category, "Report")
                || Contains(result.Category, "Quest")
                || Contains(result.Category, "Comment");
        }

        private static bool IsDevelopmentOwnerResult(QAResult result)
        {
            return Contains(result.Category, "Scene")
                || Contains(result.Category, "Prefab")
                || Contains(result.Category, "Play Flow")
                || Contains(result.Message, "Missing Script")
                || Contains(result.Message, "깨진 Object Reference")
                || Contains(result.Message, "OnClick")
                || Contains(result.Message, "Presenter")
                || Contains(result.Message, "View")
                || Contains(result.Message, "Manager가 없어");
        }

        private static bool MatchesSearch(QAResult result, string searchText)
        {
            return Contains(result.Category, searchText)
                || Contains(result.Message, searchText)
                || Contains(result.AssetPath, searchText)
                || Contains(result.Severity.ToString(), searchText)
                || Contains(GetTriageStatus(result), searchText)
                || Contains(GetOwnerStatus(result), searchText);
        }

        private static bool Contains(string value, string searchText)
        {
            return !string.IsNullOrEmpty(value)
                && value.IndexOf(searchText, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
