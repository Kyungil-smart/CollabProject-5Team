using UnityEngine;

public class NPCLeave : INPCState
{
    public void Enter(NPCController npc)
    {
        // 에이전트 활성화 체크
        if (npc.Agent != null)
        {
            npc.Agent.enabled = true;
            npc.Anim.SetBool("IsWalking", true);

            npc.Agent.SetDestination(GameManager.Instance.currentNpcSpawnPoint.position);
        }
    }

    public void Update(NPCController npc)
    {
        // 에이전트가 꺼져있으면 종료
        if (npc.Agent == null || !npc.Agent.enabled) return;

        if (npc.Agent.pathPending) return;
        
        // 목적지 도착 체크
        if (!npc.Agent.pathPending && npc.Agent.remainingDistance <= 0.5f)
        {
            Debug.Log($"[DEBUG] {npc.name}이 NPCLeave 상태로 목적지에 도착하여 SetActive(false)를 호출합니다.");

            npc.Anim.SetBool("IsWalking", false);
            
            // 에이전트 끄기
            npc.Agent.enabled = false;

            npc.gameObject.SetActive(false);
        }
    }
    public void Exit(NPCController npc)
    {
        
    }
}
