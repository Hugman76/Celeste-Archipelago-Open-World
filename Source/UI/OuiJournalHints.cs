using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net.Enums;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.Celeste_Multiworld.UI
{
    // Journal page listing every hint involving this slot, along with the hint points available. Scrolls with Up/Down.
    internal class OuiJournalHints : OuiJournalScrollPage
    {
        private const int VisibleHints = 10;

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

        private readonly List<ArchipelagoManager.HintDisplayInfo> hints;
        private readonly int hintPoints;
        private readonly int hintCost;

        protected override int ItemCount => this.hints.Count;
        protected override int VisibleItemCount => VisibleHints;

        public OuiJournalHints(OuiJournal journal) : base(journal)
        {
            this.hints = ArchipelagoManager.Instance.GetHintDisplayList();
            this.hintPoints = ArchipelagoManager.Instance.HintPoints;
            this.hintCost = ArchipelagoManager.Instance.HintCost;
        }

        private OuiJournalPage.Table BuildTable()
        {
            OuiJournalPage.Table table = new OuiJournalPage.Table()
                .AddColumn(new OuiJournalPage.TextCell("HINTS", new Vector2(0f, 0.5f), 1f, Color.Black * 0.7f, ItemWidth, true))
                .AddColumn(new OuiJournalPage.EmptyCell(GapWidth))
                .AddColumn(new OuiJournalPage.EmptyCell(LocationWidth))
                .AddColumn(new OuiJournalPage.EmptyCell(GapWidth))
                .AddColumn(new OuiJournalPage.EmptyCell(PlayerWidth))
                .AddColumn(new OuiJournalPage.EmptyCell(GapWidth))
                .AddColumn(new OuiJournalPage.EmptyCell(StatusWidth));

            table.AddRow()
                .Add(new OuiJournalPage.TextCell($"Hint Points: {this.hintPoints}", new Vector2(0f, 0.5f), InfoScale, InfoColor, ItemWidth, true))
                .Add(null)
                .Add(new OuiJournalPage.TextCell($"Next Hint Cost: {this.hintCost}", new Vector2(0f, 0.5f), InfoScale, InfoColor, LocationWidth, true));

            table.AddRow().Add(new OuiJournalPage.EmptyCell(16f));

            if (this.hints.Count == 0)
            {
                table.AddRow()
                    .Add(new OuiJournalPage.TextCell("No hints yet.", new Vector2(0f, 0.5f), InfoScale, InfoColor, ItemWidth, true));
                return table;
            }

            table.AddRow()
                .Add(new OuiJournalPage.TextCell("Item", new Vector2(0f, 0.5f), LabelScale, LabelColor, ItemWidth, true))
                .Add(null)
                .Add(new OuiJournalPage.TextCell("Location", new Vector2(0f, 0.5f), LabelScale, LabelColor, LocationWidth, true))
                .Add(null)
                .Add(new OuiJournalPage.TextCell("Player", new Vector2(0f, 0.5f), LabelScale, LabelColor, PlayerWidth, true))
                .Add(null)
                .Add(new OuiJournalPage.TextCell("Status", new Vector2(0f, 0.5f), LabelScale, LabelColor, StatusWidth, true));

            foreach (ArchipelagoManager.HintDisplayInfo hint in this.hints.Skip(this.ScrollOffset).Take(VisibleHints))
            {
                Color itemColor = hint.Found ? FoundColor : GetItemColor(hint.Flags);
                Color rowColor = hint.Found ? FoundColor : TextColorDefault;

                table.AddRow()
                    .Add(new OuiJournalPage.TextCell(FitText(hint.ItemName, HintScale, ItemWidth), new Vector2(0f, 0.5f), HintScale, itemColor, ItemWidth, true))
                    .Add(null)
                    .Add(new OuiJournalPage.TextCell(FitText(hint.LocationName, HintScale, LocationWidth), new Vector2(0f, 0.5f), HintScale, rowColor, LocationWidth, true))
                    .Add(null)
                    .Add(new OuiJournalPage.TextCell(FitText(GetPlayerText(hint), HintScale, PlayerWidth), new Vector2(0f, 0.5f), HintScale, rowColor, PlayerWidth, true))
                    .Add(null)
                    .Add(new OuiJournalPage.TextCell(GetStatusText(hint), new Vector2(0f, 0.5f), HintScale, rowColor, StatusWidth, true));
            }

            return table;
        }

        public static string GetPlayerText(ArchipelagoManager.HintDisplayInfo hint)
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

        public static string GetStatusText(ArchipelagoManager.HintDisplayInfo hint)
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

        public override void Redraw(Monocle.VirtualRenderTarget buffer)
        {
            base.Redraw(buffer);
            Monocle.Draw.SpriteBatch.Begin();
            BuildTable().Render(new Vector2(60f, 20f));
            DrawScrollIndicators(60f, 960f);
            Monocle.Draw.SpriteBatch.End();
        }
    }
}
