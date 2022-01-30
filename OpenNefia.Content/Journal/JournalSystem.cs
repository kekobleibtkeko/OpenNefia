using OpenNefia.Content.Quest;
using OpenNefia.Core;
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
        public const string NewsPageId = "JournalNews";
        public const string QuestPageId = "JournalQuest";
        public const string QuestItemPageId = "JournalQuestItem";
        public const string TitleRankPageId = "JournalTitleRank";
        public const string IncomePageId = "JournalIncome";
        public const string CompletedQuestsPageId = "JournalCompletedQuests";

        public override void Initialize()
        {
            SubscribeLocalEvent<QuestProgressComponent, OpenJournalArgs>(AddQuestEntries, nameof(AddQuestEntries));
        }

        private void AddQuestEntries(EntityUid uid, QuestProgressComponent component, OpenJournalArgs args)
        {
            foreach(var (id, status) in component.QuestProgress)
            {
                if (status.Status == QuestStatusType.Unstarted)
                    continue;
                args.Entries.Add(new JournalQuestEntry(id, status.Status == QuestStatusType.Completed ? CompletedQuestsPageId : QuestItemPageId));
            }
        }

        public OpenJournalArgs GetJournalArgs()
        {
            var args = new OpenJournalArgs();
            args.Pages.Add(new(NewsPageId, $"Elona.Journal.{NewsPageId}"));
            args.Pages.Add(new(QuestPageId, $"Elona.Journal.{QuestPageId}", NewsPageId));
            args.Pages.Add(new(QuestItemPageId, $"Elona.Journal.{QuestItemPageId}", QuestPageId));
            args.Pages.Add(new(TitleRankPageId, $"Elona.Journal.{TitleRankPageId}", QuestItemPageId));
            args.Pages.Add(new(IncomePageId, $"Elona.Journal.{IncomePageId}", TitleRankPageId));
            args.Pages.Add(new(CompletedQuestsPageId, $"Elona.Journal.{CompletedQuestsPageId}", IncomePageId));

            RaiseLocalEvent(GameSession.Player, args);

            return args;
        }
    }

    public sealed class JournalPage
    {
        public string PageID { get; set; } = "";
        public string? OrderAfter { get; set; }
        public LocaleKey LocaleKey { get; set; }

        public JournalPage(string id, LocaleKey localeKey, string? after = null)
        {
            PageID = id;
            OrderAfter = after;
            LocaleKey = localeKey;
        }
    }

    public sealed class OpenJournalArgs
    {
        public List<JournalPage> Pages { get; } = new();
        public List<IJournalEntry> Entries { get; } = new();
        public JournalUiLayer Layer { get; } = new VanillaJournalLayer();
    }
}
