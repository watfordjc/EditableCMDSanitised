using System;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using uk.JohnCook.dotnet.EditableCMD.ConsoleSessions;
using uk.JohnCook.dotnet.EditableCMD.InputProcessing;
using uk.JohnCook.dotnet.EditableCMD.InputHandlers;
using uk.JohnCook.dotnet.EditableCMDLibrary.ConsoleSessions;
using uk.JohnCook.dotnet.EditableCMDLibrary.Interop;
using uk.JohnCook.dotnet.EditableCMDLibrary.Utils;
using System.Diagnostics.CodeAnalysis;

namespace uk.JohnCook.dotnet.EditableCMD
{
    [SupportedOSPlatform("windows")]
    class Program
    {
        #region static variables

        // Console state and output
        private static ConsoleState state;
        // Console input event thread
        private static ConsoleInputThread? consoleInputThread = null;
        // Instance of ConsoleKeyInputHandler
        private static ConsoleKeyInputHandler? keyInputHandler = null;

        #endregion

        /// <summary>
        /// Useless constructor
        /// </summary>
        /// <remarks>
        /// <para>
        /// This constructor is only used to set non-nullable static variables to null/default.
        /// </para>
        /// <para>
        /// All static variables set to null/default in this constructor <b>must</b> have a MemberNotNull attribute added to <see cref="Main(string[])"/>.
        /// </para>
        /// </remarks>
        static Program()
        {
            state = default!;
        }

        /// <summary>
        /// Main() - entry point of program
        /// </summary>
        /// <param name="args">command-line parameters and switches</param>
        [MemberNotNull(nameof(state))]
        static int Main(string[] args)
        {
            // Add event handler for console control signals
            AddConsoleCtrlHandler();
            // Create a new ConsoleState instance; pass in an instance of CommandPromptParams that has parsed the (string[] args) supplied to main
            Guid sessionGuid;
#if DEBUG
            sessionGuid = Guid.Empty;
#else
            sessionGuid = new();
#endif
            state = new(new CommandPromptParams(null, args), sessionGuid, strings.applicationName);
            // Process and execute any command string parsed during CommandPromptParams instantiation
            StartupArgs.ProcessCommandString();
            // If the command string says we now need to close (e.g. /C was used), cleanup and exit
            if (state.Closing)
            {
                ExitCleanup();
            }
            else
            {
                // Create a new ConsoleInputThread instance tied to the current console's ConsoleState
                consoleInputThread = new(state);

                keyInputHandler = new(state);

                // Add input event handlers
                consoleInputThread.KeyboardInput += keyInputHandler.OnKeyboardInput;
                consoleInputThread.MouseInput += ConsoleMouseInputHandler.OnMouseInput;
                // Add events that enter/exit edit mode
                ConsoleMouseInputHandler.EditModeChanged += state.OnEditModeChanged;
            }
            return Environment.ExitCode;
        }

        /// <summary>
        /// Registers a handler routine called when console control signals are received
        /// </summary>
        private static void AddConsoleCtrlHandler()
        {
            // Add console control event handler - runs ExitCleanup() on exit
            if (!Win32Utils.AddConsoleCtrlHandler(ConsoleControlHandler))
            {
                throw new Exception("Unable to add handler.");
            }
        }

        /// <summary>
        /// Get the current console's session state
        /// </summary>
        /// <returns>The current ConsoleState</returns>
        public static ref ConsoleState GetConsoleState()
        {
            return ref state;
        }

#region CTRL+C, CTRL+Break, Close window, etc.

        /// <summary>
        /// Cleanup before exiting - endReadConsoleInput=true ends the console input thread, so the end of this method is right before exit
        /// </summary>
        private static void ExitCleanup()
        {
            // Remove event handlers
            if (consoleInputThread != null)
            {
                if (keyInputHandler != null)
                {
                    consoleInputThread.KeyboardInput -= keyInputHandler.OnKeyboardInput;
                }
                consoleInputThread.MouseInput -= ConsoleMouseInputHandler.OnMouseInput;
            }
            ConsoleMouseInputHandler.EditModeChanged -= state.OnEditModeChanged;
        }

        /// <summary>
        /// Handle console control events
        /// </summary>
        /// <param name="sig">Received signal</param>
        /// <returns>True if signal should be ignored, False if default behaviour should subsequently occur</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static bool ConsoleControlHandler(NativeMethods.ConsoleControlType sig)
        {
            switch (sig)
            {
                case NativeMethods.ConsoleControlType.CTRL_CLOSE_EVENT:
                case NativeMethods.ConsoleControlType.CTRL_LOGOFF_EVENT:
                case NativeMethods.ConsoleControlType.CTRL_SHUTDOWN_EVENT:
                    ExitCleanup();
                    return false;
                default:
                    return false;
            }
            throw new NotImplementedException();
        }

#endregion

    }
}
