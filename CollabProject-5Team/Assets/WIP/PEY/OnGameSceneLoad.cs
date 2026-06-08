using UnityEngine;
using Cysharp.Threading.Tasks;

public class OnGameSceneLoad : MonoBehaviour
{
    private void Start()
    {
        InvokeAfterStart().Forget();
    }

    private async UniTaskVoid InvokeAfterStart()
    {
        // 다른 스크립트의 Start() 이후 실행되도록 보장
        await UniTask.Yield(PlayerLoopTiming.LastInitialization);

        DateTimeManager.OnDay?.Invoke();
    }
}