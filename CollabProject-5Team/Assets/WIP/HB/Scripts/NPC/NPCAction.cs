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
        npc.transform.rotation = targetTransform.rotation;

        switch (_pointType)
        {
            case PointType.Desk: npc.Anim.SetTrigger("Sit"); break;
            case PointType.Sofa: npc.Anim.SetFloat("RestIndex", Random.Range(0, 4));
                                 npc.Anim.SetTrigger("Rest"); break;
            case PointType.CopyMachine: npc.Anim.SetTrigger("Fax"); break;
            case PointType.Drink: npc.Anim.SetTrigger("Drink"); break;
            case PointType.ServerRoom: npc.Anim.SetTrigger("PushButton"); break;
        }

        PerformActionTask(npc).Forget();
    }

    private async UniTaskVoid PerformActionTask(NPCController npc)
    {
        float stayTime = 0;

        switch (_pointType)
        {
            case PointType.Desk:        stayTime = Random.Range(20.0f, 30.0f); break;
            case PointType.Sofa:        stayTime = Random.Range(5.0f, 8.0f); break;
            case PointType.CopyMachine: stayTime = Random.Range(20.0f, 20.0f); break;
            case PointType.Drink:       stayTime = Random.Range(10.0f, 10.0f); break;
            case PointType.ServerRoom:  stayTime = Random.Range(10.0f, 15.0f); break;
        }

        try
        {
            Debug.Log($"[디버그] {npc.name} 업무 시작: {_pointType}");
            await UniTask.Delay((int)(stayTime * 1000), cancellationToken: npc.Cts.Token);
            Debug.Log($"[디버그] {npc.name} 업무 완료됨");
        }

        catch (System.OperationCanceledException)
        {
            Debug.Log($"[디버그] {npc.name} 업무가 퇴근 명령으로 인해 중단됨!");
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

    public void Update(NPCController npc)
    {

    }

    public void Exit(NPCController npc)
    {
        npc.ReleaseCurrentTarget();
    }
}
