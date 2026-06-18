using UnityEngine;
using UnityEngine.AI;

public class NPCController : MonoBehaviour
{
    [HideInInspector] public NavMeshAgent Agent;
    [HideInInspector] public Animator Anim;

    [Header("의자 프리팹의 SitPoint참조")]
    public Transform TargetDesk;                // NPC의 자리(SitPoint) 프리팹
    private INPCState _currentState;            // 현재 상태

    private void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        Anim = GetComponent<Animator>();    
    }

    public void ChangeState(INPCState newState)
    {
        if (newState is NPCMove)
        {
            if (Agent != null && !Agent.enabled) Agent.enabled = true;
        }
        
        _currentState?.Exit(this);
        _currentState = newState;
        _currentState.Enter(this);
    }

    private void Update()
    {
        _currentState?.Update(this);
    }
}
