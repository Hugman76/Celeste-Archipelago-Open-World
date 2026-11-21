using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.Celeste_Multiworld.Items.Traps;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.Celeste_Multiworld.UI
{
    // Journal page listing the options the seed was generated with, as received in the slot data.
    internal class OuiJournalSeedSettings : OuiJournalPage
    {
        private const float LabelWidth = 330f;
        private const float GapWidth = 20f;
        private const float ValueWidth = 330f;
        private const float RowScale = 0.45f;
        private const float RightColumnX = 830f;

        private static readonly Color LabelColor = Color.Black * 0.6f;
        private static readonly Color ValueColor = Color.Black * 0.85f;

        public OuiJournalSeedSettings(OuiJournal journal) : base(journal)
        {
            MonoMod.Utils.DynamicData dynamicUI = MonoMod.Utils.DynamicData.For(this);
            dynamicUI.Set("TextJustify", new Vector2(0.5f, 0.5f));
            dynamicUI.Set("TextColor", Color.Black * 0.6f);
            this.Journal = journal;

            this.PageTexture = "page";

            ArchipelagoManager manager = ArchipelagoManager.Instance;

            List<KeyValuePair<string, string>> left = new List<KeyValuePair<string, string>>
            {
                Entry("Goal Area", modChapterMenu.LevelNameToDisplayName.TryGetValue(manager.GoalLevel, out string goalName) ? goalName : manager.GoalLevel),
                Entry("Lock Goal Area", OnOff(manager.LockGoalLevel)),
                Entry("Strawberries Required", manager.StrawberriesRequired.ToString()),
                Entry("Active Levels", manager.ActiveLevels.Count.ToString()),
                Entry("Include B-Sides", OnOff(manager.IncludeBSides)),
                Entry("Include C-Sides", OnOff(manager.IncludeCSides)),
                Entry("Include Core", OnOff(manager.IncludeCore)),
                Entry("Include Farewell", FarewellText(manager.IncludeFarewellMode)),
                Entry("Include Goldens", OnOff(manager.IncludeGoldens)),
                Entry("Golden Amnesty", manager.GoldenAmnesty.ToString()),
                Entry("Checkpointsanity", OnOff(manager.Checkpointsanity)),
                Entry("Keysanity", OnOff(manager.Keysanity)),
                Entry("Gemsanity", OnOff(manager.Gemsanity)),
                Entry("Binosanity", OnOff(manager.Binosanity)),
                Entry("Carsanity", OnOff(manager.Carsanity)),
                Entry("Roomsanity", OnOff(manager.Roomsanity)),
            };

            List<KeyValuePair<string, string>> right = new List<KeyValuePair<string, string>>
            {
                Entry("Death Link", OnOff(manager.DeathLinkActive)),
            };
            if (manager.DeathLinkActive)
            {
                right.Add(Entry("Death Link Amnesty", manager.DeathLinkAmnesty.ToString()));
                right.Add(Entry("Death Link Received", manager.DeathLinkReceiptStyle == 1 ? "Restart Level" : "Die"));
            }
            right.Add(Entry("Trap Link", OnOff(manager.TrapLinkActive)));

            int enabledTraps = TrapManager.EnabledTraps.Values.Count(weight => weight > 0);
            right.Add(Entry("Traps Enabled", enabledTraps.ToString()));
            if (enabledTraps > 0)
            {
                right.Add(Entry("Trap Duration", TrapDurationText()));
            }

            right.Add(Entry("Music Shuffle", MusicShuffleText(manager.MusicShuffle)));
            right.Add(Entry("Require Cassettes", OnOff(manager.RequireCassettes)));
            right.Add(Entry("Dash Shuffle", ModeText(manager.DashShuffle)));
            right.Add(Entry("Climb Shuffle", ModeText(manager.ClimbShuffle)));
            right.Add(Entry("Crouch Shuffle", OnOff(manager.CrouchShuffle)));
            right.Add(Entry("Split Interactables", SplitInteractablesText(manager.SplitInteractables)));

            this.leftTable = BuildTable("SEED SETTINGS", left);
            this.rightTable = BuildTable(" ", right);
        }

        private static KeyValuePair<string, string> Entry(string label, string value)
        {
            return new KeyValuePair<string, string>(label, value);
        }

        private static OuiJournalPage.Table BuildTable(string header, List<KeyValuePair<string, string>> entries)
        {
            OuiJournalPage.Table table = new OuiJournalPage.Table()
                .AddColumn(new OuiJournalPage.TextCell(header, new Vector2(0f, 0.5f), 1f, Color.Black * 0.7f, LabelWidth, true))
                .AddColumn(new OuiJournalPage.EmptyCell(GapWidth))
                .AddColumn(new OuiJournalPage.EmptyCell(ValueWidth));

            foreach (KeyValuePair<string, string> entry in entries)
            {
                table.AddRow()
                    .Add(new OuiJournalPage.TextCell(entry.Key, new Vector2(0f, 0.5f), RowScale, LabelColor, LabelWidth, true))
                    .Add(null)
                    .Add(new OuiJournalPage.TextCell(entry.Value, new Vector2(0f, 0.5f), RowScale, ValueColor, ValueWidth, true));
            }

            return table;
        }

        private static string OnOff(bool value)
        {
            return value ? "On" : "Off";
        }

        private static string FarewellText(int mode)
        {
            switch (mode)
            {
                case 1:
                    return "Empty Space";
                case 2:
                    return "Farewell";
                default:
                    return "None";
            }
        }

        private static string MusicShuffleText(int mode)
        {
            switch (mode)
            {
                case 1:
                    return "Consistent";
                case 2:
                    return "Singularity";
                default:
                    return "None";
            }
        }

        private static string TrapDurationText()
        {
            int amount = TrapManager.ExpirationAmount;
            switch (TrapManager.ExpirationAction)
            {
                case TrapExpirationAction.Deaths:
                    return amount == 1 ? "1 Death" : $"{amount} Deaths";
                case TrapExpirationAction.Screens:
                    return amount == 1 ? "1 New Screen" : $"{amount} New Screens";
                default:
                    return "Until Leaving the Level";
            }
        }

        // Shuffle options are absent (-1) from seeds that predate them, in which case the moves are always available
        private static string ModeText(int mode)
        {
            return mode < 0 ? "Off" : $"Mode {mode}";
        }

        private static string SplitInteractablesText(int mode)
        {
            switch (mode)
            {
                case 1:
                    return "Per Chapter";
                case 2:
                    return "Per Side";
                case 3:
                    return "Per Chapter and Side";
                default:
                    return "Off";
            }
        }

        public override void Redraw(Monocle.VirtualRenderTarget buffer)
        {
            base.Redraw(buffer);
            Monocle.Draw.SpriteBatch.Begin();
            this.leftTable.Render(new Vector2(60f, 20f));
            this.rightTable.Render(new Vector2(RightColumnX, 20f));
            Monocle.Draw.SpriteBatch.End();
        }

        private OuiJournalPage.Table leftTable;
        private OuiJournalPage.Table rightTable;
    }
}
