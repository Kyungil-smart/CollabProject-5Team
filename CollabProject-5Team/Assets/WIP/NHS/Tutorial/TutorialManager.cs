using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class TutorialManager : MonoBehaviour
{
    public enum ShowMode
    {
        TextOnly,
        ButtonActivated,
        HighlightSqure,
        WaitPlayerAction
    }

    [System.Serializable]
    public struct TutorialData
    {
        public string tutorialTextId;

        public ShowMode   showMode;
        public GameObject activateObject;
        public string     tutorialText; 
    }

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI _tutorialText;
    [SerializeField] private GameObject      _tutorialPanel;

    // 튜토리얼 진행
    [SerializeField] private List<TutorialData> _tutorialSteps = new List<TutorialData>();
    private int _curIndex = -1;

    private bool _isWaitingPlayerInput = false;

    private void Start()
    {
        StartTutorial();
    }

    private void Update()
    {
        if(_isWaitingPlayerInput && Input.GetMouseButtonDown(0))
        {
            _isWaitingPlayerInput = false;
            ProceedTutorial();
        }
    }

    private void StartTutorial()
    {
        ProceedTutorial();
    }

    private void ExecuteTutorial()
    {
        _tutorialPanel.SetActive(true);

        // 텍스트만 출력
        if (_tutorialSteps[_curIndex].showMode == ShowMode.TextOnly)
        {
            _tutorialText.text = _tutorialSteps[_curIndex].tutorialText;

            TutorialTextOnly();
        }

        // 버튼 눌러야 넘어가짐
        else if (_tutorialSteps[_curIndex].showMode == ShowMode.ButtonActivated)
        {
            _tutorialText.text = _tutorialSteps[_curIndex].tutorialText;

            TutorialActivateButton();
        }

        // 특정부분 강조 그 부분 터치하면 넘어감
        else if (_tutorialSteps[_curIndex].showMode == ShowMode.HighlightSqure)
        {
        }

        // 플레이어의 행동 기다림 , 특정 행동만 가능하게
        else if (_tutorialSteps[_curIndex].showMode == ShowMode.WaitPlayerAction)
        {
        }
    }

    private void ProceedTutorial()
    {
        _curIndex++;

        if(_curIndex >= _tutorialSteps.Count)
        {
            _tutorialPanel.SetActive(false);
            return;
        }

        ExecuteTutorial();
    }

    private void TutorialTextOnly()
    {
        // 터치 입력
        _isWaitingPlayerInput = true;
    }

    private void TutorialActivateButton()
    {

    }
}