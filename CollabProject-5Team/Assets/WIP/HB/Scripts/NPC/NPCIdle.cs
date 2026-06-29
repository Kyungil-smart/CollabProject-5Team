using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;

public class NPCIdle : INPCState
{
    private CancellationTokenSource _cts;

    public void Enter(NPCController npc)
    {
        npc.Agent.enabled = false;
        npc.Anim.SetBool("IsWalking", false);
        
        _cts = new CancellationTokenSource();

        IdleLogicAsync(npc, _cts.Token).Forget();
    }

    private async UniTaskVoid IdleLogicAsync(NPCController npc, CancellationToken ct)
    {
        // try-catch를 통해 캔슬 시 발생하는 예외를 안전하게 처리
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await UniTask.Delay(1500, cancellationToken: ct);

                if (npc.IsInteracting) continue;

                npc.AssignNewTask();
            }
        }
        catch (System.OperationCanceledException)
        {
            // 작업이 취소될 때 (Exit 호출 시) 이 블록으로 들어옴
        }
    }

    public void Update(NPCController npc)
    {
        
    }

    public void Exit(NPCController npc)
    {
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
    }
}
