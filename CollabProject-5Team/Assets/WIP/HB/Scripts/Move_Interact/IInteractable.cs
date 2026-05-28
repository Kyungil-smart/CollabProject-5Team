

using UnityEngine;

public interface IInteractable
{
    void OnInteract();

    // 플레이어와 상호작용할 NPC 혹은 사물과의 거리
    Transform GetTransform();
}
