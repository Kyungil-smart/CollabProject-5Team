using UnityEngine;

namespace GameDevTycoon.UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rect;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            Refresh();
        }

        private void Refresh()
        {
            // 세로 모드 고정 게임이므로 초기화 시점에 딱 한 번만 안전 영역을 적용
            Apply(Screen.safeArea);
        }

        private void Apply(Rect safeArea)
        {
            // 1. 물리 픽셀 해상도(Screen)를 기준으로 철저하게 0 ~ 1 사이의 비율만 계산
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            // 방어 코드: 0으로 나누기 방지
            if (screenWidth <= 0 || screenHeight <= 0) return;

            var anchorMin = safeArea.position;
            var anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= screenWidth;
            anchorMin.y /= screenHeight;
            anchorMax.x /= screenWidth;
            anchorMax.y /= screenHeight;

            // 2. 계산된 비율을 앵커에 대입 (Canvas Scaler 비율과 무관하게 영역이 잡힙니다)
            _rect.anchorMin = anchorMin;
            _rect.anchorMax = anchorMax;

            // 3. 핵심: 앵커가 변경되면서 생성된 마진(Offset)을 제로로 만듬
            // Canvas Scaler 환경에서 이 처리가 없으면 UI 레이아웃이 완전히 틀어집니다.
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}