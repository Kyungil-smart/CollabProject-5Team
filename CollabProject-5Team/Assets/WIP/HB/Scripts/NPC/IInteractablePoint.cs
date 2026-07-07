using UnityEngine;

public enum PointType { Desk, Sofa, CopyMachine, Drink, ServerRoom, Work, Look, Sleep, Play, Make, Find }
public interface IInteractablePoint
{
    Transform GetTransform();
    PointType GetPointType();
}
