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
        else
        {
            // 새 게임 시작시: 컷씬에서 받은 플레이어, 회사 이름을 적용
            if (!string.IsNullOrWhiteSpace(SaveLoadSystem.Instance.pendingCompanyName))
                Company.Instance.CompanyName = SaveLoadSystem.Instance.pendingCompanyName;
            if (!string.IsNullOrWhiteSpace(SaveLoadSystem.Instance.pendingPlayerName))
                Company.Instance.playerName = SaveLoadSystem.Instance.pendingPlayerName;
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
