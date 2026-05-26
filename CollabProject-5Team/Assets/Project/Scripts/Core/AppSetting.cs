using UnityEngine;

// 모바일 앱을 위한 기본 설정
public static class AppSetting
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] // 자동시작
    static void Init()
    {
        // 렌더링
        QualitySettings.vSyncCount  = 0;
        Application.targetFrameRate = 60;

        // 절전
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        // 백그라운드 진입 시 일시정지
        Application.runInBackground = false;
    }
}
