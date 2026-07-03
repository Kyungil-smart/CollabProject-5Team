using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class Newgame : MonoBehaviour
{
    [System.Serializable]
    public struct CutScenePanelUI
    {
        public GameObject panel;
        public Image image;
        public TextMeshProUGUI characterName;
        public TextMeshProUGUI dialogue;
        public Button nextButton;
    }

    [Header("회사 이름 정하기")]
    [SerializeField] private GameObject _setCompanyPanel;
    [SerializeField] private TMP_InputField _companyInputField;
    [SerializeField] private Button _companyAcceptButton;

    [Header("플레이어 이름 정하기")]
    [SerializeField] private GameObject _setPlayerNamePanel;
    [SerializeField] private TMP_InputField _playerNameInputField;
    [SerializeField] private Button _playerNameAcceptButton;

    [Header("경고 팝업")]
    [SerializeField] private GameObject _warningPanel;
    [SerializeField] private Button _warningCheckButton;
    [SerializeField] private TextMeshProUGUI _warningText;

    [Header("스킵")]
    [SerializeField] private Button _skipButton;
    private bool _isFinishedSetPlayerName = false;

    [Header("컷씬")]
    [SerializeField] private CutScenePanelUI _cutSceneUI;
    [SerializeField] private List<CutSceneData> _cutSceneList;

    [Header("사운드 타이핑 컴포넌트")]
    [SerializeField] private TextSoundTweener _textTweener; // 분리한 독립 컴포넌트

    private string _companyName = "미정";
    private string _playerName = "주인공";
    private int _currentIdx = 0;

    private float _typingSpeed = 0.5f;
    private string _currentFullDialogue = "";

    private void Start()
    {
        _companyAcceptButton.onClick.AddListener(OnCompanyConfirmed);
        _cutSceneUI.nextButton.onClick.AddListener(OnNextDialogueClicked);
        _playerNameAcceptButton.onClick.AddListener(OnPlayerNameConfirmed);
        _skipButton.onClick.AddListener(OnCanSkip);

        _setCompanyPanel.SetActive(true);
        _cutSceneUI.panel.SetActive(false);
        _setPlayerNamePanel.SetActive(false);
        _warningPanel.SetActive(false);
    }

    private void ShowCutScene()
    {
        if (_cutSceneList == null || _currentIdx >= _cutSceneList.Count)
        {
            EndCutSceen();
            return;
        }

        CutSceneData currentData = _cutSceneList[_currentIdx];

        if (currentData != null)
        {
            // 새로운 대사 시작 전 이전 타이핑 연출 안전하게 종료
            _textTweener.KillActiveTween();

            _currentFullDialogue = currentData.dialogue
                .Replace("[Company]", _companyName)
                .Replace("[Player]", _playerName);

            string name = currentData.employeeName
                .Replace("[Company]", _companyName)
                .Replace("[Player]", _playerName);

            _cutSceneUI.characterName.text = name;
            _cutSceneUI.image.sprite = currentData.cutSceenImage;

            if (currentData.id == 1000040)
            {
                _cutSceneUI.dialogue.text = "";
                OpenPlayerNamePanel();
                return;
            }

            // 독립 컴포넌트에 텍스트와 대사를 넘겨 동숲 사운드 타이핑 시작
            _textTweener.DoType(_cutSceneUI.dialogue, _currentFullDialogue, _typingSpeed);
        }
    }

    private void OnNextDialogueClicked()
    {
        // 글자가 찍히는 중이었다면 클릭 시 즉시 전체 텍스트 출력 (사운드 중지)
        if (_textTweener.CompleteActiveTween())
        {
            return;
        }

        _currentIdx++;
        ShowCutScene();
    }

    private void OpenPlayerNamePanel()
    {
        _cutSceneUI.panel.SetActive(false);
        _setPlayerNamePanel.SetActive(true);
    }

    private void OnCompanyConfirmed()
    {
        string input = _companyInputField.text.Trim();

        if (!CheckValidName(input))
        {
            _companyInputField.text = "";
            return;
        }

        _companyName = input;
        Company.Instance.CompanyName = _companyName;

        _setCompanyPanel.SetActive(false);
        _cutSceneUI.panel.SetActive(true);
        ShowCutScene();
    }

    private void OnPlayerNameConfirmed()
    {
        string input = _playerNameInputField.text.Trim();

        if (!CheckValidName(input))
        {
            _playerNameInputField.text = "";
            return;
        }

        _playerName = input;
        Company.Instance.playerName = _playerName;

        _isFinishedSetPlayerName = true;

        _setPlayerNamePanel.SetActive(false);
        _cutSceneUI.panel.SetActive(true);

        _currentIdx++;
        ShowCutScene();
    }

    private void OnCanSkip()
    {
        // 🐱 변형 유도 버그 수정: 과거 직접 쓰던 _typingTween 제어 로직을 지우고
        // 독립 컴포넌트인 _textTweener를 멈추도록 일원화했습니다.
        _textTweener.KillActiveTween();

        if (!_isFinishedSetPlayerName)
        {
            int targetIdx = _cutSceneList.FindIndex(data => data != null && data.id == 1000040);

            if (targetIdx != -1)
            {
                _currentIdx = targetIdx;
                ShowCutScene();
            }
            else
            {
                _setCompanyPanel.SetActive(false);
                OpenPlayerNamePanel();
            }
        }
        else
        {
            EndCutSceen();
        }
    }

    private bool CheckValidName(string nameToCheck)
    {
        if (string.IsNullOrWhiteSpace(nameToCheck))
        {
            _warningPanel.SetActive(true);
            _warningText.text = "이름이 비어있어요!";
            Debug.LogWarning("이름이 비어있습니다.");
            return false;
        }

        if (nameToCheck.Length < 2 || nameToCheck.Length > 8)
        {
            _warningPanel.SetActive(true);
            _warningText.text = "글자 수를 맞춰 주세요!";
            Debug.LogWarning("이름은 2자 이상, 8자 이하로 설정해야 합니다.");
            return false;
        }

        string pattern = @"^[가-힣a-zA-Z0-9]+$";
        if (!Regex.IsMatch(nameToCheck, pattern))
        {
            _warningPanel.SetActive(true);
            _warningText.text = "올바르지 않은 문자 방식이예요!";
            Debug.LogWarning("올바르지 않은 문자가 포함되어 있거나, 자음/모음만 입력되었습니다. (예: ㅇㄹㅇㄹ)");
            return false;
        }

        TextAsset badWordsFile = Resources.Load<TextAsset>("BadWords");
        if (badWordsFile != null)
        {
            string[] badWords = badWordsFile.text.Split(new[] { "\n" }, System.StringSplitOptions.RemoveEmptyEntries);

            foreach (string word in badWords)
            {
                if (nameToCheck.ToLower().Contains(word.Trim().ToLower()))
                {
                    _warningPanel.SetActive(true);
                    _warningText.text = "나쁜말은 안되요~";
                    Debug.LogWarning($"금지어가 포함되어 있습니다: {word}");
                    return false;
                }
            }
        }

        return true;
    }

    private void EndCutSceen()
    {
        _cutSceneUI.panel.SetActive(false);
        Debug.Log($"인트로 완료! 회사명: {_companyName}, 플레이어명: {_playerName}");
    }
}