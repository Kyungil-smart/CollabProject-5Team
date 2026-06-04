using GameDevTycoon.UI;
using TMPro;
using UnityEngine;

namespace Dialogue
{
    /// <summary>
    /// 플레이어 대사 View - 이름 + 텍스트 표시
    /// </summary>
    public class PlayerDialogueView : DialogueBaseView
    {
        [SerializeField] private TextMeshProUGUI _nameText;

        public void Bind(string desc, string text)
        {
            gameObject.SetActive(true);

            if (_nameText != null)
                _nameText.text = desc;

            StartTyping(text);
        }
    }
}