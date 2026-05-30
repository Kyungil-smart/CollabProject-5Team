using UnityEngine;

namespace Dialogue
{
    /// <summary> Talk_Dialogue 테이블 한 행 - 대화 노드 데이터 </summary>
    [CreateAssetMenu(fileName = "DialogueNode_", menuName = "Scriptable Objects/Dialogue/DialogueNodeSO")]
    public class DialogueNodeSO : SheetDataSOBase
    {
        public int nextId; // 선택지 없을 때 자동 이동할 ID (0이면 종료)
        public bool isChoice; // 선택지 출력 단계 여부
        [TextArea(2, 5)]
        public string text; // 대사 텍스트

        [TextArea(1, 3)]
        public string choice01; // 선택지 1 텍스트
        public int nextId01; // 선택지 1 선택 시 이동할 ID

        [TextArea(1, 3)]
        public string choice02; // 선택지 2 텍스트
        public int nextId02; // 선택지 2 선택 시 이동할 ID

        public override void SetData(string[] data)
        {
            id = ParseInt(data[0]);
            nextId = ParseInt(data[1]);
            isChoice = ParseBool(data[2]);
            text = data[3].Trim();
            choice01 = data[4].Trim();
            nextId01 = ParseInt(data[5]);
            choice02 = data[6].Trim();
            nextId02 = ParseInt(data[7]);
        }
    }
}