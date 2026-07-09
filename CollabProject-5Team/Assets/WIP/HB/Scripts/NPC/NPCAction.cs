using UnityEngine;
using Cysharp.Threading.Tasks;

public class NPCAction : INPCState
{
    private PointType _pointType;
    public NPCAction(PointType pointType) => _pointType = pointType;

    public void Enter(NPCController npc)
    {
        var targetTransform = npc.CurrentTarget.GetTransform();

        // 지터링 방지를 위해 에이전트 끄기
        npc.Agent.enabled = false;

        npc.transform.position = targetTransform.position;
        if (!npc.IsInteracting)
        {
            npc.transform.rotation = targetTransform.rotation;
        }

        ResetTargetPosition(npc);

        switch (_pointType)
        {
            case PointType.Desk: npc.Anim.SetTrigger("Sit"); break;
            case PointType.Sofa: npc.Anim.SetFloat("RestIndex", Random.Range(0, 4));
                                 npc.Anim.SetTrigger("Rest"); break;
            case PointType.CopyMachine: npc.Anim.SetTrigger("Fax"); break;
            case PointType.Drink: npc.Anim.SetTrigger("Drink"); break;
            case PointType.ServerRoom: npc.Anim.SetTrigger("PushButton"); break;
            case PointType.Look: npc.Anim.SetTrigger("Look"); break;
            case PointType.Make: npc.Anim.SetTrigger("Make"); break;
            case PointType.Find: npc.Anim.SetTrigger("Find"); break;
        }

        PerformActionTask(npc).Forget();
    }

    private async UniTaskVoid PerformActionTask(NPCController npc)
    {
        float stayTime = 0;

        switch (_pointType)
        {
            case PointType.Desk:        stayTime = Random.Range(20.0f, 30.0f); break;
            case PointType.Sofa:        stayTime = Random.Range(5.0f, 10.0f); break;
            case PointType.CopyMachine: stayTime = Random.Range(20.0f, 20.0f); break;
            case PointType.Drink:       stayTime = Random.Range(3.0f, 4.0f); break;
            case PointType.ServerRoom:  stayTime = Random.Range(4.0f, 5.0f); break;
            case PointType.Look:        stayTime = Random.Range(4.0f, 5.0f); break;
            case PointType.Make:        stayTime = Random.Range(3.0f, 4.0f); break;
            case PointType.Find:        stayTime = Random.Range(18.0f, 18.0f); break;
        }

        try
        {
            while (stayTime > 0)
            {
                if (!npc.IsInteracting) 
                {
                    // 1초씩 카운트다운
                    await UniTask.Delay(1000, cancellationToken: npc.Cts.Token);
                    stayTime -= 1.0f;
                }
                else
                {
                    // 대화 중이면 잠시 대기
                    await UniTask.Yield(PlayerLoopTiming.Update, npc.Cts.Token);
                }
            }
        }

        catch (System.OperationCanceledException)
        {
            return;
        }

        npc.ReleaseCurrentTarget();

        if (_pointType == PointType.Desk)
        {
            npc.IsMoveToRest = true;
        }

        // 휴식 공간(소파 등)에서 일어났을 때는 휴식 모드를 끔
        else
        {
            npc.IsMoveToRest = false;
        }

        npc.AssignNewTask();
        
    }

    public void ResetTargetPosition(NPCController npc)
    {
        var targetTransform = npc.CurrentTarget.GetTransform();
        npc.Agent.enabled = false;
        npc.transform.position = targetTransform.position;
        npc.transform.rotation = targetTransform.rotation;
    }

    public void Update(NPCController npc)
    {

    }

    public void Exit(NPCController npc)
    {
        npc.ReleaseCurrentTarget();
    }
}
