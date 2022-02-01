using OpenNefia.Content.UI.Element.Containers;
using OpenNefia.Content.UI.Hud;
using OpenNefia.Core.IoC;
using OpenNefia.Core.Locale;
using OpenNefia.Core.Rendering;
using OpenNefia.Core.UI;
using OpenNefia.Core.UI.Element;
using OpenNefia.Content.Prototypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenNefia.Core.Maths;
using OpenNefia.Content.UI;
using OpenNefia.Content.UI.Element;
using OpenNefia.Core;
using OpenNefia.Core.Input;
using OpenNefia.Core.Audio;
using OpenNefia.Core.Log;

namespace OpenNefia.Content.Journal
{
    public class VanillaJournalLayer : JournalUiLayer
    {
        [Dependency] private readonly IJournalSystem _journal = default!;

        public override int? DefaultZOrder => HudLayer.HudZOrder + 1000;
        private float PageWidth => 270 * UIScale;
        private const int BookWidth = 736;
        private const int BookHeight = 448;
        private const int LineAmount = 21;

        private List<UiContainer> ContentContainers { get; } = new();
        private int Page { get; set; }
        private UiContainer? LeftPage { get; set; }
        private UiContainer? RightPage { get; set; }

        [Child] private AssetDrawable BookAsset;

        public VanillaJournalLayer()
        {
            BookAsset = new(Protos.Asset.Book);
        }

        public override void GetPreferredBounds(out UIBox2 bounds)
        {
            UiUtils.GetCenteredParams(BookWidth * UIScale, BookHeight * UIScale, out bounds);
        }

        public override void SetSize(float width, float height)
        {
            base.SetSize(width, height);
            BookAsset.SetPreferredSize();
        }

        public override void SetPosition(float x, float y)
        {
            base.SetPosition(x, y);
            BookAsset.SetPosition(X, Y);
            if (LeftPage != null)
            {
                LeftPage.SetPosition(X + 75, Y + 50);
                LeftPage.Relayout();
            }
            if (RightPage != null)
            {
                RightPage.SetPosition(X + 375, Y + 50);
                RightPage.Relayout();
            }
        }

        public override void OnQuery()
        {
            base.OnQuery();
            Sounds.Play(Protos.Sound.Book1);
        }

        public override void Initialize(JournalGroupUiArgs args)
        {
            base.Initialize(args);
            SetupPages();
            SetPage(Page, false);
        }

        public override void Draw()
        {
            UiUtils.GetCenteredParams(BookWidth * UIScale, BookHeight * UIScale, out var bounds);
            BookAsset.Draw();
            if (LeftPage != null)
                LeftPage.Draw();
            if (RightPage != null)
                RightPage.Draw();
        }

        private void SetPage(int page, bool playSound = true)
        {
            if (LeftPage != null)
                RemoveChild(LeftPage);
            if (RightPage != null)
                RemoveChild(RightPage);

            LeftPage = ContentContainers.Skip(page).FirstOrDefault();
            if (LeftPage != null)
                AddChild(LeftPage);
            RightPage = ContentContainers.Skip(page + 1).FirstOrDefault();
            if (RightPage != null)
                AddChild(RightPage);

            if (playSound)
                Sounds.Play(Protos.Sound.Card1);
        }

        protected override void KeyBindDown(GUIBoundKeyEventArgs args)
        {
            var oldPage = Page;
            if (args.Function == EngineKeyFunctions.UINextPage)
            {
                if (Page * 2 > ContentContainers.Count)
                    Page = 0;
                else
                    Page += 2;
                SetPage(Page);
                SetPosition(X, Y);
            }
            else if (args.Function == EngineKeyFunctions.UIPreviousPage)
            {
                if (Page <= 0)
                    Page = (ContentContainers.Count / 2) + 1;
                else
                    Page -= 2;
                SetPage(Page);
                SetPosition(X, Y);
            }
            else if (args.Function == EngineKeyFunctions.UICancel)
            {
                Finish(new());
            }
        }

        private void SetupPages()
        {
            var pageNumTexts = new List<UiText>();
            int pageNum = 0;
            int addedLines = 0;
            UiContainer? container;

            UiVerticalContainer _MakeNewContainer(LocaleKey title)
            {
                var cont = new UiVerticalContainer
                {
                    YMin = 14,
                };
                cont.AddElement(new UiText(UiFonts.JournalText, $" - {Loc.GetString(title)} -"));
                cont.AddElement(new UiText(UiFonts.JournalText));
                return cont;
            }

            void _CompletePage()
            {
                pageNum++;
                for (int i = addedLines; i < LineAmount; i++)
                {
                    container?.AddElement(new UiText(UiFonts.JournalText));
                }
                var numText = new UiText(UiFonts.JournalText, $"{new string(' ', 16)}- {pageNum} -");
                pageNumTexts.Add(numText);
                container?.AddElement(numText);
            }

            var args = _journal.GetJournalArgs();
            var visitedIds = new HashSet<string>();
            var pages = args.Pages.Where(x => x.OrderAfter == null);
            while (pages.Any())
            {
                var curId = pages.First().PageID;
                if (visitedIds.Contains(curId))
                {
                    Logger.Warning("journal", $"Cyclic PageId for id {curId}");
                    break;
                }
                visitedIds.Add(curId);
                foreach (var page in pages)
                {
                    addedLines = 0;
                    container = _MakeNewContainer(page.LocaleKey);
                    var entries = args.Entries.Where(x => x.PageID == curId);
                    if (!entries.Any())
                    {
                        _CompletePage();
                        ContentContainers.Add(container);
                        continue;
                    }

                    foreach(var entry in entries)
                    {
                        var lines = GetPageLines(entry).ToList();
                        if (addedLines > LineAmount + lines.Count)
                        {
                            _CompletePage();
                            container = _MakeNewContainer(page.LocaleKey);
                            addedLines = 0;
                        }
                        foreach(var line in lines)
                        {
                            container.AddElement(line);
                            addedLines++;
                        }
                    }
                    _CompletePage();
                    ContentContainers.Add(container);
                }
                pages = args.Pages.Where(x => x.OrderAfter == curId);
            }

            for (int i = 0; i < pageNumTexts.Count; i++)
            {
                if (i % 2 == 0 || i >= pageNumTexts.Count - 1)
                    continue;
                var text = pageNumTexts[i];
                text.Text = text.Text + new string(' ', 18 - text.Text.Trim().Length) + Loc.GetString("Elona.Journal.More");
            }
        }

        private IEnumerable<UiElement> GetPageLines(IJournalEntry entry)
        {
            float totalWidth = 0;
            foreach(var content in entry.GetContent())
            {
                var sb = new StringBuilder();
                var words = UiHelpers.SplitString(content.Text, Loc.Language);
                foreach(var word in words)
                {
                    var wordWidth = content.Font.LoveFont.GetWidthV(UIScale, word);
                    if (totalWidth + wordWidth > PageWidth)
                    {
                        yield return new UiText(content.Font, sb.ToString().TrimStart()) { Color = content.Color };
                        totalWidth = 0;
                        sb.Clear();
                    }
                    sb.Append(word);
                    totalWidth += wordWidth;
                }
                yield return new UiText(content.Font, sb.ToString().TrimStart()) { Color = content.Color };
            }
        }
    }
}
