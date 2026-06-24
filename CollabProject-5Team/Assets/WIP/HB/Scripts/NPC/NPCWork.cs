using UnityEngine;

public class NPCWork : INPCState
{
    public void Enter(NPCController npc)
    {
        // MyDesk가 없으면 복귀
        if (npc.MyDesk == null)
        {
            npc.ChangeState(new NPCIdle());
            return;
        }

        npc.Agent.enabled = false;

        npc.transform.position = npc.MyDesk.transform.position;
        npc.transform.rotation = npc.MyDesk.transform.rotation;
        
        npc.Anim.SetTrigger("Sit");
    }

    public void Exit(NPCController npc)
    {
        // 책상에서 일어날 때 휴식 모드 활성화
        npc.IsMoveToRest = true; 
    }

    public void Update(NPCController npc)
    {
        
    }
}
