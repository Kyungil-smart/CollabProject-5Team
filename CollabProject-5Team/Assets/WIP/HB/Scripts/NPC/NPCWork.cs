using UnityEngine;

public class NPCWork : INPCState
{
    public void Enter(NPCController npc)
    {
        if (npc.TargetDesk == null || npc.TargetDesk.gameObject == null)
        {
            Debug.LogWarning($"{npc.name}의 TargetDesk가 유효하지 않습니다. 재할당 필요.");
            npc.TargetDesk = null;
            npc.ChangeState(new NPCIdle()); // 다시 Idle로 보내서 재할당 대기
            return;
        }

        // 에이전트 끄기, 지터링 방지
        npc.Agent.enabled = false;

        npc.transform.position = npc.TargetDesk.position;
        npc.transform.rotation = npc.TargetDesk.rotation;
        
        npc.Anim.SetTrigger("Sit");
    }

    public void Exit(NPCController npc)
    {

    }

    public void Update(NPCController npc)
    {

    }
}
