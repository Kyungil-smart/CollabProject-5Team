using UnityEngine;

namespace GameDevTycoon.UI
{
    /// <summary>
    /// Screen.safeArea를 RectTransform에 적용해 노치/홈바 영역을 회피.
    /// Canvas 루트 바로 아래 SafeArea GO에 부착.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rect;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            Apply(Screen.safeArea);
        }

        private void Apply(Rect safeArea)
        {
            var anchorMin = safeArea.position;
            var anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            _rect.anchorMin = anchorMin;
            _rect.anchorMax = anchorMax;
        }
    }
}