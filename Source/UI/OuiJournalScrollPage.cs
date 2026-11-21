using System;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.Celeste_Multiworld.UI
{
    // Journal page whose list can be scrolled with Up/Down, for content that does not fit on one page.
    internal abstract class OuiJournalScrollPage : OuiJournalPage
    {
        private static readonly Color IndicatorColor = Color.Black * 0.5f;

        // Index of the first visible item of the list
        protected int ScrollOffset { get; private set; } = 0;

        protected abstract int ItemCount { get; }
        protected abstract int VisibleItemCount { get; }

        protected OuiJournalScrollPage(OuiJournal journal) : base(journal)
        {
            MonoMod.Utils.DynamicData dynamicUI = MonoMod.Utils.DynamicData.For(this);
            dynamicUI.Set("TextJustify", new Vector2(0.5f, 0.5f));
            dynamicUI.Set("TextColor", Color.Black * 0.6f);
            this.Journal = journal;
            this.PageTexture = "page";
        }

        protected int MaxScrollOffset => Math.Max(0, this.ItemCount - this.VisibleItemCount);

        public override void Update()
        {
            base.Update();

            int offset = this.ScrollOffset;
            if (Input.MenuUp.Pressed)
            {
                offset--;
            }
            if (Input.MenuDown.Pressed)
            {
                offset++;
            }
            offset = Math.Clamp(offset, 0, this.MaxScrollOffset);

            if (offset != this.ScrollOffset)
            {
                Audio.Play(offset < this.ScrollOffset ? "event:/ui/main/rollover_up" : "event:/ui/main/rollover_down");
                this.ScrollOffset = offset;
                this.Redraw(this.Journal.CurrentPageBuffer);
            }
        }

        // Must be called between SpriteBatch.Begin and End: tells which part of the list is shown and that it can be scrolled
        protected void DrawScrollIndicators(float topY, float bottomY)
        {
            if (this.MaxScrollOffset == 0)
            {
                return;
            }

            int first = this.ScrollOffset + 1;
            int last = Math.Min(this.ItemCount, this.ScrollOffset + this.VisibleItemCount);

            ActiveFont.Draw($"{first}-{last} of {this.ItemCount}", new Vector2(1550f, topY), new Vector2(1f, 0.5f), Vector2.One * 0.4f, IndicatorColor);

            if (this.ScrollOffset > 0)
            {
                ActiveFont.Draw("More above (Up)", new Vector2(1550f, topY + 30f), new Vector2(1f, 0.5f), Vector2.One * 0.4f, IndicatorColor);
            }
            if (this.ScrollOffset < this.MaxScrollOffset)
            {
                ActiveFont.Draw("More below (Down)", new Vector2(1550f, bottomY), new Vector2(1f, 0.5f), Vector2.One * 0.4f, IndicatorColor);
            }
        }

        // Names from other games can be long, so they are shortened to fit their column
        public static string FitText(string text, float scale, float maxWidth)
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
    }
}
