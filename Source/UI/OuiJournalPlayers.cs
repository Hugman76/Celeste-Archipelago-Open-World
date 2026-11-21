using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net.Enums;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.Celeste_Multiworld.UI
{
    // Journal page(s) listing every player of the multiworld with their game and status.
    internal class OuiJournalPlayers : OuiJournalPage
    {
        private const int PlayersPerPage = 15;

        private const float NameWidth = 480f;
        private const float GameWidth = 560f;
        private const float StatusWidth = 300f;
        private const float GapWidth = 20f;
        private const float RowScale = 0.45f;

        private static readonly Color LabelColor = Color.Black * 0.5f;
        private static readonly Color TextColorDefault = Color.Black * 0.75f;
        private static readonly Color ThisSlotColor = Monocle.Calc.HexToColor("8E2C8E");
        private static readonly Color GoalColor = new Color(30, 120, 40);
        private static readonly Color OfflineColor = Color.Black * 0.4f;

        public static List<OuiJournalPlayers> CreatePages(OuiJournal journal)
        {
            List<ArchipelagoManager.PlayerDisplayInfo> players = ArchipelagoManager.Instance.GetPlayerDisplayList();
            int goals = players.Count(player => player.Status == ArchipelagoClientState.ClientGoal);

            int pageCount = System.Math.Max(1, (players.Count + PlayersPerPage - 1) / PlayersPerPage);
            List<OuiJournalPlayers> pages = new List<OuiJournalPlayers>();
            for (int page = 0; page < pageCount; page++)
            {
                pages.Add(new OuiJournalPlayers(journal, players.Skip(page * PlayersPerPage).Take(PlayersPerPage).ToList(), page, pageCount, goals, players.Count));
            }
            return pages;
        }

        private OuiJournalPlayers(OuiJournal journal, List<ArchipelagoManager.PlayerDisplayInfo> players, int pageNumber, int pageCount, int goals, int playerCount) : base(journal)
        {
            MonoMod.Utils.DynamicData dynamicUI = MonoMod.Utils.DynamicData.For(this);
            dynamicUI.Set("TextJustify", new Vector2(0.5f, 0.5f));
            dynamicUI.Set("TextColor", Color.Black * 0.6f);
            this.Journal = journal;

            string header = pageCount > 1 ? $"PLAYERS ({pageNumber + 1}/{pageCount})" : "PLAYERS";

            this.PageTexture = "page";
            this.table = new OuiJournalPage.Table()
                .AddColumn(new OuiJournalPage.TextCell(header, new Vector2(0f, 0.5f), 1f, Color.Black * 0.7f, NameWidth, true))
                .AddColumn(new OuiJournalPage.EmptyCell(GapWidth))
                .AddColumn(new OuiJournalPage.EmptyCell(GameWidth))
                .AddColumn(new OuiJournalPage.EmptyCell(GapWidth))
                .AddColumn(new OuiJournalPage.EmptyCell(StatusWidth));

            this.table.AddRow()
                .Add(new OuiJournalPage.TextCell($"Goals Completed: {goals} / {playerCount}", new Vector2(0f, 0.5f), 0.5f, Color.Black * 0.7f, NameWidth, true));
            this.table.AddRow().Add(new OuiJournalPage.EmptyCell(16f));

            this.table.AddRow()
                .Add(new OuiJournalPage.TextCell("Player", new Vector2(0f, 0.5f), RowScale, LabelColor, NameWidth, true))
                .Add(null)
                .Add(new OuiJournalPage.TextCell("Game", new Vector2(0f, 0.5f), RowScale, LabelColor, GameWidth, true))
                .Add(null)
                .Add(new OuiJournalPage.TextCell("Status", new Vector2(0f, 0.5f), RowScale, LabelColor, StatusWidth, true));

            foreach (ArchipelagoManager.PlayerDisplayInfo player in players)
            {
                Color statusColor = GetStatusColor(player.Status);
                string name = player.IsThisSlot ? $"{player.Name} (You)" : player.Name;

                this.table.AddRow()
                    .Add(new OuiJournalPage.TextCell(FitText(name, RowScale, NameWidth), new Vector2(0f, 0.5f), RowScale, player.IsThisSlot ? ThisSlotColor : TextColorDefault, NameWidth, true))
                    .Add(null)
                    .Add(new OuiJournalPage.TextCell(FitText(player.Game ?? "", RowScale, GameWidth), new Vector2(0f, 0.5f), RowScale, TextColorDefault, GameWidth, true))
                    .Add(null)
                    .Add(new OuiJournalPage.TextCell(GetStatusText(player.Status), new Vector2(0f, 0.5f), RowScale, statusColor, StatusWidth, true));
            }
        }

        private static string GetStatusText(ArchipelagoClientState status)
        {
            switch (status)
            {
                case ArchipelagoClientState.ClientConnected:
                    return "Connected";
                case ArchipelagoClientState.ClientReady:
                    return "Ready";
                case ArchipelagoClientState.ClientPlaying:
                    return "Playing";
                case ArchipelagoClientState.ClientGoal:
                    return "Goal Completed";
                default:
                    return "Not Connected";
            }
        }

        private static Color GetStatusColor(ArchipelagoClientState status)
        {
            switch (status)
            {
                case ArchipelagoClientState.ClientGoal:
                    return GoalColor;
                case ArchipelagoClientState.ClientUnknown:
                    return OfflineColor;
                default:
                    return TextColorDefault;
            }
        }

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
