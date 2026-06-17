using UnityEngine;

public class NPCIdle : INPCState
{
    public void Enter(NPCController npc)
    {
        npc.Agent.enabled = false;
        npc.Anim.SetBool("IsWalking", false);
    }

    public void Update(NPCController npc)
    {
        
    }

    public void Exit(NPCController npc)
    {
        
    }
}
