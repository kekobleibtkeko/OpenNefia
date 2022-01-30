using OpenNefia.Core.Game;
using OpenNefia.Core.GameObjects;
using OpenNefia.Core.IoC;
using OpenNefia.Core.Log;
using OpenNefia.Core.Prototypes;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenNefia.Content.Quest
{
    public enum QuestProgressResultType
    {
        Progressed,
        NoChange,
        Completed,
    }

    public enum GetQuestNodeType
    {
        Failed,
        Succeeded,
        Branch,
        End
    }

    public interface IQuestSystem : IEntitySystem
    {
        GetQuestNodeType TryGetCurrentNode(PrototypeId<QuestPrototype> id, out IQuestNode? node);
        QuestProgressResultType ProgressQuest(PrototypeId<QuestPrototype> id, string? branch = null);
        bool FailQuest(PrototypeId<QuestPrototype> id);
    }

    public sealed class QuestSystem : EntitySystem, IQuestSystem
    {
        [Dependency] private readonly IPrototypeManager _protos = default!;
        [Dependency] private readonly IEntityManager _entMan = default!;

        public override void Initialize()
        {
        }

        public GetQuestNodeType TryGetCurrentNode(PrototypeId<QuestPrototype> id, out IQuestNode? node)
        {
            node = null;
            var progress = _entMan.EnsureComponent<QuestProgressComponent>(GameSession.Player);

            if (!_protos.TryIndex(id, out var questProto))
                return GetQuestNodeType.Failed;

            if (!progress.QuestProgress.TryGetValue(id, out var status))
            {
                status = new QuestStatus();
                progress.QuestProgress[id] = status;
            }

            node = questProto.Node.GetNextOrCurrent(id, progress);
            for(int i = 0; i < status.CompletedSteps.Count; i++)
            {
                var next = node;
                if (status.CompletedSteps.Contains(node.ID))
                    next = node.GetNextOrCurrent(id, progress);
                if (next == node)
                {
                    return node.IsBranch ? GetQuestNodeType.Branch : GetQuestNodeType.End;
                }
                node = next;
            }
            return GetQuestNodeType.Succeeded;
        }

        public QuestProgressResultType ProgressQuest(PrototypeId<QuestPrototype> id, string? branch = null)
        {
            var progress = _entMan.EnsureComponent<QuestProgressComponent>(GameSession.Player);

            var nodeStatus = TryGetCurrentNode(id, out var current);
            if (current == null)
                return QuestProgressResultType.NoChange;

            var status = progress.QuestProgress[id];
            if (status.Status == QuestStatusType.Unstarted)
                status.Status = QuestStatusType.Started;

            switch (nodeStatus)
            {
                case GetQuestNodeType.Failed:
                    Logger.WarningS("quest", $"Unable to get current node for quest {id}");
                    return QuestProgressResultType.NoChange;
                case GetQuestNodeType.End:

                    status.Status = QuestStatusType.Completed;

                    return QuestProgressResultType.Completed;
                case GetQuestNodeType.Branch:
                    if (string.IsNullOrEmpty(branch))
                    {
                        Logger.WarningS("quest", $"Current node has branches, but no branch was defined for quest {id}");
                        return QuestProgressResultType.NoChange;
                    }
                    else
                    {
                        status.CompletedSteps.Add(branch);
                        return QuestProgressResultType.Progressed;
                    }
                default:
                    var next = current.GetNextOrCurrent(id, progress);
                    status.CompletedSteps.Add(next.ID);
                    return QuestProgressResultType.Progressed;
            }
        }

        public bool FailQuest(PrototypeId<QuestPrototype> id)
        {
            throw new NotImplementedException();
        }
    }
}
