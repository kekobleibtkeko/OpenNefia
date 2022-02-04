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
using OpenNefia.Core.Graphics;

namespace OpenNefia.Content.Journal
{
    public class VanillaJournalLayer : JournalUiLayer
    {
        private struct JournalPage
        {
            public string PageID { get; }
            public UiContainer PageContainer { get; }

            public JournalPage(string pageID, UiContainer pageContainer)
            {
                PageID = pageID;
                PageContainer = pageContainer;
            }
        }

        [Dependency] private readonly IJournalSystem _journal = default!;
        [Dependency] private readonly IGraphics _graphics = default!;

        private float PageWidth => RawPageWidth * UIScale;

        private const int RawPageWidth = 270;
        private const int BookWidth = 736;
        private const int BookHeight = 448;
        private const int LineAmount = 21;

        private List<JournalPage> Pages { get; } = new();
        private int Page { get; set; }
        private UiContainer? LeftPage { get; set; }
        private UiContainer? RightPage { get; set; }

        private int PageCount => (Pages.Count / 2) - 1 + (Pages.Count % 2);

        [Child] private AssetDrawable BookAsset;

        public VanillaJournalLayer()
        {
            BookAsset = new(Protos.Asset.Book);
        }

        public override void GetPreferredBounds(out UIBox2 bounds)
        {
            UiUtils.GetCenteredParams(BookWidth, BookHeight, out bounds);
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
            SetPagePositions();
        }

        private void SetPagePositions()
        {
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
            LayerUIScale = _graphics.WindowScale;
            SetupPages();
            for (int i = 0; i < PageCount; i++)
            {
                var page = Pages[i];
                if (page.PageID == JournalSystem.QuestPageId)
                {
                    SetPage((i / 2), false);
                    break;
                }
            }
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
            Page = MathHelper.Wrap(page, 0, PageCount);

            if (LeftPage != null)
                RemoveChild(LeftPage);
            if (RightPage != null)
                RemoveChild(RightPage);

            LeftPage = Pages.Skip(Page * 2).FirstOrDefault().PageContainer;
            if (LeftPage != null)
                AddChild(LeftPage);
            RightPage = Pages.Skip((Page * 2) + 1).FirstOrDefault().PageContainer;
            if (RightPage != null)
                AddChild(RightPage);

            if (playSound)
                Sounds.Play(Protos.Sound.Card1);

            SetPagePositions();
        }

        protected override void KeyBindDown(GUIBoundKeyEventArgs args)
        {
            if (args.Function == EngineKeyFunctions.UINextPage)
            {
                SetPage(Page + 1);
                args.Handle();
            }
            else if (args.Function == EngineKeyFunctions.UIPreviousPage)
            {
                SetPage(Page - 1);
                args.Handle();
            }
            else if (args.Function == EngineKeyFunctions.UICancel)
            {
                Finish(new());
                args.Handle();
            }
        }

        private void SetupPages()
        {
            var pageNumTexts = new List<UiText>();
            int pageNum = 0;
            int addedLines = 0;
            string curId = string.Empty;
            UiContainer? container;

            UiVerticalContainer _MakeNewContainer(LocaleKey title)
            {
                var cont = new UiVerticalContainer
                {
                    YMin = 14,
                };
                cont.AddElement(new UiText(UiFonts.JournalText, $"{Loc.GetWhitespace()}- {Loc.GetString(title)} -"));
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
                Pages.Add(new(curId, container!));
            }

            var args = _journal.GetJournalArgs();
            var visitedIds = new HashSet<string>();
            var pages = args.Pages.Where(x => x.OrderAfter == null);
            while (pages.Any())
            {
                curId = pages.First().PageID;
                if (visitedIds.Contains(curId))
                {
                    Logger.WarningS("journal", $"Cyclic page order for id {curId}");
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
                        continue;
                    }

                    foreach (var entry in entries)
                    {
                        foreach(var line in GetPageLines(entry))
                        {
                            if (addedLines + 1 > LineAmount)
                            {
                                _CompletePage();
                                container = _MakeNewContainer(page.LocaleKey);
                                addedLines = 0;
                            }
                            container.AddElement(line);
                            addedLines++;
                        }
                    }
                    _CompletePage();
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
            foreach(var content in entry.GetContent())
            {
                var wrap = content.Font.LoveFont.GetWrap(content.Text, PageWidth);
                foreach(var str in wrap.Item2)
                {
                    yield return new UiText(content.Font, str) { Color = content.Color };
                }
            }
        }
    }
}
