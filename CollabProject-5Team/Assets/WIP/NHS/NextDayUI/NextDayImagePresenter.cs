using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

public class NextDayImagePresenter : MonoBehaviour
{
    [SerializeField] private GameObject directionPanel;  // "다음날" 이미지 등이 포함된 UI 패널
    [SerializeField] private CanvasGroup canvasGroup;    // 페이드 인/아웃용 컴포넌트

    [Header("시간 조절")]
    [SerializeField, Min(0f)] float fadeDurationSeconds = 0.5f;
    [SerializeField, Min(0f)] float visibleDurationSeconds = 1f;

    private void OnEnable()
    {
        DateTimeManager.OnDateChangedVisual = PlayDateDirectionAsync;
    }

    private void OnDisable()
    {
        if (DateTimeManager.OnDateChangedVisual == PlayDateDirectionAsync)
        {
            DateTimeManager.OnDateChangedVisual = null;
        }
    }

    private void Start()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        if (directionPanel != null)
            directionPanel.SetActive(false);
    }

    private async UniTask PlayDateDirectionAsync()
    {
        if (directionPanel == null || canvasGroup == null) return;

        canvasGroup.alpha = 0f;
        directionPanel.SetActive(true);

        float duration = Mathf.Max(0f, fadeDurationSeconds);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
        canvasGroup.alpha = 1f;

        await UniTask.Delay(TimeSpan.FromSeconds(Mathf.Max(0f, visibleDurationSeconds)));

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / duration));
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
        canvasGroup.alpha = 0f;

        directionPanel.SetActive(false);
    }
}
