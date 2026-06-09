using UnityEngine;

public class Seat : MonoBehaviour
{
    [Header("앉을 위치 설정")] 
    public Transform sitPoint;

    [Header("상태 정보")]
    public bool isSeated = false;
}
