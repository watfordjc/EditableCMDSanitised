using System;
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
        /// <summary>
        /// Name of the plugin.
        /// </summary>
        public string Name => "Edit mode key input";
        /// <summary>
        /// Summary of the plugin's functionality.
        /// </summary>
        public string Description => "Handles edit mode key input.";
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
        /// <remarks>Not currently used for edit mode.</remarks>
        public ConsoleKey[] KeysHandled => null;
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
        /// Processes a <see cref="NativeMethods.KEY_EVENT_RECORD"/> in edit-mode.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The <see cref="NativeMethods.KEY_EVENT_RECORD"/> to process.</param>
        [SupportedOSPlatform("windows")]
        public void ProcessCommand(object sender, NativeMethods.ConsoleKeyEventArgs e)
        {
            if (!e.State.EditMode)
            {
                return;
            }

            NativeMethods.KEY_EVENT_RECORD record = e.KeyEventRecord;
            ConsoleState state = Program.GetConsoleState();
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
