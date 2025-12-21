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
        foreach (XmlNode entry in entries)
        {
            var packageId = entry.Attributes.GetNamedItem("IfModActive")?.Value ?? "CETeam.CombatExtended";
            var patchesPath = entry.InnerText;

            Assert.False(modsWithPath.Contains((packageId, patchesPath)), $"Mod {packageId} is listed in LoadFolders.xml twice, both pointing to {patchesPath}");
            modsWithPath.Add((packageId, patchesPath));

            DirectoryInfo patchesDir = new(Path.Combine(loadFoldersDir.FullName, patchesPath));
            Assert.True(patchesDir.Exists, $"Mod {packageId} has an invalid patch directory {patchesPath} in LoadFolders.xml");
        }

        Assert.True(modsWithPath.Count > 0, "No entries found while checking LoadFolders.xml, is the file valid?");
    }

    private DirectoryInfo FindLoadFoldersXmlDirectory()
    {
        for (DirectoryInfo directoryInfo = new(Environment.CurrentDirectory);
             directoryInfo != null;
             directoryInfo = directoryInfo.Parent)
        {
            var loadFoldersXml =
                new FileInfo(directoryInfo.FullName + Path.DirectorySeparatorChar + "LoadFolders.xml");
            if (loadFoldersXml.Exists)
            {
                return directoryInfo;
            }
        }

        Assert.Fail("Run `dotnet test` from within the CombatExtended mod folder");
        return null;
    }
}
