using TMPro;
using UnityEngine;


public class SpeechBubble : MonoBehaviour
{
    [SerializeField] private GameObject      _bubbleRoot;
    [SerializeField] private TextMeshProUGUI _text;

    private void Awake()
    {
        Hide();
    }

    public void Show(string message = "")
    {
        if (_text != null)
            _text.text = message;

        if (_bubbleRoot != null)
            _bubbleRoot.SetActive(true);
    }

    public void Hide()
    {
        if (_bubbleRoot != null)
            _bubbleRoot.SetActive(false);
    }

    public void SetText(string message)
    {
        if (_text != null)
            _text.text = message;
    }
}