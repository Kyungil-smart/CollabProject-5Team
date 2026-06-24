using System;
using System.Collections.Generic;
using Dialogue;
using UnityEngine;


public class StoryDialoguePlayer : MonoBehaviour
{
    public static StoryDialoguePlayer Instance { get; private set; }

    [SerializeField] private PlayerDialogueView _playerView;
    [SerializeField] private EmployeeDialogueView _employeeView;

    private DialogueBaseView _currentView;
    private Action _onComplete;
    private Dictionary<string, Employee> _speakerEmployees; // NPC1/NPC2/SPY/UCSPY -> 실제 배정된 직원

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Init() => Instance = null;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <param name="speakerEmployees">"NPC1"/"NPC2"/"SPY"/"UCSPY" 토큰과 실제 배정된 직원 매핑</param>
    public void StartStoryDialogue(int startNodeId, Dictionary<string, Employee> speakerEmployees, Action onComplete)
    {
        _speakerEmployees = speakerEmployees ?? new Dictionary<string, Employee>();
        _onComplete       = onComplete;
        ShowNode(startNodeId);
    }

    private void ShowNode(int nodeId)
    {
        if (nodeId == 0) { EndDialogue(); return; }

        StoryQuestNodeSO node = StoryQuestDataManager.Instance.GetNode(nodeId);
        if (node == null) { EndDialogue(); return; }

        if (_currentView != null)
        {
            _currentView.OnTypingComplete = null;
            _currentView.OnNextAction     = null;
        }

        string resolvedText = StoryTextResolver.Resolve(node.text, BuildNameMap());

        if (node.isUser)
        {
            _currentView = _playerView;
            _playerView.OnNextAction = () => ShowNode(node.nextId);
            _playerView.Bind(Company.Instance.playerName, resolvedText);
        }
        else
        {
            _currentView = _employeeView;
            _employeeView.OnNextAction = () => ShowNode(node.nextId);
            _employeeView.Bind(new EmployeeDialogueViewData
            {
                desc     = node.isBlank ? "" : ResolveSpeakerName(node.speaker),
                text     = resolvedText,
                portrait = node.isBlank ? null : ResolvePortrait(node.speaker),
            });
        }
    }

    private Dictionary<string, string> BuildNameMap()
    {
        var map = new Dictionary<string, string>();
        foreach (var kv in _speakerEmployees)
            map[kv.Key] = kv.Value != null ? kv.Value.so.Name : kv.Key;
        return map;
    }

    private string ResolveSpeakerName(string speakerToken)
    {
        if (_speakerEmployees.TryGetValue(speakerToken, out Employee emp) && emp != null)
            return emp.so.Name;
        return speakerToken;
    }

    private Sprite ResolvePortrait(string speakerToken)
    {
        if (_speakerEmployees.TryGetValue(speakerToken, out Employee emp) && emp != null)
            return emp.so.iconNormal;
        return null;
    }

    private void EndDialogue()
    {
        if (_currentView != null)
        {
            _currentView.OnTypingComplete = null;
            _currentView.OnNextAction     = null;
        }
        _currentView = null;

        if (_playerView   != null) _playerView.gameObject.SetActive(false);
        if (_employeeView != null) _employeeView.gameObject.SetActive(false);

        Action callback = _onComplete;
        _onComplete = null;
        callback?.Invoke();
    }
}