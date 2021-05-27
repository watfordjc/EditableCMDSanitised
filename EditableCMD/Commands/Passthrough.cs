using System;
using System.IO;
using System.Runtime.Versioning;
using uk.JohnCook.dotnet.EditableCMDLibrary.Commands;
using uk.JohnCook.dotnet.EditableCMDLibrary.ConsoleSessions;
using uk.JohnCook.dotnet.EditableCMDLibrary.Interop;
using uk.JohnCook.dotnet.EditableCMDLibrary.Utils;

namespace uk.JohnCook.dotnet.EditableCMD.Commands
{
    /// <summary>
    /// All unhandled commands
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class Passthrough : ICommandInput
    {
        #region Plugin Implementation Details
        /// <summary>
        /// Name of the plugin.
        /// </summary>
        public string Name => "Passthrough";
        /// <summary>
        /// Summary of the plugin's functionality.
        /// </summary>
        public string Description => "Handles the passthrough of commands not handled, passing them through to a command prompt sub-process.";
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
        public ConsoleKey[] KeysHandled => new ConsoleKey[] { ConsoleKey.Enter };
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
        /// <remarks>Matches all commands.</remarks>
        public string[] CommandsHandled => new string[] { ".*" };
        #endregion

        /// <summary>
        /// Event handler for all unhandled commands
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">The ConsoleKeyEventArgs for the event</param>
        public void ProcessCommand(object sender, NativeMethods.ConsoleKeyEventArgs e)
        {
            if (e.Handled || // Event has already been handled
                !e.KeyEventRecord.bKeyDown || // A key was not pressed
                !(e.KeyEventRecord.wVirtualKeyCode == (ushort)ConsoleKey.Enter) // The key pressed was not Enter
                )
            {
                return;
            }
            else
            {
                e.Handled = true;
            }

            ConsoleState state = e.State;
            Console.WriteLine();

            state.CmdRunning = true;
            // Start a self-closing cmd process with entered command, then silently store current directory and environment variables
            CommandPrompt commandPrompt = new(state, null);
            commandPrompt.Start();
            commandPrompt.WaitForExit();

            if (state.EchoEnabled)
            {
                Console.WriteLine();
            }
            // Copy environment variables to current environment
            if (File.Exists(state.EnvLogger.LogFile))
            {
                FileInfo envFileInfo = new(state.EnvLogger.LogFile);
                bool envFileAccessible = FileUtils.WaitForFile(envFileInfo, 1000);
                if (envFileAccessible)
                {
                    using StreamReader envFile = new(state.EnvLogger.LogFile);
                    string lineContent;
                    while ((lineContent = envFile.ReadLine()) != null)
                    {
                        string[] variable = lineContent.Split('=', 2);
                        if (!variable[0].StartsWith("EditableCmd_"))
                        {
                            Environment.SetEnvironmentVariable(variable[0], variable[1]);
                        }
                    }
                }
            }
            ConsoleOutput.WritePrompt(state, true);
        }
    }
}
