using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("맵 관리")]
    [SerializeField] private List<GameObject> _officeMaps;              // 사무실 맵 프리팹
    public int _currentMapIndex = 0;

    [Header("프리팹")]
    [SerializeField] private GameObject _playerPrefab;                  // 플레이어 프리팹
    [SerializeField] private List<GameObject> _allNpcPrefabs;           // NPC프리팹
   

    [Header("자리 배치")]
    [SerializeField] private List<Transform> _playerSpawnPoint;             // 플레이어 스폰 지점
    [SerializeField] public  List<Transform>     NpcSpawnPoint;             // NPC 스폰 지점

    private Transform _currentMapTransform;                                 // 의자가 있는 현재 맵

    private List<Transform>          _sitPoints = new List<Transform>();       // 앉을 좌표 리스트
    private List<NPCController>     _activeNpcs = new List<NPCController>();   // 활성화된 NPC를 담아둘 리스트
    private List<int>           _hiredEmployees = new List<int>();             // 고용된 NPC

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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("P 키 입력: 사무실 업그레이드를 시도합니다.");
            UpgradeOffice();
        }
    }

    private void GenerateMap()
    {
        if (_officeMaps == null || _officeMaps.Count == 0)
        {
            Debug.LogError("GameManager: _officeMaps가 비어있습니다. 인스펙터를 확인하세요.");
            return;
        }

        for (int i = 0; i < _officeMaps.Count; i++)
        {
            if (_officeMaps[i] != null)
                _officeMaps[i].SetActive(false);
        }

        if (_officeMaps[0] != null)
        {
            _officeMaps[0].SetActive(true);
            _currentMapTransform = _officeMaps[0].transform;
        }
    }

    // 처음 게임 시작 시 플레이어, NPC생성 및 배치f
    private async UniTask InitializeGameAsync()
    {
        await UniTask.Yield();

        // 플레이어 생성 및 GameManager에 참조 주입
        GameObject playerObj = Instantiate(_playerPrefab, _playerSpawnPoint[_currentMapIndex].position, Quaternion.identity);
        InjectPlayer(playerObj.GetComponent<PlayerMove>());

        // 플레이어가 생성되고 1초 대기
        await UniTask.Delay(1000);

        // 의자 정보
        RefreshSitPoints();

        // NPC 생성
        await SpawnNPCsAsync();
        GotoWorkNPCs();
    }

    // 사무실 업그레이드 시 맵 교체 및 NPC재배치
    public void UpgradeOffice()
    {
        // 현재 맵의 인덱스가 맵의 개수와 같거나 크면 리턴
        if (_currentMapIndex + 1 >= _officeMaps.Count) return;

        // 1. 기존 맵 비활성화
        if (_officeMaps[_currentMapIndex] != null)
            _officeMaps[_currentMapIndex].SetActive(false);

        // 2. 인덱스 증가시키고 새 맵 활성화
        _currentMapIndex++;

        _currentMapTransform = _officeMaps[_currentMapIndex].transform;
        _currentMapTransform.gameObject.SetActive(true);
            
        // 의자 좌표 갱신
        RefreshSitPoints();

        // 플레이어 위치 갱신
        if (player != null && _playerSpawnPoint != null)
            player.transform.position = _playerSpawnPoint[_currentMapIndex].position;

        // 기존 NPC정리 및 새 맵에 맞춰 재배치
        LeaveWorkNPCs();

        GotoWorkNPCs();

        SpawnNPCsAsync().Forget();
    }

    // 현재 맵에서 SitPoint 위치를 찾아 리스트 갱신
    public void RefreshSitPoints()
    {
        // 의자 데이터 초기화
        _sitPoints.Clear();

        // 현재 맵의 자식 오브젝트 중 Seat.cs를 참조한 오브젝트 찾음
        Seat[] foundSeats = _currentMapTransform.GetComponentsInChildren<Seat>();

        foreach (var chair in foundSeats)
        {
            // 각 의자의 SitPoint를 리스트에 추가
            _sitPoints.Add(chair.sitPoint);
        }
    }

    // 신규 채용된 직원을 씬에 생성하고 관리 리스트에 추가
    public async UniTask SpawnNPCsAsync()
    {
        // 고용된 직원명단 가져오기
        var empList = _EmployeeManager.Instance.haveEmployees.haveEmployeeList;

        int sitIndex = _activeNpcs.Count;

        foreach (var emp in empList)
        {
            // 이미 존재하는 직원인지 ID비교
            if(_activeNpcs.Any(n => n.GetComponent<Employee>().so.id == emp.so.id))
                continue;

            // 프리팹 확인
            if (!_EmployeeManager.Instance.employeeList.allEmployeePrefabs.TryGetValue(emp.so.id, out GameObject prefab))
                continue;
        
            // NPC 생성
            GameObject npcObj = Instantiate(prefab, NpcSpawnPoint[0].position, Quaternion.identity);
            npcObj.GetComponent<Employee>().MutableData = emp.MutableData;

            // 데이터 주입            
            var controller = npcObj.GetComponent<NPCController>();

            // 생성된 NPC를 List에 담음
            _activeNpcs.Add(controller);

            // 생성된 NPC의 자리를 정해줌
            if (sitIndex < _sitPoints.Count)
            {
                controller.TargetDesk = _sitPoints[sitIndex];
                sitIndex++;
            }

            // 1초 간격으로 생성
            await UniTask.Delay(1000);
        }
    }

    // 활성 NPC 중 아무나 한 명의 Transform을 랜덤으로 반환 (퀘스트 말풍선 표시용)
    public Transform GetRandomActiveNpcTransform()
    {
        List<NPCController> validNpcs = _activeNpcs.FindAll(npc => npc != null);
        if (validNpcs.Count == 0) return null;

        return validNpcs[Random.Range(0, validNpcs.Count)].transform;
    }

    // 퇴근 명령 SpawnPoint로 이동 후 비활성화
    public void LeaveWorkNPCs()
    {
        foreach (var npc in _activeNpcs)
        {
            if (npc != null)
            {
                npc.ChangeState(new NPCLeave());
            }
        }
    }

    // 현재 고용된 인원들 출근
    public void GotoWorkNPCs()
    {
        // 현재 회사에 고용된 모든 직원 ID 가져오기
        var currentEmployeeIds = _EmployeeManager.Instance.haveEmployees.haveEmployeeList
                                    .Select(e => e.so.id).ToHashSet();

        for (int i = _activeNpcs.Count - 1; i >= 0; i--)
        {
            var npc = _activeNpcs[i];

            // 해고된 직원인지 확인
            if (npc != null && !currentEmployeeIds.Contains(npc.GetComponent<Employee>().so.id))
            {
                // 명단에 없으면 파괴하고 리스트에서 제거
                Destroy(npc.gameObject);
                _activeNpcs.RemoveAt(i);
            }
        }

        // 남은 직원 출근, 자리 배치
        for (int i = 0; i < _activeNpcs.Count; i++)
        {
            if (_activeNpcs[i] != null && i < _sitPoints.Count)
            {
                _activeNpcs[i].gameObject.SetActive(true);
                _activeNpcs[i].TargetDesk = _sitPoints[i];
                _activeNpcs[i].ChangeState(new NPCMove()); 
            }
        }
    }


    // 새로 채용한 직원 ID를 담아둠
    public void ReserveHire(int employeeID)
    {
        _hiredEmployees.Add(employeeID);
    }

    // 고용, 해고 반영 후 출근 실행
    public async Task HiredNPCGoToWork()
    {
        foreach (int id in _hiredEmployees)
        {
            _EmployeeManager.Instance.HireEmployee(id);
        }
        
        _hiredEmployees.Clear();

        // 신규직원 생성 기다리기
        await SpawnNPCsAsync();
        
        // 모든 NPC에게 자리로 이동 명령
        GotoWorkNPCs();
    }

    public void InjectPlayer(PlayerMove player)
    {
        this.player = player;
    }
}
