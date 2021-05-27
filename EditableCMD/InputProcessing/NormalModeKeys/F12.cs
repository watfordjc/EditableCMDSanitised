using System;
using System.Linq;
using System.Runtime.Versioning;
using uk.JohnCook.dotnet.EditableCMDLibrary.Commands;
using uk.JohnCook.dotnet.EditableCMDLibrary.ConsoleSessions;
using uk.JohnCook.dotnet.EditableCMDLibrary.Interop;
using static uk.JohnCook.dotnet.EditableCMDLibrary.ConsoleSessions.ConsoleState;

namespace uk.JohnCook.dotnet.EditableCMD.InputProcessing.NormalModeKeys
{
    /// <summary>
    /// Class for F12 key
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class F12 : ICommandInput, IDisposable
    {
        #region Plugin Implementation Details
        /// <summary>
        /// Name of the plugin.
        /// </summary>
        public string Name => "F12 key";
        /// <summary>
        /// Summary of the plugin's functionality.
        /// </summary>
        public string Description => "Handles key F12.";
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
        public ConsoleKey[] KeysHandled => new ConsoleKey[] { ConsoleKey.F12 };
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
        /// Event for when this class wants to change the edit mode state.
        /// </summary>
        public event EventHandler<EditModeChangeEventArgs> EditModeChanged;

        private ConsoleState state = null;

        /// <summary>
        /// Called when adding an implementation of the interface to the list of event handlers. Approximately equivalent to a constructor.
        /// </summary>
        /// <param name="state">The <see cref="ConsoleState"/> for the current console session.</param>
        public void Init(ConsoleState state)
        {
            this.state = state;
            EditModeChanged += state.OnEditModeChanged;
            state.SessionClosing += SessionClosing;
        }

        /// <summary>
        /// Event handler for F12 key
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
            // If current input is F12, we are handling the event
            else if (KeysHandled.Contains(e.Key.ConsoleKey))
            {
                e.Handled = true;
            }
            // In all other cases we are not handling the event
            else
            {
                return;
            }

            // F12
            if (!e.Key.HasModifier)
            {
                e.Handled = false;
            }
            // Ctrl+F12
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
        public void SessionClosing(object sender, bool sessionClosing)
        {
            if (sessionClosing)
            {
                state.SessionClosing -= SessionClosing;
                Dispose();
            }
        }

        /// <summary>
        /// Remove event handlers on disposal.
        /// </summary>
        public void Dispose()
        {
            Delegate[] editModeChangedDelegates = EditModeChanged.GetInvocationList();
            foreach (Delegate delegateToDelete in editModeChangedDelegates)
            {
                EditModeChanged -= (EventHandler<EditModeChangeEventArgs>)delegateToDelete;
            }
            GC.SuppressFinalize(this);
        }
    }
}
