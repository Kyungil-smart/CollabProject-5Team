using UnityEngine;

public class NPCLeave : INPCState
{
    public void Enter(NPCController npc)
    {
        npc.Agent.enabled = true;
        npc.Anim.SetBool("IsWalking", true);

        npc.Agent.SetDestination(GameManager.Instance.NpcSpawnPoint.position);
    }

    public void Update(NPCController npc)
    {
        if (!npc.Agent.pathPending && npc.Agent.remainingDistance <= 0.5f)
        {
            npc.Anim.SetBool("IsWalking", false);
            
            Object.Destroy(npc.gameObject);
        }
    }
    public void Exit(NPCController npc)
    {
        
    }
}
