using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingSceneController : MonoBehaviour
{
    [SerializeField] Image _loadingImage;
    [SerializeField] Sprite _gameSceneSprite;

    public static string NextSceneName { get; set; }

    private void Start()
    {
        ApplySceneSprite();

        // Start에서 바로 UniTask 실행
        LoadTargetSceneAsync().Forget();
    }

    private void ApplySceneSprite()
    {
        if (NextSceneName != "GameScene") return;

        _loadingImage.sprite = _gameSceneSprite;
    }

    private async UniTaskVoid LoadTargetSceneAsync()
    {
        var op = SceneManager.LoadSceneAsync(NextSceneName);
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
        {
            await UniTask.Yield();
        }

        op.allowSceneActivation = true;
    }
}
