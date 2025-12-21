using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Xunit;

namespace Tests;

public class LoadFoldersXmlTests
{
    [Fact]
    public void TestLoadFoldersPathAreValid()
    {
        var loadFoldersDir = FindLoadFoldersXmlDirectory();
        XmlDocument doc = new();
        doc.Load(Path.Combine(loadFoldersDir.FullName, "LoadFolders.xml"));

        var root = doc.DocumentElement;
        var entries = root.FirstChild.SelectNodes("//li");
        HashSet<(string, string)> modsWithPath = [];
        Dictionary<string, string> relativePaths = [];

        foreach (XmlNode entry in entries)
        {
            var packageId = entry.Attributes.GetNamedItem("IfModActive")?.Value ?? "CETeam.CombatExtended";
            var modSpecificPath = entry.InnerText;

            Assert.False(modsWithPath.Contains((packageId, modSpecificPath)), $"Mod {packageId} is listed in LoadFolders.xml twice, both pointing to {modSpecificPath}");
            modsWithPath.Add((packageId, modSpecificPath));

            DirectoryInfo loadFolder = new(Path.Combine(loadFoldersDir.FullName, modSpecificPath.TrimStart(Path.DirectorySeparatorChar)));
            Assert.True(loadFolder.Exists, $"Mod {packageId} has an invalid patch directory {modSpecificPath} in LoadFolders.xml");

            foreach (var xmlFile in GetPatchesAndDefs(loadFolder))
            {
                var relPath = Path.GetRelativePath(loadFolder.FullName, xmlFile);
                if (relativePaths.TryGetValue(relPath, out var duplicatePackageId))
                {
                        Assert.Fail($"Relative paths collision found: {relPath} is duplicated for mods {duplicatePackageId} and {packageId}");
                }

                relativePaths[relPath] = packageId;
            }
        }

        Assert.True(modsWithPath.Count > 0, "No entries found while checking LoadFolders.xml, is the file valid?");
    }

    /// <summary>
    /// Get all XML file paths under the Defs/ and Patches/ subdirectories of a single LoadFolders.xml entry.
    /// </summary>
    private IEnumerable<string> GetPatchesAndDefs(DirectoryInfo loadFolder)
    {
        List<string> folders = ["Defs", "Patches"];
        foreach (var folder in folders)
        {
            DirectoryInfo subFolder = new(Path.Combine(loadFolder.FullName, folder));
            if (!subFolder.Exists)
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(subFolder.FullName, "*.xml", SearchOption.AllDirectories))
            {
                yield return file;
            }
        }
    }

    private DirectoryInfo FindLoadFoldersXmlDirectory()
    {
        for (DirectoryInfo directoryInfo = new(Environment.CurrentDirectory);
             directoryInfo != null;
             directoryInfo = directoryInfo.Parent)
        {
            FileInfo loadFoldersXml = new(Path.Combine(directoryInfo.FullName, "LoadFolders.xml"));
            if (loadFoldersXml.Exists)
            {
                return directoryInfo;
            }
        }

        Assert.Fail("Run `dotnet test` from within the CombatExtended mod folder");
        return null;
    }
}
