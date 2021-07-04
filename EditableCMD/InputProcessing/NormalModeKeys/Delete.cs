using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using uk.JohnCook.dotnet.EditableCMDLibrary.Commands;
using uk.JohnCook.dotnet.EditableCMDLibrary.ConsoleSessions;
using uk.JohnCook.dotnet.EditableCMDLibrary.Interop;
using uk.JohnCook.dotnet.EditableCMDLibrary.Utils;

namespace uk.JohnCook.dotnet.EditableCMD.InputProcessing.NormalModeKeys
{
    /// <summary>
    /// Class for Delete key
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class Delete : ICommandInput
    {
        #region Plugin Implementation Details
        /// <inheritdoc cref="ICommandInput.Name"/>
        public string Name => "Delete key";
        /// <inheritdoc cref="ICommandInput.Description"/>
        public string Description => "Handles key Delete.";
        /// <inheritdoc cref="ICommandInput.AuthorName"/>
        public string AuthorName => "John Cook";
        /// <inheritdoc cref="ICommandInput.AuthorTwitchUsername"/>
        public string AuthorTwitchUsername => "WatfordJC";
        /// <inheritdoc cref="ICommandInput.KeysHandled"/>
        public ConsoleKey[]? KeysHandled => new ConsoleKey[] { ConsoleKey.Delete };
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
        /// Event handler for Delete key
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
            // If current input is Delete, we are handling the event
            else if (KeysHandled?.Contains(e.Key.ConsoleKey) == true)
            {
                e.Handled = true;
            }
            // In all other cases we are not handling the event
            else
            {
                return;
            }

            // Delete (no modifier keys)
            if (!e.Key.HasModifier)
            {
                ConsoleState state = e.State;
                state.autoComplete.AutoCompleteEnd();
                // Delete key functionality
                NativeMethods.COORD currentPosition = ConsoleCursorUtils.GetCurrentCursorPosition();
                int charPositionInCommandString = ConsoleCursorUtils.CursorPositionToCharPosition(state, currentPosition);
                int totalTabOffset = state.Input.Text.ToString().Contains('\t') ? ConsoleCursorUtils.GetTabOffset(state, state.Input.Length, false) : 0;
                if (charPositionInCommandString < state.Input.Length)
                {
                    bool charIsTab = state.Input.TabPositions.Contains(charPositionInCommandString);
                    int charLength = charIsTab ? ConsoleCursorUtils.GetTabOffset(state, charPositionInCommandString, true) : 1;
                    state.Input.Text.Remove(charPositionInCommandString, 1);
                    if (charIsTab)
                    {
                        state.Input.TabPositions.Remove(charPositionInCommandString);
                        List<int> newTabList = new();
                        foreach (int tabPosition in state.Input.TabPositions)
                        {
                            int newPosition = tabPosition > charPositionInCommandString ? tabPosition - 1 : tabPosition;
                            newTabList.Add(newPosition);
                        }
                        state.Input.TabPositions.Clear();
                        state.Input.TabPositions.AddRange(newTabList);
                    }
                    state.InputClear(false, charLength + totalTabOffset);
                    ConsoleOutput.UpdateCurrentCommand(state, currentPosition, 0);
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
