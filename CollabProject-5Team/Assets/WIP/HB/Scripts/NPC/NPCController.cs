using UnityEngine;
using UnityEngine.AI;
using System.Linq;
using System.Threading;

public class NPCController : MonoBehaviour
{
    [HideInInspector] public NavMeshAgent Agent;
    [HideInInspector] public Animator Anim;

    [Header("오브젝트의 SitPoint or Point 참조")]
    public IInteractablePoint CurrentTarget;
    private INPCState _currentState;            // 현재 상태
    public ActionPoint MyDesk;                  // 지정석
    public bool IsFirstTask = true;             // 출근하자마자 업무자리로 가기위한 플래그
    public bool IsMoveToRest = false;           // 업무의자에서 일어나면 휴게 공간으로 이동 플래그
    public bool IsInteracting = false;          // 대화 중 상태 플래그
    private IInteractablePoint _myTargetPoint;  // 목적지 저장용

    public CancellationTokenSource Cts = new CancellationTokenSource();

    private void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        Anim = GetComponent<Animator>();    
    }    

    public void AssignNewTask()
    {
        if (IsInteracting) return;

        ReleaseCurrentTarget();

        if (_currentState is NPCLeave)
        {
            _currentState.Exit(this);
            _currentState = null;
        }

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
        if (IsInteracting) return;

        if(CurrentTarget != null)
        {
            ((ActionPoint)CurrentTarget).IsOccupied = false;
            CurrentTarget = null;
        }
    }

    public void ChangeState(INPCState newState)
    {
        if (_currentState is NPCLeave && newState is not NPCLeave)
        {
            return;
        } 

        Cts.Cancel();
        Cts = new CancellationTokenSource();
        
        if (newState is NPCMove)
        {
            if (Agent != null && !Agent.enabled) Agent.enabled = true;
        }
        
        _currentState?.Exit(this);
        _currentState = newState;
        _currentState.Enter(this);
    }

    public void StartConversation()
    {
        if (IsInteracting) return;
        IsInteracting = true;

        if (Agent != null && Agent.enabled && Agent.isOnNavMesh)
        {
            Agent.ResetPath();
            Agent.enabled = false;
        }

        if (_currentState is NPCMove)
        {
            ChangeState(new NPCIdle());
        }
    }

    public void EndConversation()
    {
        IsInteracting = false;

        if (Agent != null)
        {
            Agent.enabled = true;
        }

        if (_myTargetPoint != null && _myTargetPoint.GetTransform() != null)
        {
            Agent.ResetPath();
            Agent.SetDestination(_myTargetPoint.GetTransform().position);
        }

        else
        {
            AssignNewTask();
        }
    }

    public void RestoreActionAnimation()
    {
        if (Anim == null || CurrentTarget == null) return;

        transform.rotation = CurrentTarget.GetTransform().rotation;

        // 현재 업무 상태에 맞는 파라미터를 다시 세팅
        switch (CurrentTarget.GetPointType())
        {
            case PointType.Desk: Anim.SetTrigger("Sit"); break;
            case PointType.Sofa: Anim.SetTrigger("Rest"); break;
            case PointType.CopyMachine: Anim.SetTrigger("Fax"); break;
            case PointType.Drink: Anim.SetTrigger("Drink"); break;
            case PointType.ServerRoom: Anim.SetTrigger("PushButton"); break;
        }
    }

    public void SetTargetPoint(IInteractablePoint point) => _myTargetPoint = point;

    public INPCState GetCurrentState() 
    {
        return _currentState;
    }

    private void Update()
    {
        _currentState?.Update(this);
    }
}
