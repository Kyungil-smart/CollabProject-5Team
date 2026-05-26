using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// DoTween 무료 버전 기준 TMP Pro 전용 기능을 대체 구현한 확장 메서드 모음.
/// 각 메서드는 Tween 또는 Sequence를 반환하므로 체이닝 및 Sequence 삽입이 가능합니다.
/// </summary>
public static class TMPDoTweenExtensions
{
    /// <summary>
    /// 타이핑 효과. maxVisibleCharacters를 0에서 전체 길이까지 순차 증가시킵니다.
    /// </summary>
    /// <param name="text">출력할 전체 텍스트</param>
    /// <param name="duration">전체 타이핑 완료까지 걸리는 시간</param>
    /// <param name="onCharAppear">글자가 한 글자씩 나타날 때마다 호출. 현재 글자 인덱스를 전달합니다.</param>
    public static Tween DOTypewriter(
        this TextMeshProUGUI tmp,
        string text,
        float duration,
        Action<int> onCharAppear = null)
    {
        tmp.text = text;
        tmp.maxVisibleCharacters = 0;
        tmp.ForceMeshUpdate();

        int total = tmp.textInfo.characterCount;

        return DOTween.To(
            () => tmp.maxVisibleCharacters,
            x =>
            {
                int prev = tmp.maxVisibleCharacters;
                tmp.maxVisibleCharacters = x;
                if (x > prev) onCharAppear?.Invoke(x - 1);
            },
            total,
            duration
        ).SetEase(Ease.Linear);
    }

    /// <summary>
    /// 텍스트를 즉시 교체한 뒤 알파 0에서 1로 페이드 인합니다.
    /// </summary>
    /// <param name="newText">교체할 텍스트</param>
    /// <param name="duration">페이드 인 시간</param>
    public static Tween DOFadeInText(
        this TextMeshProUGUI tmp,
        string newText,
        float duration)
    {
        tmp.text = newText;
        tmp.alpha = 0f;
        return tmp.DOFade(1f, duration);
    }

    /// <summary>
    /// 텍스트를 페이드 아웃한 뒤 교체하고 페이드 인합니다.
    /// 대화창 텍스트 전환처럼 부드러운 교체가 필요할 때 사용합니다.
    /// </summary>
    /// <param name="newText">교체할 텍스트</param>
    /// <param name="halfDuration">페이드 아웃 / 페이드 인 각각의 시간</param>
    public static Sequence DOCrossFadeText(
        this TextMeshProUGUI tmp,
        string newText,
        float halfDuration)
    {
        return DOTween.Sequence()
            .Append(tmp.DOFade(0f, halfDuration))
            .AppendCallback(() => tmp.text = newText)
            .Append(tmp.DOFade(1f, halfDuration));
    }

    /// <summary>
    /// 텍스트 색상을 목표 색상으로 트윈합니다.
    /// TMP Pro 전용인 DOColor를 무료 버전에서 대체합니다.
    /// </summary>
    /// <param name="target">목표 색상</param>
    /// <param name="duration">트윈 시간</param>
    public static Tween DOColorText(
        this TextMeshProUGUI tmp,
        Color target,
        float duration)
    {
        return DOTween.To(
            () => tmp.color,
            c => tmp.color = c,
            target,
            duration
        );
    }

    /// <summary>
    /// 텍스트를 지정 색상으로 깜빡이게 합니다.
    /// 피로도 위험 수치, 경고 메시지 등 주의를 끌어야 할 때 사용합니다.
    /// </summary>
    /// <param name="flashColor">깜빡일 색상</param>
    /// <param name="halfDuration">깜빡임 한 사이클의 절반 시간</param>
    /// <param name="cycles">반복 횟수. -1이면 무한 반복</param>
    public static Sequence DOFlashColor(
        this TextMeshProUGUI tmp,
        Color flashColor,
        float halfDuration,
        int cycles = 2)
    {
        Color original = tmp.color;
        return DOTween.Sequence()
            .Append(tmp.DOColorText(flashColor, halfDuration))
            .Append(tmp.DOColorText(original, halfDuration))
            .SetLoops(cycles);
    }

    /// <summary>
    /// fontSize를 목표 크기로 트윈합니다.
    /// enableAutoSizing이 활성화된 경우 fontSize 직접 제어가 무시되므로 주의합니다.
    /// </summary>
    /// <param name="targetSize">목표 폰트 크기</param>
    /// <param name="duration">트윈 시간</param>
    public static Tween DOFontSize(
        this TextMeshProUGUI tmp,
        float targetSize,
        float duration)
    {
        return DOTween.To(
            () => tmp.fontSize,
            s => tmp.fontSize = s,
            targetSize,
            duration
        );
    }

    /// <summary>
    /// fontSize를 순간 키웠다가 원래 크기로 복귀하는 강조 펄스입니다.
    /// 수치 변동, 보상 획득 등 중요한 수치 변화 시점에 사용합니다.
    /// </summary>
    /// <param name="scaleFactor">최대 확대 비율. 기본값 1.2f = 120%</param>
    /// <param name="halfDuration">확대 / 복귀 각각의 시간</param>
    public static Sequence DOPulseSize(
        this TextMeshProUGUI tmp,
        float scaleFactor = 1.2f,
        float halfDuration = 0.1f)
    {
        float original = tmp.fontSize;
        return DOTween.Sequence()
            .Append(tmp.DOFontSize(original * scaleFactor, halfDuration).SetEase(Ease.OutQuad))
            .Append(tmp.DOFontSize(original, halfDuration).SetEase(Ease.InQuad));
    }

