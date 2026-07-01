using System.Collections;
using UnityEngine;

namespace GameDevTycoon.UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rect;
        private int _retryCount = 0;
        private const int MAX_RETRY = 5; // 혹시 모를 무한 루프 방지

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
        }

        private void Start()
        {
            // Start 시점에 코루틴을 돌려 유니티 엔진이 화면 정렬을 끝내도록 유도
            StartCoroutine(CoApplySafeArea());
        }

        private IEnumerator CoApplySafeArea()
        {
            // 1. 유니티 UI와 프레임 렌더링이 완전히 끝날 때까지 대기 (가장 안전한 타이밍)
            yield return new WaitForEndOfFrame();

            Rect safeArea = Screen.safeArea;
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            // 2. 방어 코드: 간혹 앱 구동 극초기에 해상도가 0 이하이거나, 
            // safeArea가 전체 화면과 완전히 똑같이(오류로 인해) 잡히는 경우를 대비
            if (screenWidth <= 0 || screenHeight <= 0 || safeArea.width <= 0)
            {
                if (_retryCount < MAX_RETRY)
                {
                    _retryCount++;
                    yield return new WaitForSecondsRealtime(0.1f); // 0.1초 쉬고 재시도
                    StartCoroutine(CoApplySafeArea());
                }
                yield break;
            }

            // 3. 비율 계산
            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= screenWidth;
            anchorMin.y /= screenHeight;
            anchorMax.x /= screenWidth;
            anchorMax.y /= screenHeight;

            // 4. 패딩 적용 (값이 정상적인 범위 일 때만 적용)
            if (anchorMin.x >= 0 && anchorMax.x <= 1 && anchorMin.y >= 0 && anchorMax.y <= 1)
            {
                _rect.anchorMin = anchorMin;
                _rect.anchorMax = anchorMax;

                _rect.offsetMin = Vector2.zero;
                _rect.offsetMax = Vector2.zero;
            }
        }
    }
}