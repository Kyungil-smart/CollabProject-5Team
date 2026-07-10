using System.Collections.Generic;
using System.Linq;

namespace GameDevTycoon.EditorQA
{
    public sealed class QAQuestCommentValidator : IQAValidator
    {
        private const string QuestRoot = "Assets/Project/DB/Quest";
        private const string CommentRoot = "Assets/Project/DB/CommentData";

        private static readonly Role[] CoreRoles =
        {
            Role.PROGRAMMER,
            Role.PLANNER,
            Role.ARTIST
        };

        private static readonly int[] TriggerThresholds = { 0, 50 };

        public string Name => "Quest / Comment Data";

        public IEnumerable<QAResult> Run()
        {
            List<AssetEntry<QuestSO>> quests =
                QAAssetUtility.FindAssetEntriesByType<QuestSO>(QuestRoot);
            List<AssetEntry<EmployeeCommentData>> comments =
                QAAssetUtility.FindAssetEntriesByType<EmployeeCommentData>(CommentRoot);

            foreach (QAResult result in ValidateQuestDuplicateIds(quests))
                yield return result;

            foreach (AssetEntry<QuestSO> entry in quests)
            {
                foreach (QAResult result in ValidateQuest(entry))
                    yield return result;
            }

            foreach (QAResult result in ValidateCommentDuplicateIds(comments))
                yield return result;

            foreach (AssetEntry<EmployeeCommentData> entry in comments)
            {
                foreach (QAResult result in ValidateComment(entry))
                    yield return result;
            }

            foreach (QAResult result in ValidateCommentCoverage(comments))
                yield return result;

            yield return new QAResult(
                QASeverity.Info,
                "Quest",
                $"일일 퀘스트 데이터 {quests.Count}개 검사를 완료했습니다.",
                quests.Count > 0 ? quests[0].Path : QuestRoot,
                quests.Count > 0 ? quests[0].Asset : null);

            yield return new QAResult(
                QASeverity.Info,
                "Comment",
                $"주간 코멘트 데이터 {comments.Count}개 검사를 완료했습니다.",
                comments.Count > 0 ? comments[0].Path : CommentRoot,
                comments.Count > 0 ? comments[0].Asset : null);
        }

        private static IEnumerable<QAResult> ValidateQuestDuplicateIds(List<AssetEntry<QuestSO>> quests)
        {
            foreach (var group in quests.GroupBy(e => e.Asset.id).Where(g => g.Key != 0 && g.Count() > 1))
            {
                AssetEntry<QuestSO> first = group.First();
                string paths = string.Join(", ", group.Select(e => e.Path));
                yield return new QAResult(
                    QASeverity.Error,
                    "Quest",
                    $"퀘스트 ID {group.Key}가 중복됩니다. 대상: {paths}",
                    first.Path,
                    first.Asset);
            }
        }

        private static IEnumerable<QAResult> ValidateQuest(AssetEntry<QuestSO> entry)
        {
            QuestSO quest = entry.Asset;

            if (quest.id <= 0)
                yield return QuestError(entry, "퀘스트 ID가 1 이상이어야 합니다.");

            if (string.IsNullOrWhiteSpace(quest.Name))
                yield return QuestError(entry, "퀘스트 이름이 비어 있습니다.");

            if (!CoreRoles.Contains(quest.role))
                yield return QuestWarning(entry, $"{quest.role} 직군 퀘스트는 현재 핵심 3직군 검증 대상이 아닙니다.");

            if (quest.targetCount <= 0)
                yield return QuestError(entry, $"targetCount는 1 이상이어야 합니다. 현재값: {quest.targetCount}");

            if (string.IsNullOrWhiteSpace(quest.activeObjects) || quest.activeObjects.Trim() == "None")
                yield return QuestError(entry, "activeObjects가 비어 있거나 None입니다. 퀘스트 시작 시 켤 오브젝트가 필요합니다.");

            if (quest.successEffect < 0)
                yield return QuestError(entry, $"successEffect가 음수입니다. 현재값: {quest.successEffect}");

            if (string.IsNullOrWhiteSpace(quest.npcDialogue))
                yield return QuestWarning(entry, "퀘스트 완료 말풍선 텍스트가 비어 있습니다.");

            if (quest.ActiveObjectCount > 0 && quest.controlType == ControlType.TAP && quest.ActiveObjectCount < quest.targetCount)
            {
                yield return QuestWarning(
                    entry,
                    $"TAP 퀘스트의 활성 오브젝트 수({quest.ActiveObjectCount})가 targetCount({quest.targetCount})보다 적습니다. 같은 오브젝트를 여러 번 누르는 의도인지 확인이 필요합니다.");
            }
        }

