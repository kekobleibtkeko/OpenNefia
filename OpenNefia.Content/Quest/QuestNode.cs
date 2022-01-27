using OpenNefia.Core.Prototypes;
using OpenNefia.Core.Serialization.Manager.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenNefia.Content.Quest
{
    [ImplicitDataDefinitionForInheritors]
    public interface IQuestNode
    {
        public string ID { get; }
        IQuestNode GetNextOrCurrent(PrototypeId<QuestPrototype> questId, QuestProgressComponent progress);
    }

    public abstract class QuestNode : IQuestNode
    {
        [DataField("id")]
        public virtual string ID { get; } = default!;

        public abstract IQuestNode GetNextOrCurrent(PrototypeId<QuestPrototype> questId, QuestProgressComponent progress);
    }

    public sealed class QuestNodeSequence : QuestNode
    {
        [DataField(required: true)]
        public List<IQuestNode> Nodes { get; } = new();

        public override IQuestNode GetNextOrCurrent(PrototypeId<QuestPrototype> questId, QuestProgressComponent progress)
        {
            var status = progress.QuestProgress[questId];
            var res = Nodes.First();
            foreach (var node in Nodes)
            {
                if (status.CompletedSteps.Contains(node.ID))
                    res = node;
            }
            return res;
        }
    }

    public sealed class QuestBranchNode : QuestNode
    {
        [DataField(required: true)]
        public List<IQuestNode> Nodes { get; } = new();

        public override IQuestNode GetNextOrCurrent(PrototypeId<QuestPrototype> questId, QuestProgressComponent progress)
        {
            var status = progress.QuestProgress[questId];
            var node = Nodes.FirstOrDefault(x => status.CompletedSteps.Contains(x.ID));
            return node?.GetNextOrCurrent(questId, progress) ?? this;
        }
    }

    public sealed class QuestPrototypeNode : QuestNode
    {
        [DataField(required: true)]
        public PrototypeId<QuestNodePrototype> PrototypeID { get; } = default!;
        public override IQuestNode GetNextOrCurrent(PrototypeId<QuestPrototype> questId, QuestProgressComponent progress)
        {
            var proto = PrototypeID.ResolvePrototype();
            return proto.Node.GetNextOrCurrent(questId, progress);
        }
    }
}
