using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using uk.JohnCook.dotnet.EditableCMDLibrary.Commands;
using uk.JohnCook.dotnet.EditableCMDLibrary.ConsoleSessions;

namespace uk.JohnCook.dotnet.EditableCMD.Commands.Plugins
{
    class PluginLoader
    {
        /// <summary>
        /// Loads plugins from the directories and files in <paramref name="pluginPaths"/>.
        /// </summary>
        /// <param name="state">The <see cref="ConsoleState"/> for the current console session.</param>
        /// <param name="pluginPaths">The paths for plugins.</param>
        internal static IEnumerable<ICommandInput> LoadPlugins(ConsoleState state, string[] pluginPaths)
        {
            // Store the paths as a list of absolute path strings.
            List<string> pluginFiles = new(pluginPaths.Select(path => GetAbsolutePath(path)));
            // Remove any paths that do not exist.
            pluginFiles.RemoveAll(path => !Directory.Exists(path) && !File.Exists(path));
            // Move the paths that are directories to a new list.
            List<string> pluginDirectories = new(pluginFiles.Where(path => Directory.Exists(path)));
            if (pluginDirectories.Count > 0)
            {
                // Remove directories from the file list.
                pluginFiles.RemoveAll(path => pluginDirectories.Contains(path));
                // Recursively search directories for files matching *.dll (case-insensitive) and add those found to the files list.
                DirectoryInfo directoryInfo = null;
                pluginFiles.AddRange(
                    pluginDirectories.SelectMany(directoryPath =>
                    {
                        directoryInfo = new(directoryPath);
                        return directoryInfo.GetFiles("*.dll", SearchOption.AllDirectories).Select(fileInfo => fileInfo.FullName);
                    }).ToList()
                );
                // Clear directory variables
                directoryInfo = null;
                pluginDirectories.Clear();
            }
            // Remove any duplicates from the list - Distinct() uses default comparer for the Type, so string.Equals()
            pluginFiles = pluginFiles.Distinct().ToList();

            // Load the assemblies and create a collection of class instances that implement ICommandInput 
            IEnumerable<ICommandInput> commands = pluginFiles.SelectMany(pluginPath =>
            {
                // Create a PluginLoadContext and use it to load the assembly at path pluginPath.
                Assembly pluginAssembly = LoadPlugin(pluginPath, out PluginLoadContext loadContext);
                // Create instances of classes that implement ICommandInput in the assembly, and store them in a list.
                IEnumerable<ICommandInput> newCommands = CreateCommands(pluginAssembly).ToList();
                // If newCommands contains no instance of ICommandInput, try to unload the context and assembly.
                if (!newCommands.Any())
                {
                    // PluginLoadContext instances should be collectible.
                    if (loadContext.IsCollectible)
                    {
                        loadContext.Unload();
                    }
                }
                return newCommands;
            }).ToList();

            // Return the collection of ICommandInput implementation instances.
            return commands;
        }

        /// <summary>
        /// Converts an absolute or relative path <paramref name="path"/> to an absolute path.
        /// </summary>
        /// <param name="path">A relative or absolute path.</param>
        /// <returns>An absolute path.</returns>
        private static string GetAbsolutePath(string path)
        {
            return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(AppContext.BaseDirectory), path.Replace('\\', Path.DirectorySeparatorChar)));
        }

        /// <summary>
        /// Loads an <see cref="Assembly"/> from an absolute path.
        /// </summary>
        /// <param name="pluginLocation">The absolute path to the assembly.</param>
        /// <param name="loadContext">The <see cref="PluginLoadContext"/> used to load the assembly.</param>
        /// <returns>The loaded <see cref="Assembly"/>, or throws.</returns>
        private static Assembly LoadPlugin(string pluginLocation, out PluginLoadContext loadContext)
        {
            // Create a PluginLoadContext instance for loading the assembly, using the absolute path for its name.
            loadContext = new(pluginLocation, true);
            // Load the assembly and return it.
            return loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(pluginLocation)));
        }

        /// <summary>
        /// Loads <see cref="ICommandInput"/> plugin classes from an <see cref="Assembly"/>.
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly"/> to load plugins from.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> containing all valid implementations of <see cref="ICommandInput"/> in <paramref name="assembly"/>.</returns>
        private static IEnumerable<ICommandInput> CreateCommands(Assembly assembly)
        {
            int count = 0;

            // Loop through all the Types in the Assembly:
            foreach (Type type in assembly.GetTypes())
            {
                /*
                 * Implementations of ICommandInput have the following properties:
                 *  - They are a class.
                 *  - They are not an abstract class.
                 *  - They are a public class.
                 *  - They have at least one constructor (ICommandInput's)
                 *  - They have at least one parameterless constructor (ICommandInput's)
                 *  - Instantiating an instance using the parameterless constructor results in an object of Type ICommandInput
                 */
                if (type.IsClass && !type.IsAbstract && type.IsPublic && type.GetConstructors().Length > 0 && type.GetConstructors().Where(ctor => ctor.GetParameters().Length == 0).Any() && Activator.CreateInstance(type) is ICommandInput result)
                {
                    // If an instance of ICommandInput has been created, increase the count and add the instance to the return value.
                    count++;
                    yield return result;
                }
            }

            // If the Assembly didn't contain any useable ICommandInput Types, log the problematic assembly (if debugging).
            if (count == 0)
            {
#if DEBUG
                string availableTypes = string.Join(",", assembly.GetTypes().Select(t => t.FullName));
                ApplicationException exception = new(
                    $"Can't find any type which implements ICommandInput in {assembly} from {assembly.Location}.\n" +
                    $"Available types: {availableTypes}");
                Debug.WriteLine($"\n\n{exception.Message}");
#endif
                // If an assembly doesn't contain any implementations of ICommandInput, return an instance of NullPlugin.
                yield break;
            }
        }
    }
}
