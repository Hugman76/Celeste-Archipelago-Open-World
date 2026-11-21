using System;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.Celeste_Multiworld.UI
{
    // Shows the state of the Archipelago connection in the top-right corner of the HUD.
    // While connected, it is only displayed in the pause menu; any other state is always displayed.
    public class ConnectionDisplay : Monocle.Entity
    {
        private static readonly Color ConnectedColor = Monocle.Calc.HexToColor("7CFC00");
        private static readonly Color ReconnectingColor = Monocle.Calc.HexToColor("FFA500");
        private static readonly Color DisconnectedColor = Monocle.Calc.HexToColor("FF5E5E");

        private float alpha = 0f;
        private float pulseTimer = 0f;

        public ConnectionDisplay()
        {
            base.Depth = -101;
            base.Tag = Tags.HUD | Tags.Global | Tags.PauseUpdate | Tags.TransitionUpdate;
        }

        private static string GetStatusText(ArchipelagoManager.ConnectionStatus status)
        {
            switch (status)
            {
                case ArchipelagoManager.ConnectionStatus.Connected:
                    return "Archipelago: Connected";
                case ArchipelagoManager.ConnectionStatus.Connecting:
                    return "Archipelago: Connecting...";
                case ArchipelagoManager.ConnectionStatus.Reconnecting:
                    return $"Archipelago: Connection lost, reconnecting... (attempt {ArchipelagoManager.Instance.ReconnectAttemptCount})";
                case ArchipelagoManager.ConnectionStatus.WaitingToReconnect:
                    return "Archipelago: Connection lost, retrying shortly...";
                default:
                    return "Archipelago: Disconnected";
            }
        }

        private static Color GetStatusColor(ArchipelagoManager.ConnectionStatus status)
        {
            switch (status)
            {
                case ArchipelagoManager.ConnectionStatus.Connected:
                    return ConnectedColor;
                case ArchipelagoManager.ConnectionStatus.Disconnected:
                    return DisconnectedColor;
                default:
                    return ReconnectingColor;
            }
        }

        public override void Update()
        {
            base.Update();

            Level level = base.Scene as Level;
            bool paused = level != null && level.Paused;
            bool connected = ArchipelagoManager.Instance.CurrentConnectionStatus == ArchipelagoManager.ConnectionStatus.Connected;

            float target = (!connected || paused) ? 1f : 0f;
            this.alpha = Monocle.Calc.Approach(this.alpha, target, Monocle.Engine.RawDeltaTime * 4f);
            this.pulseTimer += Monocle.Engine.RawDeltaTime;
        }

        public override void Render()
        {
            if (this.alpha <= 0f)
            {
                return;
            }

            ArchipelagoManager.ConnectionStatus status = ArchipelagoManager.Instance.CurrentConnectionStatus;

            float drawAlpha = this.alpha;
            if (status != ArchipelagoManager.ConnectionStatus.Connected)
            {
                // Gentle pulse so a lost connection gets noticed without being distracting
                drawAlpha *= 0.75f + 0.25f * (float)Math.Sin(this.pulseTimer * 4f);
            }

            ActiveFont.DrawOutline(GetStatusText(status), new Vector2(1890f, 40f), new Vector2(1f, 0.5f), Vector2.One * 0.6f, GetStatusColor(status) * drawAlpha, 2f, Color.Black * drawAlpha);
        }
    }
}
