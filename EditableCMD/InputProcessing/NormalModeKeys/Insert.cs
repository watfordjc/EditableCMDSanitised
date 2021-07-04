using System;
using System.Linq;
using System.Runtime.Versioning;
using uk.JohnCook.dotnet.EditableCMDLibrary.Commands;
using uk.JohnCook.dotnet.EditableCMDLibrary.ConsoleSessions;
using uk.JohnCook.dotnet.EditableCMDLibrary.Interop;

namespace uk.JohnCook.dotnet.EditableCMD.InputProcessing.NormalModeKeys
{
    /// <summary>
    /// Class for Insert key
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class Insert : ICommandInput
    {
        #region Plugin Implementation Details
        /// <inheritdoc cref="ICommandInput.Name"/>
        public string Name => "Insert key";
        /// <inheritdoc cref="ICommandInput.Description"/>
        public string Description => "Handles key Insert.";
        /// <inheritdoc cref="ICommandInput.AuthorName"/>
        public string AuthorName => "John Cook";
        /// <inheritdoc cref="ICommandInput.AuthorTwitchUsername"/>
        public string AuthorTwitchUsername => "WatfordJC";
        /// <inheritdoc cref="ICommandInput.KeysHandled"/>
        public ConsoleKey[]? KeysHandled => new ConsoleKey[] { ConsoleKey.Insert };
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
        /// Event handler for Insert key
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
            // If current input is Insert, we are handling the event
            else if (KeysHandled?.Contains(e.Key.ConsoleKey) == true)
            {
                e.Handled = true;
            }
            // In all other cases we are not handling the event
            else
            {
                return;
            }

            // Insert (no modifier keys)
            if (!e.Key.HasModifier)
            {
                e.State.OverwriteMode = !e.State.OverwriteMode;
                int cursorHeight = e.State.OverwriteMode ? 50 : 25;
                Console.CursorSize = cursorHeight;
            }
            // Other modifier combinations not currently handled
            else
            {
                e.Handled = false;
            }
        }
    }
}