        private static IEnumerable<QAResult> ValidateCommentDuplicateIds(
            List<AssetEntry<EmployeeCommentData>> comments)
        {
            foreach (var group in comments.GroupBy(e => e.Asset.id).Where(g => g.Key != 0 && g.Count() > 1))
            {
                AssetEntry<EmployeeCommentData> first = group.First();
                string paths = string.Join(", ", group.Select(e => e.Path));
                yield return new QAResult(
                    QASeverity.Error,
                    "Comment",
                    $"코멘트 ID {group.Key}가 중복됩니다. 대상: {paths}",
                    first.Path,
                    first.Asset);
            }
        }

        private static IEnumerable<QAResult> ValidateComment(AssetEntry<EmployeeCommentData> entry)
        {
            EmployeeCommentData comment = entry.Asset;

            if (comment.id <= 0)
                yield return CommentError(entry, "코멘트 ID가 1 이상이어야 합니다.");

            if (!IsKnownTrigger(comment.trigger_desire))
                yield return CommentWarning(entry, $"trigger_desire는 현재 0 또는 50 기준을 권장합니다. 현재값: {comment.trigger_desire}");

            if (!IsKnownTrigger(comment.trigger_fatigue))
                yield return CommentWarning(entry, $"trigger_fatigue는 현재 0 또는 50 기준을 권장합니다. 현재값: {comment.trigger_fatigue}");

            if (!IsKnownTrigger(comment.trigger_loyalty))
                yield return CommentWarning(entry, $"trigger_loyalty는 현재 0 또는 50 기준을 권장합니다. 현재값: {comment.trigger_loyalty}");

            if (!CoreRoles.Contains(comment.target_role))
                yield return CommentWarning(entry, $"{comment.target_role} 직군 코멘트는 현재 핵심 3직군 검증 대상이 아닙니다.");

            if (string.IsNullOrWhiteSpace(comment.comment_text))
                yield return CommentError(entry, "코멘트 본문이 비어 있습니다.");
        }

        private static IEnumerable<QAResult> ValidateCommentCoverage(
            List<AssetEntry<EmployeeCommentData>> comments)
        {
            var lookup = new HashSet<(Role role, int desire, int fatigue, int loyalty)>(
                comments.Select(c => (
                    c.Asset.target_role,
                    c.Asset.trigger_desire,
                    c.Asset.trigger_fatigue,
                    c.Asset.trigger_loyalty)));

            foreach (Role role in CoreRoles)
            {
                foreach (int desire in TriggerThresholds)
                {
                    foreach (int fatigue in TriggerThresholds)
                    {
                        foreach (int loyalty in TriggerThresholds)
                        {
                            if (lookup.Contains((role, desire, fatigue, loyalty)))
                                continue;

                            yield return new QAResult(
                                QASeverity.Warning,
                                "Comment Coverage",
                                $"{role} 코멘트 조합이 비어 있습니다. desire={desire}, fatigue={fatigue}, loyalty={loyalty}",
                                CommentRoot);
                        }
                    }
                }
            }
        }

        private static bool IsKnownTrigger(int value)
        {
            return value == 0 || value == 50;
        }

        private static QAResult QuestError(AssetEntry<QuestSO> entry, string message)
        {
            return new QAResult(QASeverity.Error, "Quest", message, entry.Path, entry.Asset);
        }

        private static QAResult QuestWarning(AssetEntry<QuestSO> entry, string message)
        {
            return new QAResult(QASeverity.Warning, "Quest", message, entry.Path, entry.Asset);
        }

        private static QAResult CommentError(AssetEntry<EmployeeCommentData> entry, string message)
        {
            return new QAResult(QASeverity.Error, "Comment", message, entry.Path, entry.Asset);
        }

        private static QAResult CommentWarning(AssetEntry<EmployeeCommentData> entry, string message)
        {
            return new QAResult(QASeverity.Warning, "Comment", message, entry.Path, entry.Asset);
        }
    }
}
