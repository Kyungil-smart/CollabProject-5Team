using UnityEngine;

public class NPCMove : INPCState
{
    public void Enter(NPCController npc)
    {
        // 에이전트 활성화
        npc.Agent.enabled = true;
        // 이동
        npc.Agent.SetDestination(npc.TargetDesk.position);
        // 애니메이션 실행
        npc.Anim.SetBool("IsWalking", true);
    }

    public void Update(NPCController npc)
    {
        // 도착지점에 가까이 도착하면 업무상태로 변경
        if (!npc.Agent.pathPending && npc.Agent.remainingDistance <= 0.5f)
        {
            npc.ChangeState(new NPCWork());
        }
    }

    public void Exit(NPCController npc)
    {
        // 이동이 끝나면 걷는 애니메이션 꺼주기
        npc.Anim.SetBool("IsWalking", false);
    }
}
