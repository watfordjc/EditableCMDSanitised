using System;
using System.Diagnostics.CodeAnalysis;
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
        /// <inheritdoc cref="ICommandInput.Name"/>
        public string Name => "Passthrough";
        /// <inheritdoc cref="ICommandInput.Description"/>
        public string Description => "Handles the passthrough of commands not handled, passing them through to a command prompt sub-process.";
        /// <inheritdoc cref="ICommandInput.AuthorName"/>
        public string AuthorName => "John Cook";
        /// <inheritdoc cref="ICommandInput.AuthorTwitchUsername"/>
        public string AuthorTwitchUsername => "WatfordJC";
        /// <inheritdoc cref="ICommandInput.KeysHandled"/>
        public ConsoleKey[]? KeysHandled => new ConsoleKey[] { ConsoleKey.Enter };
        /// <inheritdoc cref="ICommandInput.NormalModeHandled"/>
        public bool NormalModeHandled => true;
        /// <inheritdoc cref="ICommandInput.EditModeHandled"/>
        public bool EditModeHandled => false;
        /// <inheritdoc cref="ICommandInput.MarkModeHandled"/>
        public bool MarkModeHandled => false;
        /// <inheritdoc cref="ICommandInput.CommandsHandled"/>
        /// <remarks>Matches all commands.</remarks>
        public string[]? CommandsHandled => new string[] { ".*" };
        #endregion

        private ConsoleState? state;

        /// <inheritdoc cref="ICommandInput.Init(ConsoleState)"/>
        [MemberNotNull(nameof(state))]
        public void Init(ConsoleState state)
        {
            this.state = state;
        }

        /// <summary>
        /// Event handler for all unhandled commands
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
                    string? lineContent;
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
