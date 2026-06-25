using R3;
using UnityEngine;
using Dialogue;

public class TutorialObserver : MonoBehaviour
{
    private readonly CompositeDisposable _disposables = new();

    private void Start()
    {
        DialogueEvents.OnNodeTypingCompleted.Subscribe(nodeId =>
        {
            Debug.Log($"[Observer] 인게임 일반 대화 완독 감지: {nodeId}");

            if (TutorialManager.Instance != null)
            {
            }
        }).AddTo(_disposables);
    }

    private void OnDestroy()
    {
        _disposables.Dispose();
    }
}