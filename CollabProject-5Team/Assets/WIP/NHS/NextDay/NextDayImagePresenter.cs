using Cysharp.Threading.Tasks;
using NUnit.Framework;
using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class NextDayImagePresenter : MonoBehaviour
{
    public enum DirectionMode
    {
        FadeInOut,
        ClockFill
    }

    public struct Week
    {
        public DayOfWeek dayOfWeek;
        public List<string> randomMessage;
    }


    [Header("연출 모드 선택")]
    [SerializeField] private DirectionMode visualMode = DirectionMode.FadeInOut;

    [Header("[모드 1] 페이드인/아웃 컴포넌트")]
    [SerializeField] private GameObject  _fadeGroupObj;
    [SerializeField] private CanvasGroup _fadeCanvasGroup;

    [Header("[모드 2] 시계 쿨타임 컴포넌트")]
    [SerializeField] private GameObject _clockGroupObj;
    [SerializeField] private Image      _clockFillImage;

    [Header("낮 잠 전환 컴포넌트")]
    [SerializeField] private GameObject _nightGroupObj;
    [SerializeField] private Image      _nightFillImage;

    [Header("화면 전환 후 왼쪽에서 나타날 요일 UI")]
    [SerializeField] private GameObject    _slidePopupGroupObj;
    [SerializeField] private RectTransform _slidePopupRect;
    [SerializeField] private CanvasGroup   _slidePopupCanvasGroup; 

    [Header("시간 조절 (화면 가림용)")]
    [SerializeField, Min(0f)] float    fadeDurationSeconds = 0.5f;
    [SerializeField, Min(0f)] float visibleDurationSeconds = 1f;

    [Header("시간 조절 (왼쪽 슬라이드 UI)")]
    [SerializeField] private float slideDuration        = 0.4f; // 들어오고 나가는 이동 시간
    [SerializeField] private float slideVisibleDuration = 1.5f; // 화면에 머무르는 시간
    [SerializeField] private float startXPosition       = -250; // 시작 위치 (화면 왼쪽 밖 X 좌표)
    [SerializeField] private float targetXPosition      = +250; // 도달 위치 (화면 안쪽 X 좌표)

    [SerializeField] List<>

    private void OnEnable()
    {
        DateTimeManager.OnDateChangedVisual = PlayDateDirectionAsync;
        DateTimeManager.OnTimeChangedVisual = PlayNightDirectionAsync;
    }

    private void OnDisable()
    {
        if (DateTimeManager.OnDateChangedVisual == PlayDateDirectionAsync)
            DateTimeManager.OnDateChangedVisual = null;

        if (DateTimeManager.OnTimeChangedVisual == PlayNightDirectionAsync)
            DateTimeManager.OnTimeChangedVisual = null;
    }

    private void Start()
    {
        HideAllGroups();
    }

    private void HideAllGroups()
    {
        if ( _fadeGroupObj != null)  _fadeGroupObj.SetActive(false);
        if (_clockGroupObj != null) _clockGroupObj.SetActive(false);
        if (_nightGroupObj != null) _nightGroupObj.SetActive(false);

        if (_slidePopupGroupObj != null) _slidePopupGroupObj.SetActive(false);

        if (_fadeCanvasGroup != null) _fadeCanvasGroup.alpha = 0f;
        if ( _clockFillImage != null) _clockFillImage.fillAmount = 0f;
        if ( _nightFillImage != null) _nightFillImage.fillAmount = 0f;

        if (_slidePopupCanvasGroup != null) _slidePopupCanvasGroup.alpha = 0f;
    }

    private async UniTask PlayNightDirectionAsync()
    {
        HideAllGroups();

        float duration = Mathf.Max(0f, fadeDurationSeconds);
        float elapsed = 0f;

        if (_nightGroupObj == null || _nightFillImage == null) return;
        _nightGroupObj.SetActive(true);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _nightFillImage.fillAmount = Mathf.Clamp01(elapsed / duration);
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
        _nightFillImage.fillAmount = 1f;

        if (DateTimeManager.Instance != null)
        {
            await DateTimeManager.Instance.ProcessDateLogic();
            DateTimeManager.OnDateUIChanged?.Invoke();
        }

        await UniTask.Delay(TimeSpan.FromSeconds(Mathf.Max(0f, visibleDurationSeconds)));

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _nightFillImage.fillAmount = Mathf.Clamp01(1f - (elapsed / duration));
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
        _nightFillImage.fillAmount = 0f;
        _nightGroupObj.SetActive(false);

        await PlayLeftSlideAnimationAsync();

        HideAllGroups();
    }

    private async UniTask PlayDateDirectionAsync()
    {
        HideAllGroups();

        float duration = Mathf.Max(0f, fadeDurationSeconds);
        float elapsed = 0f;

        // 1. 화면 가리기 연출
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

        // 2. 데이터 및 텍스트 미리 세팅
        if (DateTimeManager.Instance != null)
        {
            await DateTimeManager.Instance.ProcessDateLogic();
            DateTimeManager.OnDateUIChanged?.Invoke();
        }

        await UniTask.Delay(TimeSpan.FromSeconds(Mathf.Max(0f, visibleDurationSeconds)));

        // 3. 화면 다시 원래대로 돌리기
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
            _fadeGroupObj.SetActive(false);
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
            _clockGroupObj.SetActive(false);
        }

        // 4. 화면이 완전히 원래대로 돌아오면 왼쪽 슬라이드 UI 연출 시작
        await PlayLeftSlideAnimationAsync();

        HideAllGroups();
    }

    private async UniTask PlayLeftSlideAnimationAsync()
    {
        if (_slidePopupGroupObj == null || _slidePopupRect == null) return;

        Vector2 anchoredPos = _slidePopupRect.anchoredPosition;
        _slidePopupRect.anchoredPosition = new Vector2(startXPosition, anchoredPos.y);
        if (_slidePopupCanvasGroup != null) _slidePopupCanvasGroup.alpha = 0f;

        _slidePopupGroupObj.SetActive(true);

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);

            float currentX = Mathf.Lerp(startXPosition, targetXPosition, t);
            _slidePopupRect.anchoredPosition = new Vector2(currentX, anchoredPos.y);

            if (_slidePopupCanvasGroup != null) _slidePopupCanvasGroup.alpha = t;

            await UniTask.Yield(PlayerLoopTiming.Update);
        }
        _slidePopupRect.anchoredPosition = new Vector2(targetXPosition, anchoredPos.y);
        if (_slidePopupCanvasGroup != null) _slidePopupCanvasGroup.alpha = 1f;

        await UniTask.Delay(TimeSpan.FromSeconds(Mathf.Max(0f, slideVisibleDuration)));

        elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);

            float currentX = Mathf.Lerp(targetXPosition, startXPosition, t);
            _slidePopupRect.anchoredPosition = new Vector2(currentX, anchoredPos.y);

            if (_slidePopupCanvasGroup != null) _slidePopupCanvasGroup.alpha = 1f - t;

            await UniTask.Yield(PlayerLoopTiming.Update);
        }
        _slidePopupRect.anchoredPosition = new Vector2(startXPosition, anchoredPos.y);
        if (_slidePopupCanvasGroup != null) _slidePopupCanvasGroup.alpha = 0f;
    }
}