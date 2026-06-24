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

        // 모든 메니저 초기화 후에 실행할 로직
        BootstrapGameSceneAsync();
    }

    private void BootstrapGameSceneAsync()
    {
        if (SaveLoadSystem.Instance.TryConsumePendingLoad(out int _, out SaveData loadedData))
        {
            SaveLoadSystem.Instance.LoadGame(loadedData);
        }

        GameManager.Instance.InitializeForSaveSystem().Forget();

        switch (DateTimeManager.Instance.currentTime)
        {
            case TimeOfDay.Night:
                DateTimeManager.OnReportEnd?.Invoke();
                break;
            default:
                DateTimeManager.OnDay?.Invoke();
                break;
        }
    }
}
