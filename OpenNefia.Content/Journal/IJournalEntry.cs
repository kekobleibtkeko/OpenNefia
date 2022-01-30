using OpenNefia.Content.Quest;
using OpenNefia.Content.UI;
using OpenNefia.Core;
using OpenNefia.Core.GameObjects;
using OpenNefia.Core.IoC;
using OpenNefia.Core.Locale;
using OpenNefia.Core.Maths;
using OpenNefia.Core.Prototypes;
using OpenNefia.Core.Rendering;
using OpenNefia.Core.Serialization.Manager.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenNefia.Content.Journal
{
    public interface IJournalEntry
    {
        public string PageID { get; }

        IEnumerable<IJournalEntryContent> GetContent();
    }

    public abstract class JournalEntry : IJournalEntry
    {
        public JournalEntry(string pageID)
        {
            PageID = pageID;
        }

        public virtual string PageID { get; set; }

        public abstract IEnumerable<IJournalEntryContent> GetContent();
    }

    public sealed class JournalQuestEntry : JournalEntry
    {
        [Dependency] private readonly IQuestSystem _quest = default!;

        public PrototypeId<QuestPrototype> QuestID { get; }

        public JournalQuestEntry(PrototypeId<QuestPrototype> questId, string pageID) : base(pageID)
        {
            QuestID = questId;
        }

        public override IEnumerable<IJournalEntryContent> GetContent()
        {
            EntitySystem.InjectDependencies(this);
            if (_quest.TryGetCurrentNode(QuestID, out var node) == GetQuestNodeType.Failed)
                yield break;

            var proto = QuestID.ResolvePrototype()!;
            if (proto.TitleLoc != LocaleKey.Empty)
                yield return new JournalHeader(proto.TitleLoc);
            if (node!.Desc != LocaleKey.Empty)
                yield return new JournalContent(node.Desc);
        }
    }

    public interface IJournalEntryContent
    {
        public string Text { get; }
        public Color Color { get; }
        public FontSpec Font { get; }
    }

    public abstract class JournalEntryContent : IJournalEntryContent
    {
        public virtual string Text { get; set; } = "";
        public virtual Color Color { get; set; } = Color.Black;
        public virtual FontSpec Font { get; set; } = UiFonts.JournalText;

        public JournalEntryContent(LocaleKey textLocKey)
        {
            Text = Loc.GetString(textLocKey);
        }
    }

    public sealed class JournalHeader : JournalEntryContent
    {
        public JournalHeader(LocaleKey textLocKey) : base(textLocKey)
        {
        }
    }

    public sealed class JournalContent : JournalEntryContent
    {
        public JournalContent(LocaleKey textLocKey) : base(textLocKey)
        {
        }
    }
}
