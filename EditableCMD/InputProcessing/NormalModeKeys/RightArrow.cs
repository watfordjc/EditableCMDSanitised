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
        /// <inheritdoc cref="ICommandInput.Name"/>
        public string Name => "RightArrow key";
        /// <inheritdoc cref="ICommandInput.Description"/>
        public string Description => "Handles key RightArrow.";
        /// <inheritdoc cref="ICommandInput.AuthorName"/>
        public string AuthorName => "John Cook";
        /// <inheritdoc cref="ICommandInput.AuthorTwitchUsername"/>
        public string AuthorTwitchUsername => "WatfordJC";
        /// <inheritdoc cref="ICommandInput.KeysHandled"/>
        public ConsoleKey[]? KeysHandled => new ConsoleKey[] { ConsoleKey.RightArrow };
        /// <inheritdoc cref="ICommandInput.NormalModeHandled"/>
        public bool NormalModeHandled => true;
        /// <inheritdoc cref="ICommandInput.EditModeHandled"/>
        public bool EditModeHandled => false;
        /// <inheritdoc cref="ICommandInput.MarkModeHandled"/>
        public bool MarkModeHandled => false;
        /// <inheritdoc cref="ICommandInput.CommandsHandled"/>
        public string[]? CommandsHandled => null;
        #endregion

        /// <summary>
        /// Event handler for RightArrow key
        /// </summary>
        /// <inheritdoc cref="ICommandInput.ProcessCommand(object, NativeMethods.ConsoleKeyEventArgs)" path="param"/>
        public void ProcessCommand(object? sender, NativeMethods.ConsoleKeyEventArgs e)
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
            else if (KeysHandled?.Contains(e.Key.ConsoleKey) == true)
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
