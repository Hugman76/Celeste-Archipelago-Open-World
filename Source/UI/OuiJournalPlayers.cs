using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net.Enums;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.Celeste_Multiworld.UI
{
    // Journal page listing every player of the multiworld with their game and status. Scrolls with Up/Down.
    internal class OuiJournalPlayers : OuiJournalScrollPage
    {
        private const int VisiblePlayers = 10;

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

        private readonly List<ArchipelagoManager.PlayerDisplayInfo> players;
        private readonly int goals;

        protected override int ItemCount => this.players.Count;
        protected override int VisibleItemCount => VisiblePlayers;

        public OuiJournalPlayers(OuiJournal journal) : base(journal)
        {
            this.players = ArchipelagoManager.Instance.GetPlayerDisplayList();
            this.goals = this.players.Count(player => player.Status == ArchipelagoClientState.ClientGoal);
        }

        private OuiJournalPage.Table BuildTable()
        {
            OuiJournalPage.Table table = new OuiJournalPage.Table()
                .AddColumn(new OuiJournalPage.TextCell("PLAYERS", new Vector2(0f, 0.5f), 1f, Color.Black * 0.7f, NameWidth, true))
                .AddColumn(new OuiJournalPage.EmptyCell(GapWidth))
                .AddColumn(new OuiJournalPage.EmptyCell(GameWidth))
                .AddColumn(new OuiJournalPage.EmptyCell(GapWidth))
                .AddColumn(new OuiJournalPage.EmptyCell(StatusWidth));

            table.AddRow()
                .Add(new OuiJournalPage.TextCell($"Goals Completed: {this.goals} / {this.players.Count}", new Vector2(0f, 0.5f), 0.5f, Color.Black * 0.7f, NameWidth, true));
            table.AddRow().Add(new OuiJournalPage.EmptyCell(16f));

            table.AddRow()
                .Add(new OuiJournalPage.TextCell("Player", new Vector2(0f, 0.5f), RowScale, LabelColor, NameWidth, true))
                .Add(null)
                .Add(new OuiJournalPage.TextCell("Game", new Vector2(0f, 0.5f), RowScale, LabelColor, GameWidth, true))
                .Add(null)
                .Add(new OuiJournalPage.TextCell("Status", new Vector2(0f, 0.5f), RowScale, LabelColor, StatusWidth, true));

            foreach (ArchipelagoManager.PlayerDisplayInfo player in this.players.Skip(this.ScrollOffset).Take(VisiblePlayers))
            {
                string name = player.IsThisSlot ? $"{player.Name} (You)" : player.Name;

                table.AddRow()
                    .Add(new OuiJournalPage.TextCell(FitText(name, RowScale, NameWidth), new Vector2(0f, 0.5f), RowScale, player.IsThisSlot ? ThisSlotColor : TextColorDefault, NameWidth, true))
                    .Add(null)
                    .Add(new OuiJournalPage.TextCell(FitText(player.Game ?? "", RowScale, GameWidth), new Vector2(0f, 0.5f), RowScale, TextColorDefault, GameWidth, true))
                    .Add(null)
                    .Add(new OuiJournalPage.TextCell(GetStatusText(player.Status), new Vector2(0f, 0.5f), RowScale, GetStatusColor(player.Status), StatusWidth, true));
            }

            return table;
        }

        public static string GetStatusText(ArchipelagoClientState status)
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
