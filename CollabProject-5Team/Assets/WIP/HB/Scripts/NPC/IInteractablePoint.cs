using UnityEngine;

public enum PointType { Desk, Sofa, CopyMachine, Drink, ServerRoom, Work }
public interface IInteractablePoint
{
    Transform GetTransform();
    PointType GetPointType();
}
