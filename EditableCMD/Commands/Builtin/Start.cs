﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using uk.JohnCook.dotnet.EditableCMDLibrary.Commands;
using uk.JohnCook.dotnet.EditableCMDLibrary.ConsoleSessions;
using uk.JohnCook.dotnet.EditableCMDLibrary.Interop;

namespace uk.JohnCook.dotnet.EditableCMD.Commands
{
    /// <summary>
    /// START command
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class Start : ICommandInput
    {
        #region Plugin Implementation Details
        /// <summary>
        /// Name of the plugin.
        /// </summary>
        public string Name => "Start";
        /// <summary>
        /// Summary of the plugin's functionality.
        /// </summary>
        public string Description => "Handles command START with no parameters.";
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
        public string[] CommandsHandled => new string[] { "start" };
        #endregion

        private string regexCommandString = string.Empty;

        /// <summary>
        /// Called when adding an implementation of the interface to the list of event handlers. Approximately equivalent to a constructor.
        /// </summary>
        /// <param name="state">The <see cref="ConsoleState"/> for the current console session.</param>
        public void Init(ConsoleState state)
        {
            // Add all commands listed in CommandsHandled to the regex string for matching if this plugin handles the command.
            if (KeysHandled.Contains(ConsoleKey.Enter) && CommandsHandled.Length > 0)
            {
                regexCommandString = string.Concat("^(", string.Join('|', CommandsHandled), ")$");
            }
        }

        /// <summary>
        /// Event handler for the START command
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">The ConsoleKeyEventArgs for the event</param>
        public void ProcessCommand(object sender, NativeMethods.ConsoleKeyEventArgs e)
        {
            // Return early if we're not interested in the event
            if (e.Handled || // Event has already been handled
                !e.Key.KeyDown || // A key was not pressed
                !(e.Key.ConsoleKey == ConsoleKey.Enter) || // The key pressed was not Enter
                e.State.EditMode // Edit mode is enabled
                )
            {
                return;
            }
            // If current input matches START (and is only one word), we are handling the event
            else if (!string.IsNullOrEmpty(regexCommandString) && Regex.Match(e.State.Input.Text.ToString().Trim(), regexCommandString, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Success)
            {
                e.Handled = true;
            }
            // In all other cases we are not handling the event
            else
            {
                return;
            }

            ConsoleState state = e.State;
            // start with no parameters opens another cmd window
            Console.WriteLine();
            Process newProcess = new()
            {
                StartInfo = new ProcessStartInfo()
                {
                    WorkingDirectory = state.CurrentDirectory,
                    FileName = string.Concat(Path.GetDirectoryName(AppContext.BaseDirectory), Path.DirectorySeparatorChar, "cmd.exe"),
                    Arguments = state.StartupParams.CmdParams,
                    UseShellExecute = true
                }
            };
            newProcess.Start();
            Console.WriteLine();
            ConsoleOutput.WritePrompt(state, true);
        }
    }
}
