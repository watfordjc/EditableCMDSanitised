using System;
using System.Diagnostics.CodeAnalysis;
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
        /// <inheritdoc cref="ICommandInput.Name"/>
        public string Name => "LeftArrow key";
        /// <inheritdoc cref="ICommandInput.Description"/>
        public string Description => "Handles key LeftArrow.";
        /// <inheritdoc cref="ICommandInput.AuthorName"/>
        public string AuthorName => "John Cook";
        /// <inheritdoc cref="ICommandInput.AuthorTwitchUsername"/>
        public string AuthorTwitchUsername => "WatfordJC";
        /// <inheritdoc cref="ICommandInput.KeysHandled"/>
        public ConsoleKey[]? KeysHandled => new ConsoleKey[] { ConsoleKey.LeftArrow };
        /// <inheritdoc cref="ICommandInput.NormalModeHandled"/>
        public bool NormalModeHandled => true;
        /// <inheritdoc cref="ICommandInput.EditModeHandled"/>
        public bool EditModeHandled => false;
        /// <inheritdoc cref="ICommandInput.MarkModeHandled"/>
        public bool MarkModeHandled => false;
        /// <inheritdoc cref="ICommandInput.CommandsHandled"/>
        public string[]? CommandsHandled => null;
        #endregion

        private ConsoleState? state;

        /// <inheritdoc cref="ICommandInput.Init(ConsoleState)"/>
        [MemberNotNull(nameof(state))]
        public void Init(ConsoleState state)
        {
            this.state = state;
        }

        /// <summary>
        /// Event handler for LeftArrow key
        /// </summary>
        /// <inheritdoc cref="ICommandInput.ProcessCommand(object, NativeMethods.ConsoleKeyEventArgs)" path="param"/>
        public void ProcessCommand(object? sender, NativeMethods.ConsoleKeyEventArgs e)
        {
            // Call Init() again if state isn't set
            if (state == null)
            {
                Init(e.State);
            }
            if (e.Handled || // Event has already been handled
                !e.Key.KeyDown || // A key was not pressed
                state.EditMode // Edit mode is enabled
                )
            {
                return;
            }
            // If current input is LeftArrow, we are handling the event
            else if (KeysHandled?.Contains(e.Key.ConsoleKey) == true)
            {
                e.Handled = true;
            }
            // In all other cases we are not handling the event
            else
            {
                return;
            }

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
