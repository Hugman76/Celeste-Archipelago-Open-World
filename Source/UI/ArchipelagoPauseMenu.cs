using System;
using Monocle;

namespace Celeste.Mod.Celeste_Multiworld.UI
{
    // Adds an "Archipelago" entry under Retry in the pause menu, opening a menu with the Archipelago Book and Messages screens.
    public static class ArchipelagoPauseMenu
    {
        public static void Load()
        {
            Everest.Events.Level.OnCreatePauseMenuButtons += AddPauseMenuButton;
        }

        public static void Unload()
        {
            Everest.Events.Level.OnCreatePauseMenuButtons -= AddPauseMenuButton;
        }

        private static void AddPauseMenuButton(Level level, TextMenu menu, bool minimal)
        {
            TextMenu.Button button = new TextMenu.Button("Archipelago");
            button.Pressed(() =>
            {
                int returnIndex = menu.items.IndexOf(button);
                menu.RemoveSelf();
                level.PauseMainMenuOpen = false;
                OpenMenu(level, returnIndex, minimal, 0);
            });
            menu.Add(button);

            // Buttons can only be added at the end, so the new one is then moved right under Retry (or Resume if there is no Retry)
            int anchor = FindButton(menu, "menu_pause_retry");
            if (anchor < 0)
            {
                anchor = FindButton(menu, "menu_pause_resume");
            }
            if (anchor >= 0)
            {
                menu.items.Remove(button);
                menu.items.Insert(anchor + 1, button);
            }
        }

        private static int FindButton(TextMenu menu, string dialogKey)
        {
            string label = Dialog.Clean(dialogKey, null);
            return menu.items.FindIndex(item => item is TextMenu.Button button && button.Label == label);
        }

        // The Archipelago menu; going back reopens the pause menu on the Archipelago entry
        private static void OpenMenu(Level level, int pauseReturnIndex, bool minimal, int selectedEntry)
        {
            TextMenu menu = new TextMenu();
            menu.Add(new TextMenu.Header("ARCHIPELAGO"));

            Action reopen0 = () => OpenMenu(level, pauseReturnIndex, minimal, 0);
            Action reopen1 = () => OpenMenu(level, pauseReturnIndex, minimal, 1);

            TextMenu.Button book = new TextMenu.Button("Archipelago Book");
            book.Pressed(() =>
            {
                menu.RemoveSelf();
                level.Add(new ArchipelagoBookOverlay(level, reopen0));
            });
            menu.Add(book);

            TextMenu.Button messages = new TextMenu.Button("Archipelago Messages");
            messages.Pressed(() =>
            {
                menu.RemoveSelf();
                level.Add(new MessagesOverlay(reopen1));
            });
            menu.Add(messages);

            menu.Selection = menu.items.IndexOf(selectedEntry == 1 ? messages : book);

            Action back = () =>
            {
                Audio.Play("event:/ui/main/button_back");
                menu.RemoveSelf();
                level.Pause(pauseReturnIndex, minimal, false);
            };
            menu.OnCancel = back;
            menu.OnESC = back;
            menu.OnPause = back;

            level.Add(menu);
        }
    }
}
