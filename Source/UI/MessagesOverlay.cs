using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;

namespace Celeste.Mod.Celeste_Multiworld.UI
{
    // Full screen overlay opened from the pause menu: shows the Archipelago message history and lets the player type chat messages and server commands.
    public class MessagesOverlay : Entity
    {
        private const float HistoryLeft = 120f;
        private const float HistoryRight = 1800f;
        private const float HistoryTop = 150f;
        private const float HistoryBottom = 920f;
        private const float HistoryScale = 0.5f;
        private const float InputScale = 0.6f;
        private const int MaxInputLength = 256;
        // Ignore input briefly so the press that opened the overlay is not typed into it
        private const float InputDelay = 0.15f;

        private static readonly Color DefaultTextColor = Color.White;
        private static readonly Color TimeColor = Color.Gray;

        private struct Segment
        {
            public string Text;
            public Color Color;
        }

        // Called once the overlay is closed, to reopen the menu it was opened from
        private readonly Action onClose;

        private string input = "";
        private string status = "";
        private float inputDelayTimer = InputDelay;
        private float caretTimer = 0f;
        private bool closing = false;

        // Lines of the history, oldest first, already wrapped to the history width
        private List<List<Segment>> historyLines = new List<List<Segment>>();
        private int historyVersion = -1;
        // How many lines the view is scrolled up from the newest message
        private int scrollOffset = 0;

        public MessagesOverlay(Action onClose)
        {
            this.onClose = onClose;

            base.Depth = -10000;
            base.Tag = Tags.HUD | Tags.PauseUpdate;
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            TextInput.OnInput += OnTextInput;
        }

        public override void Removed(Scene scene)
        {
            TextInput.OnInput -= OnTextInput;
            base.Removed(scene);
        }

        public override void SceneEnd(Scene scene)
        {
            TextInput.OnInput -= OnTextInput;
            base.SceneEnd(scene);
        }

        private void OnTextInput(char c)
        {
            if (this.closing || this.inputDelayTimer > 0f)
            {
                return;
            }

            if (c == '\b')
            {
                if (this.input.Length > 0)
                {
                    this.input = this.input.Substring(0, this.input.Length - 1);
                }
            }
            else if (c == '\r' || c == '\n')
            {
                Send();
            }
            else if (!char.IsControl(c) && this.input.Length < MaxInputLength)
            {
                this.input += c;
            }
        }

        private void Send()
        {
            string text = this.input.Trim();
            if (text.Length == 0)
            {
                return;
            }

            if (ArchipelagoManager.Instance.SendChatMessage(text))
            {
                this.input = "";
                this.status = "";
                this.scrollOffset = 0;
                Audio.Play("event:/ui/main/button_select");
            }
            else
            {
                this.status = "Not connected to Archipelago, the message was not sent.";
                Audio.Play("event:/ui/main/button_invalid");
            }
        }

        private void Close()
        {
            this.closing = true;
            Audio.Play("event:/ui/main/button_back");
            RemoveSelf();
            this.onClose?.Invoke();
        }

        private bool GamepadPressed(Buttons button)
        {
            return MInput.GamePads[Input.Gamepad].Pressed(button);
        }

        public override void Update()
        {
            base.Update();

            if (this.closing)
            {
                return;
            }

            RefreshHistory();

            if (this.inputDelayTimer > 0f)
            {
                this.inputDelayTimer -= Engine.RawDeltaTime;
                return;
            }

            this.caretTimer += Engine.RawDeltaTime;

            // Raw keys are used instead of the menu bindings, as those can be letters that need to be typed
            if (MInput.Keyboard.Pressed(Keys.Escape) || GamepadPressed(Buttons.B) || GamepadPressed(Buttons.Start))
            {
                Close();
                return;
            }

            int visibleLines = VisibleLineCount();
            int maxScroll = Math.Max(0, this.historyLines.Count - visibleLines);

            if (MInput.Keyboard.Pressed(Keys.Up) || GamepadPressed(Buttons.DPadUp))
            {
                this.scrollOffset++;
            }
            if (MInput.Keyboard.Pressed(Keys.Down) || GamepadPressed(Buttons.DPadDown))
            {
                this.scrollOffset--;
            }
            if (MInput.Keyboard.Pressed(Keys.PageUp))
            {
                this.scrollOffset += visibleLines;
            }
            if (MInput.Keyboard.Pressed(Keys.PageDown))
            {
                this.scrollOffset -= visibleLines;
            }

            this.scrollOffset = Math.Clamp(this.scrollOffset, 0, maxScroll);
        }

