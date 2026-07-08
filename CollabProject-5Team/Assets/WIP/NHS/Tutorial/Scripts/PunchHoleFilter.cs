    using UnityEngine;
    using static TutorialManager;

    public class PunchHoleFilter : MonoBehaviour, ICanvasRaycastFilter
    {
        private RectTransform _holeTargetRect;
        private Camera        _canvasCamera;

        private Vector4 _customScreenRect = Vector4.zero;
        private bool _usePunchHole = false;

        public HoleShape _punchHoleMode { get; set; }

        public void SetTarget(RectTransform targetRect, HoleShape mode = HoleShape.Square)
        {
              _usePunchHole = false;
             _punchHoleMode = mode;
            _holeTargetRect = targetRect;

            Canvas canvas = targetRect.GetComponentInParent<Canvas>();
            if (canvas != null) _canvasCamera = canvas.worldCamera;
        }

        public void SetCustomScreenRect(Vector4 screenRect)
        {
                _usePunchHole = true;
              _holeTargetRect = null;
            _customScreenRect = screenRect;
        }

        public void ClearTarget()
        {
                _usePunchHole = false;
              _holeTargetRect = null;
            _customScreenRect = Vector4.zero;
        }

    public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
    {
        bool isInside = false;

        if (_usePunchHole)
        {
            // 1. 3D 타겟
            if (_punchHoleMode == HoleShape.Circle)
            {
                Vector2 center = new Vector2((_customScreenRect.x + _customScreenRect.z) * 0.5f,
                                             (_customScreenRect.y + _customScreenRect.w) * 0.5f);
                float radius = (_customScreenRect.z - _customScreenRect.x) * 0.5f;
                isInside = Vector2.Distance(sp, center) <= radius;
            }
            else if(_punchHoleMode == HoleShape.Square)
            {
                isInside = sp.x >= _customScreenRect.x && sp.x <= _customScreenRect.z &&
                           sp.y >= _customScreenRect.y && sp.y <= _customScreenRect.w;
            }   
        }

        else if (_holeTargetRect != null)
        {
            // 2. UI 타겟\
            if (_punchHoleMode == HoleShape.Circle)
            {
                Vector3[] corners = new Vector3[4];
                _holeTargetRect.GetWorldCorners(corners);
                Vector2 center = (Vector2)corners[0] + (Vector2)(corners[2] - corners[0]) * 0.5f;
                float radius = Vector2.Distance(corners[0], corners[3]) * 0.5f;
                isInside = Vector2.Distance(sp, center) <= radius;
            }
            else if (_punchHoleMode == HoleShape.Square)
            {
                isInside = RectTransformUtility.RectangleContainsScreenPoint(_holeTargetRect, sp, _canvasCamera ?? eventCamera);
            }
        }
        else
        {
            return true;
        }

        return !isInside;
    }
}