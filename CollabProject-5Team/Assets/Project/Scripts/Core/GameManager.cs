using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("맵 관리")]
    [SerializeField] private List<GameObject> _officeMaps;              // 사무실 맵 프리팹
    private int _currentMapIndex = 0;

    [Header("프리팹")]
    [SerializeField] private GameObject _playerPrefab;                  // 플레이어 프리팹
    [SerializeField] private List<GameObject> _allNpcPrefabs;           // NPC프리팹
   

    [Header("자리 배치")]
    [SerializeField] private Transform _playerSpawnPoint;               // 플레이어 스폰 지점
    [SerializeField] public Transform NpcSpawnPoint;                    // NPC 스폰 지점
    [SerializeField] private Transform _map;                            // 의자가 있는 현재 맵

    private List<Transform> _sitPoints = new List<Transform>();         // 앉을 좌표 리스트

    private List<NPCController> _activeNpcs = new List<NPCController>();   // 활성화된 NPC를 담아둘 리스트

    [Header("자동 주입")]
    public PlayerMove player;

    #region DontDestroyOnLoad없는 그냥 Instance 설정
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Init() => Instance = null;

    private void Awake()
    {
        Instance = this;

        GenerateMap();
    #endregion
    }

    private void Start()
    {
        InitializeGameAsync().Forget();
    }

    private void GenerateMap()
    {
        for (int i = 0; i < _officeMaps.Count; i++)
        {
            _officeMaps[i].SetActive(i == 0);
        }
        _map = _officeMaps[0].transform;
    }

    private async UniTask InitializeGameAsync()
    {
        // 플레이어 생성 및 GameManager에 참조 주입
        GameObject playerObj = Instantiate(_playerPrefab, _playerSpawnPoint.position, Quaternion.identity);
        InjectPlayer(playerObj.GetComponent<PlayerMove>());

        // 플레이어가 생성되고 1초 대기
        await UniTask.Delay(1000);

        // 의자 정보
        RefreshSitPoints();

        // NPC 생성
        await SpawnNPCsAsync(10);
    }

    public void UpgradeOffice()
    {
        // 현재 맵의 인덱스가 맵의 개수와 같거나 크면 리턴
        if (_currentMapIndex + 1 >= _officeMaps.Count)
        {
            return;
        }

        // 기존 맵 비활성화
        _officeMaps[_currentMapIndex].SetActive(false);

        // 인덱스 증가시키고 새 맵 활성화
        _currentMapIndex++;
        _map = _officeMaps[_currentMapIndex].transform;
        _map.gameObject.SetActive(true);

        // 의자 좌표 갱신
        RefreshSitPoints();

        // 기존 NPC정리 및 새 맵에 맞춰 재배치
        LeaveWorkNPCs();
        SpawnNPCsAsync(10).Forget();
    }

    public void RefreshSitPoints()
    {
        // 의자 데이터 초기화
        _sitPoints.Clear();
        // 현재 맵의 자식 오브젝트 중 Seat.cs를 참조한 오브젝트 찾음
        Seat[] foundSeats = _map.GetComponentsInChildren<Seat>();

        foreach (var chair in foundSeats)
        {
            // 각 의자의 SitPoint를 리스트에 추가
            _sitPoints.Add(chair.sitPoint);
        }
    }

    public async UniTask SpawnNPCsAsync(int count)
    {
        for (int i = 0; i < count; i++)
        {
            // 생성할 NPC 프리팹이나 의자 리스트보다 인덱스가 크면 생성 중단
            if (i >= _allNpcPrefabs.Count || i >= _sitPoints.Count) break;

            // NPC 생성
            GameObject npcObj 
            = Instantiate(_allNpcPrefabs[i], NpcSpawnPoint.position, Quaternion.identity);
            
            // 각 프리팹 내부의 데이터 초기화
            npcObj.GetComponent<Employee>().Init(); 
            
            // 각 NPC에게 의자 좌표를 전달해 이동시킴
            var controller =  npcObj.GetComponent<NPCController>();
            controller.TargetDesk = _sitPoints[i];

            // 생성된 NPC를 List에 담음
            _activeNpcs.Add(controller);

            // 1초 간격으로 생성
            await UniTask.Delay(1000);
        }
    }

    // 퇴근 명령 함수
    public void LeaveWorkNPCs()
    {
        foreach (var npc in _activeNpcs)
        {
            if (npc != null)
            {
                npc.ChangeState(new NPCLeave());
            }
        }

        _activeNpcs.Clear();
    }

    public void InjectPlayer(PlayerMove player)
    {
        this.player = player;
    }
}
