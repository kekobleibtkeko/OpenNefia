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

namespace OpenNefia.Content.Journal
{
    public class VanillaJournalLayer : JournalUiLayer
    {
        [Dependency] private readonly IJournalSystem _journal = default!;

        public override int? DefaultZOrder => HudLayer.HudZOrder + 1000;
        private const int BookWidth = 736;
        private const int BookHeight = 448;
        private const int PageWidth = 320;
        private const int LineAmount = 21;

        private List<UiContainer> ContentContainers { get; } = new();

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
        }

        public override void Initialize(JournalGroupUiArgs args)
        {
            base.Initialize(args);
            SetupPages();
        }

        private void SetupPages()
        {
            UiVerticalContainer _MakeNewContainer(LocaleKey title)
            {
                var cont = new UiVerticalContainer
                {
                    YMin = 14,
                };
                cont.AddElement(new UiText(UiFonts.JournalText, Loc.GetString(title)));
                cont.AddElement(new UiText(UiFonts.JournalText, ""));
                return cont;
            }

            var args = _journal.GetJournalArgs();
            var visitedIds = new HashSet<string>();
            var pages = args.Pages.Where(x => x.OrderAfter == null);
            while (pages.Any())
            {
                var curId = pages.First().PageID;
                if (visitedIds.Contains(curId))
                    break;
                visitedIds.Add(curId);
                int pageNum = 0;
                foreach (var page in pages)
                {
                    var container = _MakeNewContainer(page.LocaleKey);
                    int addedLines = 0;
                    var entries = args.Entries.Where(x => x.PageID == curId);
                    if (!entries.Any())
                    {
                        ContentContainers.Add(container);
                        continue;
                    }

                    foreach(var entry in entries)
                    {
                        var lines = GetPageLines(entry).ToList();
                        if (addedLines < LineAmount + lines.Count)
                        {
                            container = _MakeNewContainer(page.LocaleKey);
                            addedLines = 0;
                        }
                        foreach(var line in lines)
                        {
                            container.AddElement(line);
                            addedLines++;
                        }
                    }
                    for (int i = addedLines; i < LineAmount; i++)
                    {
                        container.AddElement(new UiText());
                    }
                    container.AddElement(new UiText(UiFonts.JournalText, $"-{pageNum}-"));
                    ContentContainers.Add(container);
                    pageNum++;
                }
                pages = args.Pages.Where(x => x.OrderAfter == curId);
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

        public override void Draw()
        {
            UiUtils.GetCenteredParams(BookWidth * UIScale, BookHeight * UIScale, out var bounds);
            BookAsset.Draw();
        }
    }
}