        private static float HistoryLineHeight => ActiveFont.LineHeight * HistoryScale;

        private static int VisibleLineCount()
        {
            return Math.Max(1, (int)((HistoryBottom - HistoryTop) / HistoryLineHeight));
        }

        private void RefreshHistory()
        {
            int version = ArchipelagoManager.Instance.MessageHistoryVersion;
            if (version == this.historyVersion)
            {
                return;
            }

            int previousCount = this.historyLines.Count;

            List<List<Segment>> lines = new List<List<Segment>>();
            foreach (ArchipelagoManager.MessageHistoryEntry entry in ArchipelagoManager.Instance.GetMessageHistory())
            {
                List<Segment> segments = new List<Segment>
                {
                    new Segment { Text = $"[{entry.Time:HH:mm}] ", Color = TimeColor }
                };
                segments.AddRange(ParseColoredText(entry.Text, DefaultTextColor));
                lines.AddRange(WrapSegments(segments, HistoryRight - HistoryLeft, HistoryScale));
            }

            // Keep the same messages in view when new ones arrive while scrolled up
            if (this.scrollOffset > 0)
            {
                this.scrollOffset += Math.Max(0, lines.Count - previousCount);
            }

            this.historyLines = lines;
            this.historyVersion = version;
        }

        // Splits text using the "{#RRGGBB}...{#}" color markup used by the popups; any other brace is kept as text
        private static List<Segment> ParseColoredText(string text, Color defaultColor)
        {
            List<Segment> segments = new List<Segment>();
            StringBuilder current = new StringBuilder();
            Color color = defaultColor;

            int i = 0;
            while (i < text.Length)
            {
                int end = (text[i] == '{' && i + 1 < text.Length && text[i + 1] == '#') ? text.IndexOf('}', i) : -1;
                if (end > i)
                {
                    string code = text.Substring(i + 2, end - i - 2);
                    if (code.Length == 0 || (code.Length == 6 && IsHex(code)))
                    {
                        if (current.Length > 0)
                        {
                            segments.Add(new Segment { Text = current.ToString(), Color = color });
                            current.Clear();
                        }
                        color = code.Length == 0 ? defaultColor : Calc.HexToColor(code);
                        i = end + 1;
                        continue;
                    }
                }

                current.Append(text[i]);
                i++;
            }

            if (current.Length > 0)
            {
                segments.Add(new Segment { Text = current.ToString(), Color = color });
            }

            return segments;
        }

        private static bool IsHex(string text)
        {
            foreach (char c in text)
            {
                if (!Uri.IsHexDigit(c))
                {
                    return false;
                }
            }
            return true;
        }

        // Word wraps colored segments so that every line fits in the given width
        private static List<List<Segment>> WrapSegments(List<Segment> segments, float maxWidth, float scale)
        {
            List<List<Segment>> lines = new List<List<Segment>>();
            List<Segment> line = new List<Segment>();
            float lineWidth = 0f;

            foreach (Segment segment in segments)
            {
                foreach (string word in SplitKeepingSpaces(segment.Text.Replace('\n', ' ')))
                {
                    float wordWidth = ActiveFont.Measure(word).X * scale;
                    if (lineWidth + wordWidth > maxWidth && line.Count > 0 && word.Trim().Length > 0)
                    {
                        lines.Add(line);
                        line = new List<Segment>();
                        lineWidth = 0f;
                    }

                    line.Add(new Segment { Text = word, Color = segment.Color });
                    lineWidth += wordWidth;
                }
            }

            if (line.Count > 0)
            {
                lines.Add(line);
            }

            return lines;
        }

