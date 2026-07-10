using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameDevTycoon.EditorQA
{
    public sealed class QAWindow : EditorWindow
    {
        private enum QAViewMode
        {
            All,
            Planning,
            Development
        }

        private enum DataPendingFilterMode
        {
            Include,
            Hide,
            Only
        }

        private readonly List<IQAValidator> _validators = new();
        private readonly List<QAResult> _results = new();
        private readonly Dictionary<string, bool> _expandedGroups = new();
        private Vector2 _scrollPosition;
        private bool _showInfo = true;
        private bool _showWarning = true;
        private bool _showError = true;
        private bool _showChecklist;
        private bool _groupSimilarResults = true;
        private DataPendingFilterMode _dataPendingFilter = DataPendingFilterMode.Include;
        private QAViewMode _viewMode = QAViewMode.All;
        private string _selectedCategory = AllCategories;
        private string _selectedTriage = AllTriage;
        private string _selectedOwner = AllOwners;
        private string _selectedWorkType = AllWorkTypes;
        private string _searchText = string.Empty;

        private const string AllCategories = "전체";
        private const string AllTriage = "전체";
        private const string AllOwners = "전체";
        private const string AllWorkTypes = "전체";
        private const int MaxVisibleResults = 400;

        [MenuItem("Tools/QA/2. Data & Connection QA", false, 102)]
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
            DrawModeSelector();
            DrawChecklist();
            DrawSummary();
            DrawActionGuide();
            DrawFilters();
            DrawResults();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button(GetRunButtonLabel(), EditorStyles.toolbarButton, GUILayout.Width(130f)))
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
                    _expandedGroups.Clear();
                }
            }
        }

        private void DrawModeSelector()
        {
            EditorGUILayout.Space(6f);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("QA 보기 모드", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawModeButton(QAViewMode.All, "전체", EditorStyles.miniButtonLeft);
                    DrawModeButton(QAViewMode.Planning, "기획 데이터 QA", EditorStyles.miniButtonMid);
                    DrawModeButton(QAViewMode.Development, "개발 연결 QA", EditorStyles.miniButtonRight);
                }

                EditorGUILayout.LabelField(GetModeDescription(), EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.HelpBox(GetModeGuide(), MessageType.Info);
            }
        }

        private void DrawModeButton(QAViewMode mode, string label, GUIStyle style)
        {
            bool selected = _viewMode == mode;
            bool nextSelected = GUILayout.Toggle(selected, label, style);
            if (!selected && nextSelected)
            {
                _viewMode = mode;
                _selectedOwner = AllOwners;
            }
        }

        private string GetModeDescription()
        {
            return _viewMode switch
            {
                QAViewMode.Planning => "기획 데이터 QA: 테이블, 보고서 조건, 코멘트 조건, 공식처럼 기획자가 먼저 판단할 항목만 실행합니다.",
                QAViewMode.Development => "개발 연결 QA: 씬, 프리팹, 버튼, Presenter/View, 플레이 플로우처럼 개발자가 연결 상태를 확인할 항목만 실행합니다.",
                _ => "전체 QA: 기획 데이터와 개발 연결 상태를 함께 확인합니다. 결과가 많으면 먼저 기획/개발 모드를 나누어 실행하세요."
            };
        }

        private string GetModeGuide()
        {
            return _viewMode switch
            {
                QAViewMode.Planning => "권장 사용: 기획자가 새 데이터 입력 후 실행 → 데이터 입력 대기/기획 확인 항목만 우선 확인 → 개발 수정이 필요한 항목은 개발 QA로 넘깁니다.",
                QAViewMode.Development => "권장 사용: 개발자가 씬/프리팹/UI 연결 작업 후 실행 → Missing Script, 깨진 참조, 버튼 연결 누락을 우선 확인합니다.",
                _ => "밸런스 검증은 Tools > Balance 메뉴의 시뮬레이터에서 분리해서 확인합니다. 이 창은 데이터/연결 상태 QA에 집중합니다."
            };
        }

        private void DrawChecklist()
        {
            EditorGUILayout.Space(6f);
            _showChecklist = EditorGUILayout.Foldout(_showChecklist, "현재 모드에서 실행되는 검사 항목", true);
            if (!_showChecklist)
                return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(
                    "선택한 모드에서 어떤 검사를 실행하는지 확인하는 용도입니다.",
                    EditorStyles.miniLabel);

                foreach (IQAValidator validator in _validators)
                {
                    if (!ValidatorMatchesViewMode(validator))
                        continue;

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

        private bool ValidatorMatchesViewMode(IQAValidator validator)
        {
            if (_viewMode == QAViewMode.All || validator == null)
                return true;

            string owner = GetValidatorOwner(validator.Name);
            return _viewMode switch
            {
                QAViewMode.Planning => owner == "기획 QA",
                QAViewMode.Development => owner == "개발 QA",
                _ => true
            };
        }

        private static string GetValidatorOwner(string validatorName)
        {
            return validatorName switch
            {
                "Sheet Sync" => "기획 QA",
                "Employee Data" => "기획 QA",
                "Report Data" => "기획 QA",
                "Report Generation Simulation" => "기획 QA",
                "Project Formula" => "기획 QA",
                "Quest / Comment Data" => "기획 QA",
                "Scene / Prefab References" => "개발 QA",
                "Play Flow" => "개발 QA",
                _ => "공통 QA"
            };
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
            EditorGUILayout.LabelField($"1차 QA 에디터 - {GetModeLabel()}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                $"결과 요약: Error {errorCount} / Warning {warningCount} / Info {infoCount} / 현재 표시 {visibleCount}");
            EditorGUILayout.LabelField(
                $"조치 분류: 즉시 수정 {fixNowCount} / 데이터 입력 대기 {dataPendingCount} / 기획 확인 {designReviewCount} / 담당 확인 {checkNeededCount}");
            EditorGUILayout.LabelField(
                $"확인 담당: 기획 QA {designOwnerCount} / 개발 QA {devOwnerCount} / 공통 QA {sharedOwnerCount}");
            EditorGUILayout.Space(4f);
        }

        private void DrawActionGuide()
        {
            if (_results.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "QA 실행 후 이 영역에 권장 확인 순서가 표시됩니다. 먼저 기획 데이터 QA와 개발 연결 QA를 나누어 실행하면 결과를 읽기 쉽습니다.",
                    MessageType.Info);
                return;
            }

            List<QAResult> modeResults = _results.Where(MatchesViewMode).ToList();
            int fixNowCount = modeResults.Count(result => GetTriageStatus(result) == "즉시 수정");
            int devFixCount = modeResults.Count(result => GetOwnerStatus(result) == "개발 QA" && result.Severity == QASeverity.Error);
            int dataPendingCount = modeResults.Count(result => GetTriageStatus(result) == "데이터 입력 대기");
            int designReviewCount = modeResults.Count(result => GetTriageStatus(result) == "기획/정책 확인");

            string guide =
                "권장 확인 순서\n" +
                $"1. 즉시 수정: 플레이/빌드를 막을 수 있는 Error부터 확인 ({fixNowCount}건)\n" +
                $"2. 개발 연결: Missing Script, 깨진 참조, 버튼 연결 누락 확인 ({devFixCount}건)\n" +
                $"3. 데이터 입력 대기: 아직 덜 입력된 보고서/코멘트/시트 항목 확인 ({dataPendingCount}건)\n" +
                $"4. 기획 확인: 공식, 기준값, 예외 처리처럼 결정이 필요한 항목 확인 ({designReviewCount}건)";

            EditorGUILayout.HelpBox(guide, fixNowCount > 0 ? MessageType.Warning : MessageType.Info);
        }

        private void DrawFilters()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                _showError = GUILayout.Toggle(_showError, "Error", EditorStyles.miniButtonLeft);
                _showWarning = GUILayout.Toggle(_showWarning, "Warning", EditorStyles.miniButtonMid);
                _showInfo = GUILayout.Toggle(_showInfo, "Info", EditorStyles.miniButtonRight);
                _groupSimilarResults = GUILayout.Toggle(_groupSimilarResults, "동일 유형 묶기", EditorStyles.miniButton, GUILayout.Width(110f));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("데이터 입력 대기", GUILayout.Width(92f));
                DrawDataPendingFilterButton(DataPendingFilterMode.Include, "전체", EditorStyles.miniButtonLeft);
                DrawDataPendingFilterButton(DataPendingFilterMode.Hide, "숨기기", EditorStyles.miniButtonMid);
                DrawDataPendingFilterButton(DataPendingFilterMode.Only, "대기만", EditorStyles.miniButtonRight);
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

                EditorGUILayout.LabelField("검사 영역", GUILayout.Width(70f));
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

                EditorGUILayout.LabelField("조치 분류", GUILayout.Width(70f));
                int nextTriageIndex = EditorGUILayout.Popup(currentTriageIndex, triageOptions);
                _selectedTriage = triageOptions[nextTriageIndex];
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                string[] workTypeOptions = BuildWorkTypeOptions();
                int currentWorkTypeIndex = System.Array.IndexOf(workTypeOptions, _selectedWorkType);
                if (currentWorkTypeIndex < 0)
                {
                    currentWorkTypeIndex = 0;
                    _selectedWorkType = AllWorkTypes;
                }

                EditorGUILayout.LabelField("작업 유형", GUILayout.Width(70f));
                int nextWorkTypeIndex = EditorGUILayout.Popup(currentWorkTypeIndex, workTypeOptions);
                _selectedWorkType = workTypeOptions[nextWorkTypeIndex];
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

                EditorGUILayout.LabelField("확인 담당", GUILayout.Width(70f));
                int nextOwnerIndex = EditorGUILayout.Popup(currentOwnerIndex, ownerOptions);
                _selectedOwner = ownerOptions[nextOwnerIndex];

                EditorGUILayout.LabelField("검색", GUILayout.Width(36f));
                _searchText = EditorGUILayout.TextField(_searchText);

                EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_searchText));
                if (GUILayout.Button("초기화", GUILayout.Width(54f)))
                    _searchText = string.Empty;
                EditorGUI.EndDisabledGroup();
            }
        }

        private void DrawDataPendingFilterButton(DataPendingFilterMode mode, string label, GUIStyle style)
        {
            bool selected = _dataPendingFilter == mode;
            bool nextSelected = GUILayout.Toggle(selected, label, style);
            if (!selected && nextSelected)
                _dataPendingFilter = mode;
        }

        private void DrawResults()
        {
            EditorGUILayout.Space(6f);

            List<QAResult> visibleResults = _results
                .Where(ShouldShow)
                .OrderBy(GetSeverityOrder)
                .ThenBy(GetTriageOrder)
                .ToList();

            if (_groupSimilarResults)
            {
                DrawGroupedResults(visibleResults);
                return;
            }

            if (visibleResults.Count > MaxVisibleResults)
            {
                EditorGUILayout.HelpBox(
                    $"표시 결과가 {visibleResults.Count}개라 최초 {MaxVisibleResults}개만 보여줍니다. 검사 영역/조치 분류/검색 필터로 범위를 좁혀 확인하세요.",
                    MessageType.Warning);
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            foreach (QAResult result in visibleResults.Take(MaxVisibleResults))
            {
                DrawResult(result);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawGroupedResults(List<QAResult> visibleResults)
        {
            var groups = visibleResults
                .GroupBy(GetResultGroupKey)
                .Select(group => new
                {
                    Key = group.Key,
                    First = group.First(),
                    Results = group.ToList(),
                    Count = group.Count()
                })
                .OrderBy(group => GetSeverityOrder(group.First))
                .ThenBy(group => GetTriageOrder(group.First))
                .ThenByDescending(group => group.Count)
                .ToList();

            EditorGUILayout.LabelField(
                $"동일 유형 묶기: 결과 {visibleResults.Count}건 / 그룹 {groups.Count}개",
                EditorStyles.miniLabel);

            if (groups.Count > MaxVisibleResults)
            {
                EditorGUILayout.HelpBox(
                    $"표시 그룹이 {groups.Count}개라 최초 {MaxVisibleResults}개만 보여줍니다. 검사 영역/조치 분류/검색 필터로 범위를 좁혀 확인하세요.",
                    MessageType.Warning);
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            foreach (var group in groups.Take(MaxVisibleResults))
            {
                DrawResultGroup(group.Key, group.First, group.Results, group.Count);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawResultGroup(string groupKey, QAResult first, List<QAResult> results, int count)
        {
            string triageStatus = GetTriageStatus(first);
            string ownerStatus = GetOwnerStatus(first);
            string title = GetGroupTitle(first);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                bool expanded = _expandedGroups.TryGetValue(groupKey, out bool savedExpanded) && savedExpanded;
                expanded = EditorGUILayout.Foldout(
                    expanded,
                    $"[{first.Severity}] {title} - {count}건",
                    true);
                _expandedGroups[groupKey] = expanded;

                EditorGUILayout.LabelField(
                    $"확인 담당: {ownerStatus} / 조치 분류: {triageStatus} / 작업 유형: {GetWorkType(first)} / 검사 영역: {first.Category}",
                    EditorStyles.miniLabel);
                EditorGUILayout.LabelField(
                    $"대표 메시지: {first.Message}",
                    EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField(
                    $"대표 원인: {GetGroupCause(first)}",
                    EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField(
                    $"권장 작업: {GetGroupAction(first)}",
                    EditorStyles.wordWrappedMiniLabel);

                if (!expanded)
                    return;

                int sampleCount = Mathf.Min(results.Count, 20);
                EditorGUILayout.LabelField($"상세 샘플 {sampleCount}/{results.Count}", EditorStyles.miniLabel);
                foreach (QAResult result in results.Take(sampleCount))
                {
                    DrawResult(result);
                }

                if (results.Count > sampleCount)
                {
                    EditorGUILayout.HelpBox(
                        $"나머지 {results.Count - sampleCount}건은 같은 유형이라 접어두었습니다. 검사 영역/조치 분류/검색 필터로 더 좁혀 확인할 수 있습니다.",
                        MessageType.Info);
                }
            }
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
                EditorGUILayout.LabelField($"확인 담당: {ownerStatus} - {ownerMemo}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"조치 분류: {triageStatus} - {triageMemo}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"작업 유형: {GetWorkType(result)}", EditorStyles.miniLabel);
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

        private string GetResultGroupKey(QAResult result)
        {
            return string.Join(
                "|",
                result.Severity.ToString(),
                result.Category ?? string.Empty,
                GetOwnerStatus(result),
                GetTriageStatus(result),
                GetNormalizedMessage(result));
        }

        private string GetGroupTitle(QAResult result)
        {
            return $"[{result.Category}] {GetNormalizedMessage(result)}";
        }

        private static string GetGroupCause(QAResult result)
        {
            string normalized = GetNormalizedMessage(result);
            return normalized switch
            {
                "targetSOList 미연결 SO" => "시트와 ScriptableObject 연결 목록이 아직 맞지 않습니다.",
                "targetSOList 외부 폴더 SO 연결" => "시트 동기화 대상 폴더 밖의 에셋이 연결되어 있을 수 있습니다.",
                "직원 특성 조합별 보고서 구간 누락" => "직원 특성/등급/시작 보고서 조건에 맞는 보고서 데이터가 부족합니다.",
                "직원별 보고서 생성 후보 없음" => "해당 직원의 특성 조합으로 조회 가능한 보고서 후보가 없습니다.",
                "1인 팀 조합 보고서 후보 누락" => "프로젝트 인원 조합에서 최소 보고서 후보가 비는 케이스가 있습니다.",
                "주간 코멘트 조건 조합 누락" => "의욕/피로도/충성도 조건 조합에 대응하는 코멘트가 부족합니다.",
                "버튼 Inspector OnClick 미연결" => "UI 버튼이 클릭 이벤트를 호출할 대상과 연결되지 않았습니다.",
                "Missing Script" => "씬 또는 프리팹에 삭제되었거나 이름이 바뀐 스크립트 참조가 남아 있습니다.",
                "깨진 Object Reference" => "씬 또는 프리팹의 Inspector 참조가 끊어져 있습니다.",
                "필수 참조 누락" => "Presenter, View, Manager 등 실행에 필요한 참조가 비어 있습니다.",
                _ => QAResultAdvisor.GetAdvice(result).ExpectedCause
            };
        }

        private static string GetGroupAction(QAResult result)
        {
            string normalized = GetNormalizedMessage(result);
            return normalized switch
            {
                "targetSOList 미연결 SO" => "시트 동기화 대상 리스트를 갱신하거나 의도적으로 제외할 데이터인지 확인합니다.",
                "targetSOList 외부 폴더 SO 연결" => "에셋 위치가 규칙에 맞는지 보고, 필요하면 targetSOList에서 제거합니다.",
                "직원 특성 조합별 보고서 구간 누락" => "데이터 입력 중이면 추적만 하고, 완성 대상이면 해당 조건의 보고서를 추가합니다.",
                "직원별 보고서 생성 후보 없음" => "직원 특성 테이블과 보고서 테이블의 trait/startRepo/grade 조합을 맞춥니다.",
                "1인 팀 조합 보고서 후보 누락" => "소형/최소 인원 프로젝트에서도 보고서 후보가 나오도록 데이터 범위를 보강합니다.",
                "주간 코멘트 조건 조합 누락" => "해당 직군과 상태 조합의 주간 코멘트를 추가합니다.",
                "버튼 Inspector OnClick 미연결" => "버튼을 선택해 OnClick 대상과 메서드를 연결합니다.",
                "Missing Script" => "프리팹/씬에서 Missing Script 컴포넌트를 제거하거나 올바른 스크립트를 다시 붙입니다.",
                "깨진 Object Reference" => "Select 버튼으로 대상을 찾아 Inspector 참조를 복구합니다.",
                "필수 참조 누락" => "해당 Presenter/View/Manager 필드에 필요한 오브젝트를 연결합니다.",
                _ => QAResultAdvisor.GetAdvice(result).FixHint
            };
        }

        private static string GetNormalizedMessage(QAResult result)
        {
            string message = result?.Message ?? string.Empty;

            if (Contains(message, "targetSOList에는 연결되어 있지 않습니다")
                || Contains(message, "targetSOList 미연결 SO"))
                return "targetSOList 미연결 SO";

            if (Contains(message, "같은 폴더 밖"))
                return "targetSOList 외부 폴더 SO 연결";

            if (Contains(message, "직원의 특성 조합")
                && Contains(message, "보고서 구간"))
                return "직원 특성 조합별 보고서 구간 누락";

            if (Contains(message, "보고서 생성 후보가 없습니다"))
                return "직원별 보고서 생성 후보 없음";

            if (Contains(message, "1인 팀 조합에서 보고서 후보가 비는"))
                return "1인 팀 조합 보고서 후보 누락";

            if (Contains(message, "코멘트 조합이 비어 있습니다"))
                return "주간 코멘트 조건 조합 누락";

            if (Contains(message, "버튼에 Inspector OnClick 연결이 없습니다"))
                return "버튼 Inspector OnClick 미연결";

            if (Contains(message, "Missing Script"))
                return "Missing Script";

            if (Contains(message, "깨진 Object Reference"))
                return "깨진 Object Reference";

            if (Contains(message, "필수 참조가 비어 있습니다"))
                return "필수 참조 누락";

            return message;
        }

        private void RunAllValidators()
        {
            _results.Clear();
            _expandedGroups.Clear();
            _results.AddRange(QARunner.RunAll(_validators.Where(ValidatorMatchesViewMode)));
        }

        private string GetRunButtonLabel()
        {
            return _viewMode switch
            {
                QAViewMode.Planning => "기획 데이터 QA 실행",
                QAViewMode.Development => "개발 연결 QA 실행",
                _ => "전체 QA 실행"
            };
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

            bool dataPending = GetTriageStatus(result) == "데이터 입력 대기";
            if (_dataPendingFilter == DataPendingFilterMode.Hide && dataPending)
                return false;

            if (_dataPendingFilter == DataPendingFilterMode.Only && !dataPending)
                return false;

            if (_selectedCategory != AllCategories && result.Category != _selectedCategory)
                return false;

            if (_selectedTriage != AllTriage && GetTriageStatus(result) != _selectedTriage)
                return false;

            if (!MatchesViewMode(result))
                return false;

            if (_selectedOwner != AllOwners && GetOwnerStatus(result) != _selectedOwner)
                return false;

            if (_selectedWorkType != AllWorkTypes && GetWorkType(result) != _selectedWorkType)
                return false;

            if (!string.IsNullOrWhiteSpace(_searchText) && !MatchesSearch(result, _searchText.Trim()))
                return false;

            return true;
        }

        private bool MatchesViewMode(QAResult result)
        {
            string owner = GetOwnerStatus(result);
            return _viewMode switch
            {
                QAViewMode.Planning => owner == "기획 QA",
                QAViewMode.Development => owner == "개발 QA",
                _ => true
            };
        }

        private string GetModeLabel()
        {
            return _viewMode switch
            {
                QAViewMode.Planning => "기획 데이터 QA 모드",
                QAViewMode.Development => "개발 연결 QA 모드",
                _ => "전체 QA 모드"
            };
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

        private string[] BuildWorkTypeOptions()
        {
            return new[] { AllWorkTypes }
                .Concat(_results
                    .Select(GetWorkType)
                    .Where(workType => !string.IsNullOrWhiteSpace(workType))
                    .Distinct()
                    .OrderBy(GetWorkTypeOrder))
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

        private static int GetWorkTypeOrder(string workType)
        {
            return workType switch
            {
                "즉시 실행 오류 수정" => 0,
                "프리팹/씬 참조 복구" => 1,
                "UI 버튼 연결" => 2,
                "보고서 데이터 보강" => 3,
                "주간 코멘트 보강" => 4,
                "시트 동기화 확인" => 5,
                "공식/기준값 확인" => 6,
                "데이터 확인" => 7,
                "기타 확인" => 8,
                _ => 9
            };
        }

        private static string GetWorkType(QAResult result)
        {
            string normalized = GetNormalizedMessage(result);

            if (normalized == "버튼 Inspector OnClick 미연결")
                return "UI 버튼 연결";

            if (normalized == "Missing Script"
                || normalized == "깨진 Object Reference"
                || normalized == "필수 참조 누락")
                return "프리팹/씬 참조 복구";

            if (normalized == "targetSOList 미연결 SO"
                || normalized == "targetSOList 외부 폴더 SO 연결")
                return "시트 동기화 확인";

            if (normalized == "주간 코멘트 조건 조합 누락")
                return "주간 코멘트 보강";

            if (normalized == "직원 특성 조합별 보고서 구간 누락"
                || normalized == "직원별 보고서 생성 후보 없음"
                || normalized == "1인 팀 조합 보고서 후보 누락")
                return "보고서 데이터 보강";

            if (IsDesignReviewResult(result))
                return "공식/기준값 확인";

            if (IsDataPendingResult(result))
                return "데이터 확인";

            if (result.Severity == QASeverity.Error)
                return "즉시 실행 오류 수정";

            return "기타 확인";
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
                "확인 필요" => "기획/개발 중 실제 담당자가 확인할 항목입니다.",
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
                "기획 QA" => "테이블, 수치, 공식, 콘텐츠 누락처럼 기획자가 먼저 판단할 항목입니다.",
                "개발 QA" => "씬, 프리팹, 버튼, 참조, 빌드처럼 개발자가 연결 상태를 확인할 항목입니다.",
                "공통 QA" => "정상 집계이거나 기획/개발이 함께 확인할 항목입니다.",
                _ => "확인 담당을 확인합니다."
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
                || Contains(GetOwnerStatus(result), searchText)
                || Contains(GetWorkType(result), searchText);
        }

        private static bool Contains(string value, string searchText)
        {
            return !string.IsNullOrEmpty(value)
                && value.IndexOf(searchText, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
