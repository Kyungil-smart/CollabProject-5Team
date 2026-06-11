using R3;
using UnityEngine;

public class QuestResultSpawner : MonoBehaviour
{
    [SerializeField] private SpeechBubble dialogueBubble; // 퀘스트 성공 시 주변 직원 대사 출력용 말풍선

    private void Start()
    {
        QuestManager.Instance.dailyQuestState.Subscribe(OnQuestStateChanged).AddTo(this);
    }

    private void OnQuestStateChanged(QuestState state)
    {
        if (state != QuestState.End) return;

        DailyQuest quest = QuestManager.Instance.curDailyQuest;
        if (quest.result != QuestResult.Success) return;

        foreach (string objName in quest.so.resultObjects.Split(','))
        {
            string trimmed = objName.Trim();
            if (trimmed == "" || trimmed == "None") continue;

            Transform target = FindDeepChild(transform, trimmed);
            if (target != null) target.gameObject.SetActive(true);
        }

        dialogueBubble?.Show(quest.so.npcDialogue);
    }

    private Transform FindDeepChild(Transform root, string name)
    {
        foreach (Transform child in root)
        {
            if (child.name == name) return child;

            Transform found = FindDeepChild(child, name);
            if (found != null) return found;
        }
        return null;
    }
}