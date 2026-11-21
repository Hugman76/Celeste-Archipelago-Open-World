using System;
using System.Collections.Generic;
using Celeste.Mod.Celeste_Multiworld.Items.Traps;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.Celeste_Multiworld.UI
{
    // Lists the active traps and how long each one still lasts, under the connection status in the top-right corner of the HUD.
    public class TrapDisplay : Monocle.Entity
    {
        private static readonly Color TrapColor = Monocle.Calc.HexToColor("FA8072");
        private static readonly Color QueueColor = Monocle.Calc.HexToColor("FAFAD2");

        private const float TopY = 90f;
        private const float LineHeight = 38f;
        private const float TextScale = 0.5f;

        private float alpha = 0f;

        public TrapDisplay()
        {
            base.Depth = -101;
            base.Tag = Tags.HUD | Tags.Global | Tags.PauseUpdate | Tags.TransitionUpdate;
        }

        private static List<string> GetLines()
        {
            List<string> lines = new List<string>();

            if (TrapManager.Instance == null)
            {
                return lines;
            }

            foreach (BaseTrapInstance trap in TrapManager.Instance.ActiveTraps)
            {
                lines.Add($"{GetTrapName(trap.type)}: {GetRemainingText(trap)}");
            }

            int queued = TrapManager.Instance.QueuedTraps.Count;
            if (queued > 0)
            {
                lines.Add(queued == 1 ? "1 trap queued" : $"{queued} traps queued");
            }

            return lines;
        }

        private static string GetTrapName(TrapType type)
        {
            long itemID = 0xCA10000 + (int)type;
            return Items.APItemData.ItemIDToString.TryGetValue(itemID, out string name) ? name : type.ToString();
        }

        private static string GetRemainingText(BaseTrapInstance trap)
        {
            switch (trap.expirationAction)
            {
                case TrapExpirationAction.Deaths:
                {
                    int remaining = Math.Max(0, trap.expirationAmount - trap.trackedDeaths);
                    return remaining == 1 ? "1 death left" : $"{remaining} deaths left";
                }
                case TrapExpirationAction.Screens:
                {
                    // The screen the trap started in counts, and the trap ends upon entering one screen past the amount
                    int remaining = Math.Clamp(trap.expirationAmount - trap.trackedScreens.Count + 1, 0, trap.expirationAmount);
                    return remaining == 1 ? "1 screen left" : $"{remaining} screens left";
                }
                default:
                {
                    return "until you leave the level";
                }
            }
        }

        public override void Update()
        {
            base.Update();

            bool show = Celeste_MultiworldModule.Settings.ActiveTrapsDisplay && GetLines().Count > 0;
            this.alpha = Monocle.Calc.Approach(this.alpha, show ? 1f : 0f, Monocle.Engine.RawDeltaTime * 4f);
        }

        public override void Render()
        {
            if (this.alpha <= 0f || !Celeste_MultiworldModule.Settings.ActiveTrapsDisplay)
            {
                return;
            }

            List<string> lines = GetLines();
            int queuedLine = TrapManager.Instance != null && TrapManager.Instance.QueuedTraps.Count > 0 ? lines.Count - 1 : -1;

            for (int i = 0; i < lines.Count; i++)
            {
                Color color = (i == queuedLine ? QueueColor : TrapColor) * this.alpha;
                ActiveFont.DrawOutline(lines[i], new Vector2(1890f, TopY + i * LineHeight), new Vector2(1f, 0.5f), Vector2.One * TextScale, color, 2f, Color.Black * this.alpha);
            }
        }
    }
}
