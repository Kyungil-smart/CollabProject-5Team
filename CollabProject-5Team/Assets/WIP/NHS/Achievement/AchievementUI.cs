using TMPro;
using UnityEngine;
using DG.Tweening;

public class AchievementUI : MonoBehaviour
{
    [SerializeField] private RectTransform _panelRect;

    [SerializeField] private TextMeshProUGUI _textTitle;
    [SerializeField] private TextMeshProUGUI _textDetail;

    [Header("연출 설정")]
    [SerializeField] private float _moveX        = 450f;
    [SerializeField] private float _moveDuration = 0.5f;
    [SerializeField] private float _showDuration = 2.0f;

    private Vector2 _startPosition;
    private Sequence _showSequence;

    private void Awake()
    {
        _startPosition = _panelRect.anchoredPosition;
        HideImmediately();
    }

    private void OnDestroy()
    {
        if (_showSequence != null)
            _showSequence.Kill();
    }

    public void ShowAchievement(string title, string description)
    {
        _textTitle.text = title;
        _textDetail.text = description;

        if (_showSequence != null && _showSequence.IsActive())
            _showSequence.Kill();

        HideImmediately();

        _showSequence = DOTween.Sequence();

        float targetX = _startPosition.x - _moveX;

        _showSequence
            .Append(_panelRect.DOAnchorPos(new Vector2(targetX, _startPosition.y), _moveDuration).SetEase(Ease.OutBack))
            .AppendInterval(_showDuration)
            .Append(_panelRect.DOAnchorPos(_startPosition, _moveDuration).SetEase(Ease.InBack))
            .OnComplete(() =>
            {
            });
    }

    private void HideImmediately()
    {
        _panelRect.anchoredPosition = _startPosition;
    }
}