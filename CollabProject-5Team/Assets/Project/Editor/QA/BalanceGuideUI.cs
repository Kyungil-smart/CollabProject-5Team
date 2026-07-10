using UnityEditor;
using UnityEngine;

namespace GameDevTycoon.EditorQA
{
    internal static class BalanceGuideUI
    {
        public static void Draw(string title, string firstAdjust, string resultWatch, string warningSignals)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawColumn("먼저 조절할 값", firstAdjust);
                    DrawColumn("결과에서 볼 것", resultWatch);
                    DrawColumn("위험 신호", warningSignals);
                }
            }
        }

        public static void DrawDataFlow(string inputSource, string changeScope, string resultFlow)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("입력값과 계산 흐름", EditorStyles.boldLabel);
                DrawFlowLine("입력값 출처", inputSource);
                DrawFlowLine("변경 범위", changeScope);
                DrawFlowLine("결과 반영 방식", resultFlow);
            }
        }

        public static void DrawInterpretation(string message, MessageType type = MessageType.Info)
        {
            EditorGUILayout.HelpBox($"결과 해석: {message}", type);
        }

        public static void DrawFormulaNotice(string text)
        {
            EditorGUILayout.LabelField($"계산 기준: {text}", EditorStyles.miniLabel);
        }

        public static void DrawAutoCheck(string title, bool isRisk, string detail)
        {
            string prefix = isRisk ? "확인 필요" : "정상";
            MessageType type = isRisk ? MessageType.Warning : MessageType.Info;
            EditorGUILayout.HelpBox($"{prefix}: {title} - {detail}", type);
        }

        public static void DrawBaselineHint(bool hasBaseline)
        {
            if (!hasBaseline)
                EditorGUILayout.LabelField("기준값을 저장한 뒤 수치를 바꾸면 변경 전후 차이를 바로 비교할 수 있습니다.", EditorStyles.wordWrappedMiniLabel);
        }

        private static void DrawFlowLine(string title, string body)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(body, EditorStyles.wordWrappedMiniLabel);
            }
        }

        private static void DrawColumn(string title, string body)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.MinWidth(180f)))
            {
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(body, EditorStyles.wordWrappedMiniLabel);
            }
        }
    }
}
