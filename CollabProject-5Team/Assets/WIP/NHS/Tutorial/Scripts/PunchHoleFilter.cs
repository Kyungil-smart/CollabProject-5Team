using UnityEngine;

public class PunchHoleFilter : MonoBehaviour, ICanvasRaycastFilter
{
    private RectTransform _targetRect;
    private Camera        _canvasCamera;

    private Vector4 _customScreenRect = Vector4.zero;
    private bool _useCustomRect = false;

    public void SetTarget(RectTransform targetRect)
    {
           _targetRect = targetRect;
        _useCustomRect = false;

        Canvas canvas = targetRect.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            _canvasCamera = canvas.worldCamera;
        }
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
        if (_useCustomRect)
        {
            bool isInsideCustom = sp.x >= _customScreenRect.x && sp.x <= _customScreenRect.z &&
                                  sp.y >= _customScreenRect.y && sp.y <= _customScreenRect.w;

            return !isInsideCustom;
        }

        if (_targetRect == null) return true;

        bool isInside = RectTransformUtility.RectangleContainsScreenPoint(_targetRect, sp, _canvasCamera ?? eventCamera);
        return !isInside;
    }
}