        // "Sent  item to" becomes "Sent ", " ", "item ", "to": each piece is a word followed by its spaces
        private static List<string> SplitKeepingSpaces(string text)
        {
            List<string> words = new List<string>();
            StringBuilder current = new StringBuilder();

            foreach (char c in text)
            {
                current.Append(c);
                if (c == ' ')
                {
                    words.Add(current.ToString());
                    current.Clear();
                }
            }

            if (current.Length > 0)
            {
                words.Add(current.ToString());
            }

            return words;
        }

        public override void Render()
        {
            Draw.Rect(0f, 0f, 1920f, 1080f, Color.Black * 0.85f);

            ActiveFont.DrawOutline("ARCHIPELAGO MESSAGES", new Vector2(960f, 80f), new Vector2(0.5f, 0.5f), Vector2.One, Color.White, 2f, Color.Black);

            RenderHistory();
            RenderInput();
        }

        private void RenderHistory()
        {
            if (this.historyLines.Count == 0)
            {
                ActiveFont.Draw("No messages yet.", new Vector2(960f, (HistoryTop + HistoryBottom) / 2f), new Vector2(0.5f, 0.5f), Vector2.One * HistoryScale, Color.Gray);
                return;
            }

            int visibleLines = VisibleLineCount();
            int last = this.historyLines.Count - 1 - this.scrollOffset;
            int first = Math.Max(0, last - visibleLines + 1);

            // The newest visible line sits at the bottom of the history area
            float y = HistoryBottom - HistoryLineHeight * (last - first + 0.5f);
            for (int i = first; i <= last; i++)
            {
                float x = HistoryLeft;
                foreach (Segment segment in this.historyLines[i])
                {
                    ActiveFont.Draw(segment.Text, new Vector2(x, y), new Vector2(0f, 0.5f), Vector2.One * HistoryScale, segment.Color);
                    x += ActiveFont.Measure(segment.Text).X * HistoryScale;
                }
                y += HistoryLineHeight;
            }

            if (first > 0 || this.scrollOffset > 0)
            {
                string scrollHint = this.scrollOffset > 0 ? $"Scrolled up {this.scrollOffset} lines" : "Up/Down to scroll";
                ActiveFont.Draw(scrollHint, new Vector2(HistoryRight, HistoryTop - 25f), new Vector2(1f, 0.5f), Vector2.One * 0.4f, Color.Gray);
            }
        }

        private void RenderInput()
        {
            Draw.Rect(HistoryLeft - 20f, 950f, HistoryRight - HistoryLeft + 40f, 60f, Color.White * 0.12f);

            string caret = (this.caretTimer % 1f) < 0.5f ? "_" : " ";
            string body = this.input + caret;
            string text = "> " + body;

            // Only the end of a long message is shown, so the caret always stays visible
            float maxWidth = HistoryRight - HistoryLeft;
            if (ActiveFont.Measure(text).X * InputScale > maxWidth)
            {
                int start = 0;
                while (start < body.Length - 1 && ActiveFont.Measure("> ..." + body.Substring(start)).X * InputScale > maxWidth)
                {
                    start++;
                }
                text = "> ..." + body.Substring(start);
            }

            ActiveFont.Draw(text, new Vector2(HistoryLeft, 980f), new Vector2(0f, 0.5f), Vector2.One * InputScale, Color.White);

            string help = this.status != "" ? this.status : "Type a message or a command such as !hint, Enter to send, Escape to close";
            Color helpColor = this.status != "" ? Calc.HexToColor("FF5E5E") : Color.Gray;
            ActiveFont.Draw(help, new Vector2(960f, 1040f), new Vector2(0.5f, 0.5f), Vector2.One * 0.4f, helpColor);
        }
    }
}
