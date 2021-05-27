using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        /// <summary>
        /// Name of the plugin.
        /// </summary>
        public string Name => "Printable Character keys";
        /// <summary>
        /// Summary of the plugin's functionality.
        /// </summary>
        public string Description => "Handles Printable Characters.";
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
        /// <remarks>Not currently used for printable characters.</remarks>
        public ConsoleKey[] KeysHandled => null;
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
        /// Event handler for printable characters
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">The ConsoleKeyEventArgs for the event</param>
        public void ProcessCommand(object sender, NativeMethods.ConsoleKeyEventArgs e)
        {
            // Return early if we're not interested in the event
            if (e.Handled || // Event has already been handled
                !e.Key.KeyDown || // A key was not pressed
                (e.Key.CtrlModifier && !e.Key.AltGrModifier) || // Ctrl+PrintableCharacter
                (e.Key.AltModifier && !e.Key.AltGrModifier) || // Alt+PrintableCharacter
                e.State.EditMode // Edit mode is enabled
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

            ConsoleState state = e.State;

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
