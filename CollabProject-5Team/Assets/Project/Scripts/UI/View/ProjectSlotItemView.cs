using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// 신규 프로젝트 슬롯 아이템 프리팹 바인딩.
    /// Panel_SlotSelect.SlotScroll Content에 동적 생성.
    /// </summary>
    public sealed class ProjectSlotItemView : MonoBehaviour
    {
        [SerializeField] private Button          _slotButton;
        [SerializeField] private TextMeshProUGUI _slotStateLabel;
        [SerializeField] private TextMeshProUGUI _slotNumberLabel;
        [SerializeField] private GameObject      _lockOverlay;
        [SerializeField] private TextMeshProUGUI _lockConditionLabel;

        public void Setup(int slotIndex, bool isOccupied, bool isLocked, string lockCondition = "")
        {
            _slotNumberLabel.text = $"{slotIndex + 1}";
            _lockOverlay.SetActive(isLocked);
            _slotButton.interactable = !isOccupied && !isLocked;

            if (isLocked)
            {
                _slotStateLabel.text    = "잠금";
                _lockConditionLabel.text = lockCondition;
                return;
            }

            _slotStateLabel.text = isOccupied ? "진행프로젝트에서 확인" : "새로운 프로젝트 설정";
        }
    }
}