using TMPro;
using UnityEngine;
using DG.Tweening;

public class AchievementUI : MonoBehaviour
{
    [SerializeField] private RectTransform _panelRect;

    [SerializeField] private TextMeshProUGUI _textTitle;
    [SerializeField] private TextMeshProUGUI _textDetail;

    [Header("연출 설정")]
    [SerializeField] private float _moveX = 450f;
    [SerializeField] private float _moveDuration = 0.5f;
    [SerializeField] private float _showDuration = 2.0f;

    private Vector2 _startPosition;
    private Sequence _showSequence;

    private void Awake()
    {
        _panelRect.anchoredPosition = _panelRect.anchoredPosition; 
    }

    private void Start()
    {
        Canvas.ForceUpdateCanvases();
        _startPosition = _panelRect.anchoredPosition;
    }

    private void OnDestroy()
    {
        _showSequence?.Kill();
    }

    public void ShowAchievement(string title, string description)
    {
        _textTitle.text = title;
        _textDetail.text = description;

        if (_showSequence != null && _showSequence.IsActive())
            _showSequence.Kill();

        if (_startPosition == Vector2.zero)
            _startPosition = _panelRect.anchoredPosition;

        _panelRect.anchoredPosition = _startPosition;

        _showSequence = DOTween.Sequence();

        float targetX = _startPosition.x - _moveX;

        _showSequence
            .Append(_panelRect.DOAnchorPosX(targetX, _moveDuration).SetEase(Ease.OutBack))
            .AppendInterval(_showDuration)
            .Append(_panelRect.DOAnchorPosX(_startPosition.x, _moveDuration).SetEase(Ease.InBack));
    }
}