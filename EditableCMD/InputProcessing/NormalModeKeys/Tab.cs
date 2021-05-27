using System;
using System.Linq;
using System.Runtime.Versioning;
using uk.JohnCook.dotnet.EditableCMDLibrary.Commands;
using uk.JohnCook.dotnet.EditableCMDLibrary.ConsoleSessions;
using uk.JohnCook.dotnet.EditableCMDLibrary.Interop;
using uk.JohnCook.dotnet.EditableCMDLibrary.Utils;

namespace uk.JohnCook.dotnet.EditableCMD.InputProcessing.NormalModeKeys
{
    /// <summary>
    /// Class for Tab key
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class Tab : ICommandInput
    {
        #region Plugin Implementation Details
        /// <summary>
        /// Name of the plugin.
        /// </summary>
        public string Name => "Tab key";
        /// <summary>
        /// Summary of the plugin's functionality.
        /// </summary>
        public string Description => "Handles key Tab.";
        /// <summary>
        /// Author's name (can be <see cref="string.Empty"/>)
        /// </summary>
        public string AuthorName => "John Cook";
        /// <summary>
        /// Author's Twitch username (can be <see cref="string.Empty"/>)
        /// </summary>
        public string AuthorTwitchUsername => "WatfordJC";
        /// <summary>
        /// An array of the keys handled by the plugin. For commands, this should be <see cref="ConsoleKey.Enter"/>.
        /// </summary>
        public ConsoleKey[] KeysHandled => new ConsoleKey[] { ConsoleKey.Tab };
        /// <summary>
        /// Whether the plugin handles keys/commands input in normal mode (such as a command entered at the prompt).
        /// </summary>
        public bool NormalModeHandled => true;
        /// <summary>
        /// Whether the plugin handles keys input in edit mode.
        /// </summary>
        public bool EditModeHandled => false;
        /// <summary>
        /// Whether the plugin handles keys input in mark mode.
        /// </summary>
        public bool MarkModeHandled => false;
        /// <summary>
        /// An array of commands handled by the plugin, in lowercase.
        /// </summary>
        public string[] CommandsHandled => null;
        #endregion

        /// <summary>
        /// Event handler for Tab key
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">The ConsoleKeyEventArgs for the event</param>
        public void ProcessCommand(object sender, NativeMethods.ConsoleKeyEventArgs e)
        {
            // Return early if we're not interested in the event
            if (e.Handled || // Event has already been handled
                !e.Key.KeyDown || // A key was not pressed
                e.State.EditMode // Edit mode is enabled
                )
            {
                return;
            }
            // If current input is Tab, we are handling the event
            else if (KeysHandled.Contains(e.Key.ConsoleKey))
            {
                e.Handled = true;
            }
            // In all other cases we are not handling the event
            else
            {
                return;
            }

            // Any combination of modifiers with Tab that include Alt are handled by the operating system
            if (e.Key.AltModifier)
            {
                e.Handled = false;
                return;
            }
            // Any combination of modifiers with Tab that include Ctrl are not currently handled
            if (e.Key.CtrlModifier)
            {
                e.Handled = false;
                return;
            }

            ConsoleState state = e.State;

            // Tab characters when filename completion is disabled (/f:off) are tab characters.
            if (!state.StartupParams.FileCompletion)
            {
                SoundUtils.Beep(state.soundPlayer);
                return;
                NativeMethods.COORD currentPosition = ConsoleCursorUtils.GetCurrentCursorPosition();
                int charPositionInCommandString = ConsoleCursorUtils.CursorPositionToCharPosition(state, currentPosition);
                int totalTabOffset = state.Input.Text.ToString().Contains('\t') ? ConsoleCursorUtils.GetTabOffset(state, state.Input.Length, false) : 0;
                if (!state.OverwriteMode || charPositionInCommandString == state.Input.Length)
                {
                    state.InputInsert(charPositionInCommandString, '\t');
                    state.Input.TabPositions.Add(charPositionInCommandString);
                }
                else
                {
                    state.Input.Text.Replace(state.Input.Text[charPositionInCommandString], '\t', charPositionInCommandString, 1);
                }
                state.InputClear(false, totalTabOffset);
                // Refresh display of current command - ineffecient, but deals with line-wrapping
                ConsoleOutput.UpdateCurrentCommand(state, currentPosition, 8 - (currentPosition.X % 8));
                return;
            }

            state.autoComplete.Reset();
            // Tab
            if (!e.Key.HasModifier)
            {
                state.autoComplete.Complete(state, false);
            }
            // Shift+Tab
            else if (e.Key.ShiftModifier && !e.Key.CtrlModifier)
            {
                state.autoComplete.Complete(state, true);
            }
        }
    }
}
