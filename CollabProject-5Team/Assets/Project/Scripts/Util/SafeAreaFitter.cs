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
            while (_retryCount < MAX_RETRY)
            {
                // 유니티 UI와 프레임 렌더링이 완전히 끝날 때까지 대기
                yield return new WaitForEndOfFrame();

                Rect safeArea = Screen.safeArea;
                float screenWidth = Screen.width;
                float screenHeight = Screen.height;

                // 방어 코드: 간혹 앱 구동 극초기에 해상도가 0 이하인 경우를 대비
                if (screenWidth <= 0 || screenHeight <= 0 || safeArea.width <= 0 || safeArea.height <= 0)
                {
                    _retryCount++;
                    yield return new WaitForSecondsRealtime(0.1f);
                    continue;
                }

                Vector2 anchorMin = safeArea.position;
                Vector2 anchorMax = safeArea.position + safeArea.size;

                anchorMin.x /= screenWidth;
                anchorMin.y /= screenHeight;
                anchorMax.x /= screenWidth;
                anchorMax.y /= screenHeight;

                anchorMin.x = Mathf.Clamp01(anchorMin.x);
                anchorMin.y = Mathf.Clamp01(anchorMin.y);
                anchorMax.x = Mathf.Clamp01(anchorMax.x);
                anchorMax.y = Mathf.Clamp01(anchorMax.y);

                _rect.anchorMin = anchorMin;
                _rect.anchorMax = anchorMax;

                _rect.offsetMin = Vector2.zero;
                _rect.offsetMax = Vector2.zero;

                yield break;
            }

#if UNITY_EDITOR
            Debug.LogWarning("[SafeAreaFitter] SafeArea 적용에 실패했습니다.");
#endif
        }
    }
}