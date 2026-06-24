using UnityEngine;

public enum PointType { Desk, Sofa, CopyMachine, Drink, ServerRoom }
public interface IInteractablePoint
{
    Transform GetTransform();
    PointType GetPointType();
}
