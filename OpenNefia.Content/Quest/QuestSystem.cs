using OpenNefia.Core.Game;
using OpenNefia.Core.GameObjects;
using OpenNefia.Core.IoC;
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
    }

    public interface IQuestSystem : IEntitySystem
    {
        bool TryGetCurrentNode(PrototypeId<QuestPrototype> id, out IQuestNode? node);
        QuestProgressResultType ProgressQuest(PrototypeId<QuestPrototype> id);
        bool FailQuest(PrototypeId<QuestPrototype> id);
    }

    public sealed class QuestSystem : EntitySystem, IQuestSystem
    {
        [Dependency] private readonly IPrototypeManager _protos = default!;
        [Dependency] private readonly IEntityManager _entMan = default!;

        public override void Initialize()
        {
        }

        public bool TryGetCurrentNode(PrototypeId<QuestPrototype> id, [NotNullWhen(true)] out IQuestNode? node)
        {
            node = null;
            if (!_entMan.TryGetComponent(GameSession.Player, out QuestProgressComponent progress))
                return false;

            if (!_protos.TryIndex(id, out var questProto))
                return false;

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
                    break;
                node = next;
            }
            return true;
        }

        public QuestProgressResultType ProgressQuest(PrototypeId<QuestPrototype> id)
        {
            throw new NotImplementedException();
        }

        public bool FailQuest(PrototypeId<QuestPrototype> id)
        {
            throw new NotImplementedException();
        }
    }
}
