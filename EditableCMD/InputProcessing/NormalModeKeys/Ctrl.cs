using System;
using System.Runtime.Versioning;
using uk.JohnCook.dotnet.EditableCMDLibrary.Commands;
using uk.JohnCook.dotnet.EditableCMDLibrary.ConsoleSessions;
using uk.JohnCook.dotnet.EditableCMDLibrary.Interop;

namespace uk.JohnCook.dotnet.EditableCMD.InputProcessing.NormalModeKeys
{
    /// <summary>
    /// Class for Ctrl key - a Ctrl on it its own should be ignored
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class Ctrl : ICommandInput
    {
        #region Plugin Implementation Details
        /// <inheritdoc cref="ICommandInput.Name"/>
        public string Name => "Ctrl key";
        /// <inheritdoc cref="ICommandInput.Description"/>
        public string Description => "Handles key CTRL.";
        /// <inheritdoc cref="ICommandInput.AuthorName"/>
        public string AuthorName => "John Cook";
        /// <inheritdoc cref="ICommandInput.AuthorTwitchUsername"/>
        public string AuthorTwitchUsername => "WatfordJC";
        /// <inheritdoc cref="ICommandInput.KeysHandled"/>
        public ConsoleKey[]? KeysHandled => new ConsoleKey[] { (ConsoleKey)0x11 };
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
        /// Event handler for Ctrl key
        /// </summary>
        /// <inheritdoc cref="ICommandInput.ProcessCommand(object, NativeMethods.ConsoleKeyEventArgs)" path="param"/>
        public void ProcessCommand(object? sender, NativeMethods.ConsoleKeyEventArgs e)
        {
            // Return early if we're not interested in the event
            if (e.Handled || // Event has already been handled
                !e.Key.KeyDown // A key was not pressed
                )
            {
                return;
            }
            // If current input is Ctrl, we are handling the event
            else if (e.KeyEventRecord.wVirtualKeyCode == 0x11)
            {
                e.Handled = true;
            }
            // In all other cases we are not handling the event
            else
            {
                return;
            }
        }
    }
}
