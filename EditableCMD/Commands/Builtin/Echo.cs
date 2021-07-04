﻿using System;
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
        /// <inheritdoc cref="ICommandInput.Name"/>
        public string Name => "Echo";
        /// <inheritdoc cref="ICommandInput.Description"/>
        public string Description => "Handles commands ECHO, ECHO ON, and ECHO OFF.";
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
        public string[]? CommandsHandled => new string[] { "echo", "echo on", "echo off" };
        #endregion

        private string regexCommandString = string.Empty;

        /// <inheritdoc cref="ICommandInput.Init(ConsoleState)"/>
        public void Init(ConsoleState state)
        {
            // Add all commands listed in CommandsHandled to the regex string for matching if this plugin handles the command.
            if (KeysHandled?.Contains(ConsoleKey.Enter) == true && CommandsHandled?.Length > 0)
            {
                regexCommandString = string.Concat("^(", string.Join('|', CommandsHandled), ")$");
            }
        }

        /// <summary>
        /// Event handler for the ECHO command
        /// </summary>
        /// <inheritdoc cref="ICommandInput.ProcessCommand(object?, NativeMethods.ConsoleKeyEventArgs)" path="param"/>
        public void ProcessCommand(object? sender, NativeMethods.ConsoleKeyEventArgs e)
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
