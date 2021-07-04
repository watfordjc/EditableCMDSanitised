using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Versioning;
using uk.JohnCook.dotnet.EditableCMDLibrary.Commands;
using uk.JohnCook.dotnet.EditableCMDLibrary.ConsoleSessions;
using uk.JohnCook.dotnet.EditableCMDLibrary.Interop;

namespace uk.JohnCook.dotnet.EditableCMD.InputProcessing.NormalModeKeys
{
    /// <summary>
    /// Class for UpArrow key
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class UpArrow : ICommandInput
    {
        #region Plugin Implementation Details
        /// <inheritdoc cref="ICommandInput.Name"/>
        public string Name => "UpArrow key";
        /// <inheritdoc cref="ICommandInput.Description"/>
        public string Description => "Handles key UpArrow.";
        /// <inheritdoc cref="ICommandInput.AuthorName"/>
        public string AuthorName => "John Cook";
        /// <inheritdoc cref="ICommandInput.AuthorTwitchUsername"/>
        public string AuthorTwitchUsername => "WatfordJC";
        /// <inheritdoc cref="ICommandInput.KeysHandled"/>
        public ConsoleKey[]? KeysHandled => new ConsoleKey[] { ConsoleKey.UpArrow };
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
        /// Event handler for UpArrow key
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
            // If current input is UpArrow, we are handling the event
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

            // UpArrow (no modifier keys)
            if (!e.Key.HasModifier)
            {
                // UpArrow - command history
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
                    state.InputAppend(state.PreviousCommands[currentCommandPosition - 1]);
                    state.MoveCursorToStartOfInput();
                    Console.Write(state.Input.Text.ToString());
                    Console.CursorVisible = true;
                }
            }
            // Ctrl+UpArrow
            else if (e.Key.CtrlModifier && !e.Key.AltModifier && !e.Key.ShiftModifier)
            {
                // VT100 sequence for CTRL+UpArrow
                Console.Write("\x1b[1;5A");
            }
            // Other modifier combinations not currently handled
            else
            {
                e.Handled = false;
            }
        }
    }
}