    /// <summary>
    /// RectTransform을 흔드는 Shake 효과입니다.
    /// DoTween 무료 버전은 TMP 직접 Shake를 지원하지 않으므로 rectTransform에 적용합니다.
    /// </summary>
    /// <param name="duration">흔들림 지속 시간</param>
    /// <param name="strength">흔들림 강도</param>
    /// <param name="vibrato">흔들림 진동 횟수</param>
    public static Tween DOShakeText(
        this TextMeshProUGUI tmp,
        float duration = 0.3f,
        float strength = 10f,
        int vibrato = 20)
    {
        return tmp.rectTransform.DOShakeAnchorPos(duration, strength, vibrato, 90f, false);
    }

    /// <summary>
    /// 알파를 0에서 1로 페이드 인합니다.
    /// </summary>
    /// <param name="duration">페이드 인 시간</param>
    public static Tween DOShow(this TextMeshProUGUI tmp, float duration)
    {
        tmp.alpha = 0f;
        return tmp.DOFade(1f, duration);
    }

    /// <summary>
    /// 알파를 1에서 0으로 페이드 아웃합니다.
    /// </summary>
    /// <param name="duration">페이드 아웃 시간</param>
    /// <param name="deactivateOnComplete">true이면 완료 후 GameObject를 비활성화합니다.</param>
    public static Tween DOHide(
        this TextMeshProUGUI tmp,
        float duration,
        bool deactivateOnComplete = false)
    {
        Tween t = tmp.DOFade(0f, duration);
        if (deactivateOnComplete)
            t.OnComplete(() => tmp.gameObject.SetActive(false));
        return t;
    }

    /// <summary>
    /// 정수를 from에서 to까지 카운팅하며 텍스트로 표시합니다.
    /// 자금, 점수 등 정수 수치 변동 연출에 사용합니다.
    /// </summary>
    /// <param name="from">시작 값</param>
    /// <param name="to">목표 값</param>
    /// <param name="duration">카운팅 완료까지 걸리는 시간</param>
    /// <param name="format">C# 숫자 포맷. 예: "N0" = 1,000 / "D4" = 0001</param>
    public static Tween DOCountInt(
        this TextMeshProUGUI tmp,
        int from,
        int to,
        float duration,
        string format = "N0")
    {
        int current = from;
        return DOTween.To(
            () => current,
            x =>
            {
                current = x;
                tmp.text = x.ToString(format);
            },
            to,
            duration
        ).SetEase(Ease.OutCubic);
    }

    /// <summary>
    /// 실수를 from에서 to까지 카운팅하며 텍스트로 표시합니다.
    /// 평점, 퍼센트 등 소수점이 필요한 수치 변동 연출에 사용합니다.
    /// </summary>
    /// <param name="from">시작 값</param>
    /// <param name="to">목표 값</param>
    /// <param name="duration">카운팅 완료까지 걸리는 시간</param>
    /// <param name="format">C# 숫자 포맷. 예: "F1" = 4.5 / "P0" = 45%</param>
    public static Tween DOCountFloat(
        this TextMeshProUGUI tmp,
        float from,
        float to,
        float duration,
        string format = "F1")
    {
        float current = from;
        return DOTween.To(
            () => current,
            x =>
            {
                current = x;
                tmp.text = x.ToString(format);
            },
            to,
            duration
        ).SetEase(Ease.OutCubic);
    }

    /// <summary>
    /// 여러 줄의 텍스트를 줄 단위로 순차 타이핑합니다.
    /// 대화창에서 대사가 여러 줄에 걸쳐 출력될 때 사용합니다.
    /// </summary>
    /// <param name="lines">순서대로 출력할 텍스트 배열</param>
    /// <param name="lineDuration">줄 하나의 타이핑 완료까지 걸리는 시간</param>
    /// <param name="lineDelay">다음 줄 타이핑 시작 전 대기 시간</param>
    public static Sequence DOTypewriterLines(
        this TextMeshProUGUI tmp,
        string[] lines,
        float lineDuration,
        float lineDelay = 0.1f)
    {
        Sequence seq = DOTween.Sequence();
        string accumulated = string.Empty;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            string snapshot = accumulated;

            seq.AppendCallback(() =>
            {
                tmp.text = snapshot + line;
                tmp.maxVisibleCharacters = snapshot.Length;
                tmp.ForceMeshUpdate();
            });

            int baseLen = accumulated.Length;
            accumulated += line + "\n";
            int totalSoFar = baseLen + line.Length;

            seq.Append(DOTween.To(
                () => tmp.maxVisibleCharacters,
                x => tmp.maxVisibleCharacters = x,
                totalSoFar,
                lineDuration
            ).SetEase(Ease.Linear));

            if (i < lines.Length - 1)
                seq.AppendInterval(lineDelay);
        }

        return seq;
    }

    /// <summary>
    /// maxVisibleCharacters를 int.MaxValue로 설정해 텍스트 전체를 즉시 표시합니다.
    /// 대화 스킵 처리 시 사용합니다.
    /// </summary>
    public static void ShowAll(this TextMeshProUGUI tmp)
    {
        tmp.maxVisibleCharacters = int.MaxValue;
    }

    /// <summary>
    /// 해당 TMP에 걸린 모든 트윈을 즉시 완료 상태로 종료합니다.
    /// ShowAll을 함께 호출해 텍스트가 잘린 채 멈추는 것을 방지합니다.
    /// </summary>
    public static void CompleteTweens(this TextMeshProUGUI tmp)
    {
        DOTween.Complete(tmp);
        tmp.ShowAll();
    }
}