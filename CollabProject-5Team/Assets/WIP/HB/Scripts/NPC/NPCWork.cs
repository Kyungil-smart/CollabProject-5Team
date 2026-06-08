using UnityEngine;

public class NPCWork : INPCState
{
    public void Enter(NPCController npc)
    {
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
