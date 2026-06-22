using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("맵 관리")]
    [SerializeField] private List<MapInfo> _offices;              // 사무실 맵 프리팹
    public int _currentOfficeIndex = 0;

    [Header("프리팹")]
    [SerializeField] private GameObject _playerPrefab;                  // 플레이어 프리팹
    [SerializeField] private List<GameObject> _allNpcPrefabs;           // NPC프리팹

    private Transform _currentPlayerSpawnPoint;
    public  Transform  currentNpcSpawnPoint => _currentNpcSpawnPoint;
    private Transform _currentNpcSpawnPoint;
    private Transform _currentMapTransform;                                 // 의자가 있는 현재 맵

    private List<Transform>          _sitPoints = new List<Transform>();       // 앉을 좌표 리스트
    private List<Employee>     _activeEmployees = new List<Employee>();        // 활성화된 직원 NPC를 담아둘 리스트
    public bool isUpgradeReserved = false;                                   // 맵 증축 저장용

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


    public async UniTask InitializeForSaveSystem()
    {
        GenerateOffice(Company.Instance.level);
        await InitializeGameAsync();
    }

    private void GenerateOffice(int companyLevel)
    {
        if (_offices == null || _offices.Count == 0)
        {
            Debug.LogError("GameManager: _offices가 비어있습니다.");
            return;
        }

        int maxLevel = _offices.Count;
        int clampedLevel = Mathf.Clamp(companyLevel, 1, maxLevel);
        _currentOfficeIndex = clampedLevel - 1;

        MapInfo prefab = _offices[_currentOfficeIndex];
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

        CameraManager.Instance.MapSettings(firstMap);
    }

    // 처음 게임 시작 시 플레이어, NPC생성 및 배치
    private async UniTask InitializeGameAsync()
    {
        await UniTask.Yield();

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
        foreach (var emp in _EmployeeManager.Instance.haveEmployees.haveEmployeeList)
        {
            await SpawnNPCsAsync(emp);
        }
    }

    // 월요일 아침에 호출
    public async UniTask TryProcessUpgradeAsync()
    {
        if (!isUpgradeReserved) return;

        await UpgradeOfficeAsync();
        isUpgradeReserved = false;    
    }
    // 사무실 업그레이드 시 맵 교체 및 NPC재배치
    public async UniTask UpgradeOfficeAsync()
    {
        foreach (var employee in _activeEmployees)
        {
            var npc = employee.GetComponent<NPCController>();
            npc.TargetDesk = null;
            npc.ChangeState(new NPCIdle());
        }

        // 현재 맵의 인덱스가 맵의 개수와 같거나 크면 리턴
        if (_currentOfficeIndex + 1 >= _offices.Count) return;

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
        _currentOfficeIndex++;

        MapInfo newOffice = Instantiate(_offices[_currentOfficeIndex], Vector3.zero, Quaternion.identity);
        
        _currentMapTransform = newOffice.transform;

        CameraManager.Instance.MapSettings(newOffice);     

        // 스폰 포인트 찾기
        _currentPlayerSpawnPoint = newOffice.PlayerSpawn;
        _currentNpcSpawnPoint = newOffice.NpcSpawn;

        // 의자 좌표 갱신
        RefreshSitPoints();

        // 교체된 맵의 퀘스트 오브젝트 루트를 QuestManager에 재주입
        QuestManager.Instance.SetQuestObjectsRoot(_currentMapTransform);

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

        foreach (var employee in _activeEmployees)
        {
            var npc = employee.GetComponent<NPCController>();
            npc.transform.position = _currentNpcSpawnPoint.position;
            npc.transform.rotation = Quaternion.identity;

            npc.ChangeState(new NPCIdle());
        }
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
    public async UniTask SpawnNPCsAsync(Employee emp)
    {
        // NPC 생성
        if (emp == null) return;

        var spawnedEmp = emp;
        spawnedEmp.transform.position = _currentNpcSpawnPoint.position;
        spawnedEmp.transform.rotation = Quaternion.identity;

        // 데이터 주입            
        var controller = spawnedEmp.GetComponent<NPCController>();
        
        // 생성된 직원을 List에 담음
        if (!_activeEmployees.Contains(spawnedEmp))
            _activeEmployees.Add(spawnedEmp);

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
        if (_activeEmployees.Count == 0) return null;
        return _activeEmployees[Random.Range(0, _activeEmployees.Count)].transform;
    }

    // 퇴근 명령 SpawnPoint로 이동 후 비활성화
    public void LeaveWorkNPCs()
    {
        foreach (var emp in _activeEmployees)
        {
            emp.GetComponent<NPCController>().ChangeState(new NPCLeave());
        }
    }

    // 고용, 해고 반영 후 출근 실행
    public async UniTask HiredNPCGoToWork()
    {
        _activeEmployees.RemoveAll(e => e == null);

        var hiredEmployees = _EmployeeManager.Instance.haveEmployees.haveEmployeeList;

        for (int i = _activeEmployees.Count - 1; i >= 0; i--)
        {
            var activeEmployee = _activeEmployees[i];
            if (!hiredEmployees.Exists(e => e.so == activeEmployee.so))
            {
                Destroy(activeEmployee.gameObject);
                _activeEmployees.RemoveAt(i);
            }
        }

        foreach (var employee in hiredEmployees)
        {
            var activeEmployee = _activeEmployees.Find(e => e != null && e.so == employee.so);
            if (activeEmployee != null)
            {
                var existingNpc = activeEmployee.GetComponent<NPCController>();
                existingNpc.gameObject.SetActive(true);

                if (existingNpc.TargetDesk == null)
                {
                    existingNpc.TargetDesk = GetEmptySitPoint();
                    existingNpc.ChangeState(new NPCMove());
                }
                else
                {
                    existingNpc.ChangeState(new NPCMove());
                }
            }
            else
            {
                await SpawnNPCsAsync(employee);
            }
        }
    }
    public void InjectPlayer(PlayerMove player)
    {
        this.player = player;
    }

    public bool CanHireMore()
    {
        OfficeUpgradeData data = Company.Instance._upgradeData.GetData(Company.Instance.level);
        int maxEmployee = data.MaxEmployee;

        return _EmployeeManager.Instance.haveEmployees.haveEmployeeList.Count < maxEmployee;
    }



    private Transform GetEmptySitPoint()
    {
        foreach (var sitPoint in _sitPoints)
        {
            bool isOccupied = false;
            foreach (var employee in _activeEmployees)
            {
                if (employee.GetComponent<NPCController>().TargetDesk == sitPoint)
                {
                    isOccupied = true;
                    break;
                }
            }
            if (!isOccupied) return sitPoint;
        }

        return null;
    }

    public void RemoveNpcFromScene(Employee employee)
    {
        var activeEmployee = _activeEmployees.Find(e => e != null && e.so == employee.so);
        if (activeEmployee != null)
        {
            _activeEmployees.Remove(activeEmployee); // 리스트에서 제거
            Destroy(activeEmployee.gameObject); // 씬에서 파괴
            Debug.Log($"[GameManager] {employee.so.Name} 직원 씬에서 즉시 삭제 완료");
        }
    }

}

