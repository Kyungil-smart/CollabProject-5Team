using UnityEngine;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// Canvas_NightBackground 담당 View.
    /// 낮 3D 배경을 가리는 밤 전용 2D 배경 이미지 표시만 담당.
    /// </summary>
    public sealed class NightBackgroundView : MonoBehaviour
    {
        [SerializeField] private GameObject _nightBackground;

        public void Show() => _nightBackground.SetActive(true);
        public void Hide() => _nightBackground.SetActive(false);
    }
}