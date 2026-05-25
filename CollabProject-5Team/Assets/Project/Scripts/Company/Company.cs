using UnityEngine;

public class Company : MonoBehaviour
{
    public static Company Instance;

    public int day;             // 현재 날짜
    public int money;           // 보유 자금 (단위: 만원)
    public int level;           // 회사 레벨

    public Project[] curProjects; // 현재 진행중인 프로젝트들


    #region 싱글톤 설정
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Init() => Instance = null;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this; DontDestroyOnLoad(gameObject);
    #endregion
    }
}
