using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.Celeste_Multiworld.UI
{
    // Journal page showing which Cassette items were received. With Require Cassettes, a level's music only plays once its Cassette is received.
    internal class OuiJournalCassettes : OuiJournalPage
    {
        private const float SideWidth = 150f;
        private static readonly string[] SideLetters = { "a", "b", "c" };
        private static readonly string[] SideNames = { "A-Side", "B-Side", "C-Side" };

        public OuiJournalCassettes(OuiJournal journal) : base(journal)
        {
            MonoMod.Utils.DynamicData dynamicUI = MonoMod.Utils.DynamicData.For(this);
            dynamicUI.Set("TextJustify", new Vector2(0.5f, 0.5f));
            dynamicUI.Set("TextColor", Color.Black * 0.6f);
            this.Journal = journal;

            this.PageTexture = "page";
            this.table = new OuiJournalPage.Table()
                .AddColumn(new OuiJournalPage.TextCell("CASSETTES", new Vector2(0f, 0.5f), 1f, Color.Black * 0.7f, 0f, false))
                .AddColumn(new OuiJournalPage.EmptyCell(40f));
            foreach (string sideName in SideNames)
            {
                this.table.AddColumn(new OuiJournalPage.TextCell(sideName, new Vector2(0.5f, 0.5f), 0.5f, Color.Black * 0.7f, SideWidth, true));
            }

            int received = 0;
            int total = 0;

            for (int level = 0; level <= 10; level++)
            {
                bool anySide = false;
                List<OuiJournalPage.Cell> sideCells = new List<OuiJournalPage.Cell>();

                for (int side = 0; side < SideLetters.Length; side++)
                {
                    string levelName = $"{level}{SideLetters[side]}";
                    if (!ArchipelagoManager.Instance.ActiveLevels.Contains(levelName) || !Items.APItemData.AreaModeToCassetteID.TryGetValue(levelName, out long cassetteID))
                    {
                        sideCells.Add(new OuiJournalPage.EmptyCell(SideWidth));
                        continue;
                    }

                    anySide = true;
                    total++;

                    Celeste_MultiworldModule.SaveData.CassetteItems.TryGetValue(cassetteID, out bool haveCassette);
                    if (haveCassette)
                    {
                        received++;
                    }
                    sideCells.Add(new OuiJournalPage.IconsCell(haveCassette ? "cassette" : "dot"));
                }

                if (anySide)
                {
                    OuiJournalPage.Row row = this.table.AddRow()
                        .Add(new OuiJournalPage.TextCell(modJournal.level_to_str[level], new Vector2(1f, 0.5f), 0.6f, Color.Black * 0.6f, 0f, false))
                        .Add(null);
                    foreach (OuiJournalPage.Cell cell in sideCells)
                    {
                        row.Add(cell);
                    }
                }
            }

            this.table.AddRow();
            this.table.AddRow()
                .Add(new OuiJournalPage.TextCell(Dialog.Clean("journal_totals", null), new Vector2(1f, 0.5f), 0.7f, Color.Black * 0.6f, 0f, false))
                .Add(null)
                .Add(new OuiJournalPage.TextCell($"{received} / {total}", new Vector2(0.5f, 0.5f), 0.6f, Color.Black * 0.6f, SideWidth, true));
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
