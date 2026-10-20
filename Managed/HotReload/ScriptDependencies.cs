using System;
using System.Collections.Generic;
using System.IO;

namespace GameEngine.HotReload;

/// <summary>
/// Finds the assemblies a script project staged beside its scripts assembly.
/// </summary>
/// <remarks>
/// A scripts assembly is read as bytes and loaded from a stream, so it has no
/// <see cref="System.Reflection.Assembly.Location"/> and nothing can resolve "next to it". A project
/// therefore stages the libraries its scripts reference into <c>Dependencies/</c>, and the host loads that
/// folder into <c>AssemblyLoadContext.Default</c> before the scripts assembly is read.
/// <para>
/// Not <c>Packages/</c>: that folder is pruned at startup against the resolved package set, so a
/// dependency staged there is deleted again before anything can load it.
/// </para>
/// </remarks>
public static class ScriptDependencies
{
    /// <summary>The folder name, beside the scripts assembly, that holds project-staged dependencies.</summary>
    public const string FolderName = "Dependencies";

    /// <summary>
    /// Every <c>*.dll</c> under <c>Dependencies/</c> beside <paramref name="scriptsAssemblyPath"/>, ordered by
    /// name so loading is deterministic. Empty when the folder, the path, or either directory component is absent.
    /// </summary>
    public static IReadOnlyList<string> Enumerate(string? scriptsAssemblyPath)
    {
        var found = new List<string>();
        if (string.IsNullOrWhiteSpace(scriptsAssemblyPath))
            return found;

        string? directory = Path.GetDirectoryName(scriptsAssemblyPath);
        if (string.IsNullOrEmpty(directory))
            return found;

        string dependencies = Path.Combine(directory, FolderName);
        if (!Directory.Exists(dependencies))
            return found;

        var paths = new List<string>(Directory.EnumerateFiles(dependencies, "*.dll", SearchOption.TopDirectoryOnly));
        paths.Sort(StringComparer.OrdinalIgnoreCase);
        found.AddRange(paths);
        return found;
    }
}
