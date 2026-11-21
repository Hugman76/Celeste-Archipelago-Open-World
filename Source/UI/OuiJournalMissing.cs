using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.Celeste_Multiworld.UI
{
    // Journal page listing every location of this slot that has not been checked yet, grouped by chapter. Scrolls with Up/Down.
    internal class OuiJournalMissing : OuiJournalScrollPage
    {
        private const float HeaderScale = 0.45f;
        private const float EntryScale = 0.4f;
        private const float LeftX = 90f;
        private const float EntryIndent = 30f;
        private const float RightX = 1400f;
        private const float TopY = 150f;
        private const float BottomY = 930f;

        private static readonly Color TitleColor = Color.Black * 0.7f;
        private static readonly Color HeaderColor = Color.Black * 0.8f;
        private static readonly Color EntryColor = Color.Black * 0.6f;

        private struct Line
        {
            public string Text;
            public bool IsHeader;
        }

        private static float LineHeight => ActiveFont.LineHeight * HeaderScale + 4f;

        private readonly List<Line> lines;
        private readonly string subtitle;

        protected override int ItemCount => this.lines.Count;
        protected override int VisibleItemCount => System.Math.Max(1, (int)((BottomY - TopY) / LineHeight));

        public OuiJournalMissing(OuiJournal journal) : base(journal)
        {
            List<string> missing = ArchipelagoManager.Instance.GetMissingLocationNames();
            this.lines = BuildLines(missing);
            this.subtitle = missing.Count == 1 ? "1 location left" : $"{missing.Count} locations left";
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

        public override void Redraw(Monocle.VirtualRenderTarget buffer)
        {
            base.Redraw(buffer);
            Monocle.Draw.SpriteBatch.Begin();

            ActiveFont.Draw("MISSING LOCATIONS", new Vector2(LeftX, 70f), new Vector2(0f, 0.5f), Vector2.One, TitleColor);
            ActiveFont.Draw(this.subtitle, new Vector2(LeftX, 120f), new Vector2(0f, 0.5f), Vector2.One * 0.45f, TitleColor);

            float y = TopY + LineHeight / 2f;
            foreach (Line line in this.lines.Skip(this.ScrollOffset).Take(this.VisibleItemCount))
            {
                float scale = line.IsHeader ? HeaderScale : EntryScale;
                float x = line.IsHeader ? LeftX : LeftX + EntryIndent;
                ActiveFont.Draw(FitText(line.Text, scale, RightX - x), new Vector2(x, y), new Vector2(0f, 0.5f), Vector2.One * scale, line.IsHeader ? HeaderColor : EntryColor);
                y += LineHeight;
            }

            DrawScrollIndicators(70f, 960f);

            Monocle.Draw.SpriteBatch.End();
        }
    }
}
