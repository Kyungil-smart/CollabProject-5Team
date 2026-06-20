using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.UI;

public class NextDayImagePresenter : MonoBehaviour
{
    public enum DirectionMode
    {
        FadeInOut,
        ClockFill
    }

    [Header("연출 모드 선택")]
    [SerializeField] private DirectionMode visualMode = DirectionMode.FadeInOut;
    [SerializeField] private float duration = 0.5f; // 연출 속도

    [Header("UI 컴포넌트")]
    [SerializeField] private GameObject  _directionPanel;  
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Image       _clockFillImage;

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
        Init();
    }

    private void Init()
    {
        if (_canvasGroup != null)
            _canvasGroup.alpha = (visualMode == DirectionMode.FadeInOut) ? 0f : 1f;

        if (_directionPanel != null)
            _directionPanel.SetActive(false);

        if (_clockFillImage != null)
            _clockFillImage.fillAmount = 0f;
    }

    private async UniTask PlayDateDirectionAsync()
    {
        if (_directionPanel == null || _canvasGroup == null) return;

        Init();
        _directionPanel.SetActive(true);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);

            if (visualMode == DirectionMode.FadeInOut && _canvasGroup != null)
            {
                _canvasGroup.alpha = progress;
            }
            else if (visualMode == DirectionMode.ClockFill && _clockFillImage != null)
            {
                _clockFillImage.fillAmount = progress;
            }

            await UniTask.Yield(PlayerLoopTiming.Update);
        }

        if (visualMode == DirectionMode.FadeInOut && _canvasGroup != null) 
            _canvasGroup.alpha = 1f;
        if (visualMode == DirectionMode.ClockFill && _clockFillImage != null) 
            _clockFillImage.fillAmount = 1f;

        if (DateTimeManager.Instance != null)
        {
            await DateTimeManager.Instance.ProcessDateLogic();
            DateTimeManager.OnDateUIChanged?.Invoke();
        }

        await UniTask.Delay(TimeSpan.FromSeconds(1f));

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);

            if (visualMode == DirectionMode.FadeInOut && _canvasGroup != null)
            {
                _canvasGroup.alpha = 1f - progress;
            }
            else if (visualMode == DirectionMode.ClockFill && _clockFillImage != null)
            {
                Color color = _clockFillImage.color;
                color.a = 1f - progress;
                _clockFillImage.color = color;
            }

            await UniTask.Yield(PlayerLoopTiming.Update);
        }

        if (visualMode == DirectionMode.ClockFill && _clockFillImage != null)
        {
            Color color = _clockFillImage.color;
            color.a = 1f;
            _clockFillImage.color = color;
            _clockFillImage.fillAmount = 0f;
        }

        if (_canvasGroup != null) _canvasGroup.alpha = 0f;
        _directionPanel.SetActive(false);
    }
}