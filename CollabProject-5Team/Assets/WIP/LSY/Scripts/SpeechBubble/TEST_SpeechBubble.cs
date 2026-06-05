using UnityEngine;

/// <summary>
/// SpeechBubble 테스트용 스크립트.
/// 테스트 후 삭제.
/// </summary>
public class TEST_SpeechBubble : MonoBehaviour
{
    [SerializeField] private SpeechBubble _speechBubble;
    [SerializeField] private string       _testMessage = "퀘스트 시작!";

    private GUIStyle _buttonStyle;

    private void OnGUI()
    {
        if (_buttonStyle == null)
        {
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize  = 30,
                fontStyle = FontStyle.Bold,
            };
        }

        GUILayout.BeginArea(new Rect(20, 20, 300, 200));

        if (GUILayout.Button("말풍선 표시", _buttonStyle, GUILayout.Height(80)))
            _speechBubble?.Show(_testMessage);

        GUILayout.Space(10);

        if (GUILayout.Button("말풍선 숨기기", _buttonStyle, GUILayout.Height(80)))
            _speechBubble?.Hide();

        GUILayout.EndArea();
    }
}