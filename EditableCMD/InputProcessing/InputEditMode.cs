using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using uk.JohnCook.dotnet.EditableCMDLibrary.Commands;
using uk.JohnCook.dotnet.EditableCMDLibrary.ConsoleSessions;
using uk.JohnCook.dotnet.EditableCMDLibrary.Interop;
using uk.JohnCook.dotnet.EditableCMDLibrary.Utils;
using static uk.JohnCook.dotnet.EditableCMDLibrary.ConsoleSessions.ConsoleState;

namespace uk.JohnCook.dotnet.EditableCMD.InputProcessing
{
    /// <summary>
    /// Class containing input event handling and helpers when in edit-mode
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class InputEditMode : ICommandInput, IDisposable
    {
        #region Plugin Implementation Details
        /// <inheritdoc cref="ICommandInput.Name"/>
        public string Name => "Edit mode key input";
        /// <inheritdoc cref="ICommandInput.Description"/>
        public string Description => "Handles edit mode key input.";
        /// <inheritdoc cref="ICommandInput.AuthorName"/>
        public string AuthorName => "John Cook";
        /// <inheritdoc cref="ICommandInput.AuthorTwitchUsername"/>
        public string AuthorTwitchUsername => "WatfordJC";
        /// <inheritdoc cref="ICommandInput.KeysHandled"/>
        /// <remarks>Not currently used for edit mode.</remarks>
        public ConsoleKey[]? KeysHandled => null;
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
        /// Processes a <see cref="NativeMethods.KEY_EVENT_RECORD"/> in edit-mode.
        /// </summary>
        /// <inheritdoc cref="ICommandInput.ProcessCommand(object, NativeMethods.ConsoleKeyEventArgs)" path="param"/>
        [SupportedOSPlatform("windows")]
        public void ProcessCommand(object? sender, NativeMethods.ConsoleKeyEventArgs e)
        {
            // Call Init() again if state isn't set
            if (state == null)
            {
                Init(e.State);
            }
            // Return early if edit mode is not enabled.
            if (!state.EditMode)
            {
                return;
            }

            NativeMethods.KEY_EVENT_RECORD record = e.KeyEventRecord;
            NativeMethods.COORD cursorPosition = ConsoleCursorUtils.GetCurrentCursorPosition();

            // If a printable character, shift the rest of the line to the right by 1 character and insert the character.
            if (record.UnicodeChar > 31 && record.UnicodeChar != 127)
            {
                Console.MoveBufferArea(cursorPosition.X, cursorPosition.Y, Console.BufferWidth - cursorPosition.X, 1, cursorPosition.X + 1, cursorPosition.Y);
                Console.Write(record.UnicodeChar);
                e.Handled = true;
            }
            // Non-printable characters need some processing.
            else
            {
                switch (record.wVirtualKeyCode)
                {
                    case 0x11: // Control Key
                    case 0x12: // Alt Key
                        break;
                    case (ushort)ConsoleKey.Enter:
                    case (ushort)ConsoleKey.Escape:
                        // Exit edit-mode
                        EditModeChanged?.Invoke(null, new EditModeChangeEventArgs(false));
                        e.Handled = true;
                        break;
                    case (ushort)ConsoleKey.UpArrow:
                        // Move cursor up if possible
                        if (cursorPosition.Y > 0)
                        {
                            Console.SetCursorPosition(cursorPosition.X, cursorPosition.Y - 1);
                        }
                        e.Handled = true;
                        break;
                    case (ushort)ConsoleKey.DownArrow:
                        // Move cursor down if possible, else exit edit-mode
                        if (ConsoleCursorUtils.CoordIsInsideEditableArea(state, new NativeMethods.COORD((short)cursorPosition.X, (short)(cursorPosition.Y + 1))))
                        {
                            Console.SetCursorPosition(cursorPosition.X, cursorPosition.Y + 1);
                        }
                        else
                        {
                            EditModeChanged?.Invoke(null, new EditModeChangeEventArgs(false));
                        }
                        e.Handled = true;
                        break;
                    case (ushort)ConsoleKey.RightArrow:
                        // Move cursor right if possible
                        if (cursorPosition.X + 1 < Console.BufferWidth)
                        {
                            Console.SetCursorPosition(cursorPosition.X + 1, cursorPosition.Y);
                        }
                        e.Handled = true;
                        break;
                    case (ushort)ConsoleKey.LeftArrow:
                        // Move cursor left if possible
                        if (cursorPosition.X > 0)
                        {
                            Console.SetCursorPosition(cursorPosition.X - 1, cursorPosition.Y);
                        }
                        e.Handled = true;
                        break;
                    case (ushort)ConsoleKey.Home:
                        // Move cursor to X=0 on the current line
                        Console.SetCursorPosition(0, cursorPosition.Y);
                        e.Handled = true;
                        break;
                    case (ushort)ConsoleKey.Backspace:
                        // Shift the rest of the line one character to the left and move the cursor if possible
                        if (cursorPosition.X > 0)
                        {
                            Console.MoveBufferArea(cursorPosition.X, cursorPosition.Y, Console.BufferWidth - cursorPosition.X, 1, cursorPosition.X - 1, cursorPosition.Y);
                            Console.SetCursorPosition(cursorPosition.X - 1, cursorPosition.Y);
                        }
                        e.Handled = true;
                        break;
                    case (ushort)ConsoleKey.Delete:
                        // Shift the rest of the line one character to the right
                        Console.MoveBufferArea(cursorPosition.X + 1, cursorPosition.Y, Console.BufferWidth - cursorPosition.X - 1, 1, cursorPosition.X, cursorPosition.Y);
                        e.Handled = true;
                        break;
                }
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
                Delegate[]? editModeChangedDelegates = EditModeChanged.GetInvocationList();
                foreach (Delegate delegateToDelete in editModeChangedDelegates)
                {
                    EditModeChanged -= (EventHandler<EditModeChangeEventArgs>)delegateToDelete;
                }
            }
            GC.SuppressFinalize(this);
        }
    }
}
