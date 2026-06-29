using UnityEditor;
using UnityEngine;

namespace GameDevTycoon.EditorQA
{
    public sealed class QAToolHubWindow : EditorWindow
    {
        [MenuItem("Tools/QA/1. QA Hub", false, 101)]
        public static void Open()
        {
            QAToolHubWindow window = GetWindow<QAToolHubWindow>("QA Hub");
            window.minSize = new Vector2(520f, 360f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("QA Hub", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "QA는 데이터/연결 상태를 확인하는 창입니다. 수치 밸런스 반복 검증은 Tools > Balance 메뉴에서 진행합니다.",
                MessageType.Info);

            DrawSection(
                "1. 기획 데이터 QA",
                "직원, 보고서, 코멘트, 공식, 시트 동기화처럼 기획자가 먼저 확인할 데이터 항목을 검증합니다.",
                "기획 데이터 QA 열기",
                QAWindow.Open);

            DrawSection(
                "2. 기획 QA 대시보드",
                "직원/보고서/퀘스트/프로젝트 데이터 현황을 요약해서 확인합니다.",
                "기획 QA 대시보드 열기",
                PlanningQADashboardWindow.Open);

            DrawSection(
                "3. 빌드 사전 검사",
                "빌드 전에 Error가 남아 있는지 빠르게 확인합니다. 상세 분석은 Data & Connection QA에서 확인합니다.",
                "빌드 사전 검사 실행",
                QABuildPrecheck.RunFromMenu);

            EditorGUILayout.Space(8f);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("추천 사용 순서", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("1. 기획 데이터 QA 실행", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("2. 데이터 입력 대기/작업 유형 필터로 결과 정리", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("3. 개발 연결 QA에서 Missing Script, 깨진 참조, 버튼 연결 확인", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("4. 필요하면 Markdown 리포트로 공유", EditorStyles.miniLabel);
            }
        }

        private static void DrawSection(string title, string description, string buttonLabel, System.Action onClick)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(description, EditorStyles.wordWrappedMiniLabel);
                if (GUILayout.Button(buttonLabel, GUILayout.Height(28f)))
                    onClick?.Invoke();
            }
        }
    }
}
