using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.Celeste_Multiworld.UI
{
    // Journal page(s) listing every location of this slot that has not been checked yet, grouped by chapter.
    internal class OuiJournalMissing : OuiJournalPage
    {
        private const float HeaderScale = 0.45f;
        private const float EntryScale = 0.38f;
        private const float ColumnWidth = 700f;
        private const float TopY = 140f;
        private const float BottomY = 960f;
        private static readonly float[] ColumnX = { 90f, 830f };

        private static readonly Color TitleColor = Color.Black * 0.7f;
        private static readonly Color HeaderColor = Color.Black * 0.8f;
        private static readonly Color EntryColor = Color.Black * 0.6f;

        private struct Line
        {
            public string Text;
            public bool IsHeader;
        }

        private static float LineHeight => ActiveFont.LineHeight * HeaderScale + 4f;
        private static int LinesPerColumn => System.Math.Max(2, (int)((BottomY - TopY) / LineHeight));

        private readonly List<List<Line>> columns;
        private readonly string title;
        private readonly string subtitle;

        public static List<OuiJournalMissing> CreatePages(OuiJournal journal)
        {
            List<string> missing = ArchipelagoManager.Instance.GetMissingLocationNames();
            List<Line> lines = BuildLines(missing);

            // Fill columns, never leaving a chapter header alone at the bottom of a column
            List<List<Line>> allColumns = new List<List<Line>>();
            List<Line> column = new List<Line>();
            foreach (Line line in lines)
            {
                bool full = column.Count >= LinesPerColumn;
                bool orphanHeader = line.IsHeader && column.Count >= LinesPerColumn - 1;
                if (full || orphanHeader)
                {
                    allColumns.Add(column);
                    column = new List<Line>();
                }
                if (!(line.IsHeader == false && line.Text == "" && column.Count == 0))
                {
                    column.Add(line);
                }
            }
            if (column.Count > 0 || allColumns.Count == 0)
            {
                allColumns.Add(column);
            }

            int pageCount = (allColumns.Count + ColumnX.Length - 1) / ColumnX.Length;
            List<OuiJournalMissing> pages = new List<OuiJournalMissing>();
            for (int page = 0; page < pageCount; page++)
            {
                List<List<Line>> pageColumns = allColumns.Skip(page * ColumnX.Length).Take(ColumnX.Length).ToList();
                string title = pageCount > 1 ? $"MISSING LOCATIONS ({page + 1}/{pageCount})" : "MISSING LOCATIONS";
                string subtitle = missing.Count == 1 ? "1 location left" : $"{missing.Count} locations left";
                pages.Add(new OuiJournalMissing(journal, pageColumns, title, subtitle));
            }

            return pages;
        }

        // Location names look like "Forsaken City A - Strawberry 3": the part before " - " is used as the chapter
        private static List<Line> BuildLines(List<string> missing)
        {
            List<Line> lines = new List<Line>();

            if (missing.Count == 0)
            {
                lines.Add(new Line { Text = "Every location has been checked!", IsHeader = true });
                return lines;
            }

            var groups = missing
                .Select(name =>
                {
                    int separator = name.IndexOf(" - ");
                    return separator > 0
                        ? new { Group = name.Substring(0, separator), Entry = name.Substring(separator + 3) }
                        : new { Group = "Other", Entry = name };
                })
                .GroupBy(location => location.Group)
                .OrderBy(group => GetChapterOrder(group.Key))
                .ThenBy(group => group.Key);

            foreach (var group in groups)
            {
                if (lines.Count > 0)
                {
                    lines.Add(new Line { Text = "", IsHeader = false });
                }
                lines.Add(new Line { Text = $"{group.Key} ({group.Count()})", IsHeader = true });
                foreach (var location in group)
                {
                    lines.Add(new Line { Text = location.Entry, IsHeader = false });
                }
            }

            return lines;
        }

        private static int GetChapterOrder(string group)
        {
            foreach (KeyValuePair<int, string> level in modJournal.level_to_str.OrderBy(level => level.Key))
            {
                if (group.StartsWith(level.Value))
                {
                    return level.Key;
                }
            }
            return int.MaxValue;
        }

        private OuiJournalMissing(OuiJournal journal, List<List<Line>> columns, string title, string subtitle) : base(journal)
        {
            MonoMod.Utils.DynamicData dynamicUI = MonoMod.Utils.DynamicData.For(this);
            dynamicUI.Set("TextJustify", new Vector2(0.5f, 0.5f));
            dynamicUI.Set("TextColor", Color.Black * 0.6f);
            this.Journal = journal;

            this.PageTexture = "page";
            this.columns = columns;
            this.title = title;
            this.subtitle = subtitle;
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

            ActiveFont.Draw(this.title, new Vector2(90f, 70f), new Vector2(0f, 0.5f), Vector2.One, TitleColor);
            ActiveFont.Draw(this.subtitle, new Vector2(1530f, 70f), new Vector2(1f, 0.5f), Vector2.One * 0.5f, TitleColor);

            for (int c = 0; c < this.columns.Count && c < ColumnX.Length; c++)
            {
                float y = TopY;
                foreach (Line line in this.columns[c])
                {
                    if (line.Text != "")
                    {
                        float scale = line.IsHeader ? HeaderScale : EntryScale;
                        float x = line.IsHeader ? ColumnX[c] : ColumnX[c] + 20f;
                        ActiveFont.Draw(FitText(line.Text, scale, ColumnWidth - (x - ColumnX[c])), new Vector2(x, y), new Vector2(0f, 0.5f), Vector2.One * scale, line.IsHeader ? HeaderColor : EntryColor);
                    }
                    y += LineHeight;
                }
            }

            Monocle.Draw.SpriteBatch.End();
        }
    }
}
