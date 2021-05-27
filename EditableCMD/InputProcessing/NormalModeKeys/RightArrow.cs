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
    /// Class for RightArrow key
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class RightArrow : ICommandInput
    {
        #region Plugin Implementation Details
        /// <summary>
        /// Name of the plugin.
        /// </summary>
        public string Name => "RightArrow key";
        /// <summary>
        /// Summary of the plugin's functionality.
        /// </summary>
        public string Description => "Handles key RightArrow.";
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
        public ConsoleKey[] KeysHandled => new ConsoleKey[] { ConsoleKey.RightArrow };
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
        /// Event handler for RightArrow key
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
            // If current input is RightArrow, we are handling the event
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
            int charPositionInCommandString = ConsoleCursorUtils.CursorPositionToCharPosition(state, currentPosition);
            int tabOffset = ConsoleCursorUtils.GetTabOffset(state, charPositionInCommandString, true);

            // RightArrow (no modifier keys)
            if (!e.Key.HasModifier)
            {
                if (charPositionInCommandString < state.Input.Length)
                {
                    // RightArrow - move cursor to the right
                    ConsoleOutput.UpdateCurrentCommand(state, currentPosition, tabOffset == 0 ? 1 : tabOffset);
                }
            }
            // Ctrl+RightArrow
            else if (e.Key.CtrlModifier && !e.Key.AltModifier && !e.Key.ShiftModifier)
            {
                // CTRL+RightArrow - go to next space character or end of line
                int nextSpace;
                if (charPositionInCommandString + 1 < state.Input.Length && state.Input.Text[charPositionInCommandString] == ' ')
                {
                    nextSpace = state.Input.Text.ToString().IndexOf(' ', charPositionInCommandString + 1);
                }
                else
                {
                    nextSpace = state.Input.Text.ToString().IndexOf(' ', charPositionInCommandString);
                }
                if (nextSpace == -1)
                {
                    state.MoveCursorToEndOfInput();
                }
                else
                {
                    ConsoleOutput.UpdateCurrentCommand(state, currentPosition, nextSpace - charPositionInCommandString + 1);
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
