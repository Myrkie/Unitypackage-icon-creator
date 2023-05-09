using System;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using UnityEditor;
using UnityEngine;

public class AddPackageIcon : EditorWindow
{
    [MenuItem("M6Tools/Add package icon")]
    private static void AddPngFileToPackage()
    {
        var tempdir = GetTemporaryDirectory();
        try
        {
            var packageFilePath = EditorUtility.OpenFilePanel("Open Unity package", "", "unitypackage");
            var packageFileInfo = new FileInfo(packageFilePath);
            if (packageFileInfo.Extension != ".unitypackage")
            {
                Debug.LogError("Not a Unity package file");
                return;
            }

            var icon = EditorUtility.OpenFilePanel("Select PNG file to add", "", "png");
            var iconaddinfo = new FileInfo(icon);
            if (iconaddinfo.Extension != ".png")
            {
                Debug.LogError("Selected file is not a PNG file");
                return;
            }
            
            var savedir = EditorUtility.SaveFilePanel("Select save Path and name", "", packageFileInfo.Name, "");
            if (savedir == null)
            {
                Debug.LogError("Save path invalid");
                return;
            }
            ExtractTgz(packageFilePath, tempdir);
            
            CreateTarGz(savedir, tempdir, icon);
            
            Directory.Delete(tempdir, true);
            
            Debug.Log("Icon added to package: " + icon);
        }
        catch (Exception e)
        {
            Directory.Delete(tempdir, true);
            Debug.Log(e);
        }
    }
    
    public static string GetTemporaryDirectory()
    {
        var tempDirectory = Path.Combine($"{Path.GetTempPath()}\\PackageIconCreator", Path.GetRandomFileName());
        Directory.CreateDirectory(tempDirectory);
        return tempDirectory;
    }
    
    public static void ExtractTgz(string gzArchiveName, string destFolder)
    {
        Stream inStream = File.OpenRead(gzArchiveName);
        Stream gzipStream = new GZipInputStream(inStream);

        var tarArchive = TarArchive.CreateInputTarArchive(gzipStream, Encoding.UTF8);
        tarArchive.ExtractContents(destFolder);
        tarArchive.Close();

        gzipStream.Close();
        inStream.Close();
    }
    // taken from SharpZipLib documentation edited by Myrkur
    // https://github.com/icsharpcode/SharpZipLib/wiki/GZip-and-Tar-Samples#anchorTGZ
    private static void CreateTarGz(string tgzFilename, string sourceDirectory, string iconFilePath)
    {
        Stream outStream = File.Create(tgzFilename);
        Stream gzoStream = new GZipOutputStream(outStream);
        var tarArchive = TarArchive.CreateOutputTarArchive(gzoStream);

        // Note that the RootPath is currently case sensitive and must be forward slashes e.g. "c:/temp"
        // and must not end with a slash, otherwise cuts off first char of filename
        // This is scheduled for fix in next release
        tarArchive.RootPath = sourceDirectory.Replace('\\', '/');
        if (tarArchive.RootPath.EndsWith("/"))
            tarArchive.RootPath = tarArchive.RootPath.Remove(tarArchive.RootPath.Length - 1);

        // Add the icon file to the tar without compression
        var iconFileName = ".icon.png";
        var iconTarEntry = TarEntry.CreateEntryFromFile(iconFilePath);
        iconTarEntry.Name = $"{tarArchive.RootPath}/{iconFileName}";
        iconTarEntry.TarHeader.TypeFlag = TarHeader.LF_NORMAL;
        tarArchive.WriteEntry(iconTarEntry, false);

        // Write each file to the tar.
        var filenames = Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories);
        foreach (var filename in filenames)
        {
            var relativePath = filename.Substring(sourceDirectory.Length);
            if (relativePath.StartsWith("\\") || relativePath.StartsWith("/"))
                relativePath = relativePath.Substring(1);
            relativePath = relativePath.Replace('\\', '/');
            var tarEntry = TarEntry.CreateEntryFromFile(filename);
            tarEntry.Name = $"{tarArchive.RootPath}/{relativePath}";
            tarArchive.WriteEntry(tarEntry, true);
        }

        tarArchive.Close();
    }
    
}
