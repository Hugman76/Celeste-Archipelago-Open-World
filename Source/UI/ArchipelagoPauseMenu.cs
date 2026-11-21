using System;
using Monocle;

namespace Celeste.Mod.Celeste_Multiworld.UI
{
    // Adds the Archipelago entries to the pause menu, set apart from the game's own entries.
    public static class ArchipelagoPauseMenu
    {
        public static void Load()
        {
            Everest.Events.Level.OnCreatePauseMenuButtons += AddPauseMenuButtons;
        }

        public static void Unload()
        {
            Everest.Events.Level.OnCreatePauseMenuButtons -= AddPauseMenuButtons;
        }

        private static void AddPauseMenuButtons(Level level, TextMenu menu, bool minimal)
        {
            // An empty, non-selectable line as a small gap above the Archipelago entries
            menu.Add(new TextMenu.SubHeader(""));

            AddOverlayButton("Archipelago Messages", level, menu, minimal, returnIndex => new MessagesOverlay(level, returnIndex, minimal));
            AddOverlayButton("Archipelago Hints and Players", level, menu, minimal, returnIndex => new ArchipelagoInfoOverlay(level, returnIndex, minimal));
        }

        // The button closes the pause menu and opens the overlay, which reopens the pause menu on this button when it closes
        private static void AddOverlayButton(string label, Level level, TextMenu menu, bool minimal, Func<int, Entity> createOverlay)
        {
            TextMenu.Button button = new TextMenu.Button(label);
            button.Pressed(() =>
            {
                int returnIndex = menu.items.IndexOf(button);
                menu.RemoveSelf();
                level.PauseMainMenuOpen = false;
                level.Add(createOverlay(returnIndex));
            });
            menu.Add(button);
        }
    }
}
