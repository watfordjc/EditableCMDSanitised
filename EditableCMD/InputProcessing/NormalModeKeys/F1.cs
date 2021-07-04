using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Versioning;
using uk.JohnCook.dotnet.EditableCMDLibrary.Commands;
using uk.JohnCook.dotnet.EditableCMDLibrary.ConsoleSessions;
using uk.JohnCook.dotnet.EditableCMDLibrary.Interop;
using static uk.JohnCook.dotnet.EditableCMDLibrary.ConsoleSessions.ConsoleState;

namespace uk.JohnCook.dotnet.EditableCMD.InputProcessing.NormalModeKeys
{
    /// <summary>
    /// Class for F1 key
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class F1 : ICommandInput, IDisposable
    {
        #region Plugin Implementation Details
        /// <inheritdoc cref="ICommandInput.Name"/>
        public string Name => "F1 key";
        /// <inheritdoc cref="ICommandInput.Description"/>
        public string Description => "Handles key F1.";
        /// <inheritdoc cref="ICommandInput.AuthorName"/>
        public string AuthorName => "John Cook";
        /// <inheritdoc cref="ICommandInput.AuthorTwitchUsername"/>
        public string AuthorTwitchUsername => "WatfordJC";
        /// <inheritdoc cref="ICommandInput.KeysHandled"/>
        public ConsoleKey[]? KeysHandled => new ConsoleKey[] { ConsoleKey.F1 };
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
        /// Event for when this class wants to change the edit mode state.
        /// </summary>
        public event EventHandler<EditModeChangeEventArgs>? EditModeChanged;

        private ConsoleState? state;

        /// <inheritdoc cref="ICommandInput.Init(ConsoleState)"/>
        [MemberNotNull(nameof(state))]
        public void Init(ConsoleState state)
        {
            this.state = state;
            EditModeChanged += state.OnEditModeChanged;
            state.SessionClosing += SessionClosing;
        }

        /// <summary>
        /// Event handler for F1 key
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
            // If current input is F1, we are handling the event
            else if (KeysHandled?.Contains(e.Key.ConsoleKey) == true)
            {
                e.Handled = true;
            }
            // In all other cases we are not handling the event
            else
            {
                return;
            }

            // F1
            if (!e.Key.HasModifier)
            {
                e.Handled = false;
            }
            // Ctrl+F1
            if (e.Key.CtrlModifier && !e.Key.AltModifier && !e.Key.ShiftModifier)
            {
                // Enter edit mode
                EditModeChanged?.Invoke(null, new EditModeChangeEventArgs(true));
            }
            // Other modifier combinations not currently handled
            else
            {
                e.Handled = false;
            }
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
