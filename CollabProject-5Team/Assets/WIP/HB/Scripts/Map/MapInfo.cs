using UnityEngine;

public class MapInfo : MonoBehaviour
{
    public Transform PlayerSpawn;
    public Transform NpcSpawn;

    [Header("카메라 설정")]
    public float DiamondLength = 10f;
    public float DiamondWidth = 10f;
    public float MinSize = 5f;
    public float MaxSize = 13f;
    public float DefaultSize = 13f;
}
