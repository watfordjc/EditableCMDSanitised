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
    /// Class for LeftArrow key
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class LeftArrow : ICommandInput
    {
        #region Plugin Implementation Details
        /// <summary>
        /// Name of the plugin.
        /// </summary>
        public string Name => "LeftArrow key";
        /// <summary>
        /// Summary of the plugin's functionality.
        /// </summary>
        public string Description => "Handles key LeftArrow.";
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
        public ConsoleKey[] KeysHandled => new ConsoleKey[] { ConsoleKey.LeftArrow };
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
        /// Event handler for LeftArrow key
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">The ConsoleKeyEventArgs for the event</param>
        public void ProcessCommand(object sender, NativeMethods.ConsoleKeyEventArgs e)
        {
            if (e.Handled || // Event has already been handled
                !e.Key.KeyDown || // A key was not pressed
                e.State.EditMode // Edit mode is enabled
                )
            {
                return;
            }
            // If current input is LeftArrow, we are handling the event
            else if (KeysHandled.Contains(e.Key.ConsoleKey))
            {
                e.Handled = true;
            }
            // In all other cases we are not handling the event
            else
            {
                return;
            }

            ConsoleState state = e.State;
            state.autoComplete.AutoCompleteEnd();
            // Maths is broken for arrow keys on tabs in /e:off
            if (state.Input.Text.ToString().Contains('\t'))
            {
                SoundUtils.Beep(state.soundPlayer);
                return;
            }
            NativeMethods.COORD currentPosition = ConsoleCursorUtils.GetCurrentCursorPosition();
            int charPositionInCommandString = ConsoleCursorUtils.CursorPositionToCharPosition(state, currentPosition) - 1;
            int tabOffset = ConsoleCursorUtils.GetTabOffset(state, charPositionInCommandString, true);

            // LeftArrow (no key modifiers)
            if (!e.Key.HasModifier)
            {
                if (charPositionInCommandString + tabOffset > 0)
                {
                    // LeftArrow - move cursor to the left
                    ConsoleOutput.UpdateCurrentCommand(state, currentPosition, tabOffset == 0 ? -1 : 0 - tabOffset);
                }
            }
            // Ctrl+LeftArrow
            else if (e.Key.CtrlModifier && !e.Key.AltModifier && !e.Key.ShiftModifier)
            {
                // CTRL+LeftArrow - go to previous space character or start of input
                int previousSpace = -1;
                if (charPositionInCommandString - 2 > 0)
                {
                    previousSpace = state.Input.Text.ToString().LastIndexOf(' ', charPositionInCommandString - 2);
                }
                if (previousSpace == -1)
                {
                    state.MoveCursorToStartOfInput();
                }
                else
                {
                    int charTabOffset = state.Input.Text.ToString().Contains('\t') ? ConsoleCursorUtils.GetTabOffset(state, charPositionInCommandString - 2, true) : 0;
                    ConsoleOutput.UpdateCurrentCommand(state, currentPosition, previousSpace - charPositionInCommandString);
                }
            }
            // Other modifier combinations not currently handled
            else
            {
                e.Handled = false;
            }
        }
    }
}
