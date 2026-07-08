using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameDevTycoon.Core
{
    /// <summary>
    /// DontDestroyOnLoad 싱글톤 기반 비동기 씬 전환 서비스.
    /// </summary>
    public sealed class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }

        public bool IsLoading { get; private set; }

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

        /// <summary>
        /// 지정한 씬으로 비동기 전환합니다.
        /// 전환 중 중복 호출은 무시됩니다.
        /// </summary>
        public async UniTask LoadAsync(SceneName sceneName, CancellationToken cancellationToken = default)
        {
            if (IsLoading) return;

            IsLoading = true;

            try
            {
                await SceneManager.LoadSceneAsync(sceneName.ToSceneString())
                    .ToUniTask(cancellationToken: cancellationToken);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Additive 모드로 씬을 추가 로드합니다.
        /// </summary>
        public async UniTask LoadAdditiveAsync(SceneName sceneName, CancellationToken cancellationToken = default)
        {
            await SceneManager.LoadSceneAsync(sceneName.ToSceneString(), LoadSceneMode.Additive)
                .ToUniTask(cancellationToken: cancellationToken);
        }

        public async UniTask UnloadAsync(SceneName sceneName, CancellationToken cancellationToken = default)
        {
            await SceneManager.UnloadSceneAsync(sceneName.ToSceneString())
                .ToUniTask(cancellationToken: cancellationToken);
        }

        // SceneLoader.cs
        public async UniTask LoadGameFlowAsync()
        {
            bool isLoading = SaveLoadSystem.Instance != null && SaveLoadSystem.Instance.pendingLoadSlot.HasValue;

            if (!isLoading)
            {
                await LoadWithLoadingSceneAsync(SceneName.CutScene);
                await SceneFlowManager.Instance.WaitForFlowCompletion();

                await LoadWithLoadingSceneAsync(SceneName.GameScene_TutorialDay);
                await SceneFlowManager.Instance.WaitForFlowCompletion();

                await LoadWithLoadingSceneAsync(SceneName.GameScene_TutorialNight);
                await SceneFlowManager.Instance.WaitForFlowCompletion();
            }

            await LoadWithLoadingSceneAsync(SceneName.Game);
        }

        public async UniTask LoadWithLoadingSceneAsync(SceneName targetScene)
        {
            if (IsLoading) return;
            IsLoading = true;

            LoadingSceneController.NextSceneName = targetScene.ToSceneString();

            await SceneManager.LoadSceneAsync("LoadingScene").ToUniTask();

            IsLoading = false;
        }
    }

    public enum SceneName
    {
        Title,
        Game,
        CutScene,               
        GameScene_TutorialDay,  
        GameScene_TutorialNight
    }

    public static class SceneNameExtensions
    {
        public static string ToSceneString(this SceneName sceneName) => sceneName switch
        {
            SceneName.Title  => "TitleScene",
            SceneName.Game => "GameScene",
            SceneName.CutScene => "CutScene",
            SceneName.GameScene_TutorialDay => "GameScene_TutorialDay",
            SceneName.GameScene_TutorialNight => "GameScene_TutorialNight",
            _                => string.Empty
        };
    }
}
