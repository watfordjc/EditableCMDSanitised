﻿using System;
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
    /// Class for End key
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class End : ICommandInput
    {
        #region Plugin Implementation Details
        /// <inheritdoc cref="ICommandInput.Name"/>
        public string Name => "End key";
        /// <inheritdoc cref="ICommandInput.Description"/>
        public string Description => "Handles key End.";
        /// <inheritdoc cref="ICommandInput.AuthorName"/>
        public string AuthorName => "John Cook";
        /// <inheritdoc cref="ICommandInput.AuthorTwitchUsername"/>
        public string AuthorTwitchUsername => "WatfordJC";
        /// <inheritdoc cref="ICommandInput.KeysHandled"/>
        public ConsoleKey[]? KeysHandled => new ConsoleKey[] { ConsoleKey.End };
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
        /// Event handler for End key
        /// </summary>
        /// <inheritdoc cref="ICommandInput.ProcessCommand(object, NativeMethods.ConsoleKeyEventArgs)" path="param"/>
        public void ProcessCommand(object? sender, NativeMethods.ConsoleKeyEventArgs e)
        {
            // Call Init() again if state isn't set
            if (state == null)
            {
                Init(e.State);
            }
            // Return early if we're not interested in the event
            if (e.Handled || // Event has already been handled
                !e.Key.KeyDown || // A key was not pressed
                state.EditMode // Edit mode is enabled
                )
            {
                return;
            }
            // If current input is End, we are handling the event
            else if (KeysHandled?.Contains(e.Key.ConsoleKey) == true)
            {
                e.Handled = true;
            }
            // In all other cases we are not handling the event
            else
            {
                return;
            }

            // End (no modifier keys)
            if (!e.Key.HasModifier)
            {
                state.autoComplete.AutoCompleteEnd();
                // Move cursor to end of input
                state.MoveCursorToEndOfInput();
            }
            // Ctrl+End
            else if (e.Key.CtrlModifier && !e.Key.AltModifier && !e.Key.ShiftModifier)
            {
                state.autoComplete.AutoCompleteEnd();
                // CTRL+End - Delete all characters to the right
                NativeMethods.COORD currentPosition = ConsoleCursorUtils.GetCurrentCursorPosition();
                int charPositionInCommandString = ConsoleCursorUtils.CursorPositionToCharPosition(state, currentPosition);
                state.InputClear(false, 1);
                state.Input.Text.Remove(charPositionInCommandString, state.Input.Length - charPositionInCommandString);
                ConsoleOutput.UpdateCurrentCommand(state, currentPosition, 0);
            }
            // Other modifier combinations not currently handled
            else
            {
                e.Handled = false;
            }
        }
    }
}
