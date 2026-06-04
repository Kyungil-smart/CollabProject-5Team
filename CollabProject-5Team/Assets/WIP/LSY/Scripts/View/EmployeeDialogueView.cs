using GameDevTycoon.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dialogue
{
    /// <summary>
    /// 직원 대화 UI View - 초상화 + 이름 + 텍스트 표시
    /// </summary>
    public class EmployeeDialogueView : DialogueBaseView, IBindable<EmployeeDialogueViewData>
    {
        [SerializeField] private Image _portrait;
        [SerializeField] private TextMeshProUGUI _nameText;

        public void Bind(EmployeeDialogueViewData data)
        {
            if (data == null) return;

            gameObject.SetActive(true);

            if (_portrait != null)
            {
                _portrait.sprite = data.portrait;
                _portrait.gameObject.SetActive(data.portrait != null);
            }

            if (_nameText != null)
                _nameText.text = data.desc;

            StartTyping(data.text);
        }
    }
}