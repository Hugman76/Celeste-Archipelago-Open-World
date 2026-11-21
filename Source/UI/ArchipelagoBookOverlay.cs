using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.Celeste_Multiworld.UI
{
    // Full screen overlay opened from the pause menu, showing every page of the Archipelago Journal during a level.
    public class ArchipelagoBookOverlay : Entity
    {
        // Same size as the page buffers of the chapter select Journal
        private const int PageWidth = 1610;
        private const int PageHeight = 1000;
        private const float PageScale = 0.92f;
        private const float InputDelay = 0.15f;

        private readonly Level level;
        // Called once the book is closed, to reopen the menu it was opened from
        private readonly Action onClose;

        private OuiJournal journal;
        private List<OuiJournalPage> pages;
        private VirtualRenderTarget buffer;
        private int pageIndex = 0;
        private bool needsRedraw = true;
        private bool drawnOnce = false;
        private float inputDelayTimer = InputDelay;
        private bool closing = false;

        public ArchipelagoBookOverlay(Level level, Action onClose, int startPage = 0)
        {
            this.level = level;
            this.onClose = onClose;
            this.pageIndex = startPage;

            base.Depth = -10000;
            base.Tag = Tags.HUD | Tags.PauseUpdate;
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);

            // The Journal textures belong to the chapter select, so they may not be loaded during a level
            if (MTN.Journal == null)
            {
                MTN.Load();
            }

            this.buffer = VirtualContent.CreateRenderTarget("archipelago-book-page", PageWidth, PageHeight);

            // The pages only use their journal to know which buffer to redraw into
            this.journal = new OuiJournal();
            this.journal.CurrentPageBuffer = this.buffer;
            this.pages = modJournal.CreatePages(this.journal);
            for (int i = 0; i < this.pages.Count; i++)
            {
                this.pages[i].PageIndex = i;
            }
            this.pageIndex = Math.Clamp(this.pageIndex, 0, Math.Max(0, this.pages.Count - 1));
        }

        public override void Removed(Scene scene)
        {
            DisposeBuffer();
            base.Removed(scene);
        }

        public override void SceneEnd(Scene scene)
        {
            DisposeBuffer();
            base.SceneEnd(scene);
        }

        private void DisposeBuffer()
        {
            if (this.buffer != null)
            {
                this.buffer.Dispose();
                this.buffer = null;
            }
        }

        private void Close()
        {
            this.closing = true;
            Audio.Play("event:/ui/main/button_back");
            RemoveSelf();
            this.onClose?.Invoke();
        }

        // Pages are drawn into their buffer during the update, as the chapter select Journal does, never in the middle of rendering
        private void RedrawPage()
        {
            this.pages[this.pageIndex].Redraw(this.buffer);
            Engine.Graphics.GraphicsDevice.SetRenderTarget(null);
            this.needsRedraw = false;
            this.drawnOnce = true;
        }

        public override void Update()
        {
            base.Update();

            if (this.closing || this.pages == null || this.pages.Count == 0)
            {
                return;
            }

            if (this.needsRedraw)
            {
                RedrawPage();
            }

            if (this.inputDelayTimer > 0f)
            {
                this.inputDelayTimer -= Engine.RawDeltaTime;
                return;
            }

            if (Input.MenuCancel.Pressed || Input.ESC.Pressed)
            {
                Close();
                return;
            }

            int previousPage = this.pageIndex;
            if (Input.MenuLeft.Pressed && this.pageIndex > 0)
            {
                this.pageIndex--;
            }
            else if (Input.MenuRight.Pressed && this.pageIndex < this.pages.Count - 1)
            {
                this.pageIndex++;
            }

            if (this.pageIndex != previousPage)
            {
                Audio.Play(this.pageIndex > previousPage ? "event:/ui/world_map/journal/page_main_forward" : "event:/ui/world_map/journal/page_main_back");
                RedrawPage();
                return;
            }

            // Scrollable pages handle Up/Down themselves and redraw into the journal's current buffer. Other pages, like the
            // poem, rely on the chapter select Journal to run their animations, so they are only shown here.
            if (this.pages[this.pageIndex] is OuiJournalScrollPage scrollPage)
            {
                scrollPage.Update();
                Engine.Graphics.GraphicsDevice.SetRenderTarget(null);
            }
        }

        // In the chapter select, the Journal itself draws the paper behind its pages, which only hold their content
        private void RenderPaper(Vector2 position)
        {
            string paper = this.pages[this.pageIndex].PageTexture;
            if (MTN.Journal != null && !string.IsNullOrEmpty(paper) && MTN.Journal.Has(paper))
            {
                MTexture texture = MTN.Journal[paper];
                Vector2 scale = new Vector2(PageWidth * PageScale / texture.Width, PageHeight * PageScale / texture.Height);
                texture.Draw(position, Vector2.Zero, Color.White, scale);
            }
            else
            {
                Draw.Rect(position.X, position.Y, PageWidth * PageScale, PageHeight * PageScale, new Color(240, 232, 214));
            }
        }

        public override void Render()
        {
            Draw.Rect(0f, 0f, 1920f, 1080f, Color.Black * 0.85f);

            if (this.drawnOnce && this.buffer != null)
            {
                Vector2 position = new Vector2((1920f - PageWidth * PageScale) / 2f, 20f);
                RenderPaper(position);
                Draw.SpriteBatch.Draw(this.buffer.Target, position, null, Color.White, 0f, Vector2.Zero, PageScale, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
            }

            if (this.pages != null)
            {
                ActiveFont.Draw($"Page {this.pageIndex + 1} / {this.pages.Count}", new Vector2(960f, 1000f), new Vector2(0.5f, 0.5f), Vector2.One * 0.45f, Color.White * 0.8f);
            }
            ActiveFont.Draw("Left/Right: turn the page   Up/Down: scroll   Back: close", new Vector2(960f, 1045f), new Vector2(0.5f, 0.5f), Vector2.One * 0.4f, Color.Gray);
        }
    }
}
