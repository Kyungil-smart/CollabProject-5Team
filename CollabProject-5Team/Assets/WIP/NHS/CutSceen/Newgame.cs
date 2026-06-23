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
        public GameObject      panel;
        public Image           image;
        public TextMeshProUGUI characterName;
        public TextMeshProUGUI dialogue;
        public Button          nextButton;
    }

    [Header("회사 이름 정하기")]
    [SerializeField] private GameObject     _setCompanyPanel;
    [SerializeField] private TMP_InputField _companyInputField; 
    [SerializeField] private Button         _companyAcceptButton;

    [Header("플레이어 이름 정하기")]
    [SerializeField] private GameObject     _setPlayerNamePanel;
    [SerializeField] private TMP_InputField _playerNameInputField; 
    [SerializeField] private Button         _playerNameAcceptButton;

    [Header("컷씬")]
    [SerializeField] private CutScenePanelUI    _cutSceneUI;
    [SerializeField] private List<CutSceneData> _cutSceneList;

    private string _companyName = "미정";
    private string _playerName  = "주인공";
    private int    _currentIdx  = 0;

    private void Start()
    {
           _companyAcceptButton.onClick.AddListener(OnCompanyConfirmed);
         _cutSceneUI.nextButton.onClick.AddListener(OnNextDialogueClicked);
        _playerNameAcceptButton.onClick.AddListener(OnPlayerNameConfirmed);

           _setCompanyPanel.SetActive(true);
          _cutSceneUI.panel.SetActive(false);
        _setPlayerNamePanel.SetActive(false);
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
            string msg = currentData.dialogue
                .Replace("[Company]", _companyName)
                .Replace("[Player]", _playerName);

            string name = currentData.employeeName
                .Replace("[Company]", _companyName)
                .Replace("[Player]", _playerName);

            _cutSceneUI.characterName.text = name;
            _cutSceneUI.dialogue.text = msg;
            _cutSceneUI.image.sprite = currentData.cutSceenImage;

            if (currentData.id == 1000040)
            {
                OpenPlayerNamePanel();
                return;
            }
        }
    }

    private void OpenPlayerNamePanel()
    {
          _cutSceneUI.panel.SetActive(false);
        _setPlayerNamePanel.SetActive(true);
    }

    private void OnNextDialogueClicked()
    {
        _currentIdx++;
        ShowCutScene();
    }

    private void OnCompanyConfirmed()
    {
        string input = _companyInputField.text.Trim();

        if (!CheckValidName(input))
        {
            _companyInputField.text = ""; // 비워버림
            return;
        }

        _companyName = input;
        Company.Instance.CompanyName = _companyName;

        _setCompanyPanel.SetActive(false);

        // 컷신 패널을 켜고 첫 컷신 시작
        _cutSceneUI.panel.SetActive(true);
        ShowCutScene();
    }

    private void OnPlayerNameConfirmed()
    {
        string input = _playerNameInputField.text.Trim();

        if (!CheckValidName(input))
        {
            _companyInputField.text = ""; // 비워버림
            return;
        }

        _playerName = input;
        Company.Instance.playerName = _playerName;

        _setPlayerNamePanel.SetActive(false);

        _cutSceneUI.panel.SetActive(true);

        _currentIdx++;
        ShowCutScene();
    }

    private bool CheckValidName(string nameToCheck)
    {
        // 1. 빈칸 검사
        if (string.IsNullOrWhiteSpace(nameToCheck))
        {
            Debug.LogWarning("이름이 비어있습니다.");
            return false;
        }

        // 2. 글자 수 제한 (예: 2자 이상 8자 이하)
        if (nameToCheck.Length < 2 || nameToCheck.Length > 8)
        {
            Debug.LogWarning("이름은 2자 이상, 8자 이하로 설정해야 합니다.");
            return false;
        }

        // 3. ㅇㄹㅇㄹㅇㄹ, ㅋㅋㅋ, ㄱㄱㄱ 같은 단순 자음/모음 나열 차단 (정규식)
        // 한글 완성형(가~힣)이나 영어(a-z, A-Z), 숫자(0-9)만 허용하고, 자음/모음만 단독으로 있는 건 튕겨냅니다.
        string pattern = @"^[가-힣a-zA-Z0-9]+$";
        if (!Regex.IsMatch(nameToCheck, pattern))
        {
            Debug.LogWarning("올바르지 않은 문자가 포함되어 있거나, 자음/모음만 입력되었습니다. (예: ㅇㄹㅇㄹ)");
            return false;
        }

        // 4. 메모장(BadWords)에 적어둔 욕설(시발, fuck 등) 검사
        TextAsset badWordsFile = Resources.Load<TextAsset>("BadWords");
        if (badWordsFile != null)
        {
            // 메모장 내용을 줄바꿈 단위로 쪼개서 배열로 만듦
            string[] badWords = badWordsFile.text.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);

            foreach (string word in badWords)
            {
                // 유저가 입력한 이름에 욕설 단어가 '포함'되어 있는지 대소문자 구분 없이 검사
                if (nameToCheck.ToLower().Contains(word.Trim().ToLower()))
                {
                    Debug.LogWarning($"금지어가 포함되어 있습니다: {word}");
                    return false; // 하나라도 걸리면 즉시 컷!
                }
            }
        }

        return true; // 모든 난관을 통과하면 비로소 참(True) 반환!
    }

    private void EndCutSceen()
    {
        _cutSceneUI.panel.SetActive(false);
        Debug.Log($"인트로 완료! 회사명: {_companyName}, 플레이어명: {_playerName}");
        // [TODO] 다음 씬 전환 로직 기입 (예: SceneManager.LoadScene)
    }
}