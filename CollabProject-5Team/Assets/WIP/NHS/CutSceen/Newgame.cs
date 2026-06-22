using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

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
            if (currentData.id == 1000000)
            {
                OpenPlayerNamePanel();
                return;
            }

            string msg = currentData.dialogue
                .Replace("[Company]", _companyName)
                .Replace("[Player]", _playerName);

            string name = currentData.employeeName
                .Replace("[Company]", _companyName)
                .Replace("[Player]", _playerName);

            _cutSceneUI.characterName.text = name;
            _cutSceneUI.dialogue.text = msg;
            _cutSceneUI.image.sprite = currentData.cutSceenImage;
        }
    }

    private void OpenPlayerNamePanel()
    {
        _setPlayerNamePanel.SetActive(true);
        
        _cutSceneUI.nextButton.interactable = false; 
    }

    private void OnNextDialogueClicked()
    {
        if (_setPlayerNamePanel.activeSelf) return;

        _currentIdx++;
        ShowCutScene();
    }

    private void OnCompanyConfirmed()
    {
        string input = _companyInputField.text.Trim();
        if (string.IsNullOrWhiteSpace(input)) return;

        _companyName = input;
        _setCompanyPanel.SetActive(false);

        // 컷신 패널을 켜고 첫 컷신 시작
        _cutSceneUI.panel.SetActive(true);
        ShowCutScene();
    }

    private void OnPlayerNameConfirmed()
    {
        string input = _playerNameInputField.text.Trim();
        if (string.IsNullOrWhiteSpace(input)) return;

        _playerName = input;
        _setPlayerNamePanel.SetActive(false);
        
        _cutSceneUI.nextButton.interactable = true;

        _currentIdx++;
        ShowCutScene();
    }

    private void EndCutSceen()
    {
        _cutSceneUI.panel.SetActive(false);
        Debug.Log($"인트로 완료! 회사명: {_companyName}, 플레이어명: {_playerName}");
        // [TODO] 다음 씬 전환 로직 기입 (예: SceneManager.LoadScene)
    }
}