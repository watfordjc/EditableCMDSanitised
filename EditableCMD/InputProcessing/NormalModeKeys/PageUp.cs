using System;
using System.Linq;
using System.Runtime.Versioning;
using uk.JohnCook.dotnet.EditableCMDLibrary.Commands;
using uk.JohnCook.dotnet.EditableCMDLibrary.ConsoleSessions;
using uk.JohnCook.dotnet.EditableCMDLibrary.Interop;

namespace uk.JohnCook.dotnet.EditableCMD.InputProcessing.NormalModeKeys
{
    /// <summary>
    /// Class for PageUp key
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class PageUp : ICommandInput
    {
        #region Plugin Implementation Details
        /// <inheritdoc cref="ICommandInput.Name"/>
        public string Name => "PageUp key";
        /// <inheritdoc cref="ICommandInput.Description"/>
        public string Description => "Handles key PageUp.";
        /// <inheritdoc cref="ICommandInput.AuthorName"/>
        public string AuthorName => "John Cook";
        /// <inheritdoc cref="ICommandInput.AuthorTwitchUsername"/>
        public string AuthorTwitchUsername => "WatfordJC";
        /// <inheritdoc cref="ICommandInput.KeysHandled"/>
        public ConsoleKey[]? KeysHandled => new ConsoleKey[] { ConsoleKey.PageUp };
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
        /// Event handler for PageUp key
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
            // If current input is PageUp, we are handling the event
            else if (KeysHandled?.Contains(e.Key.ConsoleKey) == true)
            {
                e.Handled = true;
            }
            // In all other cases we are not handling the event
            else
            {
                return;
            }

            // PageUp (no modifier keys)
            if (!e.Key.HasModifier)
            {
                ConsoleState state = e.State;
                state.autoComplete.AutoCompleteEnd();
                // PageUp - first command in command history
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
                    else
                    {
                        // Add current input to end of command history so DownArrow can return to it
                        state.PreviousCommands.Add(state.Input.Text.ToString());
                    }
                }
                if (currentCommandPosition > 0)
                {
                    state.InputClear(true, 1);
                    state.InputAppend(state.PreviousCommands[0]);
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
