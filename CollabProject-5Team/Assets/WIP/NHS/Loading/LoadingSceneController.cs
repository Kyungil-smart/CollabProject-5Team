using Cysharp.Threading.Tasks;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingSceneController : MonoBehaviour
{
    public static string NextSceneName { get; set; }

    [SerializeField] Image progressBar;

    private void Start()
    {
        LoadTargetSceneAsync().Forget();
    }
    private async UniTaskVoid LoadTargetSceneAsync()
    {
        var op = SceneManager.LoadSceneAsync(NextSceneName);
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
        {
            progressBar.fillAmount = op.progress;
            await UniTask.Yield();
        }

        float timer = 0f;
        while (timer < 1.0f)
        {
            timer += Time.unscaledDeltaTime;
            progressBar.fillAmount = Mathf.Lerp(0.9f, 1f, timer);
            await UniTask.Yield();
        }

        op.allowSceneActivation = true;
    }
}
