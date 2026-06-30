using UnityEditor;
using UnityEngine;

namespace GameDevTycoon.EditorQA
{
    internal static class BalanceGuideUI
    {
        public const string WindowSource = "[창 조절]";
        public const string DataSource = "[SO/테이블]";
        public const string FormulaSource = "[코드 공식]";

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

        public static void DrawSourceLegend()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("수치 출처", EditorStyles.boldLabel, GUILayout.Width(70f));
                EditorGUILayout.LabelField($"{WindowSource} 이 창에서 바로 바꾸는 임시값", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"{DataSource} 직원/프로젝트/보고서 데이터", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"{FormulaSource} 현재 코드 공식으로 계산", EditorStyles.miniLabel);
            }
        }

        public static void DrawInterpretation(string message, MessageType type = MessageType.Info)
        {
            EditorGUILayout.HelpBox($"결과 해석: {message}", type);
        }

        public static string WithSource(string source, string label)
        {
            return $"{source} {label}";
        }

        public static void DrawFormulaNotice(string text)
        {
            EditorGUILayout.LabelField($"{FormulaSource} {text}", EditorStyles.miniLabel);
        }

        public static void DrawImpactMap(string body)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("조절값 영향 범위", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(body, EditorStyles.wordWrappedMiniLabel);
            }
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
