using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using System.Text;
using uk.JohnCook.dotnet.EditableCMDLibrary.Commands;
using uk.JohnCook.dotnet.EditableCMDLibrary.ConsoleSessions;
using uk.JohnCook.dotnet.EditableCMDLibrary.Interop;
using uk.JohnCook.dotnet.EditableCMDLibrary.Utils;

namespace uk.JohnCook.dotnet.EditableCMD.InputProcessing.NormalModeKeys
{
    /// <summary>
    /// Class for printable characters
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class PrintableCharacter : ICommandInput
    {
        #region Plugin Implementation Details
        /// <inheritdoc cref="ICommandInput.Name"/>
        public string Name => "Printable Character keys";
        /// <inheritdoc cref="ICommandInput.Description"/>
        public string Description => "Handles Printable Characters.";
        /// <inheritdoc cref="ICommandInput.AuthorName"/>
        public string AuthorName => "John Cook";
        /// <inheritdoc cref="ICommandInput.AuthorTwitchUsername"/>
        public string AuthorTwitchUsername => "WatfordJC";
        /// <inheritdoc cref="ICommandInput.KeysHandled"/>
        /// <remarks>Not currently used for printable characters.</remarks>
        public ConsoleKey[]? KeysHandled => null;
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
        /// Event handler for printable characters
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
                (e.Key.CtrlModifier && !e.Key.AltGrModifier) || // Ctrl+PrintableCharacter
                (e.Key.AltModifier && !e.Key.AltGrModifier) || // Alt+PrintableCharacter
                state.EditMode // Edit mode is enabled
                )
            {
                return;
            }
            // If current input is a printable character, we are handling the event
            else if (e.KeyEventRecord.UnicodeChar > 31 && e.KeyEventRecord.UnicodeChar != 127)
            {
                e.Handled = true;
            }
            // In all other cases we are not handling the event
            else
            {
                return;
            }

            // Insert printable character at current cursor position, replace next character if in overwrite mode
            NativeMethods.COORD currentPosition = ConsoleCursorUtils.GetCurrentCursorPosition();
            int charPositionInCommandString = ConsoleCursorUtils.CursorPositionToCharPosition(state, currentPosition);
#if DEBUG
            Debug.WriteLine(string.Format("Current position: {0}", charPositionInCommandString));
#endif
            int totalTabOffset = state.Input.Text.ToString().Contains('\t') ? ConsoleCursorUtils.GetTabOffset(state, state.Input.Length, false) : 0;
#if DEBUG
            StringBuilder sb = new();
            sb.Append("Previous array: ");
            foreach (char c in state.Input.Text.ToString())
            {
                if (c > 31 && c != 127)
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append(string.Format("{{{0}}}", (int)c));
                }
            }
            Debug.WriteLine(sb.ToString());
#endif
            if (charPositionInCommandString == state.Input.Length)
            {
                state.InputInsert(charPositionInCommandString, e.KeyEventRecord.UnicodeChar);
            }
            else if (!state.OverwriteMode && charPositionInCommandString >= 0 && charPositionInCommandString < state.Input.Length)
            {
                state.InputInsert(charPositionInCommandString, e.KeyEventRecord.UnicodeChar);
                if (state.Input.TabPositions.Count > 0 && state.Input.TabPositions[^1] >= charPositionInCommandString)
                {
                    state.Input.TabPositions.Remove(charPositionInCommandString);
                    List<int> newTabList = new();
                    foreach (int tabPosition in state.Input.TabPositions)
                    {
                        int newPosition = tabPosition > charPositionInCommandString ? tabPosition + 1 : tabPosition;
                        newTabList.Add(newPosition);
                    }
                    state.Input.TabPositions.Clear();
                    state.Input.TabPositions.AddRange(newTabList);
                }
            }
            else if (state.OverwriteMode && charPositionInCommandString >= 0)
            {
                state.Input.Text[charPositionInCommandString] = e.KeyEventRecord.UnicodeChar;
            }
            else
            {
                // Shouldn't be reachable. If reached, ignore the character.
                return;
            }
#if DEBUG
            Debug.Write("Current array: ");
            foreach (char c in state.Input.Text.ToString())
            {
                if (c > 31 && c != 127)
                {
                    Debug.Write(c);
                }
                else
                {
                    Debug.Write(string.Format("{{{0}}}", (int)c));
                }
            }
            Debug.Write("\n----------\n");
#endif
            state.InputClear(false, totalTabOffset + 1);
            // Refresh display of current command - ineffecient, but deals with line-wrapping
            ConsoleOutput.UpdateCurrentCommand(state, currentPosition, 1);
            state.autoComplete.AutoCompleteEnd();
        }

    }
}
