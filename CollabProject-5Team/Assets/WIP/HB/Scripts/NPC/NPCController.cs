using UnityEngine;
using UnityEngine.AI;
using System.Linq;
using System.Threading;

public class NPCController : MonoBehaviour
{
    [HideInInspector] public NavMeshAgent Agent;
    [HideInInspector] public Animator Anim;

    [Header("의자 프리팹의 SitPoint참조")]
    public IInteractablePoint CurrentTarget;
    private INPCState _currentState;            // 현재 상태
    public ActionPoint MyDesk;                  // 지정석
    public bool IsFirstTask = true;             // 출근하자마자 업무자리로 가기위한 플래그
    public bool IsMoveToRest = false;           // 업무의자에서 일어나면 휴게 공간으로 이동 플래그
    
    public CancellationTokenSource Cts = new CancellationTokenSource();

    private void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        Anim = GetComponent<Animator>();    
    }

    private void Start()
    {
        AssignNewTask();
    }
    

    public void AssignNewTask()
    {
        if (_currentState is NPCLeave) return;

        ReleaseCurrentTarget();
        ActionPoint target = null;

        // 초기 스폰 (무조건 책상)
        if (IsFirstTask)
        {
            target = PointManager.Instance.GetPointsByType(PointType.Desk).FirstOrDefault(p => !p.IsOccupied && p.Owner == null);
            if (target != null) { MyDesk = target; target.Owner = this; }
            IsFirstTask = false;
        }
        
        // 휴식 모드 (책상에서 일어난 직후)
        else if (IsMoveToRest)
        {
            var publicPoints = PointManager.Instance.GetPublicPoints().Where(p => !p.IsOccupied).ToList();
            if (publicPoints.Count > 0)
            {
                target = publicPoints.OrderBy(x => Random.value).FirstOrDefault();
            }
            else
            {
                // 휴식처가 없다면 Idle로 대기
                ChangeState(new NPCIdle());
                return;
            }
        }

        // 평상시 상태 (휴식 모드가 아닐 때만 실행)
        else
        {
            if (MyDesk != null && !MyDesk.IsOccupied && Random.value < 0.7f)
                target = MyDesk;
            else
                target = PointManager.Instance.GetPublicPoints().FirstOrDefault(p => !p.IsOccupied);
        }

        // 최종 할당
        if (target != null)
        {
            CurrentTarget = target;
            ((ActionPoint)target).IsOccupied = true;
            ChangeState(new NPCMove());
        }
        else
        {
            ChangeState(new NPCIdle());
        }
    }

    public void ReleaseCurrentTarget()
    {
        if(CurrentTarget != null)
        {
            ((ActionPoint)CurrentTarget).IsOccupied = false;
            CurrentTarget = null;
        }
    }

    public void ChangeState(INPCState newState)
    {
        Cts.Cancel();
        Cts = new CancellationTokenSource();
        
        if (_currentState is NPCLeave) return;
        
        if (newState is NPCMove)
        {
            if (Agent != null && !Agent.enabled) Agent.enabled = true;
        }
        
        _currentState?.Exit(this);
        _currentState = newState;
        _currentState.Enter(this);
    }

    public INPCState GetCurrentState() 
    {
        return _currentState;
    }

    private void Update()
    {
        _currentState?.Update(this);
    }
}
