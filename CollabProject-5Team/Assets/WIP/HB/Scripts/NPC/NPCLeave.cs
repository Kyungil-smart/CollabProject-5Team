using UnityEngine;

public class NPCLeave : INPCState
{
    public void Enter(NPCController npc)
    {
        npc.ReleaseCurrentTarget();
        
        // 에이전트 활성화 체크
        if (npc.Agent != null && npc.Agent.isOnNavMesh)
        {
            npc.Agent.enabled = true;
            npc.Anim.SetBool("IsWalking", true);

            npc.Agent.SetDestination(GameManager.Instance.currentNpcSpawnPoint.position);
        }
    }

    public void Update(NPCController npc)
    {
        // 에이전트가 꺼져있거나 네브메시를 벗어났다면 종료
        if (npc.Agent == null || !npc.Agent.enabled || !npc.Agent.isOnNavMesh) return;
        
        // 목적지 도착 체크
        if (!npc.Agent.pathPending && npc.Agent.remainingDistance <= 0.5f)
        {
            npc.Anim.SetBool("IsWalking", false);
            
            // 에이전트 끄기
            npc.Agent.enabled = false;

            // 비활성화
            npc.gameObject.SetActive(false);
        }
    }
    public void Exit(NPCController npc)
    {
        
    }
}
