using UnityEngine;

public class ActionPoint : MonoBehaviour, IInteractablePoint
{
    public Transform SitPoint;
    public PointType PointType;
    [SerializeField] private bool _isOccupied;
    public bool IsOccupied
    {
        get => _isOccupied;
        set
        {
            if (_isOccupied == value) return;

            _isOccupied = value;
        }
    }
    public NPCController Owner;     // 업무 책상 자리의 주인
    public PointType GetPointType() => PointType;

    public Transform GetTransform() => SitPoint;
}
