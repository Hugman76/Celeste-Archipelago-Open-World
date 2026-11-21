using Microsoft.Xna.Framework;

namespace Celeste.Mod.Celeste_Multiworld.UI
{
    // Journal page summarizing everything required to finish the game.
    internal class OuiJournalGoal : OuiJournalPage
    {
        private static readonly Color DoneColor = new Color(30, 120, 40);
        private static readonly Color NotDoneColor = new Color(160, 40, 40);
        private static readonly Color LabelColor = Color.Black * 0.7f;

        private const float LabelScale = 0.6f;
        private const float ValueScale = 0.6f;

        public OuiJournalGoal(OuiJournal journal) : base(journal)
        {
            MonoMod.Utils.DynamicData dynamicUI = MonoMod.Utils.DynamicData.For(this);
            dynamicUI.Set("TextJustify", new Vector2(0.5f, 0.5f));
            dynamicUI.Set("TextColor", Color.Black * 0.6f);
            this.Journal = journal;

            this.PageTexture = "page";
            this.table = new OuiJournalPage.Table()
                .AddColumn(new OuiJournalPage.TextCell("GOAL", new Vector2(0f, 0.5f), 1f, Color.Black * 0.7f, 0f, false))
                .AddColumn(new OuiJournalPage.EmptyCell(40f))
                .AddColumn(new OuiJournalPage.EmptyCell(700f));

            ArchipelagoManager manager = ArchipelagoManager.Instance;
            int strawberries = Celeste_MultiworldModule.SaveData.Strawberries;
            int required = manager.StrawberriesRequired;
            bool enoughStrawberries = strawberries >= required;
            bool goalAreaCleared = Celeste_MultiworldModule.SaveData.GoalItem;
            bool isPoetryGoal = manager.GoalLevel == "poetry";

            string goalName = modChapterMenu.LevelNameToDisplayName.TryGetValue(manager.GoalLevel, out string displayName) ? displayName : manager.GoalLevel;

            this.table.AddRow().Add(new OuiJournalPage.EmptyCell(32f));

            AddEntry(isPoetryGoal ? "Goal" : "Goal Area", goalName, LabelColor);

            AddEntry(isPoetryGoal ? "Poem Completed" : "Goal Area Cleared",
                     goalAreaCleared ? "Yes" : "No",
                     goalAreaCleared ? DoneColor : NotDoneColor);

            AddEntry("Strawberries",
                     $"{strawberries} / {required}" + (enoughStrawberries ? "" : $"  ({required - strawberries} missing)"),
                     enoughStrawberries ? DoneColor : NotDoneColor);

            if (isPoetryGoal)
            {
                AddEntry("How To Clear", "Sort the poem in the Journal", LabelColor);
            }
            else if (manager.LockGoalLevel)
            {
                AddEntry("Goal Area Access",
                         enoughStrawberries ? "Unlocked" : $"Locked (needs {required} Strawberries)",
                         enoughStrawberries ? DoneColor : NotDoneColor);
            }
            else
            {
                AddEntry("Goal Area Access", "Unlocked", DoneColor);
            }

            bool epilogueUnlocked = enoughStrawberries && goalAreaCleared;
            string epilogueText = "Unlocked";
            if (!epilogueUnlocked)
            {
                if (!enoughStrawberries && !goalAreaCleared)
                {
                    epilogueText = isPoetryGoal ? "Locked (needs Strawberries and the poem)" : "Locked (needs Strawberries and Goal Area clear)";
                }
                else if (!enoughStrawberries)
                {
                    epilogueText = "Locked (needs Strawberries)";
                }
                else
                {
                    epilogueText = isPoetryGoal ? "Locked (needs the poem)" : "Locked (needs Goal Area clear)";
                }
            }
            AddEntry("Epilogue", epilogueText, epilogueUnlocked ? DoneColor : NotDoneColor);

            bool gameCompleted = Celeste_MultiworldModule.SaveData.LevelClearLocations.Contains("8_0_Clear");
            AddEntry("Game Completed", gameCompleted ? "Yes" : "No", gameCompleted ? DoneColor : NotDoneColor);
        }

        private void AddEntry(string label, string value, Color valueColor)
        {
            this.table.AddRow()
                .Add(new OuiJournalPage.TextCell(label, new Vector2(0f, 0.5f), LabelScale, LabelColor, 0f, false))
                .Add(null)
                .Add(new OuiJournalPage.TextCell(value, new Vector2(0f, 0.5f), ValueScale, valueColor, 0f, false));
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
