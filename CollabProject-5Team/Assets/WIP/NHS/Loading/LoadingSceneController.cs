using Cysharp.Threading.Tasks;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingSceneController : MonoBehaviour
{
    public static string NextSceneName { get; set; }

    private void Start()
    {
        // Start에서 바로 UniTask 실행
        LoadTargetSceneAsync().Forget();
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
