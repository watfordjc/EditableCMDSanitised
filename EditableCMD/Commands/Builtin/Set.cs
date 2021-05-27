using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using uk.JohnCook.dotnet.EditableCMDLibrary.Commands;
using uk.JohnCook.dotnet.EditableCMDLibrary.ConsoleSessions;
using uk.JohnCook.dotnet.EditableCMDLibrary.Interop;

namespace uk.JohnCook.dotnet.EditableCMD.Commands
{
    /// <summary>
    /// SET command
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class Set : ICommandInput
    {
        #region Plugin Implementation Details
        /// <summary>
        /// Name of the plugin.
        /// </summary>
        public string Name => "Set";
        /// <summary>
        /// Summary of the plugin's functionality.
        /// </summary>
        public string Description => "Handles SET command.";
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
        public string[] CommandsHandled => new string[] { "set" };
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
                regexCommandString = string.Concat("^(", string.Join('|', CommandsHandled), ")( .*)?$");
            }
        }

        /// <summary>
        /// Event handler for the SET command
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
            // If current input matches SET, we are handling the event
            else if (!string.IsNullOrEmpty(regexCommandString) && Regex.Match(e.State.Input.Text.ToString().Trim(), regexCommandString, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Success)
            {
                e.Handled = true;
            }
            // In all other cases we are not handling the event
            else
            {
                return;
            }

            // TODO: Command prompt doesn't trim spaces
            string[] commandWords = e.State.Input.Text.ToString().Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            // Command is "SET" - print environment variables
            if (commandWords.Length == 1)
            {
                Console.WriteLine();
                // Initialise a SortedDictionary<string, string> with the Environment.GetEnvironmentVariables() HashTable<string, string> cast to a Dictionary<string, string>
                SortedDictionary<string, string> sortedEnvVars = new(Environment.GetEnvironmentVariables().Cast<DictionaryEntry>().ToDictionary(keyValuePair => (string)keyValuePair.Key, keyValuePair => (string)keyValuePair.Value));
                foreach (KeyValuePair<string, string> envVar in sortedEnvVars)
                {
                    Console.WriteLine(string.Concat(envVar.Key, "=", envVar.Value));
                }
                Console.WriteLine();
                ConsoleOutput.WritePrompt(e.State, true);
                return;
            }
            // Command is "SET ..."
            else if (commandWords.Length > 1)
            {
                int equalsPos = -1;
                for (int i = 0; equalsPos < 0 && i < commandWords.Length; i++)
                {
                    if (commandWords[i].Contains('='))
                    {
                        equalsPos = i;
                    }
                }
                bool hasArithmeticParam = commandWords[1].ToLower().Equals("/a");
                bool hasPromptedParam = commandWords[1].ToLower().Equals("/p");
                bool hasHelpParam = commandWords[1].Equals("/?");
                int variableNamePosition = (hasArithmeticParam || hasPromptedParam) ? 2 : 1;
                // Command "SET /?" should be sent to a cmd.exe process, "SET /A ..." maths can be offloaded to cmd.exe
                if (!hasHelpParam && !hasArithmeticParam && commandWords.Length >= variableNamePosition)
                {
                    Console.WriteLine();
                    bool settingVariable = commandWords[variableNamePosition].Contains('=');
                    // Command is "SET something=..." - set/unset variable
                    if (settingVariable)
                    {
                        string[] setParams = e.State.Input.Text.ToString().Split(' ', variableNamePosition + 1, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[variableNamePosition].Split('=', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        if (hasPromptedParam && setParams.Length == 2)
                        {
                            Console.Write(setParams.Length == 2 ? setParams[1] : "");
                            setParams[1] = Console.ReadLine();
                        }
                        else if (hasPromptedParam && setParams.Length == 1)
                        {
                            setParams = new string[2] { setParams[0], string.Empty };
                            setParams[1] = Console.ReadLine();
                        }
                        if (setParams.Length == 2)
                        {
                            Environment.SetEnvironmentVariable(setParams[0], setParams[1]);
                        }
                        else
                        {
                            Environment.SetEnvironmentVariable(setParams[0], null);
                        }
                        Console.WriteLine();
                        ConsoleOutput.WritePrompt(e.State, true);
                        return;
                    }
                    // (SET /P something) - does not have a value for the prompt
                    else if (hasPromptedParam && !commandWords[variableNamePosition].Contains('='))
                    {
                        Console.WriteLine("The syntax of the command is incorrect.\n");
                        ConsoleOutput.WritePrompt(e.State, true);
                        return;
                    }
                    // Command is "SET something" - print name and value of variable
                    else
                    {
                        // Initialise a SortedDictionary<string, string> with the Environment.GetEnvironmentVariables() HashTable<string, string> cast to a Dictionary<string, string>
                        SortedDictionary<string, string> sortedEnvVars = new(Environment.GetEnvironmentVariables().Cast<DictionaryEntry>()
                            .Where(x => (x.Key as string).ToLower().StartsWith(commandWords[1].ToLower()))
                            .ToDictionary(keyValuePair => (string)keyValuePair.Key, keyValuePair => (string)keyValuePair.Value));
                        if (sortedEnvVars.Any())
                        {
                            foreach (KeyValuePair<string, string> envVar in sortedEnvVars.Where(x => x.Key.ToLower().StartsWith(commandWords[1].ToLower())))
                            {
                                Console.WriteLine(string.Concat(envVar.Key, "=", envVar.Value));
                            }
                        }
                        else
                        {
                            Console.WriteLine("Environment variable {0} not defined", commandWords[1]);
                        }
                        Console.WriteLine();
                        ConsoleOutput.WritePrompt(e.State, true);
                        return;
                    }
                }
                else if (hasArithmeticParam)
                {
                    string[] setParams = e.State.Input.Text.ToString().Split(' ', variableNamePosition + 1, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[variableNamePosition].Split('=', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    Console.WriteLine();
                    if (setParams.Length == 1)
                    {
                        Console.WriteLine("0");
                        ConsoleOutput.WritePrompt(e.State, true);
                        return;
                    }
                    // Offload command prompt arithmetic parsing and calculation to command prompt
                    // This creates starts a CommandPrompt instance which sets up a thread using the parameters supplied
                    CommandPrompt commandPrompt = new(e.State, null, false, false, true, false);
                    // The result of the aritmetic will be on the first line of the output - we can ignore the rest
                    bool firstLineReceived = false;
                    // Local method that will get called whenever CommandPrompt receives a line of output
                    void onNewOutput(object sender, int newLineCount)
                    {
                        if (!firstLineReceived && newLineCount == 1)
                        {
                            setParams[1] = commandPrompt.Output.Dequeue();
                            firstLineReceived = true;
                            Environment.SetEnvironmentVariable(setParams[0], setParams[1]);
                            commandPrompt.NewOutput -= onNewOutput;
                        }
                    }
                    // Local method that will get called when CommandPrompt's thread has finished executing
                    void onComplete(object sender, bool completed)
                    {
                        commandPrompt.NewOutput -= onNewOutput;
                        commandPrompt.Completed -= onComplete;
                        Console.WriteLine(setParams[1]);
                        ConsoleOutput.WritePrompt(e.State, true);
                        return;
                    }
                    // Add the above local methods as event handlers to the CommandPrompt instance
                    commandPrompt.NewOutput += onNewOutput;
                    commandPrompt.Completed += onComplete;
                    // Start the CommandPrompt thread and block until it exits
                    commandPrompt.Start();
                    commandPrompt.WaitForExit();
                }
            }
        }
    }
}
