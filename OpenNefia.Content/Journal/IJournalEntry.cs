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
        public int Priority { get; }

        IEnumerable<IJournalEntryContent> GetContent();
    }

    public abstract class JournalEntry : IJournalEntry
    {
        public JournalEntry(string pageID)
        {
            PageID = pageID;
        }

        public virtual int Priority { get; set; }

        public virtual string PageID { get; set; }

        public abstract IEnumerable<IJournalEntryContent> GetContent();
    }

    public sealed class JournalQuestEntry : JournalEntry
    {
        public JournalQuestEntry(string pageID) : base(pageID)
        {
        }

        public override IEnumerable<IJournalEntryContent> GetContent()
        {
            throw new NotImplementedException();
        }
    }

    public interface IJournalEntryContent
    {
        public string Text { get; }
    }

    public abstract class JournalEntryContent : IJournalEntryContent
    {
        public virtual string Text { get; set; } = "";
    }

    public sealed class JournalQuestHeader : JournalEntryContent
    {

    }

    public sealed class JournalQuestContent : JournalEntryContent
    {
        public int Priority { get; set; }
    }
}
