using System;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net.Enums;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.Celeste_Multiworld.UI
{
    // Full screen overlay opened from the pause menu, with tabs showing the hints, letting the player request new hints, and listing the players.
    public class ArchipelagoInfoOverlay : Entity
    {
        private enum Tab
        {
            Hints,
            RequestHint,
            Players
        }

        private static readonly string[] TabNames = { "Hints", "Request a Hint", "Players" };

        private const float ContentLeft = 120f;
        private const float ContentRight = 1800f;
        private const float ContentTop = 300f;
        private const float ContentBottom = 960f;
        private const float RowScale = 0.5f;
        private const float InputDelay = 0.15f;

        private static readonly Color DimColor = Color.Gray;
        private static readonly Color SelectedColor = Calc.HexToColor("84FF54");
        private static readonly Color ErrorColor = Calc.HexToColor("FF5E5E");
        private static readonly Color GoodColor = Calc.HexToColor("7CFC00");
        private static readonly Color ProgressionColor = Calc.HexToColor("AF99EF");
        private static readonly Color UsefulColor = Calc.HexToColor("6D8BE8");
        private static readonly Color TrapColor = Calc.HexToColor("FA8072");
        private static readonly Color FillerColor = Calc.HexToColor("00EEEE");

        private readonly Level level;
        private readonly int returnIndex;
        private readonly bool minimal;

        private Tab tab = Tab.Hints;
        // First visible row of the current tab, and the selected row of the hint request list
        private int scrollOffset = 0;
        private int selection = 0;
        private float inputDelayTimer = InputDelay;
        private bool closing = false;
        private string status = "";
        private Color statusColor = Color.White;

        private List<ArchipelagoManager.HintDisplayInfo> hints = new List<ArchipelagoManager.HintDisplayInfo>();
        private List<ArchipelagoManager.HintableItem> hintableItems = new List<ArchipelagoManager.HintableItem>();
        private List<ArchipelagoManager.PlayerDisplayInfo> players = new List<ArchipelagoManager.PlayerDisplayInfo>();
        private int hintsVersion = -1;
        private float refreshTimer = 0f;

        public ArchipelagoInfoOverlay(Level level, int returnIndex, bool minimal)
        {
            this.level = level;
            this.returnIndex = returnIndex;
            this.minimal = minimal;

            base.Depth = -10000;
            base.Tag = Tags.HUD | Tags.PauseUpdate;

            Refresh();
        }

        private void Refresh()
        {
            ArchipelagoManager manager = ArchipelagoManager.Instance;
            this.hints = manager.GetHintDisplayList();
            this.hintableItems = manager.GetHintableItems();
            this.players = manager.GetPlayerDisplayList();
            this.hintsVersion = manager.TrackedHints.Length;
            this.selection = Math.Clamp(this.selection, 0, Math.Max(0, this.hintableItems.Count - 1));
        }

        private static float RowHeight => ActiveFont.LineHeight * RowScale + 6f;
        private static int VisibleRows => Math.Max(1, (int)((ContentBottom - ContentTop) / RowHeight));

        private int RowCount
        {
            get
            {
                switch (this.tab)
                {
                    case Tab.Hints:
                        return this.hints.Count;
                    case Tab.RequestHint:
                        return this.hintableItems.Count;
                    default:
                        return this.players.Count;
                }
            }
        }

        private void Close()
        {
            this.closing = true;
            Audio.Play("event:/ui/main/button_back");
            RemoveSelf();
            this.level.Pause(this.returnIndex, this.minimal, false);
        }

        public override void Update()
        {
            base.Update();

            if (this.closing)
            {
                return;
            }

            // Hints and statuses change as the server sends updates, so the lists are refreshed regularly
            this.refreshTimer -= Engine.RawDeltaTime;
            if (this.refreshTimer <= 0f || ArchipelagoManager.Instance.TrackedHints.Length != this.hintsVersion)
            {
                this.refreshTimer = 1f;
                Refresh();
            }

            if (this.inputDelayTimer > 0f)
            {
                this.inputDelayTimer -= Engine.RawDeltaTime;
                return;
            }

            // Pause is not used to close: on keyboards it is usually bound to Enter, which also confirms
            if (Input.MenuCancel.Pressed || Input.ESC.Pressed)
            {
                Close();
                return;
            }

            if (Input.MenuLeft.Pressed || Input.MenuRight.Pressed)
            {
                int tabCount = TabNames.Length;
                this.tab = (Tab)(((int)this.tab + (Input.MenuRight.Pressed ? 1 : tabCount - 1)) % tabCount);
                this.scrollOffset = 0;
                this.status = "";
                Audio.Play("event:/ui/main/rollover_up");
            }

            int maxScroll = Math.Max(0, this.RowCount - VisibleRows);

            if (this.tab == Tab.RequestHint)
            {
                int previous = this.selection;
                if (Input.MenuUp.Pressed)
                {
                    this.selection--;
                }
                if (Input.MenuDown.Pressed)
                {
                    this.selection++;
                }
                this.selection = Math.Clamp(this.selection, 0, Math.Max(0, this.hintableItems.Count - 1));
                if (this.selection != previous)
                {
                    Audio.Play(this.selection < previous ? "event:/ui/main/rollover_up" : "event:/ui/main/rollover_down");
                }

                // Keep the selection visible
                if (this.selection < this.scrollOffset)
                {
                    this.scrollOffset = this.selection;
                }
                else if (this.selection >= this.scrollOffset + VisibleRows)
                {
                    this.scrollOffset = this.selection - VisibleRows + 1;
                }

                if (Input.MenuConfirm.Pressed)
                {
                    RequestSelectedHint();
                }
            }
            else
            {
                if (Input.MenuUp.Pressed)
                {
                    this.scrollOffset--;
                }
                if (Input.MenuDown.Pressed)
                {
                    this.scrollOffset++;
                }
            }

            this.scrollOffset = Math.Clamp(this.scrollOffset, 0, maxScroll);
        }

        private void RequestSelectedHint()
        {
            if (this.hintableItems.Count == 0)
            {
                return;
            }

            ArchipelagoManager.HintableItem item = this.hintableItems[this.selection];
            bool alreadyHinted = IsHinted(item.Name);

            // Asking again for an item that is already hinted costs nothing
            if (!alreadyHinted && !ArchipelagoManager.Instance.CanAffordHint)
            {
                this.status = $"Not enough hint points ({ArchipelagoManager.Instance.HintPoints} / {ArchipelagoManager.Instance.HintCost}).";
                this.statusColor = ErrorColor;
                Audio.Play("event:/ui/main/button_invalid");
                return;
            }

            if (ArchipelagoManager.Instance.RequestHint(item.Name))
            {
                this.status = $"Hint requested for {item.Name}. The answer appears in the Hints tab.";
                this.statusColor = GoodColor;
                this.refreshTimer = 0.5f;
                Audio.Play("event:/ui/main/button_select");
            }
            else
            {
                this.status = "Not connected to Archipelago, the hint was not requested.";
                this.statusColor = ErrorColor;
                Audio.Play("event:/ui/main/button_invalid");
            }
        }

        private bool IsHinted(string itemName)
        {
            return this.hints.Any(hint => hint.IsForThisSlot && hint.ItemName == itemName);
        }

        public override void Render()
        {
            Draw.Rect(0f, 0f, 1920f, 1080f, Color.Black * 0.85f);

            ActiveFont.DrawOutline("ARCHIPELAGO", new Vector2(960f, 70f), new Vector2(0.5f, 0.5f), Vector2.One, Color.White, 2f, Color.Black);

            RenderTabs();
            RenderSummary();

            switch (this.tab)
            {
                case Tab.Hints:
                    RenderHints();
                    break;
                case Tab.RequestHint:
                    RenderRequestList();
                    break;
                default:
                    RenderPlayers();
                    break;
            }

            RenderFooter();
        }

        private void RenderTabs()
        {
            float x = 960f - 600f;
            for (int i = 0; i < TabNames.Length; i++)
            {
                bool selected = i == (int)this.tab;
                ActiveFont.Draw(TabNames[i], new Vector2(x + i * 600f, 145f), new Vector2(0.5f, 0.5f), Vector2.One * (selected ? 0.7f : 0.55f), selected ? Color.White : DimColor);
            }
        }

        private void RenderSummary()
        {
            ArchipelagoManager manager = ArchipelagoManager.Instance;
            int strawberries = Celeste_MultiworldModule.SaveData.Strawberries;
            string goalName = modChapterMenu.LevelNameToDisplayName.TryGetValue(manager.GoalLevel, out string name) ? name : manager.GoalLevel;
            string goalState = Celeste_MultiworldModule.SaveData.GoalItem ? "cleared" : "not cleared";

            string summary = $"Strawberries: {strawberries} / {manager.StrawberriesRequired}     Goal: {goalName} ({goalState})     Hint Points: {manager.HintPoints} (next hint costs {manager.HintCost})";
            ActiveFont.Draw(summary, new Vector2(960f, 215f), new Vector2(0.5f, 0.5f), Vector2.One * 0.45f, Color.White * 0.8f);
        }

        private void DrawRow(string text, float x, float width, float y, Color color)
        {
            ActiveFont.Draw(OuiJournalScrollPage.FitText(text, RowScale, width), new Vector2(x, y), new Vector2(0f, 0.5f), Vector2.One * RowScale, color);
        }

        private float RowY(int visibleIndex)
        {
            return ContentTop + RowHeight * (visibleIndex + 0.5f);
        }

        private void RenderHints()
        {
            if (this.hints.Count == 0)
            {
                ActiveFont.Draw("No hints yet. Request one in the next tab.", new Vector2(960f, 500f), new Vector2(0.5f, 0.5f), Vector2.One * RowScale, DimColor);
                return;
            }

            DrawRow("Item", ContentLeft, 480f, ContentTop - 25f, DimColor);
            DrawRow("Location", 640f, 580f, ContentTop - 25f, DimColor);
            DrawRow("Player", 1250f, 330f, ContentTop - 25f, DimColor);
            DrawRow("Status", 1610f, 190f, ContentTop - 25f, DimColor);

            int row = 0;
            foreach (ArchipelagoManager.HintDisplayInfo hint in this.hints.Skip(this.scrollOffset).Take(VisibleRows))
            {
                float y = RowY(row++);
                Color textColor = hint.Found ? DimColor : Color.White;
                DrawRow(hint.ItemName, ContentLeft, 480f, y, hint.Found ? DimColor : GetItemColor(hint.Flags));
                DrawRow(hint.LocationName, 640f, 580f, y, textColor);
                DrawRow(OuiJournalHints.GetPlayerText(hint), 1250f, 330f, y, textColor);
                DrawRow(OuiJournalHints.GetStatusText(hint), 1610f, 190f, y, hint.Found ? DimColor : Color.White);
            }
        }

        private void RenderRequestList()
        {
            if (this.hintableItems.Count == 0)
            {
                ActiveFont.Draw("Nothing left to hint: every useful item was received.", new Vector2(960f, 500f), new Vector2(0.5f, 0.5f), Vector2.One * RowScale, DimColor);
                return;
            }

            DrawRow("Item", ContentLeft + 40f, 900f, ContentTop - 25f, DimColor);
            DrawRow("Type", 1250f, 300f, ContentTop - 25f, DimColor);

            int row = 0;
            for (int i = this.scrollOffset; i < this.hintableItems.Count && row < VisibleRows; i++)
            {
                ArchipelagoManager.HintableItem item = this.hintableItems[i];
                float y = RowY(row++);
                bool selected = i == this.selection;
                bool hinted = IsHinted(item.Name);

                if (selected)
                {
                    ActiveFont.Draw(">", new Vector2(ContentLeft, y), new Vector2(0f, 0.5f), Vector2.One * RowScale, SelectedColor);
                }

                Color color = selected ? SelectedColor : (hinted ? DimColor : Color.White);
                DrawRow(hinted ? $"{item.Name} (already hinted)" : item.Name, ContentLeft + 40f, 1050f, y, color);
                DrawRow(item.Category, 1250f, 300f, y, selected ? SelectedColor : DimColor);
            }
        }

        private void RenderPlayers()
        {
            DrawRow("Player", ContentLeft, 600f, ContentTop - 25f, DimColor);
            DrawRow("Game", 760f, 600f, ContentTop - 25f, DimColor);
            DrawRow("Status", 1400f, 400f, ContentTop - 25f, DimColor);

            int row = 0;
            foreach (ArchipelagoManager.PlayerDisplayInfo player in this.players.Skip(this.scrollOffset).Take(VisibleRows))
            {
                float y = RowY(row++);
                Color statusColor = player.Status == ArchipelagoClientState.ClientGoal ? GoodColor : (player.Status == ArchipelagoClientState.ClientUnknown ? DimColor : Color.White);
                DrawRow(player.IsThisSlot ? $"{player.Name} (You)" : player.Name, ContentLeft, 600f, y, player.IsThisSlot ? ProgressionColor : Color.White);
                DrawRow(player.Game ?? "", 760f, 600f, y, Color.White);
                DrawRow(OuiJournalPlayers.GetStatusText(player.Status), 1400f, 400f, y, statusColor);
            }
        }

        private void RenderFooter()
        {
            int maxScroll = Math.Max(0, this.RowCount - VisibleRows);
            if (maxScroll > 0)
            {
                int first = this.scrollOffset + 1;
                int last = Math.Min(this.RowCount, this.scrollOffset + VisibleRows);
                ActiveFont.Draw($"{first}-{last} of {this.RowCount}", new Vector2(ContentRight, ContentTop - 25f), new Vector2(1f, 0.5f), Vector2.One * 0.4f, DimColor);
            }

            string help = this.tab == Tab.RequestHint
                ? "Left/Right: change tab   Up/Down: choose an item   Confirm: request a hint   Back: close"
                : "Left/Right: change tab   Up/Down: scroll   Back: close";

            if (this.status != "")
            {
                ActiveFont.Draw(this.status, new Vector2(960f, 1000f), new Vector2(0.5f, 0.5f), Vector2.One * 0.45f, this.statusColor);
            }
            ActiveFont.Draw(help, new Vector2(960f, 1045f), new Vector2(0.5f, 0.5f), Vector2.One * 0.4f, DimColor);
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
    }
}
