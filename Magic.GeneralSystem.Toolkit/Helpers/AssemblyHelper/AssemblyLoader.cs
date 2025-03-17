using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Magic.GeneralSystem.Toolkit.Helpers.AssemblyHelper
{
    public static class AssemblyLoader
    {
        public static void EnsureAllAssembliesLoaded(List<Assembly> assemblies)
        {
            var loadedAssemblies = assemblies.Select(a => a.GetName().Name).ToHashSet();
            var assemblyFiles = Directory.GetFiles(AppContext.BaseDirectory, "*.dll");

            foreach (var assemblyFile in assemblyFiles)
            {
                var assemblyName = Path.GetFileNameWithoutExtension(assemblyFile);

                // Skip already loaded assemblies
                if (loadedAssemblies.Contains(assemblyName))
                    continue;

                try
                {
                    Assembly.LoadFrom(assemblyFile);
                    Console.WriteLine($"[Loaded] {assemblyFile}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Skipped] {assemblyFile} (Failed to load: {ex.Message})");
                }
            }
        }
    }
}
