using UnityEngine;

public class InteractionPoint : MonoBehaviour, IInteractablePoint, IInteractable
{
    public PointType type;
    public bool IsOccupied = false;
    [SerializeField] private Transform _sitPoint;

    public Transform GetTransform()
    {
        return _sitPoint != null ? _sitPoint : transform;
    }
    
    public PointType GetPointType() => type;

    public bool IsDesk => type == PointType.Desk;

    public void OnInteract()
    {
        
    }
}
