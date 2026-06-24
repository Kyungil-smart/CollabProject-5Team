using UnityEngine;

public class NPCIdle : INPCState
{
    private float _timer = 0f;

    public void Enter(NPCController npc)
    {
        npc.Agent.enabled = false;
        npc.Anim.SetBool("IsWalking", false);
        _timer = 0f;
    }

    public void Update(NPCController npc)
    {
        _timer += Time.deltaTime;

        if (_timer >= 1.5f)
        {
            _timer = 0f;
            Debug.Log($"{npc.name}이(가) Idle 상태에서 재탐색 시도...");
            npc.AssignNewTask();
        }
    }

    public void Exit(NPCController npc)
    {
        
    }
}
