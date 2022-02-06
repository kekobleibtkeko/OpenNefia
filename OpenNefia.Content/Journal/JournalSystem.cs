using OpenNefia.Content.Quest;
using OpenNefia.Content.Prototypes;
using OpenNefia.Core;
using OpenNefia.Core.Game;
using OpenNefia.Core.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenNefia.Core.Locale;

namespace OpenNefia.Content.Journal
{
    public interface IJournalSystem : IEntitySystem
    {
        OpenJournalArgs GetJournalArgs();
    }

    public class JournalSystem : EntitySystem, IJournalSystem
    {
        public const string QuestHeaderMain = "Header.QuestMain";
        public const string QuestHeaderSub = "Header.QuestSub";

        public const string NewsPageId = "News";
        public const string QuestPageId = "Quest";
        public const string QuestItemPageId = "QuestItem";
        public const string TitleRankPageId = "TitleRank";
        public const string IncomePageId = "Income";
        public const string CompletedQuestsPageId = "CompletedQuests";
        public const string FailedQuestsPageId = "FailedQuests";

        private LocaleKey JournalKey = new("Elona.Journal");

        public override void Initialize()
        {
            SubscribeLocalEvent<QuestProgressComponent, OpenJournalArgs>(AddQuestEntries, nameof(AddQuestEntries));
        }

        private void AddQuestEntries(EntityUid uid, QuestProgressComponent component, OpenJournalArgs args)
        {
            args.Entries.Add(new JournalQuestHeaderEntry(Loc.GetString(JournalKey.With(QuestHeaderMain)), QuestPageId));
            args.Entries.Add(new JournalQuestEntry(Protos.Quest.MainQuest, QuestPageId));
            args.Entries.Add(new JournalSpacerEntry(1, QuestPageId));
            args.Entries.Add(new JournalQuestHeaderEntry(Loc.GetString(JournalKey.With(QuestHeaderSub)), QuestPageId));
            foreach (var (id, status) in component.QuestProgress)
            {
                if (status.Status == QuestStatusType.Unstarted || id == Protos.Quest.MainQuest)
                    continue;

                var pageTarget = status.Status switch
                {
                    QuestStatusType.Completed => CompletedQuestsPageId,
                    QuestStatusType.Failed => FailedQuestsPageId,
                    _ => QuestPageId
                };
                args.Entries.Add(new JournalQuestEntry(id, pageTarget));
            }
        }

        public OpenJournalArgs GetJournalArgs()
        {
            var args = new OpenJournalArgs();
            args.Pages.Add(new(NewsPageId, JournalKey.With(NewsPageId)));
            args.Pages.Add(new(QuestPageId, JournalKey.With(QuestPageId), NewsPageId));
            args.Pages.Add(new(QuestItemPageId, JournalKey.With(QuestItemPageId), QuestPageId));
            args.Pages.Add(new(TitleRankPageId, JournalKey.With(TitleRankPageId), QuestItemPageId));
            args.Pages.Add(new(IncomePageId, JournalKey.With(IncomePageId), TitleRankPageId));
            args.Pages.Add(new(CompletedQuestsPageId, JournalKey.With(CompletedQuestsPageId), IncomePageId));
            args.Pages.Add(new(FailedQuestsPageId, JournalKey.With(FailedQuestsPageId), CompletedQuestsPageId));

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
