using System;
using System.Linq;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using uk.JohnCook.dotnet.EditableCMDLibrary.Commands;
using uk.JohnCook.dotnet.EditableCMDLibrary.ConsoleSessions;
using uk.JohnCook.dotnet.EditableCMDLibrary.Interop;

namespace uk.JohnCook.dotnet.EditableCMD.Commands
{
    /// <summary>
    /// ECHO command
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class Echo : ICommandInput
    {
        #region Plugin Implementation Details
        /// <summary>
        /// Name of the plugin.
        /// </summary>
        public string Name => "Echo";
        /// <summary>
        /// Summary of the plugin's functionality.
        /// </summary>
        public string Description => "Handles commands ECHO, ECHO ON, and ECHO OFF.";
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
        public string[] CommandsHandled => new string[] { "echo", "echo on", "echo off" };
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
        /// Event handler for the ECHO command
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
            // If current input matches ECHO (and is optionally followed by ON or OFF), we are handling the event
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
            string[] commandWords = e.State.Input.Text.ToString().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (commandWords.Length == 1)
            {
                // Displays ECHO status
                Console.WriteLine("\nECHO is {0}.{1}", state.EchoEnabled ? "on" : "off", state.EchoEnabled ? "\n" : "");
                ConsoleOutput.WritePrompt(state, false);
            }
            else if (commandWords.Length == 2)
            {
                if (commandWords[1].ToLower() == "on")
                {
                    // Enables ECHO
                    Console.WriteLine("\n");
                    state.EchoEnabled = true;
                    ConsoleOutput.WritePrompt(state, false);
                }
                else if (commandWords[1].ToLower() == "off")
                {
                    // Disables ECHO
                    Console.WriteLine();
                    state.EchoEnabled = false;
                    ConsoleOutput.WritePrompt(state, false);
                }
            }
        }
    }
}
