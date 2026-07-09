using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameDevTycoon.Core
{
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

        public async UniTask LoadGameFlowAsync()
        {
            bool isLoading = SaveLoadSystem.Instance != null && SaveLoadSystem.Instance.pendingLoadSlot.HasValue;

            if (!isLoading)
            {
                await LoadWithLoadingSceneAsync(SceneName.CutScene_Start);
                await SceneFlowManager.Instance.WaitForFlowCompletion();

                await LoadWithLoadingSceneAsync(SceneName.GameScene_TutorialDay);
                await SceneFlowManager.Instance.WaitForFlowCompletion();

                await LoadWithLoadingSceneAsync(SceneName.GameScene_TutorialNight);
                await SceneFlowManager.Instance.WaitForFlowCompletion();
            }

            await LoadWithLoadingSceneAsync(SceneName.Game);
        }

        public async UniTask ReturnFromEndingAsync()
        {
            if (SaveLoadSystem.Instance.HasSaveData(SaveLoadSystem.EndingSaveSlot))
            {
                SaveLoadSystem.Instance.SetPendingLoad(SaveLoadSystem.EndingSaveSlot);
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

        public async UniTask ShowOverlayScene(SceneName sceneName)
        {
            Time.timeScale = 0f;
            await LoadAdditiveAsync(sceneName);
        }

        public async UniTask HideOverlayScene(SceneName sceneName)
        {
            await UnloadAsync(sceneName);
            Time.timeScale = 1f;
        }
    }

    public enum SceneName
    {
        Title,
        Game,
        CutScene_Start,               
        CutScene_End,
        GameScene_TutorialDay,  
        GameScene_TutorialNight
    }

    public static class SceneNameExtensions
    {
        public static string ToSceneString(this SceneName sceneName) => sceneName switch
        {
            SceneName.Title  => "TitleScene",
            SceneName.Game => "GameScene",
            SceneName.CutScene_Start => "CutScene_Start",
            SceneName.CutScene_End => "CutScene_End",
            SceneName.GameScene_TutorialDay => "GameScene_TutorialDay",
            SceneName.GameScene_TutorialNight => "GameScene_TutorialNight",
            _                => string.Empty
        };
    }
}
