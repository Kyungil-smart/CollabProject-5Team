using UnityEngine;

public class NPCLeave : INPCState
{
    public void Enter(NPCController npc)
    {
        npc.Agent.enabled = true;
        npc.Anim.SetBool("IsWalking", true);

        npc.Agent.SetDestination(GameManager.Instance.currentNpcSpawnPoint.position);
    }

    public void Update(NPCController npc)
    {
        if (npc.Agent == null || !npc.Agent.enabled) return;
        
        if (!npc.Agent.pathPending && npc.Agent.remainingDistance <= 0.5f)
        {
            npc.Anim.SetBool("IsWalking", false);
            
            npc.gameObject.SetActive(false);

            npc.Agent.enabled = false;
        }
    }
    public void Exit(NPCController npc)
    {
        
    }
}
