using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using uk.JohnCook.dotnet.EditableCMDLibrary.ConsoleSessions;
using uk.JohnCook.dotnet.EditableCMDLibrary.Utils;

namespace uk.JohnCook.dotnet.EditableCMD.ConsoleSessions
{
    /// <summary>
    /// Class for executing the application's startup command line arguments
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class StartupArgs
    {
        /// <summary>
        /// Parse command line string
        /// </summary>
        public static void ProcessCommandString()
        {
            ConsoleState state = Program.GetConsoleState();

            state.ChangeCurrentDirectory(!string.IsNullOrEmpty(state.StartupParams.RunDirectory) ? state.StartupParams.RunDirectory : StringUtils.GetDefaultWorkingDirectory());
            state.CmdProcessStartInfo.WorkingDirectory = state.CurrentDirectory;
            // If called via AutoRun script and there were command line parameters
            if (!state.StartupParams.Help && state.StartupParams.RunExecutable.Length > 0)
            {
                string runCommand = state.StartupParams.RunCommandCliParams?.RunCommand ?? state.StartupParams.RunCommand;

                if (runCommand.Length > 0)
                {
                    if (state.StartupParams.RunExecutable.ToLower() == StringUtils.GetComSpec().ToLower())
                    {
                        state.CmdProcessStartInfo.FileName = StringUtils.GetComSpec();
                        bool surroundedQuotes = runCommand.EndsWith("\"\"");
                        state.CmdProcessStartInfo.Arguments = string.Concat(" /D /Q ", surroundedQuotes ? runCommand[..^1] : runCommand, " & CD >", state.PathLogger.LogFile, " & SET >", state.EnvLogger.LogFile, surroundedQuotes ? "\"" : "");
                    }
                    else
                    {
                        state.CmdProcessStartInfo.FileName = state.StartupParams.RunExecutable;
                        state.CmdProcessStartInfo.Arguments = runCommand;
                    }
                    state.CmdProcess = Process.Start(state.CmdProcessStartInfo);
                    state.CmdProcess?.WaitForExit();
                }
                state.CmdProcessStartInfo.FileName = StringUtils.GetComSpec();
            }
            // If called directly and there were command line parameters
            else if (state.StartupParams.CommandString != string.Empty)
            {
                state.CmdProcessStartInfo.FileName = StringUtils.GetComSpec();
                // Start a self-closing cmd process with entered command (if /? wasn't used), then silently store current directory and environment variables
                if (!state.StartupParams.Help)
                {
                    // The /Q switch only disables echo output for the command string command
                    string cliCommandParams = state.StartupParams.EchoOff ? state.StartupParams.CmdParams + " /Q" : state.StartupParams.CmdParams;
                    state.CmdProcessStartInfo.Arguments = string.Concat(cliCommandParams, " /D /C ", state.StartupParams.CommandString, " & CD >", state.PathLogger.LogFile, " & SET >", state.EnvLogger.LogFile);
                }
                else
                {
                    // The /? switch is treated differently - all other parameters get ignored
                    state.CmdProcessStartInfo.Arguments = "/?";
                }
                state.CmdProcess = Process.Start(state.CmdProcessStartInfo);
                state.CmdProcess?.WaitForExit();
                // The command string has finished executing.
            }

            // The switches /C and /? cause cmd to close instead of giving a prompt
            if (!state.StartupParams.Close && !state.StartupParams.Help)
            {
                // No header is printed if /K is used
                if (!state.StartupParams.Keep && !state.StartupParams.HeaderOff && state.StartupParams.RunCommandCliParams == null)
                {
                    Console.WriteLine(StringUtils.GetPromptHeader(true));
                }
                else
                {
                    // If / K is used, a newline is printed after the command string has executed
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
                ConsoleOutput.WritePrompt(state, false);
            }
            else
            {
                state.EndSession();
            }
        }
    }
}
