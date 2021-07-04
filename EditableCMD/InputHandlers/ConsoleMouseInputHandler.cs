using System;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using uk.JohnCook.dotnet.EditableCMDLibrary.ConsoleSessions;
using uk.JohnCook.dotnet.EditableCMDLibrary.Interop;
using uk.JohnCook.dotnet.EditableCMDLibrary.Utils;
using static uk.JohnCook.dotnet.EditableCMDLibrary.ConsoleSessions.ConsoleState;

namespace uk.JohnCook.dotnet.EditableCMD.InputHandlers
{
    /// <summary>
    /// Class containing event handler for console mouse input events
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class ConsoleMouseInputHandler
    {
        /// <summary>
        /// Event for when this class wants to change the edit mode state.
        /// </summary>
        public static event EventHandler<EditModeChangeEventArgs>? EditModeChanged;

        /// <summary>
        /// Handler for mouse events
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The record to parse.</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void OnMouseInput(object? sender, NativeMethods.ConsoleMouseEventArgs e)
        {
            ConsoleState state = Program.GetConsoleState();
            NativeMethods.MOUSE_EVENT_RECORD record = e.MouseEventRecord;
            // Left mouse click with no other flags set
            if (record.dwEventFlags == 0 && (record.dwButtonState & NativeMethods.MOUSE_EVENT_RECORD.FROM_LEFT_1ST_BUTTON_PRESSED) != 0)
            {
                // If in edit mode and the mouse pointer is outside the editable area, leave edit mode
                if (state.EditMode && !ConsoleCursorUtils.CoordIsInsideEditableArea(state, record.dwMousePosition))
                {
                    EditModeChanged?.Invoke(null, new EditModeChangeEventArgs(false));
                    return;
                }
                // Change cursor position
                Console.SetCursorPosition(record.dwMousePosition.X, record.dwMousePosition.Y);
                return;
            }
        }
    }
}
