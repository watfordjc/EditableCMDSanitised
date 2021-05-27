using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using uk.JohnCook.dotnet.EditableCMD.Commands.Plugins;
using uk.JohnCook.dotnet.EditableCMD.InputProcessing;
using uk.JohnCook.dotnet.EditableCMDLibrary.Commands;
using uk.JohnCook.dotnet.EditableCMDLibrary.ConsoleSessions;
using uk.JohnCook.dotnet.EditableCMDLibrary.Interop;

namespace uk.JohnCook.dotnet.EditableCMD.InputHandlers
{
    /// <summary>
    /// Class containing event handler for console key input events
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class ConsoleKeyInputHandler
    {
        /// <summary>
        /// Event where (bKeyDown == true) OR ((bKeyDown == false) AND the KeyUp event is being treated as a KeyDown event)
        /// </summary>
        public event EventHandler<NativeMethods.ConsoleKeyEventArgs> KeyPressed;

        private readonly InputProcessing.NormalModeKeys.Enter enterCommand = null;

        private void AddInputEventHandler(ConsoleState state, ICommandInput handler)
        {
            handler.Init(state);
            KeyPressed += handler.ProcessCommand;
        }

        private void AddCommandEventHandler(ConsoleState state, ICommandInput handler)
        {
            handler.Init(state);
            enterCommand.CommandEntered += handler.ProcessCommand;
        }

        /// <summary>
        /// Event handler setup
        /// </summary>
        /// <param name="state">The <see cref="ConsoleState"/> for the current console session.</param>
        protected internal ConsoleKeyInputHandler(ConsoleState state)
        {
            // Event handlers are tried in the order they are added to an event
            // If an event handler handles the event, it should set e.Handled to true so the other handlers ignore it

            #region Normal Key Presses

            #region Ignoreable Key Presses
            // Some key input events should be ignored
            // Ctrl key pressed - not a modifier, no enhanced key
            AddInputEventHandler(state, new InputProcessing.NormalModeKeys.Ctrl());
            // Alt key pressed - not a modifier, no enhanced key
            AddInputEventHandler(state, new InputProcessing.NormalModeKeys.Alt());
            #endregion

            #region Plugin Loading
            // Create a string for path "Plugins\Commands"
            string hardcodedPluginPath = string.Join(Path.DirectorySeparatorChar, new string[] { "Plugins", "Commands" });
            // Create an array of possible absolute directory paths - ".\Plugins\Commands", "[...]\EditableCMDSanitised\Plugins\Commands", and "[...]\EditableCMD\Plugins\Commands"
            string[] pluginPaths = new string[]
            {
                string.Join(Path.DirectorySeparatorChar, ".", hardcodedPluginPath), // Relative to the directory containing the executable
                // EditableCMDSanitised
                string.Join(Path.DirectorySeparatorChar, Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), strings.applicationName, hardcodedPluginPath), // %LocalAppData% - Local app settings
                string.Join(Path.DirectorySeparatorChar, Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), strings.applicationName, hardcodedPluginPath), // %AppData% - Roaming app settings (downloads/uploads to fileserver on login/logout)
                string.Join(Path.DirectorySeparatorChar, Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), strings.applicationName, hardcodedPluginPath), // %ProgramData% - Shared settings between users of the same computer
                string.Join(Path.DirectorySeparatorChar, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), strings.applicationName, hardcodedPluginPath), // %ProgramFiles% -  Installation directory, if installed
                // EditableCMD
                string.Join(Path.DirectorySeparatorChar, Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), strings.unsanitisedApplicationName, hardcodedPluginPath), // %LocalAppData% - Local app settings
                string.Join(Path.DirectorySeparatorChar, Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), strings.unsanitisedApplicationName, hardcodedPluginPath), // %AppData% - Roaming app settings (downloads/uploads to fileserver on login/logout)
                string.Join(Path.DirectorySeparatorChar, Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), strings.unsanitisedApplicationName, hardcodedPluginPath), // %ProgramData% - Shared settings between users of the same computer
                string.Join(Path.DirectorySeparatorChar, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), strings.unsanitisedApplicationName, hardcodedPluginPath) // %ProgramFiles% -  Installation directory, if installed
            };
            IEnumerable<ICommandInput> plugins = PluginLoader.LoadPlugins(state, pluginPaths);
            #endregion

            #region Edit mode
            // Edit-mode plugins
            IEnumerable<ICommandInput> editModePlugins = plugins.Where(plugin => plugin.EditModeHandled);
            InitPlugins(state, editModePlugins);
            // Currently handles all keyboard input in edit-mode
            AddInputEventHandler(state, new InputEditMode());
            #endregion

            #region Normal mode
            // Key input plugins
            IEnumerable<ICommandInput> keyInputPlugins = plugins.Where(plugin => plugin.NormalModeHandled && plugin.CommandsHandled == null);
            InitPlugins(state, keyInputPlugins);
            // Printable characters - i.e. typing
            AddInputEventHandler(state, new InputProcessing.NormalModeKeys.PrintableCharacter());
            // Enter key (also the Return key) - forwards the event to the Enter.CommandEntered event if there is also input text that needs handling
            enterCommand = new InputProcessing.NormalModeKeys.Enter();
            KeyPressed += enterCommand.ProcessCommand;
            // Other non-printable character keys
            AddInputEventHandler(state, new InputProcessing.NormalModeKeys.F1());
            AddInputEventHandler(state, new InputProcessing.NormalModeKeys.F12());
            AddInputEventHandler(state, new InputProcessing.NormalModeKeys.Tab());
            AddInputEventHandler(state, new InputProcessing.NormalModeKeys.Escape());
            AddInputEventHandler(state, new InputProcessing.NormalModeKeys.Backspace());
            AddInputEventHandler(state, new InputProcessing.NormalModeKeys.Insert());
            AddInputEventHandler(state, new InputProcessing.NormalModeKeys.Delete());
            AddInputEventHandler(state, new InputProcessing.NormalModeKeys.Home());
            AddInputEventHandler(state, new InputProcessing.NormalModeKeys.End());
            AddInputEventHandler(state, new InputProcessing.NormalModeKeys.PageUp());
            AddInputEventHandler(state, new InputProcessing.NormalModeKeys.PageDown());
            AddInputEventHandler(state, new InputProcessing.NormalModeKeys.UpArrow());
            AddInputEventHandler(state, new InputProcessing.NormalModeKeys.DownArrow());
            AddInputEventHandler(state, new InputProcessing.NormalModeKeys.RightArrow());
            AddInputEventHandler(state, new InputProcessing.NormalModeKeys.LeftArrow());
            #endregion

            #endregion

            #region Commands

            // Command input plugins
            IEnumerable<ICommandInput> commandInputPlugins = plugins.Where(plugin => plugin.NormalModeHandled && plugin.KeysHandled?.Contains(ConsoleKey.Enter) == true && plugin.CommandsHandled?.Length > 0);
            InitPlugins(state, commandInputPlugins);

            #region Custom Commands
            // EDIT and UNDO - enter edit mode
            AddCommandEventHandler(state, new Commands.EnterEditMode());
            #endregion

            #region Built-in Commands
            // Change drive - drive letter and colon, e.g. D:
            AddCommandEventHandler(state, new Commands.ChangeDrive());
            // EXIT - exit the application
            AddCommandEventHandler(state, new Commands.Exit());
            // COLOR - reset console colours to default (parameterless command)
            AddCommandEventHandler(state, new Commands.Color());
            // START - opens a new window (parameterless command)
            AddCommandEventHandler(state, new Commands.Start());
            // ECHO - enables/disables echo or shows status (doesn't support echoing)
            AddCommandEventHandler(state, new Commands.Echo());
            // SET - sets/unsets/displays environment variables
            AddCommandEventHandler(state, new Commands.Set());
            // VER - display Windows version
            AddCommandEventHandler(state, new Commands.Ver());
            #endregion

            #region External Commands
            #endregion

            #region Passthrough Command - send command to Command Prompt
            AddCommandEventHandler(state, new Commands.Passthrough());
            #endregion

            #endregion
        }

        /// <summary>
        /// Adds plugin event handlers to events.
        /// </summary>
        /// <param name="state">The <see cref="ConsoleState"/> for the current console session.</param>
        /// <param name="commands">The <see cref="ICommandInput"/> implementations to add.</param>
        private void InitPlugins(ConsoleState state, IEnumerable<ICommandInput> commands)
        {
            foreach (ICommandInput command in commands)
            {
                if (command.EditModeHandled || command.CommandsHandled?.Length == null)
                {
                    AddInputEventHandler(state, command);
                }
                else
                {
                    AddCommandEventHandler(state, command);
                }
                string pluginLoadStringFormat = "Loaded {0} by {1} - {2}";
                object[] pluginLoadStringArgs = new object[] {
                    command.Name,
                    !string.IsNullOrEmpty(command.AuthorTwitchUsername) ? command.AuthorTwitchUsername : !string.IsNullOrEmpty(command.AuthorName) ? command.AuthorName : "Anonymous",
                    command.Description
                };
                Debug.WriteLine(pluginLoadStringFormat, pluginLoadStringArgs);
                state.InputLogger?.FormattedLog(pluginLoadStringFormat, pluginLoadStringArgs);
            };
        }

        /// <summary>
        /// Handler for keyboard events.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The ConsoleKeyEventArgs to parse.</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void OnKeyboardInput(object sender, NativeMethods.ConsoleKeyEventArgs e)
        {
            if (e.Key.KeyDown)
            {
                if (enterCommand == null)
                {
                    KeyPressed?.Invoke(null, e);
                }
                else
                {
                    if (e.Key.ConsoleKey == ConsoleKey.Enter && !e.Key.HasModifier && e.State.Input.Length > 0)
                    {
                        enterCommand.ProcessCommand(sender, e);
                    }
                    else
                    {
                        KeyPressed?.Invoke(null, e);
                    }
                }
            }
        }
    }
}
