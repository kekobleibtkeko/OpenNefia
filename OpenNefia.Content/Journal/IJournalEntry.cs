using OpenNefia.Content.Quest;
using OpenNefia.Content.UI;
using OpenNefia.Core;
using OpenNefia.Core.Game;
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

    public sealed class JournalQuestHeaderEntry : JournalEntry
    {
        private string Content { get; }

        public JournalQuestHeaderEntry(string content, string pageID) : base(pageID)
        {
            Content = content;
        }

        public override IEnumerable<IJournalEntryContent> GetContent()
        {
            yield return new JournalEntryContent(Content)
            {
                Color = new Color(23, 54, 22),
                Font = UiFonts.JournalQuestHeader
            };
        }
    }

    public sealed class JournalQuestEntry : JournalEntry
    {
        [Dependency] private readonly IQuestSystem _quest = default!;
        [Dependency] private readonly IEntityManager _entMan = default!;

        public PrototypeId<QuestPrototype> QuestID { get; }

        public JournalQuestEntry(PrototypeId<QuestPrototype> questId, string pageID) : base(pageID)
        {
            QuestID = questId;
        }

        public override IEnumerable<IJournalEntryContent> GetContent()
        {
            EntitySystem.InjectDependencies(this);
            var nodeRes = _quest.GetCurrentNode(QuestID, out var node);
            if (nodeRes == GetQuestNodeType.Failed)
                yield break;

            var title = Loc.GetPrototypeString(QuestID, "Title");
            if (!title.StartsWith("<"))
            {
                title = nodeRes switch
                {
                    GetQuestNodeType.End => Loc.GetString("Elona.Journal.QuestDone") + title,
                    _ => $"({title})",
                };
                yield return new JournalHeader(title);
            }

            var progress = _entMan.EnsureComponent<QuestProgressComponent>(GameSession.Player);
            if (nodeRes == GetQuestNodeType.Succeeded)
            {
                yield return new JournalContent(node!.GetDescription(QuestID, progress));
                yield return new JournalEntryContent(string.Empty);
            }
        }
    }

    public sealed class JournalSpacerEntry : JournalEntry
    {
        public int LineCount { get; }

        public JournalSpacerEntry(int lineCount, string pageID) : base(pageID)
        {
            LineCount = lineCount;
        }

        public override IEnumerable<IJournalEntryContent> GetContent()
        {
            for (int i = 0; i < LineCount; i++)
            {
                yield return new JournalEntryContent(string.Empty);
            }
        }
    }

    public interface IJournalEntryContent
    {
        public string Text { get; }
        public Color Color { get; }
        public FontSpec Font { get; }
    }

    public class JournalEntryContent : IJournalEntryContent
    {
        public virtual string Text { get; set; } = "";
        public virtual Color Color { get; set; } = Color.Black;
        public virtual FontSpec Font { get; set; } = UiFonts.JournalText;

        public JournalEntryContent(string content)
        {
            Text = content;
        }
    }

    public sealed class JournalHeader : JournalEntryContent
    {
        public JournalHeader(string content) : base(content)
        {
        }
    }

    public sealed class JournalContent : JournalEntryContent
    {
        public JournalContent(string content) : base(content)
        {
        }
    }
}
