using System;
using System.Linq;
using System.Runtime.Versioning;
using uk.JohnCook.dotnet.EditableCMDLibrary.Commands;
using uk.JohnCook.dotnet.EditableCMDLibrary.ConsoleSessions;
using uk.JohnCook.dotnet.EditableCMDLibrary.Interop;

namespace uk.JohnCook.dotnet.EditableCMD.InputProcessing.NormalModeKeys
{
    /// <summary>
    /// Class for PageDown key
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class PageDown : ICommandInput
    {
        #region Plugin Implementation Details
        /// <summary>
        /// Name of the plugin.
        /// </summary>
        public string Name => "PageDown key";
        /// <summary>
        /// Summary of the plugin's functionality.
        /// </summary>
        public string Description => "Handles key PageDown.";
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
        public ConsoleKey[] KeysHandled => new ConsoleKey[] { ConsoleKey.PageDown };
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
        /// Event handler for PageDown key
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
            // If current input is PageDown, we are handling the event
            else if (KeysHandled.Contains(e.Key.ConsoleKey))
            {
                e.Handled = true;
            }
            // In all other cases we are not handling the event
            else
            {
                return;
            }

            // PageDown (no modifier keys)
            if (!e.Key.HasModifier)
            {
                ConsoleState state = e.State;
                state.autoComplete.AutoCompleteEnd();
                // PageDown - latest command in command history
                if (state.PreviousCommands.Count == 0)
                {
                    return; ;
                }
                int currentCommandPosition = state.PreviousCommands.Count;
                if (state.Input.Text.ToString() != string.Empty)
                {
                    if (state.PreviousCommands.Contains(state.Input.Text.ToString()))
                    {
                        currentCommandPosition = state.PreviousCommands.IndexOf(state.Input.Text.ToString());
                    }
                }
                if (state.Input.Text.ToString() == string.Empty || currentCommandPosition + 1 < state.PreviousCommands.Count)
                {
                    state.InputClear(true, 1);
                    state.InputAppend(state.PreviousCommands[^1]);
                    state.MoveCursorToStartOfInput();
                    Console.Write(state.Input.Text.ToString());
                    Console.CursorVisible = true;
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
