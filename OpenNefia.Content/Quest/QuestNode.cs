using OpenNefia.Core;
using OpenNefia.Core.Locale;
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
        public LocaleKey ID { get; }
        public bool IsBranch { get; }
        IQuestNode GetNextOrCurrent(PrototypeId<QuestPrototype> questId, QuestProgressComponent progress);
        string GetDescription(PrototypeId<QuestPrototype> questId, QuestProgressComponent progress);
    }

    public abstract class QuestNode : IQuestNode
    {
        [DataField("id", required: true)]
        public virtual LocaleKey ID { get; } = default!;

        [DataField]
        public List<IQuestFormatData>? Format { get; }

        public virtual bool IsBranch { get; }

        public abstract IQuestNode GetNextOrCurrent(PrototypeId<QuestPrototype> questId, QuestProgressComponent progress);

        public virtual string GetDescription(PrototypeId<QuestPrototype> questId, QuestProgressComponent progress)
        {
            if (Format != null)
                return Loc.GetPrototypeString(questId, $"Desc.{ID}", 
                    Format.Select((x, ind) => new LocaleArg($"_{ind}", x.GetFormatted(questId, progress))).ToArray());
            return Loc.GetPrototypeString(questId, $"Desc.{ID}");
        }
    }

    public interface IQuestFormatData
    {
        string GetFormatted(PrototypeId<QuestPrototype> questId, QuestProgressComponent progress);
    }

    public abstract class QuestFormatData : IQuestFormatData
    {
        public abstract string GetFormatted(PrototypeId<QuestPrototype> questId, QuestProgressComponent progress);
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
                var resNext = res.GetNextOrCurrent(questId, progress);
                if (resNext != res)
                    return resNext;
            }
            return res;
        }

        public override string GetDescription(PrototypeId<QuestPrototype> questId, QuestProgressComponent progress)
        {
            return GetNextOrCurrent(questId, progress).GetDescription(questId, progress);
        }
    }

    public sealed class QuestBranchNode : QuestNode
    {
        [DataField(required: true)]
        public List<IQuestNode> Nodes { get; } = new();

        public override bool IsBranch => true;

        public override IQuestNode GetNextOrCurrent(PrototypeId<QuestPrototype> questId, QuestProgressComponent progress)
        {
            var status = progress.QuestProgress[questId];
            var node = Nodes.FirstOrDefault(x => status.CompletedSteps.Contains(x.ID));
            return node?.GetNextOrCurrent(questId, progress) ?? this;
        }

        public override string GetDescription(PrototypeId<QuestPrototype> questId, QuestProgressComponent progress)
        {
            return GetNextOrCurrent(questId, progress).GetDescription(questId, progress);
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

        public override string GetDescription(PrototypeId<QuestPrototype> questId, QuestProgressComponent progress)
        {
            return GetNextOrCurrent(questId, progress).GetDescription(questId, progress);
        }
    }

    public sealed class QuestBasicNode : QuestNode
    {
        public override IQuestNode GetNextOrCurrent(PrototypeId<QuestPrototype> questId, QuestProgressComponent progress)
        {
            return this;
        }
    }
}
