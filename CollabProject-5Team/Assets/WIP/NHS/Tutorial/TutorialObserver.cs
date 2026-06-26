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