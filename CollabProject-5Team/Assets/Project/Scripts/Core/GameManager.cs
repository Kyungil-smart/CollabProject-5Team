using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("맵 관리")]
    [SerializeField] private List<MapInfo> _offices;              // 사무실 맵 프리팹
    public int _currentOfficeLevel = 0;

    [Header("프리팹")]
    [SerializeField] private GameObject _playerPrefab;                  // 플레이어 프리팹
    [SerializeField] private List<GameObject> _allNpcPrefabs;           // NPC프리팹

    private Transform _currentPlayerSpawnPoint;
    public  Transform  currentNpcSpawnPoint => _currentNpcSpawnPoint;
    private Transform _currentNpcSpawnPoint;
    private Transform _currentMapTransform;                                 // 의자가 있는 현재 맵

    private List<Transform>          _sitPoints = new List<Transform>();       // 앉을 좌표 리스트
    private List<NPCController>     _activeNpcs = new List<NPCController>();   // 활성화된 NPC를 담아둘 리스트
    private List<int>           _hiredEmployees = new List<int>();             // 고용된 NPC
    private bool _isUpgradeReserved = false;                                   // 맵 증축 저장용

    [Header("자동 주입")]
    public PlayerMove player;

    #region DontDestroyOnLoad 없는 Instance
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Init() => Instance = null;
    private void Awake()
    {
        Instance = this;
    #endregion
    }

    private void Start()
    {
        GenerateOffice();
        InitializeGameAsync().Forget();
    }

    private void GenerateOffice()
    {
        if (_offices == null || _offices[_currentOfficeLevel] == null)
        {
            Debug.LogError("GameManager: _officeMaps가 비어있습니다. 인덱스 : {_currentOfficeIndex}");
            return;
        }

        MapInfo prefab = _offices[_currentOfficeLevel];
        MapInfo firstMap = Instantiate(prefab, Vector3.zero, Quaternion.identity);

        if (firstMap.PlayerSpawn == null)
            Debug.Log($"{prefab.name} 프리팹 내부에 PlayerSpawn이 연결되지 않음");

        if (_currentMapTransform != null)
        {
            Destroy(_currentMapTransform.gameObject);
            _currentMapTransform = null;
        }

        _currentMapTransform = firstMap.transform;

        _currentPlayerSpawnPoint = firstMap.PlayerSpawn;
        _currentNpcSpawnPoint = firstMap.NpcSpawn;

        Debug.Log($"초기 맵 생성 완료: {_offices[0].name}");
    }

    // 처음 게임 시작 시 플레이어, NPC생성 및 배치
    private async UniTask InitializeGameAsync()
    {
        await UniTask.Yield();

        Debug.Log($"InitializeGameAsync 진입 - PlayerSpawn: {_currentPlayerSpawnPoint}, NpcSpawn: {_currentNpcSpawnPoint}");

        if (_currentPlayerSpawnPoint == null)
        {
            Debug.LogError("스폰 포인트가 비어있습니다! Awake에서 할당이 안 되었나요?");
            return;
        }

        // 새로 생성된 맵의 퀘스트 오브젝트 루트를 QuestManager에 주입
        QuestManager.Instance?.SetQuestObjectsRoot(_currentMapTransform);

        // 플레이어 생성 및 GameManager에 참조 주입
        GameObject playerObj = Instantiate(_playerPrefab, _currentPlayerSpawnPoint.position, Quaternion.identity);
        InjectPlayer(playerObj.GetComponent<PlayerMove>());

        // 플레이어가 생성되고 1초 대기
        await UniTask.Delay(1000);

        // 의자 정보
        RefreshSitPoints();

        // NPC 생성
        var empList = _EmployeeManager.Instance.haveEmployees.haveEmployeeList;
        foreach (var emp in empList)
        {
            await SpawnNPCsAsync(emp.so.id);

            _activeNpcs.Last().GetComponent<Employee>().MutableData = emp.MutableData;
        }
    }

    // 사무실 업그레이드 저장용 함수
    public void ReserveOfficeUpgrade()
    {
        _isUpgradeReserved = true;
    }

    // 월요일 아침에 호출
    public async UniTask TryProcessUpgradeAsync()
    {
        if (!_isUpgradeReserved) return;

        await UpgradeOfficeAsync();
        _isUpgradeReserved = false;    
    }

    // 사무실 업그레이드 시 맵 교체 및 NPC재배치
    public async UniTask UpgradeOfficeAsync()
    {
        foreach (var npc in _activeNpcs)
        {
            if (npc != null)
            {
                npc.TargetDesk = null;
                npc.ChangeState(new NPCIdle());
            }
        }

        // 현재 맵의 인덱스가 맵의 개수와 같거나 크면 리턴
        if (_currentOfficeLevel + 1 >= _offices.Count) return;

        LeaveWorkNPCs();

        await UniTask.Yield();

        // 기존 맵 파괴
        if (_currentMapTransform != null)
        {
            Debug.Log($"기존 맵 제거 시도: {_currentMapTransform.name}");
            Destroy(_currentMapTransform.gameObject);
            _currentMapTransform = null;
        }

        // 인덱스 증가시키고 새 맵 생성
        _currentOfficeLevel++;

        MapInfo newOffice = Instantiate(_offices[_currentOfficeLevel], Vector3.zero, Quaternion.identity);
        
        _currentMapTransform = newOffice.transform;

        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.MapSettings(newOffice);
        }

        // 스폰 포인트 찾기
        _currentPlayerSpawnPoint = newOffice.PlayerSpawn;
        _currentNpcSpawnPoint = newOffice.NpcSpawn;

        // 의자 좌표 갱신
        RefreshSitPoints();

        // 교체된 맵의 퀘스트 오브젝트 루트를 QuestManager에 재주입
        QuestManager.Instance?.SetQuestObjectsRoot(_currentMapTransform);

        // 플레이어 위치 갱신
        if (player != null && _currentPlayerSpawnPoint != null)
        {
            player.ResetMovementState();

            // transform.position 대신 NavMeshAgent.Warp 사용
            var agent = player.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.Warp(_currentPlayerSpawnPoint.position);
            }
            else
            {
                player.transform.position = _currentPlayerSpawnPoint.position;
            }

            player.transform.rotation = Quaternion.identity;
        }

        foreach (var npc in _activeNpcs)
        {
            if (npc != null)
            {
                npc.transform.position = _currentNpcSpawnPoint.position;
                npc.transform.rotation = Quaternion.identity;

                npc.ChangeState(new NPCIdle());
            }
        }
        // GotoWorkNPCs();
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
    public async UniTask SpawnNPCsAsync(int employeeId)
    {
        // 프리팹 확인
        if (!_EmployeeManager.Instance.employeeList.allEmployeePrefabs.TryGetValue(employeeId, out GameObject prefab))
            return;
    
        // NPC 생성
        GameObject npcObj = Instantiate(prefab, _currentNpcSpawnPoint.position, Quaternion.identity);
        // npcObj.GetComponent<Employee>().MutableData = emp.MutableData;
        // 데이터 주입            
        var controller = npcObj.GetComponent<NPCController>();
        
        // 생성된 NPC를 List에 담음
        _activeNpcs.Add(controller);

        // 비어있는 자리 할당
        controller.TargetDesk = GetEmptySitPoint();

        // 활성화 및 이동 명령
        controller.gameObject.SetActive(true);
        if (controller.TargetDesk != null)
        {
            controller.ChangeState(new NPCMove());
        }
        // 1초 간격으로 생성
        await UniTask.Delay(1000);

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
        // 현재 고용된 직원의 ID 리스트를 가져옴
        var currentHiredIds = _EmployeeManager.Instance.haveEmployees.haveEmployeeList
                                        .Select(e => e.so.id).ToHashSet();

        // 씬에 있는 NPC들을 전수 조사하여 해고된 녀석은 지우고, 남은 녀석은 활성화
        for (int i = _activeNpcs.Count - 1; i >= 0; i--)
        {
            var npc = _activeNpcs[i];

            // NPC가 삭제되었거나 고용 명단에 없는 경우
            if (npc == null || !currentHiredIds.Contains(npc.GetComponent<Employee>().so.id))
            {
                if (npc != null) Destroy(npc.gameObject);
                _activeNpcs.RemoveAt(i);
                continue; 
            }

            // 고용된 녀석은 강제로 활성화하고 업무 상태로 전환
            if (npc.TargetDesk == null || npc.TargetDesk.gameObject == null || !npc.TargetDesk.gameObject.activeInHierarchy)
            {
                npc.TargetDesk = GetEmptySitPoint();
            }

            // 다시 업무 상태로 복귀
            npc.gameObject.SetActive(true);
            if (npc.TargetDesk != null)
            {
                npc.ChangeState(new NPCMove());
            }
        }

        // 고용 명단에는 있는데 씬에 생성 안 된 녀석을 생성
        foreach (var emp in _EmployeeManager.Instance.haveEmployees.haveEmployeeList)
        {
            if (!_activeNpcs.Any(n => n != null && n.GetComponent<Employee>().so.id == emp.so.id))
            {
                SpawnNPCsAsync(emp.so.id).Forget();
            }
        }
    }

    // 새로 채용한 직원 ID를 담아둠
    public void ReserveHire(int employeeID)
    {
        if (!_hiredEmployees.Contains(employeeID))
        {
            _hiredEmployees.Add(employeeID);    
        }
    }

    // 고용, 해고 반영 후 출근 실행
    public async UniTask HiredNPCGoToWork()
    {
        ConfirmReservations();

        // 출근 전 씬에 남아있는 죽은 객체나 데이터와 맞지 않는 객체 삭제
        _activeNpcs.RemoveAll(n => n == null);

        // 실제 고용된 ID 목록
        var hiredIds = _EmployeeManager.Instance.haveEmployees.haveEmployeeList
                        .Select(e => e.so.id).ToList();

        // 고용되지 않은 NPC는 씬에서 파괴
        for (int i = _activeNpcs.Count - 1; i >= 0; i--)
        {
            if (!hiredIds.Contains(_activeNpcs[i].GetComponent<Employee>().so.id))
            {
                Destroy(_activeNpcs[i].gameObject);
                _activeNpcs.RemoveAt(i);
            }
        }

        // 고용 명단에 있는 직원을 씬에 배치
        foreach (var id in hiredIds)
        {
            var existing = _activeNpcs.FirstOrDefault(n => n.GetComponent<Employee>().so.id == id);
            if (existing != null)
            {
                // 활성화
                existing.gameObject.SetActive(true);

                if (existing.TargetDesk == null)
                {
                    existing.TargetDesk = GetEmptySitPoint();
                    existing.ChangeState(new NPCMove());
                }

                else
                {
                    // 다시 이동 명령을 내려서 확실하게 앉게 함
                    existing.ChangeState(new NPCMove());
                }
            }
            else
            {
                // 없으면 새로 생성
                await SpawnNPCsAsync(id); 
            }
        }
    }

    public void InjectPlayer(PlayerMove player)
    {
        this.player = player;
    }

    public bool IsReserved(int employeeID)
    {
        // _hiredEmployees는 List<int>이므로 Contains로 확인 가능
        return _hiredEmployees.Contains(employeeID);
    }

    public bool CanHireMore()
    {
        // 현재 있는 NPC의 수
        int activeCount = _activeNpcs.Count;

        // 고용 예약된 인원 수
        int reservedCount = _hiredEmployees.Count;

        // 의자 수와 비교
        return (activeCount + reservedCount) < _sitPoints.Count;
    }



    private Transform GetEmptySitPoint()
    {
        foreach (var sitPoint in _sitPoints)
        {
            bool isOccupied = _activeNpcs.Any(n => n != null && n.TargetDesk == sitPoint);
            if (!isOccupied) return sitPoint;
        }

        return null;
    }

    public void RemoveNpcFromScene(int employeeId)
    {
        // 씬에 있는 해당 ID의 NPC를 찾음
        var npc = _activeNpcs.FirstOrDefault(n => n != null && n.GetComponent<Employee>().so.id == employeeId);

        if (npc != null)
        {
            _activeNpcs.Remove(npc); // 리스트에서 제거
            Destroy(npc.gameObject); // 씬에서 파괴
            Debug.Log($"[GameManager] {employeeId}번 직원 씬에서 즉시 삭제 완료");
        }
    }

    public void FireEmployee(int employeeId)
    {
        // 데이터에서만 제거
        var emp = _EmployeeManager.Instance.haveEmployees.haveEmployeeList
                      .FirstOrDefault(e => e != null && e.so.id == employeeId);
        if (emp != null) _EmployeeManager.Instance.FireEmployee(emp); 

        // 씬에서는 Destroy 하지 말고 비활성화만 함
        var targetNpc = _activeNpcs.FirstOrDefault(n => n != null && n.GetComponent<Employee>().so.id == employeeId);
        
        if (targetNpc != null)
        {
            targetNpc.gameObject.SetActive(false);
        }
    }

    public void ConfirmReservations()
    {
        foreach (var id in _hiredEmployees)
        {
            // EmployeeManager에 실제 고용 데이터 추가
            _EmployeeManager.Instance.HireEmployee(id); 
        }

        // 예약 명단 초기화
        _hiredEmployees.Clear();
    }
}