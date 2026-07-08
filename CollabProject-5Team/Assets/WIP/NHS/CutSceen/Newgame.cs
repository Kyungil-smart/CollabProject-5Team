using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Newgame : MonoBehaviour
{
    [System.Serializable]
    public struct CutScenePanelUI
    {
        public GameObject              panel;
        public Image                   image;
        public TextMeshProUGUI characterName;
        public TextMeshProUGUI      dialogue;
        public Button             nextButton;
    }

    [Header("회사 이름 정하기")]
    [SerializeField] private GameObject     _setCompanyPanel;
    [SerializeField] private TMP_InputField _companyInputField;
    [SerializeField] private Button         _companyAcceptButton;

    [Header("플레이어 이름 정하기")]
    [SerializeField] private GameObject     _setPlayerNamePanel;
    [SerializeField] private TMP_InputField _playerNameInputField;
    [SerializeField] private Button         _playerNameAcceptButton;

    [Header("경고 팝업")]
    [SerializeField] private GameObject      _warningPanel;
    [SerializeField] private TextMeshProUGUI _warningText;
    [SerializeField] private Button          _warningCheckButton;

    [Header("스킵")]
    [SerializeField] private Button _skipButton;
    private bool _isFinishedSetPlayerName = false;

    [Header("BGM")]
    [SerializeField] private AudioClip _bgmSetup;
    [SerializeField] private AudioClip _bgmGroup1;
    [SerializeField] private AudioClip _bgmGroup2;
    [SerializeField] private AudioClip _bgmGroup3;

    [Header("컷씬")]
    [SerializeField] private CutScenePanelUI    _cutSceneUI;
    [SerializeField] private List<CutSceneData> _cutSceneList;

    private string _companyName = "미정";
    private string _playerName  = "주인공";
    private int    _currentIdx  = 0;

    private bool      _isTyping            = false;
    private float     _typingSpeed         = 0.025f;
    private string    _currentFullDialogue = "";
    private Coroutine _typingCoroutine;

    private void Start()
    {
        AudioManager.Instance?.PlayBGM(_bgmSetup);

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
            if (currentData.id >= 1000009 && currentData.id < 1000040)
                AudioManager.Instance?.PlayBGM(_bgmGroup2);
            else if (currentData.id > 1000040)
                AudioManager.Instance?.PlayBGM(_bgmGroup3);

            _currentFullDialogue = currentData.dialogue
                .Replace("[Company]", _companyName)
                .Replace("[Player]", _playerName);

            string name = currentData.employeeName
                .Replace("[Company]", _companyName)
                .Replace("[Player]", _playerName);

            _cutSceneUI.characterName.text = name;
            _cutSceneUI.image.sprite = currentData.cutSceenImage;

            _isTyping = true;
            _typingCoroutine = StartCoroutine(TypeText(_currentFullDialogue));

            if (currentData.id == 1000040)
            {
                StopCoroutine(_typingCoroutine);
                _isTyping = false;
                _cutSceneUI.dialogue.text = "";
                OpenPlayerNamePanel();
            }
        }
    }

    private System.Collections.IEnumerator TypeText(string text)
    {
        _cutSceneUI.dialogue.text = "";

        foreach (char letter in text.ToCharArray())
        {
            _cutSceneUI.dialogue.text += letter;
            yield return new WaitForSeconds(_typingSpeed);
        }
        _isTyping = false;
    }

    private void OnNextDialogueClicked()
    {
        if (_isTyping)
        {
            StopCoroutine(_typingCoroutine);
            _cutSceneUI.dialogue.text = _currentFullDialogue;
            _isTyping = false;
        }
        else
        {
            _currentIdx++;
            ShowCutScene();
        }
    }

    private void OpenPlayerNamePanel()
    {
        AudioManager.Instance?.PlayBGM(_bgmSetup);
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
        AudioManager.Instance?.PlayBGM(_bgmGroup1);
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
        if (SceneFlowManager.Instance != null)
            SceneFlowManager.Instance.CompleteCurrentFlow();
    }
}