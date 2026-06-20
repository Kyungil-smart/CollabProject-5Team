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
    [SerializeField] private float duration = 0.5f; 

    [Header("[모드 1] 페이드인/아웃 컴포넌트")]
    [SerializeField] private GameObject  _fadeGroupObj;  
    [SerializeField] private CanvasGroup _fadeCanvasGroup;  

    [Header("[모드 2] 시계 쿨타임 컴포넌트")]
    [SerializeField] private GameObject _clockGroupObj; 
    [SerializeField] private Image      _clockFillImage;     

    private void OnEnable()
    {
        DateTimeManager.OnDateChangedVisual = PlayDateDirectionAsync;
    }

    private void OnDisable()
    {
        if (DateTimeManager.OnDateChangedVisual == PlayDateDirectionAsync)
            DateTimeManager.OnDateChangedVisual = null;
    }

    private void Start()
    {
        HideAllGroups();
    }

    private void HideAllGroups()
    {
        if (_fadeGroupObj  != null)  _fadeGroupObj.SetActive(false);
        if (_clockGroupObj != null) _clockGroupObj.SetActive(false);

        if (_fadeCanvasGroup    != null) _fadeCanvasGroup.alpha = 0f;
        if (_clockFillImage != null) _clockFillImage.fillAmount = 0f;
    }

    private async UniTask PlayDateDirectionAsync()
    {
        HideAllGroups();

        float elapsed = 0f;

        if (visualMode == DirectionMode.FadeInOut)
        {
            if (_fadeGroupObj == null || _fadeCanvasGroup == null) return;

            _fadeGroupObj.SetActive(true);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _fadeCanvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
            _fadeCanvasGroup.alpha = 1f;
        }
        else if (visualMode == DirectionMode.ClockFill)
        {
            if (_clockGroupObj == null || _clockFillImage == null) return;
            _clockGroupObj.SetActive(true);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _clockFillImage.fillAmount = Mathf.Clamp01(elapsed / duration);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
            _clockFillImage.fillAmount = 1f;
        }

        if (DateTimeManager.Instance != null)
        {
            await DateTimeManager.Instance.ProcessDateLogic();
            DateTimeManager.OnDateUIChanged?.Invoke();
        }

        await UniTask.Delay(TimeSpan.FromSeconds(1f));

        elapsed = 0f;

        if (visualMode == DirectionMode.FadeInOut)
        {
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _fadeCanvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / duration));
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
            _fadeCanvasGroup.alpha = 0f;
        }
        else if (visualMode == DirectionMode.ClockFill)
        {
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _clockFillImage.fillAmount = Mathf.Clamp01(1f - (elapsed / duration));
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
            _clockFillImage.fillAmount = 0f;
        }

        HideAllGroups();
    }
}