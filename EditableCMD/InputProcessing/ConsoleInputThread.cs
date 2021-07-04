using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Threading;
using uk.JohnCook.dotnet.EditableCMDLibrary.ConsoleSessions;
using uk.JohnCook.dotnet.EditableCMDLibrary.Interop;
using uk.JohnCook.dotnet.EditableCMDLibrary.Utils;

namespace uk.JohnCook.dotnet.EditableCMD.InputProcessing
{
    /// <summary>
    /// Console input record event loop
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class ConsoleInputThread
    {
        /// <summary>
        /// Event fired when there is a new console mouse input event.
        /// </summary>
        public event EventHandler<NativeMethods.ConsoleMouseEventArgs>? MouseInput;
        /// <summary>
        /// Event fired when there is a new console keyboard input event.
        /// </summary>
        public event EventHandler<NativeMethods.ConsoleKeyEventArgs>? KeyboardInput;
        /// <summary>
        /// Event fired when there is a new console window buffer size changed event.
        /// </summary>
        public event EventHandler<NativeMethods.ConsoleWindowBufferSizeEventArgs>? WindowBufferSizeChanged;

        private readonly ConsoleState state;

        /// <summary>
        /// Thread for receiving console input events and firing more specific events based on the input type.
        /// </summary>
        /// <param name="state">The <see cref="ConsoleState"/> for the current console session.</param>
        public ConsoleInputThread(ConsoleState state)
        {
            this.state = state;
            CreateThread();
        }

        /// <summary>
        /// Start the event loop thread for console input records
        /// </summary>
        private void CreateThread()
        {
            if (!state.Closing)
            {
                #region Thread for parsing console input records
                new Thread(() =>
                {
                    NativeMethods.INPUT_RECORD[] record = new NativeMethods.INPUT_RECORD[1];
                    while (true)
                    {
                        uint recordsRead = state.StandardInput.ReadNextInputRecord(ref record);
                        if (!state.Closing && recordsRead > 0)
                        {
                            switch (record[0].EventType)
                            {
                                case NativeMethods.INPUT_RECORD.MOUSE_EVENT:
                                    MouseInput?.Invoke(null, new NativeMethods.ConsoleMouseEventArgs(record[0].MouseEvent));
                                    continue;
                                case NativeMethods.INPUT_RECORD.KEY_EVENT:
                                    NativeMethods.ConsoleKeyEventArgs consoleKeyEventArgs = new(record[0].KeyEvent, DateTime.UtcNow, state);
                                    // Sleep while waiting for next prompt, unless the input uses a key modifier (excluding AltGr)
                                    while (state.CmdRunning && !(consoleKeyEventArgs.Key.HasModifier && !consoleKeyEventArgs.Key.AltGrModifier))
                                    {
                                        if (!Thread.Yield())
                                        {
                                            Thread.Sleep(1);
                                        }
                                    }
                                    KeyboardInput?.Invoke(null, consoleKeyEventArgs);
                                    continue;
                                case NativeMethods.INPUT_RECORD.WINDOW_BUFFER_SIZE_EVENT:
                                    WindowBufferSizeChanged?.Invoke(null, new NativeMethods.ConsoleWindowBufferSizeEventArgs(record[0].WindowBufferSizeEvent));
                                    continue;
                                case NativeMethods.INPUT_RECORD.FOCUS_EVENT:
                                case NativeMethods.INPUT_RECORD.MENU_EVENT:
                                    continue;
                                default:
#if DEBUG
                                    Debug.WriteLine("Discarded unknown event of type {0}.", record[0].EventType);
#endif
                                    continue;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    Environment.ExitCode = (int)Win32Utils.GetLastError();
                }).Start();
                #endregion
            }
        }
    }
}
