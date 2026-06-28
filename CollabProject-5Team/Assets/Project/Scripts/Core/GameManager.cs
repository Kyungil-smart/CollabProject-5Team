using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

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

    private List<Employee>     _activeEmployees = new List<Employee>();        // 활성화된 직원 NPC를 담아둘 리스트
    public bool isUpgradeReserved = false;                                   // 맵 증축 저장용
    int _spawnDelayMs = 850;
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
        InitializeGameAsync().Forget();
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

        // 플레이어가 생성되고 대기
        await UniTask.Delay(_spawnDelayMs);

        // 의자 정보
        RefreshSitPoints();

        // NPC 생성
        foreach (var emp in _EmployeeManager.Instance.haveEmployees.haveEmployeeList)
        {
            await SpawnNPCsAsync(emp);
        }
    }

    public void RefreshSitPoints()
    {
        // 현재 맵에서 포인트들을 찾음
        var foundPoints = _currentMapTransform.GetComponentsInChildren<ActionPoint>().ToList();

        // PointManager에게 전달 (갱신 요청)        
        PointManager.Instance.RefreshPoints(_currentMapTransform);
        

        Debug.Log($"[GameManager] 찾은 포인트 개수: {foundPoints.Count}");
    }

    public async UniTask SpawnNPCsAsync(Employee emp)
    {
        var controller = emp.GetComponent<NPCController>();

        if (!_activeEmployees.Contains(emp))
            _activeEmployees.Add(emp);

        // 빈자리 할당
        var target = PointManager.Instance.GetAllPoints().FirstOrDefault(p => !p.IsOccupied);
        if (target != null)
        {
            target.IsOccupied = true;
            controller.CurrentTarget = target;
        }

        emp.transform.position = _currentNpcSpawnPoint.position;
        emp.transform.rotation = Quaternion.identity;
        emp.gameObject.SetActive(true);
        
        if (controller.CurrentTarget != null)
            controller.ChangeState(new NPCMove());
        
        else
            controller.ChangeState(new NPCIdle());

        await UniTask.Delay(_spawnDelayMs);
    }

    // 월요일 아침에 호출
    public async UniTask TryProcessUpgradeAsync()
    {
        if (!isUpgradeReserved) return;

        await UpgradeOfficeAsync();
        isUpgradeReserved = false;    
    }

    public async UniTask UpgradeOfficeAsync()
    {
        // 기존 NPC 자리 해제, 상태 초기화
        foreach (var employee in _activeEmployees)
        {
            if (employee == null) continue;

            var npc = employee.GetComponent<NPCController>();
            // 자리 점유 해제
            npc.ReleaseCurrentTarget(); 
            npc.ChangeState(new NPCIdle());
        }

        if (_currentOfficeIndex + 1 >= _offices.Count) return;

        LeaveWorkNPCs();
        await UniTask.Yield();

        // 맵 교체
        if (_currentMapTransform != null)
        {
            DestroyImmediate(_currentMapTransform.gameObject);

            _activeEmployees.RemoveAll(e => e == null || e.gameObject == null);
        }

        await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

        _currentOfficeIndex++;

        MapInfo newOffice = Instantiate(_offices[_currentOfficeIndex], Vector3.zero, Quaternion.identity);
        _currentMapTransform = newOffice.transform;

        // 잠시 대기 후 카메라 조정
        await UniTask.Yield();

        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.MapSettings(newOffice);
        }

        // 포인트, 스폰 정보 갱신
        PointManager.Instance.RefreshPoints(_currentMapTransform);

        
        RefreshSitPoints();
        _currentNpcSpawnPoint = newOffice.NpcSpawn;

        // NPC재배치, 새로운 자리 할당
        foreach (var employee in _activeEmployees)
        {
            var npc = employee.GetComponent<NPCController>();
            // 위치 초기하
            npc.transform.position = _currentNpcSpawnPoint.position;

            var target = PointManager.Instance.GetAllPoints().FirstOrDefault(p => !p.IsOccupied);

            if (target != null)
            {
                target.IsOccupied = true;
                npc.CurrentTarget = target;
                npc.ChangeState(new NPCMove());
            }

            else
            {
                npc.ChangeState(new NPCIdle());
            }
        }
    }

    public Transform GetRandomActiveNpcTransform()
    {
        Employee employee = GetRandomActiveEmployee();
        return employee != null ? employee.transform : null;
    }
    public Employee GetRandomActiveEmployee()
    {
        _activeEmployees.RemoveAll(e => e == null || e.gameObject == null);

        if (_activeEmployees.Count == 0) return null;
        return _activeEmployees[Random.Range(0, _activeEmployees.Count)];
    }
    public Employee GetActiveEmployee(int employeeId)
    {
        if (employeeId == 0) return null;

        _activeEmployees.RemoveAll(e => e == null || e.gameObject == null);
        return _activeEmployees.Find(e => e != null && e.so.id == employeeId);
    }

    // 퇴근 명령 SpawnPoint로 이동 후 비활성화
    public void LeaveWorkNPCs()
    {        
        foreach (var emp in _activeEmployees)
        {
            if (emp == null) continue;

            var npc = emp.GetComponent<NPCController>();

            npc.ChangeState(new NPCLeave());
        }
    }

    public async UniTask HiredNPCGoToWork()
    {
        _activeEmployees.RemoveAll(e => e == null || e.gameObject == null);

        var hiredEmployees = _EmployeeManager.Instance.haveEmployees.haveEmployeeList;

        // 해고된 직원 처리
        for (int i = _activeEmployees.Count - 1; i >= 0; i--)
        {
            var activeEmployee = _activeEmployees[i];
            if (!hiredEmployees.Exists(e => e.so == activeEmployee.so))
            {
                var npc = activeEmployee.GetComponent<NPCController>();
                
                // 자리 점유 해제
                npc.ReleaseCurrentTarget(); 
                Destroy(activeEmployee.gameObject);
                _activeEmployees.RemoveAt(i);
            }
        }

        // 고용된 직원 처리
        foreach (var employee in hiredEmployees)
        {
            var activeEmployee = _activeEmployees.Find(e => e != null && e.so == employee.so);

            if (activeEmployee != null)
            {
                var npc = activeEmployee.GetComponent<NPCController>();

                npc.gameObject.SetActive(true);

                // 상태 초기화
                npc.IsFirstTask = true;
                npc.ReleaseCurrentTarget();


                if (npc.Agent != null)
                {
                    npc.Agent.enabled = false;
                    npc.Agent.Warp(npc.transform.position);
                    npc.Agent.enabled = true;
                }

                npc.AssignNewTask();
                await UniTask.Delay(_spawnDelayMs);
            }
            else
            {
                // 새로 생성
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

    public void RemoveNpcFromScene(Employee employee)
    {
        var activeEmployee = _activeEmployees.Find(e => e != null && e.so == employee.so);
        if (activeEmployee != null)
        {
            var npc = activeEmployee.GetComponent<NPCController>();

            if (npc.MyDesk != null)
            {
                npc.MyDesk.Owner = null;
                npc.MyDesk.IsOccupied = false;
            }

            npc.ReleaseCurrentTarget();

            // 리스트에서 제거
            _activeEmployees.Remove(activeEmployee); 
            // 씬에서 파괴
            Destroy(activeEmployee.gameObject); 
            Debug.Log($"[GameManager] {employee.so.Name} 직원 씬에서 즉시 삭제 완료");
        }
    }
}

