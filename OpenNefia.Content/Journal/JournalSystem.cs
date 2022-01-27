using OpenNefia.Content.Quest;
using OpenNefia.Core.Game;
using OpenNefia.Core.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenNefia.Content.Journal
{
    public interface IJournalSystem : IEntitySystem
    {
        OpenJournalArgs GetJournalArgs();
    }

    public class JournalSystem : EntitySystem, IJournalSystem
    {
        public const string PageId = "Journal";
        public const string NewsPageId = "JournalNews";
        public const int NewsPagePrio = -20000;
        public const string QuestPageId = "JournalQuest";
        public const int QuestPagePrio = -19000;

        public override void Initialize()
        {
            SubscribeLocalEvent<QuestProgressComponent, OpenJournalArgs>(AddQuestEntries, nameof(AddQuestEntries));
        }

        private void AddQuestEntries(EntityUid uid, QuestProgressComponent component, OpenJournalArgs args)
        {
            
        }

        public OpenJournalArgs GetJournalArgs()
        {
            var args = new OpenJournalArgs();
            args.Pages.Add(new(NewsPageId, NewsPagePrio));
            args.Pages.Add(new(QuestPageId, QuestPagePrio));

            RaiseLocalEvent(GameSession.Player, args);

            return args;
        }
    }

    public sealed class JournalPage
    {
        public int Priority { get; set; }
        public string Title { get; set; } = "";

        public JournalPage(string title, int priority)
        {
            Title = title;
            Priority = priority;
        }
    }

    public sealed class OpenJournalArgs
    {
        public List<JournalPage> Pages { get; } = new();
        public List<IJournalEntry> Entries { get; } = new();
    }
}
