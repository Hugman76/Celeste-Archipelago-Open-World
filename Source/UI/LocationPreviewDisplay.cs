using System.Collections.Generic;
using Archipelago.MultiClient.Net.Enums;
using Celeste.Mod.Celeste_Multiworld.Locations;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.Celeste_Multiworld.UI
{
    // When the player stands near an unchecked location, shows which item it holds and who it is for at the bottom of the HUD.
    public class LocationPreviewDisplay : Monocle.Entity
    {
        private const float PreviewDistance = 56f;
        private const int ScanInterval = 10;

        private static readonly Color ProgressionColor = Monocle.Calc.HexToColor("AF99EF");
        private static readonly Color UsefulColor = Monocle.Calc.HexToColor("6D8BE8");
        private static readonly Color TrapColor = Monocle.Calc.HexToColor("FA8072");
        private static readonly Color FillerColor = Monocle.Calc.HexToColor("00EEEE");

        private int scanTimer = 0;
        // The text stays set while fading out, hasPreview says whether a location is currently nearby
        private string text = "";
        private bool hasPreview = false;
        private Color color = Color.White;
        private float alpha = 0f;

        public LocationPreviewDisplay()
        {
            base.Depth = -101;
            base.Tag = Tags.HUD | Tags.Global | Tags.PauseUpdate | Tags.TransitionUpdate;
        }

        public override void Update()
        {
            base.Update();

            if (--this.scanTimer <= 0)
            {
                this.scanTimer = ScanInterval;
                Scan();
            }

            this.alpha = Monocle.Calc.Approach(this.alpha, this.hasPreview ? 1f : 0f, Monocle.Engine.RawDeltaTime * 6f);
        }

        private void Scan()
        {
            Level level = base.Scene as Level;
            Player player = level?.Tracker.GetEntity<Player>();

            this.hasPreview = false;

            if (!Celeste_MultiworldModule.Settings.LocationItemPreview || player == null || level.Session == null || !ArchipelagoManager.Instance.Ready)
            {
                return;
            }

            long bestLocation = -1;
            float bestDistance = PreviewDistance;

            foreach (Monocle.Entity entity in level.Entities)
            {
                float distance = Vector2.Distance(player.Center, entity.Center);
                if (distance > bestDistance)
                {
                    continue;
                }

                if (TryGetUncheckedLocation(entity, level.Session, out long locationID))
                {
                    bestLocation = locationID;
                    bestDistance = distance;
                }
            }

            if (bestLocation < 0 || !ArchipelagoManager.Instance.TryGetLocationItem(bestLocation, out string itemName, out string receiverName, out bool forThisSlot, out ItemFlags flags))
            {
                return;
            }

            this.hasPreview = true;
            this.text = forThisSlot ? $"Your {itemName}" : $"{receiverName}'s {itemName}";
            this.color = GetItemColor(flags);
        }

        // Builds the same location key as the code that sends the location, then checks it was not sent already
        private static bool TryGetUncheckedLocation(Monocle.Entity entity, Session session, out long locationID)
        {
            locationID = -1;

            string prefix = $"{session.Area.ID}_{(int)session.Area.Mode}_";
            string key;
            Dictionary<string, long> locations;
            HashSet<string> checkedLocations;

            switch (entity)
            {
                case Strawberry strawberry:
                    key = prefix + strawberry.ID;
                    locations = APLocationData.StrawberryIDToAP;
                    checkedLocations = Celeste_MultiworldModule.SaveData.StrawberryLocations;
                    break;
                case Key gameKey:
                    key = prefix + gameKey.ID;
                    locations = APLocationData.KeyIDToAP;
                    checkedLocations = Celeste_MultiworldModule.SaveData.KeyLocations;
                    break;
                case HeartGem heart:
                    // Only A-Side hearts are locations of their own, the others count as level clears
                    if (heart.IsFake || session.Area.Mode != AreaMode.Normal)
                    {
                        return false;
                    }
                    key = prefix + "CrystalHeart";
                    locations = APLocationData.CrystalHeartIDToAP;
                    checkedLocations = Celeste_MultiworldModule.SaveData.CrystalHeartLocations;
                    break;
                case Cassette:
                    key = prefix + "Cassette";
                    locations = APLocationData.CassetteIDToAP;
                    checkedLocations = Celeste_MultiworldModule.SaveData.CassetteLocations;
                    break;
                case SummitGem:
                    key = prefix + session.Level;
                    locations = APLocationData.GemIDToAP;
                    checkedLocations = Celeste_MultiworldModule.SaveData.GemLocations;
                    break;
                case Lookout lookout:
                    key = $"{prefix}{session.Level}:{lookout.X}_{lookout.Y}";
                    locations = APLocationData.BinocularsIDToAP;
                    checkedLocations = Celeste_MultiworldModule.SaveData.BinocularsLocations;
                    break;
                case IntroCar:
                    key = prefix + session.Level;
                    locations = APLocationData.CarIDToAP;
                    checkedLocations = Celeste_MultiworldModule.SaveData.CarLocations;
                    break;
                default:
                    return false;
            }

            if (checkedLocations.Contains(key) || !locations.TryGetValue(key, out locationID))
            {
                return false;
            }

            return ArchipelagoManager.Instance.LocationDictionary.ContainsKey(locationID);
        }

        private static Color GetItemColor(ItemFlags flags)
        {
            if ((flags & ItemFlags.Advancement) != 0)
            {
                return ProgressionColor;
            }
            if ((flags & ItemFlags.NeverExclude) != 0)
            {
                return UsefulColor;
            }
            if ((flags & ItemFlags.Trap) != 0)
            {
                return TrapColor;
            }
            return FillerColor;
        }

        public override void Render()
        {
            if (this.alpha <= 0f || this.text == "")
            {
                return;
            }

            ActiveFont.DrawOutline(this.text, new Vector2(960f, 960f), new Vector2(0.5f, 0.5f), Vector2.One * 0.7f, this.color * this.alpha, 2f, Color.Black * this.alpha);
        }
    }
}
