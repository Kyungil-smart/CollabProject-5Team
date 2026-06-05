using TMPro;
using UnityEngine;

/// <summary>
/// NPC 머리 위 말풍선 컴포넌트 (World Space Canvas).
/// NPC의 자식 오브젝트로 배치 후 인스펙터에서 위치 조절.
/// </summary>
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