using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net.Enums;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.Celeste_Multiworld.UI
{
    // Journal page(s) listing every hint involving this slot, along with the hint points available.
    internal class OuiJournalHints : OuiJournalPage
    {
        private const int HintsPerPage = 13;

        private const float ItemWidth = 420f;
        private const float LocationWidth = 520f;
        private const float PlayerWidth = 280f;
        private const float StatusWidth = 170f;
        private const float GapWidth = 20f;

        private const float InfoScale = 0.5f;
        private const float LabelScale = 0.45f;
        private const float HintScale = 0.42f;

        private static readonly Color InfoColor = Color.Black * 0.7f;
        private static readonly Color LabelColor = Color.Black * 0.5f;
        private static readonly Color FoundColor = Color.Black * 0.35f;
        private static readonly Color TextColorDefault = Color.Black * 0.8f;

        private static readonly Color ProgressionColor = Monocle.Calc.HexToColor("6A4FB3");
        private static readonly Color UsefulColor = Monocle.Calc.HexToColor("3A5BBF");
        private static readonly Color TrapColor = Monocle.Calc.HexToColor("B03A2E");
        private static readonly Color FillerColor = Monocle.Calc.HexToColor("1E7F7F");

        public static List<OuiJournalHints> CreatePages(OuiJournal journal)
        {
            List<ArchipelagoManager.HintDisplayInfo> hints = ArchipelagoManager.Instance.GetHintDisplayList();
            int hintPoints = ArchipelagoManager.Instance.HintPoints;
            int hintCost = ArchipelagoManager.Instance.HintCost;

            int pageCount = System.Math.Max(1, (hints.Count + HintsPerPage - 1) / HintsPerPage);

            List<OuiJournalHints> pages = new List<OuiJournalHints>();
            for (int page = 0; page < pageCount; page++)
            {
                List<ArchipelagoManager.HintDisplayInfo> pageHints = hints.Skip(page * HintsPerPage).Take(HintsPerPage).ToList();
                pages.Add(new OuiJournalHints(journal, pageHints, page, pageCount, hintPoints, hintCost));
            }

            return pages;
        }

        public OuiJournalHints(OuiJournal journal, List<ArchipelagoManager.HintDisplayInfo> hints, int pageNumber, int pageCount, int hintPoints, int hintCost) : base(journal)
        {
            MonoMod.Utils.DynamicData dynamicUI = MonoMod.Utils.DynamicData.For(this);
            dynamicUI.Set("TextJustify", new Vector2(0.5f, 0.5f));
            dynamicUI.Set("TextColor", Color.Black * 0.6f);
            this.Journal = journal;

            string header = pageCount > 1 ? $"HINTS ({pageNumber + 1}/{pageCount})" : "HINTS";

            this.PageTexture = "page";
            this.table = new OuiJournalPage.Table()
                .AddColumn(new OuiJournalPage.TextCell(header, new Vector2(0f, 0.5f), 1f, Color.Black * 0.7f, ItemWidth, true))
                .AddColumn(new OuiJournalPage.EmptyCell(GapWidth))
                .AddColumn(new OuiJournalPage.EmptyCell(LocationWidth))
                .AddColumn(new OuiJournalPage.EmptyCell(GapWidth))
                .AddColumn(new OuiJournalPage.EmptyCell(PlayerWidth))
                .AddColumn(new OuiJournalPage.EmptyCell(GapWidth))
                .AddColumn(new OuiJournalPage.EmptyCell(StatusWidth));

            this.table.AddRow()
                .Add(new OuiJournalPage.TextCell($"Hint Points: {hintPoints}", new Vector2(0f, 0.5f), InfoScale, InfoColor, ItemWidth, true))
                .Add(null)
                .Add(new OuiJournalPage.TextCell($"Next Hint Cost: {hintCost}", new Vector2(0f, 0.5f), InfoScale, InfoColor, LocationWidth, true));

            this.table.AddRow().Add(new OuiJournalPage.EmptyCell(16f));

            if (hints.Count == 0)
            {
                this.table.AddRow()
                    .Add(new OuiJournalPage.TextCell("No hints yet.", new Vector2(0f, 0.5f), InfoScale, InfoColor, ItemWidth, true));
                return;
            }

            this.table.AddRow()
                .Add(new OuiJournalPage.TextCell("Item", new Vector2(0f, 0.5f), LabelScale, LabelColor, ItemWidth, true))
                .Add(null)
                .Add(new OuiJournalPage.TextCell("Location", new Vector2(0f, 0.5f), LabelScale, LabelColor, LocationWidth, true))
                .Add(null)
                .Add(new OuiJournalPage.TextCell("Player", new Vector2(0f, 0.5f), LabelScale, LabelColor, PlayerWidth, true))
                .Add(null)
                .Add(new OuiJournalPage.TextCell("Status", new Vector2(0f, 0.5f), LabelScale, LabelColor, StatusWidth, true));

            foreach (ArchipelagoManager.HintDisplayInfo hint in hints)
            {
                Color itemColor = hint.Found ? FoundColor : GetItemColor(hint.Flags);
                Color rowColor = hint.Found ? FoundColor : TextColorDefault;

                this.table.AddRow()
                    .Add(new OuiJournalPage.TextCell(FitText(hint.ItemName, HintScale, ItemWidth), new Vector2(0f, 0.5f), HintScale, itemColor, ItemWidth, true))
                    .Add(null)
                    .Add(new OuiJournalPage.TextCell(FitText(hint.LocationName, HintScale, LocationWidth), new Vector2(0f, 0.5f), HintScale, rowColor, LocationWidth, true))
                    .Add(null)
                    .Add(new OuiJournalPage.TextCell(FitText(GetPlayerText(hint), HintScale, PlayerWidth), new Vector2(0f, 0.5f), HintScale, rowColor, PlayerWidth, true))
                    .Add(null)
                    .Add(new OuiJournalPage.TextCell(GetStatusText(hint), new Vector2(0f, 0.5f), HintScale, rowColor, StatusWidth, true));
            }
        }

        private static string GetPlayerText(ArchipelagoManager.HintDisplayInfo hint)
        {
            if (hint.IsForThisSlot && hint.IsInThisWorld)
            {
                return "Your World";
            }
            if (hint.IsForThisSlot)
            {
                return $"{hint.FindingPlayerName}'s World";
            }
            return $"For {hint.ReceivingPlayerName}";
        }

        private static string GetStatusText(ArchipelagoManager.HintDisplayInfo hint)
        {
            if (hint.Found)
            {
                return "Found";
            }

            switch (hint.Status)
            {
                case HintStatus.Priority:
                    return "Priority";
                case HintStatus.Avoid:
                    return "Avoid";
                case HintStatus.NoPriority:
                    return "No Priority";
                default:
                    return "Not Found";
            }
        }

        private static Color GetItemColor(ItemFlags flags)
        {
            if ((flags & ItemFlags.Advancement) != 0)
            {
                return ProgressionColor;
            }
            if ((flags & ItemFlags.NeverExclude) != 0)
            {
                return UsefulColor;
            }
            if ((flags & ItemFlags.Trap) != 0)
            {
                return TrapColor;
            }
            return FillerColor;
        }

        // Names from other games can be long, so they are shortened to fit their column
        private static string FitText(string text, float scale, float maxWidth)
        {
            if (ActiveFont.Measure(text).X * scale <= maxWidth)
            {
                return text;
            }

            while (text.Length > 1 && ActiveFont.Measure(text + "...").X * scale > maxWidth)
            {
                text = text.Substring(0, text.Length - 1);
            }

            return text.TrimEnd() + "...";
        }

        public override void Redraw(Monocle.VirtualRenderTarget buffer)
        {
            base.Redraw(buffer);
            Monocle.Draw.SpriteBatch.Begin();
            this.table.Render(new Vector2(60f, 20f));
            Monocle.Draw.SpriteBatch.End();
        }

        private OuiJournalPage.Table table;
    }
}
