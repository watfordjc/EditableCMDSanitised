using System;
using System.Linq;
using System.Runtime.Versioning;
using uk.JohnCook.dotnet.EditableCMDLibrary.Commands;
using uk.JohnCook.dotnet.EditableCMDLibrary.ConsoleSessions;
using uk.JohnCook.dotnet.EditableCMDLibrary.Interop;

namespace uk.JohnCook.dotnet.EditableCMD.InputProcessing.NormalModeKeys
{
    /// <summary>
    /// Class for DownArrow key
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class DownArrow : ICommandInput
    {
        #region Plugin Implementation Details
        /// <summary>
        /// Name of the plugin.
        /// </summary>
        public string Name => "DownArrow key";
        /// <summary>
        /// Summary of the plugin's functionality.
        /// </summary>
        public string Description => "Handles key DownArrow.";
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
        public ConsoleKey[] KeysHandled => new ConsoleKey[] { ConsoleKey.DownArrow };
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
        /// Event handler for DownArrow key
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
            // If current input is DownArrow, we are handling the event
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
            // DownArrow (no key modifiers)
            if (!e.Key.HasModifier)
            {
                // DownArrow - command history
                if (state.PreviousCommands.Count == 0)
                {
                    return;
                }
                int currentCommandPosition = state.PreviousCommands.Count;
                if (state.Input.Text.ToString() != string.Empty)
                {
                    if (state.PreviousCommands.Contains(state.Input.Text.ToString()))
                    {
                        currentCommandPosition = state.PreviousCommands.IndexOf(state.Input.Text.ToString());
                    }
                }
                if (currentCommandPosition + 1 < state.PreviousCommands.Count)
                {
                    state.InputClear(true, 1);
                    state.InputAppend(state.PreviousCommands[currentCommandPosition + 1]);
                    state.MoveCursorToStartOfInput();
                    Console.Write(state.Input.Text.ToString());
                    Console.CursorVisible = true;
                }
            }
            // Ctrl+DownArrow
            else if (e.Key.CtrlModifier && !e.Key.AltModifier && !e.Key.ShiftModifier)
            {
                // VT100 sequence for CTRL+DownArrow
                Console.Write("\x1b[1;5B");
            }
            // Other modifier combinations not currently handled
            else
            {
                e.Handled = false;
            }
        }
    }
}
