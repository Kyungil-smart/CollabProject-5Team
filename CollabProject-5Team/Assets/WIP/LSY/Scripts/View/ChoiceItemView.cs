using GameDevTycoon.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dialogue
{
    /// <summary>
    /// 선택지 버튼 프리팹 View - 텍스트 + 클릭 이벤트
    /// </summary>
    public class ChoiceItemView : MonoBehaviour, IBindable<ChoiceItemViewData>
    {
        [SerializeField] private Button           _button;
        [SerializeField] private TextMeshProUGUI  _choiceText;

        private ChoiceItemViewData _data;

        private void Awake()
        {
            _button.onClick.AddListener(OnClicked);
        }

        public void Bind(ChoiceItemViewData data)
        {
            _data = data;
            if (_choiceText != null)
                _choiceText.text = data.text;
        }

        private void OnClicked()
        {
            _data?.onSelected?.Invoke(_data.index);
        }
    }
}