using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using uk.JohnCook.dotnet.EditableCMD.InputProcessing;
using uk.JohnCook.dotnet.EditableCMDLibrary.Commands;
using uk.JohnCook.dotnet.EditableCMDLibrary.ConsoleSessions;
using uk.JohnCook.dotnet.EditableCMDLibrary.Interop;
using static uk.JohnCook.dotnet.EditableCMDLibrary.ConsoleSessions.ConsoleState;

namespace uk.JohnCook.dotnet.EditableCMD.Commands
{
    /// <summary>
    /// EDIT and UNDO commands
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class EnterEditMode : ICommandInput, IDisposable
    {
        #region Plugin Implementation Details
        /// <inheritdoc cref="ICommandInput.Name"/>
        public string Name => "EnterEditMode";
        /// <inheritdoc cref="ICommandInput.Description"/>
        public string Description => "Handles command EDIT and UNDO to enter edit-mode.";
        /// <inheritdoc cref="ICommandInput.AuthorName"/>
        public string AuthorName => "John Cook";
        /// <inheritdoc cref="ICommandInput.AuthorTwitchUsername"/>
        public string AuthorTwitchUsername => "WatfordJC";
        /// <inheritdoc cref="ICommandInput.KeysHandled"/>
        public ConsoleKey[]? KeysHandled => new ConsoleKey[] { ConsoleKey.Enter };
        /// <inheritdoc cref="ICommandInput.NormalModeHandled"/>
        public bool NormalModeHandled => true;
        /// <inheritdoc cref="ICommandInput.EditModeHandled"/>
        public bool EditModeHandled => false;
        /// <inheritdoc cref="ICommandInput.MarkModeHandled"/>
        public bool MarkModeHandled => false;
        /// <inheritdoc cref="ICommandInput.CommandsHandled"/>
        public string[]? CommandsHandled => new string[] { "edit", "undo" };
        #endregion

        private string regexCommandString = string.Empty;
        private ConsoleState? state;

        /// <summary>
        /// Event for when this class wants to change the edit mode state.
        /// </summary>
        public event EventHandler<EditModeChangeEventArgs>? EditModeChanged;

        /// <inheritdoc cref="ICommandInput.Init(ConsoleState)"/>
        [MemberNotNull(nameof(state))]
        public void Init(ConsoleState state)
        {
            // Add all commands listed in CommandsHandled to the regex string for matching if this plugin handles the command.
            if (KeysHandled?.Contains(ConsoleKey.Enter) == true && CommandsHandled?.Length > 0)
            {
                regexCommandString = string.Concat("^(", string.Join('|', CommandsHandled), ")( .*)?$");
            }
            this.state = state;
            EditModeChanged += state.OnEditModeChanged;
            state.SessionClosing += SessionClosing;
        }

        /// <summary>
        /// Event handler for the EDIT and UNDO commands
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
                !(e.Key.ConsoleKey == ConsoleKey.Enter) || // The key pressed was not Enter
                state.EditMode // Edit mode is enabled
                )
            {
                return;
            }
            // If current input matches EDIT or UNDO, we are handling the event
            else if (!string.IsNullOrEmpty(regexCommandString) && Regex.Match(state.Input.Text.ToString().Trim(), regexCommandString, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Success)
            {
                e.Handled = true;
            }
            // In all other cases we are not handling the event
            else
            {
                return;
            }

            Console.CursorVisible = false;
            state.MoveCursorToStartOfInput();
            Console.Write(new string(' ', state.Input.Length + 1));
            state.InputClear();
            ConsoleOutput.UpdateCurrentCommand(state, state.Input.StartPosition, 0);
            EditModeChanged?.Invoke(null, new EditModeChangeEventArgs(true));
        }

        /// <summary>
        /// Event handler for ConsoleState.SessionClosing event.
        /// </summary>
        /// <param name="sender">Sender of the event.</param>
        /// <param name="sessionClosing">True if session is closing.</param>
        public void SessionClosing(object? sender, bool sessionClosing)
        {
            if (sessionClosing)
            {
                if (state != null)
                {
                    state.SessionClosing -= SessionClosing;
                }
                Dispose();
            }
        }

        /// <summary>
        /// Remove event handlers on disposal.
        /// </summary>
        public void Dispose()
        {
            if (EditModeChanged != null)
            {
                Delegate[] editModeChangedDelegates = EditModeChanged.GetInvocationList();
                foreach (Delegate delegateToDelete in editModeChangedDelegates)
                {
                    EditModeChanged -= (EventHandler<EditModeChangeEventArgs>)delegateToDelete;
                }
            }
            GC.SuppressFinalize(this);
        }
    }
}
