using Cysharp.Threading.Tasks;
using UnityEngine;

public class SceneFlowManager : MonoBehaviour
{
    public static SceneFlowManager Instance { get; private set; }
    private UniTaskCompletionSource _tcs;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void CompleteCurrentFlow() 
    { 
        _tcs?.TrySetResult();
    }

    public UniTask WaitForFlowCompletion()
    {
        _tcs = new UniTaskCompletionSource();
        return _tcs.Task;
    }
}