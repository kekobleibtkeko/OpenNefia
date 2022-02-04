using OpenNefia.Content.DisplayName;
using OpenNefia.Core;
using OpenNefia.Core.Game;
using OpenNefia.Core.GameObjects;
using OpenNefia.Core.IoC;
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
    public enum QuestNodeType
    {
        Normal,
        Branch,
        End
    }

    [ImplicitDataDefinitionForInheritors]
    public interface IQuestNode
    {
        public LocaleKey ID { get; }
        public QuestNodeType Type { get; }
        IQuestNode GetNextOrCurrent(PrototypeId<QuestPrototype> questId, QuestProgressComponent progress);
        string GetDescription(PrototypeId<QuestPrototype> questId, QuestProgressComponent progress);
    }

    public class QuestNode : IQuestNode
    {
        [DataField("id")]
        public virtual LocaleKey ID { get; } = LocaleKey.Empty;

        [DataField]
        public List<IQuestFormatData>? FormatData { get; }

        public virtual QuestNodeType Type { get; }

        public virtual IQuestNode GetNextOrCurrent(PrototypeId<QuestPrototype> questId, QuestProgressComponent progress)
        {
            return this;
        }

        public virtual string GetDescription(PrototypeId<QuestPrototype> questId, QuestProgressComponent progress)
        {
            if (FormatData != null)
                return Loc.GetPrototypeString(questId, $"Desc.{ID}", 
                    FormatData.Select((x, ind) => new LocaleArg($"_{ind}", x.GetFormatted(questId, progress))).ToArray());
            return Loc.GetPrototypeString(questId, $"Desc.{ID}");
        }
    }

    [ImplicitDataDefinitionForInheritors]
    public interface IQuestFormatData
    {
        string GetFormatted(PrototypeId<QuestPrototype> questId, QuestProgressComponent progress);
    }

    public abstract class QuestFormatData : IQuestFormatData
    {
        public abstract string GetFormatted(PrototypeId<QuestPrototype> questId, QuestProgressComponent progress);
    }

    public sealed class QuestPlayerNameFormat : QuestFormatData
    {
        [Dependency] private readonly IDisplayNameSystem _nameSys = default!;

        public override string GetFormatted(PrototypeId<QuestPrototype> questId, QuestProgressComponent progress)
        {
            EntitySystem.InjectDependencies(this);
            return _nameSys.GetDisplayName(GameSession.Player);
        }
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
                res = node;
                if (!status.CompletedSteps.Contains(node.ID))
                    break;
            }
            return res.GetNextOrCurrent(questId, progress);
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

        public override QuestNodeType Type => QuestNodeType.Branch;

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

    public sealed class QuestEnd : QuestNode
    {
        public override QuestNodeType Type => QuestNodeType.End;
    }
}
