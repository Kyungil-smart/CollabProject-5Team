using UnityEngine;

public class ActionPoint : MonoBehaviour, IInteractablePoint
{
    public Transform SitPoint;
    public PointType PointType;
    public bool IsOccupied;
    public NPCController Owner;     // 업무 책상 자리의 주인
    public PointType GetPointType() => PointType;

    public Transform GetTransform() => SitPoint;
}
