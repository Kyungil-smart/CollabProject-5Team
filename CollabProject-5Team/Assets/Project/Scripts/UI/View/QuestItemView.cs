using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// 퀘스트 목록 아이템 프리팹 View.
    /// Checkbox(완료 여부), QuestLabel(퀘스트명 + 완료 시 취소선 처리) 담당.
    /// </summary>
    public sealed class QuestItemView : MonoBehaviour
    {
        [SerializeField] private Toggle _checkbox;
        [SerializeField] private TextMeshProUGUI _questLabel;

        public void Bind(string questName, bool isCompleted)
        {
            _questLabel.text = isCompleted
                ? $"<s>{questName}</s>"
                : questName;

            // 완료 시 투명도 50%
            _questLabel.alpha = isCompleted ? 0.5f : 1f;

            _checkbox.isOn = isCompleted;
            _checkbox.interactable = false;
        }
    }
}