using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameEngine.HotReload;
using NUnit.Framework;

namespace GameEngine.CoreBridgeTests;

/// <summary>
/// The rule that decides which files a script project staged beside its scripts assembly.
/// </summary>
/// <remarks>
/// The loading itself cannot be asserted here without polluting the default context permanently; the path
/// rule is the part that has been wrong, and it is pure.
/// </remarks>
[TestFixture]
public class ScriptDependenciesTests
{
    /// <summary>Creates a scratch project layout with an empty <c>Dependencies/</c>.</summary>
    [SetUp]
    public void SetUp()
    {
        m_Root = Path.Combine(Path.GetTempPath(), "era-scriptdeps-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(m_Root, ScriptDependencies.FolderName));
    }

    /// <summary>Removes the scratch layout, whatever the test did to it.</summary>
    [TearDown]
    public void TearDown()
    {
        try { Directory.Delete(m_Root, true); } catch { }
    }

    private string m_Root = string.Empty;

    /// <summary>Only DLLs are staged, and they load in a stable order.</summary>
    [Test]
    public void Enumerate_FindsDllsSortedAndIgnoresEverythingElse()
    {
        string dependencies = Path.Combine(m_Root, ScriptDependencies.FolderName);
        File.WriteAllText(Path.Combine(dependencies, "b.dll"), string.Empty);
        File.WriteAllText(Path.Combine(dependencies, "a.dll"), string.Empty);
        File.WriteAllText(Path.Combine(dependencies, "notes.txt"), string.Empty);
        File.WriteAllText(Path.Combine(dependencies, "a.pdb"), string.Empty);

        IReadOnlyList<string> found = ScriptDependencies.Enumerate(Path.Combine(m_Root, "GameEngine.Scripts.dll"));

        Assert.That(found.Select(Path.GetFileName), Is.EqualTo(new[] { "a.dll", "b.dll" }));
    }

    /// <summary>A project that stages nothing gets nothing, without an error.</summary>
    [Test]
    public void Enumerate_IsEmptyWhenTheFolderIsAbsent()
    {
        string elsewhere = Path.Combine(m_Root, "no-folder");
        Directory.CreateDirectory(elsewhere);

        Assert.That(ScriptDependencies.Enumerate(Path.Combine(elsewhere, "GameEngine.Scripts.dll")), Is.Empty);
    }

    /// <summary>No path means no staging, rather than a throw from deep inside the preload.</summary>
    [Test]
    public void Enumerate_IsEmptyForAMissingOrBlankPath()
    {
        Assert.That(ScriptDependencies.Enumerate(null), Is.Empty);
        Assert.That(ScriptDependencies.Enumerate(string.Empty), Is.Empty);
        Assert.That(ScriptDependencies.Enumerate(Path.Combine(m_Root, "nothing", "GameEngine.Scripts.dll")), Is.Empty);
    }
}
