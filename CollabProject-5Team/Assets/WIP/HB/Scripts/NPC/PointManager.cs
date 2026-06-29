using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PointManager : MonoBehaviour
{
    public static PointManager Instance;
    [SerializeField] private List<ActionPoint> _allPoints = new();

    private void Awake()
    {
        Instance = this;
    }

    // 포인트 리스트 반환
    public List<ActionPoint> GetAllPoints()
    {
        return _allPoints;
    }

    // 점유도히지 않은 포인트 중 랜덤 반환
    public ActionPoint GetRandomAvailablePoint()
    {
        var availablePoints = _allPoints.Where(p => !p.IsOccupied).ToList();

        if (availablePoints.Count == 0) return null;

        return availablePoints[Random.Range(0, availablePoints.Count)];
    }

    // 맵이 로드 되거나 변경될 때 포인트 리스트를 새로
    public void RefreshPoints(Transform mapRoot)
    {
        if (mapRoot == null) return;

        _allPoints = mapRoot.GetComponentsInChildren<ActionPoint>().ToList();
        foreach (var p in _allPoints)
        {
            p.IsOccupied = false;
            p.Owner = null;
        }
    }

    // 지정한 PointType만 필터링해서 반환
    public List<ActionPoint> GetPointsByType(PointType type)
    {
        return _allPoints.Where(p => p.GetPointType() == type).ToList();
    }

    // 공용 공간 현재는 Desk(업무 의자)만 빼고 반환
    public List<ActionPoint> GetPublicPoints()
    {
        return _allPoints.Where(p => !p.IsOccupied && p.PointType != PointType.Desk).ToList();
    }
}
