using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;

namespace uk.JohnCook.dotnet.EditableCMD.Commands.Plugins
{
    class PluginLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        /// <summary>
        /// Initializes a new IsCollectible (can be unloaded) instance of the <see cref="PluginLoadContext"/> class (derived from <see cref="AssemblyLoadContext"/>) with a name equal to the assembly's absolute path.
        /// </summary>
        /// <param name="assemblyPath">The absolute path to the assembly.</param>
        public PluginLoadContext(string assemblyPath) : this(assemblyPath, true)
        {
            // Redirects to PluginLoadContext(assemblyPath, true)
        }

        /// <summary>
        /// Initializes a new IsCollectible (can be unloaded) instance of the <see cref="PluginLoadContext"/> class (derived from <see cref="AssemblyLoadContext"/>) with a name equal to the assembly's absolute path.
        /// </summary>
        /// <remarks>
        /// <paramref name="isCollectible"/> is overridden to always be <b><c>true</c></b>.
        /// </remarks>
        /// <param name="assemblyPath">The absolute path to the assembly.</param>
        /// <param name="isCollectible">Whether unloading (IsCollectible) should be enabled. <b>Ignored, always <c>true</c></b>.</param>
        public PluginLoadContext(string assemblyPath, bool isCollectible) : base(assemblyPath, !isCollectible || true)
        {
            // PluginLoadContext instances should always be IsCollectible to support unloading of assemblies (such as those that don't contain plugins).
            Debug.Assert(IsCollectible, $"PluginLoadContext instances should always be IsCollectible.");
            // Initialise the dependency resolver.
            _resolver = new AssemblyDependencyResolver(assemblyPath);
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            string assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }

            return IntPtr.Zero;
        }
    }
}
