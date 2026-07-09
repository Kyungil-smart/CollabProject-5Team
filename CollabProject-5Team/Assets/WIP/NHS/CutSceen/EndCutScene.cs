using Cysharp.Threading.Tasks;
using GameDevTycoon.Core;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndCutScene : MonoBehaviour
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
    
    [Header("스킵")]
    [SerializeField] private Button _skipButton;

    [Header("BGM")]
    [SerializeField] private AudioClip _bgmSetup;
    [SerializeField] private AudioClip _bgmGroup1;
    [SerializeField] private AudioClip _bgmGroup2;
    [SerializeField] private AudioClip _bgmGroup3;

    [Header("컷씬")]
    [SerializeField] private CutScenePanelUI    _cutSceneUI;
    [SerializeField] private List<CutSceneData> _cutSceneList;

    private string _companyName;
    private string _playerName;
    private int    _currentIdx  = 0;

    private bool      _isTyping            = false;
    private float     _typingSpeed         = 0.025f;
    private string    _currentFullDialogue = "";
    private Coroutine _typingCoroutine;

    private void Start()
    {
        _companyName = Company.Instance.CompanyName;
         _playerName = Company.Instance.playerName;

        AudioManager.Instance?.PlayBGM(_bgmSetup);

         _cutSceneUI.nextButton.onClick.AddListener(OnNextDialogueClicked);
                    _skipButton.onClick.AddListener(OnCanSkip);

        ShowCutScene();
    }

    private void ShowCutScene()
    {
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
        }

        if (_cutSceneList == null || _currentIdx >= _cutSceneList.Count)
        {
            return;
        }

        CutSceneData currentData = _cutSceneList[_currentIdx];

        if (currentData != null)
        {
            //if (currentData.id >= 1000009 && currentData.id < 1000040)
            //    AudioManager.Instance?.PlayBGM(_bgmGroup2);
            //else if (currentData.id > 1000040)
            //    AudioManager.Instance?.PlayBGM(_bgmGroup3);

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

            if (_currentIdx >= _cutSceneList.Count)
            {
                TriggerSceneFlowCompletion();
            }
            else
            {
                ShowCutScene();
            }
        }
    }


    private void OnCanSkip()
    {
        TriggerSceneFlowCompletion();
    }

    private void TriggerSceneFlowCompletion()
    {
        if (SceneNameExtensions.ToSceneString(SceneName.CutScene_End) == gameObject.scene.name)
        {
            HandleEndingCutSceneCompletion().Forget();
            return;
        }

        SceneFlowManager.Instance?.CompleteCurrentFlow();
    }

    private async UniTaskVoid HandleEndingCutSceneCompletion()
    {
        AudioManager.Instance?.StopBGM();

        SaveLoadSystem.Instance.SaveGame(SaveLoadSystem.EndingSaveSlot);

        await SceneLoader.Instance.LoadWithLoadingSceneAsync(SceneName.Game);
    }
}