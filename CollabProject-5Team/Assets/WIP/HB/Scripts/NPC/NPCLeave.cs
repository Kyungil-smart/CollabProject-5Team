using UnityEngine;
using UnityEngine.AI;

public class NPCLeave : INPCState
{
    public void Enter(NPCController npc)
    {
        if (npc.Agent == null) return;

        npc.Agent.enabled = true;

        // NavMesh 재동기화가 아직 안 됐을 수 있으므로 확인
        if (!npc.Agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(npc.transform.position, out var hit, 5f, NavMesh.AllAreas))
            {
                npc.Agent.Warp(hit.position);
            }
        }

        if (npc.Agent.isOnNavMesh)
        {
            npc.Anim.SetBool("IsWalking", true);
            npc.Agent.SetDestination(GameManager.Instance.currentNpcSpawnPoint.position);
        }
        
        else
        {
            // NavMesh에 배치가 안 되면 바로 스폰 지점으로 이동시키고 비활성화
            npc.transform.position = GameManager.Instance.currentNpcSpawnPoint.position;
            npc.Anim.SetBool("IsWalking", false);
            npc.Agent.enabled = false;
            npc.gameObject.SetActive(false);
        }
    }

    public void Update(NPCController npc)
    {
        if (npc.Agent == null || !npc.Agent.enabled) return;
        if (npc.Agent.pathPending) return;

        if (!npc.Agent.pathPending && npc.Agent.remainingDistance <= 0.5f)
        {
            npc.Anim.SetBool("IsWalking", false);
            npc.Agent.enabled = false;
            npc.gameObject.SetActive(false);
        }
    }
    public void Exit(NPCController npc)
    {
        
    }
}
