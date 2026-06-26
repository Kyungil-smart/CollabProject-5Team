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

    public List<ActionPoint> GetAllPoints()
    {
        return _allPoints;
    }

    public ActionPoint GetRandomAvailablePoint()
    {
        var availablePoints = _allPoints.Where(p => !p.IsOccupied).ToList();

        if (availablePoints.Count == 0) return null;

        return availablePoints[Random.Range(0, availablePoints.Count)];
    }

    public void RefreshPoints(Transform mapRoot)
    {
        if (mapRoot == null) return;

        _allPoints = mapRoot.GetComponentsInChildren<ActionPoint>().ToList();
        foreach (var p in _allPoints)
        {
            p.IsOccupied = false;
            p.Owner = null;
        }
        Debug.Log($"[PointManager] 리스트 갱신 완료. 총 {_allPoints.Count}개");
    }

    public List<ActionPoint> GetPointsByType(PointType type)
    {
        return _allPoints.Where(p => p.GetPointType() == type).ToList();
    }

    public List<ActionPoint> GetPublicPoints()
    {
        var list = _allPoints.Where(p => p.Owner == null && p.PointType != PointType.Desk && !p.IsOccupied).ToList();
        Debug.Log($"[PointManager] 사용 가능한 공용 포인트 개수: {list.Count}");
        return list;
    }
}
