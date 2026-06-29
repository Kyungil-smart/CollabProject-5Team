using UnityEditor;
using UnityEngine;

namespace GameDevTycoon.EditorQA
{
    public sealed class BalanceHubWindow : EditorWindow
    {
        [MenuItem("Tools/Balance/1. Balance Hub", false, 201)]
        public static void Open()
        {
            BalanceHubWindow window = GetWindow<BalanceHubWindow>("Balance Hub");
            window.minSize = new Vector2(540f, 420f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Balance Hub", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Balance는 프로젝트 점수, 보고서 생성, 매출, 재화, 직원 상태를 반복 시뮬레이션하는 영역입니다. 데이터/연결 오류 검증은 Tools > QA 메뉴에서 진행합니다.",
                MessageType.Info);

            DrawSection(
                "1. 프로젝트 밸런스",
                "프로젝트 규모, 직군별 인원 배치, 일일 퀘스트 보너스가 완성도/안정성/매력도와 매출에 미치는 영향을 확인합니다.",
                "프로젝트 밸런스 열기",
                ProjectBalanceSimulatorWindow.Open);

            DrawSection(
                "2. 보고서 생성",
                "직원 능력치, 특성, 의욕에 따라 어떤 보고서 후보가 생성되는지 확인합니다.",
                "보고서 생성 시뮬레이터 열기",
                ReportSimulatorWindow.Open);

            DrawSection(
                "3. 매출 밸런스",
                "출시 후 유지력, 회사 인기, 완성도/안정성/매력도가 일일 판매량과 매출에 미치는 영향을 확인합니다.",
                "매출 밸런스 열기",
                RevenueSimulatorWindow.Open);

            DrawSection(
                "4. 재화 밸런스",
                "급여, 프로젝트 비용, 채용/교육비, 유지비, 서비스 매출을 주차 단위로 합산해 회사 생존성을 확인합니다.",
                "재화 밸런스 열기",
                EconomySimulatorWindow.Open);

            DrawSection(
                "5. 직원 생애주기",
                "대화, 보고서 채택, 교육, 프로젝트 완료 보상이 직원 성장/피로/퇴사 위험에 미치는 영향을 확인합니다.",
                "직원 생애주기 열기",
                EmployeeLifecycleSimulatorWindow.Open);

            DrawSection(
                "6. 주차/일자 지표 흐름",
                "월~금 낮 업무, 금요일 밤 보고서, 출시 후 매출까지 직원/프로젝트/재화 지표가 일자별로 어떻게 변하는지 추적합니다.",
                "지표 흐름 시뮬레이터 열기",
                FlowTimelineSimulatorWindow.Open);
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
