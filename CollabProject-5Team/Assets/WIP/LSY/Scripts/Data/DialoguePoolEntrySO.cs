using UnityEngine;

namespace Dialogue
{
    /// <summary> Talk_Dialogue_Pool 테이블 한 행 - 직원·상태별 대화 풀 데이터 </summary>
    [CreateAssetMenu(fileName = "DialoguePoolEntry_", menuName = "Scriptable Objects/Dialogue/DialoguePoolEntrySO")]
    public class DialoguePoolEntrySO : SheetDataSOBase
    {
        public int talkId; // 대화 시작 노드 ID
        public int employeeId; // 직원 ID
        public EmployeeDialogueState empStatusReq; // 출력 조건 상태

        public string branch01Effect; // 선택지 1 선택 시 효과 스크립트 ex) "desire+=10, loyalty+=10"
        public string branch02Effect; // 선택지 2 선택 시 효과 스크립트 ex) "desire-=10, loyalty-=10"

        public override void SetData(string[] data)
        {
            id = ParseInt(data[0]);
            talkId = ParseInt(data[1]);
            employeeId = ParseInt(data[2]);
            empStatusReq = ParseEnum<EmployeeDialogueState>(data[3].Trim());
            branch01Effect = data[4].Trim();
            branch02Effect = data[5].Trim();
        }
    }
}