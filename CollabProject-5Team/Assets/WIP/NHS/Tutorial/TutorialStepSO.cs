using UnityEngine;
using UnityEngine.UI;

namespace Tutorial
{
    [CreateAssetMenu(menuName = "Tutorial/Tutorial Step", fileName = "New Tutorial Step")]
    public class TutorialStepSO : ScriptableObject
    {
        [Header("트리거 조건")]
        public TutorialTriggerType triggerType;
        public int triggerValue;

        [Header("강조할 버튼 (Text Only일 경우 None)")]
        public Button targetButton;

        [Header("대사만 표시")]
        public bool isTextOnly;

        [TextArea(3, 6)]
        public string tutorialText;
    }
}