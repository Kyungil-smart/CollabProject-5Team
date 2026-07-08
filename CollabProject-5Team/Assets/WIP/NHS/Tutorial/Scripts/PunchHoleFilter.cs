using UnityEngine;

public class PunchHoleFilter : MonoBehaviour, ICanvasRaycastFilter
{
    private RectTransform _targetRect;
    private Camera        _canvasCamera;

    private Vector4 _customScreenRect = Vector4.zero;
    private bool _useCustomRect = false;

    public bool IsCircleMode { get; set; }

    public void SetTarget(RectTransform targetRect, bool isCircle = false)
    {
        _targetRect = targetRect;
        _useCustomRect = false;
        IsCircleMode = isCircle;

        Canvas canvas = targetRect.GetComponentInParent<Canvas>();
        if (canvas != null) _canvasCamera = canvas.worldCamera;
    }

    public void SetCustomScreenRect(Vector4 screenRect)
    {
              _targetRect = null;
        _customScreenRect = screenRect;
           _useCustomRect = true;
    }

    public void ClearTarget()
    {
              _targetRect = null;
           _useCustomRect = false;
        _customScreenRect = Vector4.zero;
    }

    public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
    {
        // 1. 커스텀(3D) 영역 체크
        if (_useCustomRect)
        {
            if (IsCircleMode)
            {
                Vector2 center = new Vector2((_customScreenRect.x + _customScreenRect.z) * 0.5f,
                                             (_customScreenRect.y + _customScreenRect.w) * 0.5f);
                float radius = (_customScreenRect.z - _customScreenRect.x) * 0.5f;
                return Vector2.Distance(sp, center) > radius; // 밖이면 통과(true)
            }
            bool isInsideCustom = sp.x >= _customScreenRect.x && sp.x <= _customScreenRect.z &&
                                  sp.y >= _customScreenRect.y && sp.y <= _customScreenRect.w;
            return !isInsideCustom;
        }

        // 2. UI 타겟 체크
        if (_targetRect == null) return true;

        if (IsCircleMode)
        {
            // UI 중앙과 반지름 계산
            Vector3[] corners = new Vector3[4];
            _targetRect.GetWorldCorners(corners);
            Vector2 center = (Vector2)corners[0] + (Vector2)(corners[2] - corners[0]) * 0.5f;

            // 원의 반지름 (너비 기준)
            float radius = Vector2.Distance(corners[0], corners[3]) * 0.5f;

            // 스크린 포인트와 원 중심 사이의 거리가 반지름보다 크면 밖임(true)
            return Vector2.Distance(sp, center) > radius;
        }

        // 기본 사각형 체크
        bool isInside = RectTransformUtility.RectangleContainsScreenPoint(_targetRect, sp, _canvasCamera ?? eventCamera);
        return !isInside;
    }
}