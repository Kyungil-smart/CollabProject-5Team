using UnityEngine;

public class PunchHoleFilter : MonoBehaviour, ICanvasRaycastFilter
{
    private RectTransform _holeRect; // 뚫을 영역의 RectTransform

    public void SetHole(RectTransform hole) => _holeRect = hole;

    public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
    {
        if (_holeRect == null) return true; // 구멍이 없으면 전체 클릭 막음

        // 클릭한 위치가 구멍 영역 안인지 확인
        return !RectTransformUtility.RectangleContainsScreenPoint(_holeRect, sp, eventCamera);
    }
}