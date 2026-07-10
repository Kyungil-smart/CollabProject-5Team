namespace GameDevTycoon.EditorQA
{
    public sealed class QAResultAdvice
    {
        public string Priority { get; }
        public string ExpectedCause { get; }
        public string RelatedCode { get; }
        public string FixHint { get; }

        public QAResultAdvice(
            string priority,
            string expectedCause,
            string relatedCode,
            string fixHint)
        {
            Priority = priority;
            ExpectedCause = expectedCause;
            RelatedCode = relatedCode;
            FixHint = fixHint;
        }
    }

    public static class QAResultAdvisor
    {
        public static QAResultAdvice GetAdvice(QAResult result)
        {
            if (result == null)
                return new QAResultAdvice("P3", "결과 정보가 없습니다.", "QAResult", "QA 결과 생성 지점을 확인합니다.");

            if (Contains(result.Message, "Missing Script"))
            {
                return new QAResultAdvice(
                    "P0",
                    "프리팹/씬에 삭제되었거나 이름이 바뀐 MonoBehaviour 참조가 남아 있을 가능성이 높습니다.",
                    "Unity Inspector, prefab/scene serialized data",
                    "Select로 대상 에셋을 열고 Missing Script 컴포넌트를 삭제하거나 원래 스크립트를 복구합니다.");
            }

            if (Contains(result.Message, "깨진 Object Reference") || Contains(result.Category, "Broken Reference"))
            {
                return new QAResultAdvice(
                    "P0",
                    "Inspector에 연결된 에셋/오브젝트가 삭제되었거나 GUID가 바뀐 상태입니다.",
                    "해당 prefab/scene의 Inspector 참조 필드",
                    "Select로 대상 에셋을 선택한 뒤 Inspector에서 Missing 상태인 Object Reference를 다시 연결합니다.");
            }

            if (Contains(result.Message, "OnClick"))
            {
                return new QAResultAdvice(
                    result.Severity == QASeverity.Error ? "P0" : "P2",
                    "버튼 이벤트 대상 또는 메서드명이 비어 있거나, 코드에서 동적으로 연결되는 버튼일 수 있습니다.",
                    "UnityEngine.UI.Button.onClick, 관련 Presenter/View",
                    "정적 연결 버튼이면 OnClick 대상과 메서드를 다시 지정하고, 동적 연결 버튼이면 Warning으로 관리합니다.");
            }

            if (Contains(result.Category, "Scene / Prefab") || Contains(result.Category, "Scene") || Contains(result.Category, "Prefab"))
            {
                return new QAResultAdvice(
                    result.Severity == QASeverity.Error ? "P0" : "P2",
                    "씬/프리팹 구성 또는 필수 UI 참조가 현재 코드가 기대하는 구조와 어긋났을 가능성이 있습니다.",
                    "QAScenePrefabValidator, 관련 Presenter/View, GameScene",
                    "Select로 에셋을 선택해 Inspector 필드를 확인하고, 필수 매니저/Presenter/View 참조를 연결합니다.");
            }

            if (Contains(result.Category, "Play Flow"))
            {
                return new QAResultAdvice(
                    result.Severity == QASeverity.Error ? "P0" : "P2",
                    "낮 업무 시작, 일일 퀘스트 완료, 금요일 밤 보고서, 밤 퇴근 중 한 단계의 연결이 끊겼을 가능성이 있습니다.",
                    "DateTimeManager, HUDPresenter/HUDView, DeskInteract, QuestManager, ReportPresenter/ReportView",
                    "Select로 대상 컴포넌트를 열고 Inspector 참조를 연결한 뒤, Play Mode에서 업무 시작→퀘스트 완료→금요일 밤 보고서 흐름을 확인합니다.");
            }

            if (Contains(result.Category, "Sheet Sync"))
            {
                return new QAResultAdvice(
                    result.Severity == QASeverity.Error ? "P1" : "P2",
                    "구글시트 DataRequestSet 설정과 Unity SO 리스트가 맞지 않을 가능성이 있습니다.",
                    "DataRequestSet, SheetData, SheetDataSOBase",
                    "DataRequestSet의 URL/gid/startRow/targetSOList를 확인하고, 시트에서 새 행을 추가했다면 대응 SO가 생성/연결됐는지 확인합니다.");
            }

            if (Contains(result.Category, "Employee"))
            {
                return new QAResultAdvice(
                    result.Severity == QASeverity.Error ? "P1" : "P2",
                    "직원 테이블 값 또는 직원 SO의 특성/스탯 입력이 누락되었을 가능성이 있습니다.",
                    "EmployeeImmutableData, TraitTable, QAEmployeeValidator",
                    "직원 SO와 원본 직원 테이블의 id, role, ability/desire/fatigue/loyalty, main/sub/riskTrait 값을 비교합니다.");
            }

            if (Contains(result.Category, "Report Coverage")
                || Contains(result.Category, "Report Simulation")
                || Contains(result.Category, "Report Team Simulation"))
            {
                return new QAResultAdvice(
                    "P2",
                    "직원 특성/등급/startRepo 조합에 대응하는 보고서 데이터가 아직 없거나, ReportManager 조회 키가 맞지 않을 수 있습니다.",
                    "ReportSO, ReportManager, ReportPolicy, QAReportSimulationValidator",
                    "보고서 테이블에서 trait/startRepo/grade 조합을 확인하고, 데이터 입력 중인 항목이면 Warning으로 추적합니다.");
            }

            if (Contains(result.Category, "Report"))
            {
                return new QAResultAdvice(
                    result.Severity == QASeverity.Error ? "P1" : "P2",
                    "보고서 SO의 필수 텍스트, 직군, 특성, 조회 키 설정이 누락되었을 가능성이 있습니다.",
                    "ReportSO, ReportManager, QAReportValidator",
                    "보고서 SO와 원본 보고서 테이블의 id, role, trait, grade, startRepo, title/content를 비교합니다.");
            }

            if (Contains(result.Category, "Project Formula"))
            {
                return new QAResultAdvice(
                    "P2",
                    "기획 공식과 현재 코드 구현 또는 경계값 처리 정책이 아직 확정되지 않은 상태일 수 있습니다.",
                    "PerkPolicy, ReportPolicy, ProjectSO, QAProjectFormulaValidator",
                    "런타임 코드는 바로 수정하지 말고, 기획 기준을 확정한 뒤 공식 또는 QA 기대값을 조정합니다.");
            }

            if (Contains(result.Category, "Quest") || Contains(result.Category, "Comment"))
            {
                return new QAResultAdvice(
                    result.Severity == QASeverity.Error ? "P1" : "P2",
                    "퀘스트/코멘트 테이블의 조건, 목표 수, 대사, 역할 조건이 비어 있거나 조합이 부족할 수 있습니다.",
                    "QuestSO, EmployeeCommentData, QAQuestCommentValidator",
                    "퀘스트/코멘트 SO와 원본 테이블의 id, role, trigger 조건, targetCount, dialogue 값을 비교합니다.");
            }

            if (result.Severity == QASeverity.Info)
            {
                return new QAResultAdvice(
                    "P3",
                    "정상 집계 또는 검사 완료 정보입니다.",
                    "QA validator summary",
                    "별도 조치가 필요하지 않습니다.");
            }

            return new QAResultAdvice(
                result.Severity == QASeverity.Error ? "P1" : "P2",
                "일반 QA 항목입니다. 카테고리와 대상 에셋을 기준으로 원인을 확인해야 합니다.",
                result.Category,
                "Select 버튼으로 대상 에셋을 열고, 메시지의 조건과 Inspector 값을 비교합니다.");
        }

        private static bool Contains(string value, string searchText)
        {
            return !string.IsNullOrEmpty(value)
                && value.IndexOf(searchText, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
