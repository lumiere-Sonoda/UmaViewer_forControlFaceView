#if UNITY_EDITOR
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public static class StandaloneFileBrowserMacBuildHook
{
    private const string SourceBundleRelativePath = "Assets/Plugins/StandaloneFileBrowser/Plugins/StandaloneFileBrowser.bundle";

    [PostProcessBuild(1000)]
    public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
    {
#if UNITY_EDITOR_OSX
        if (target != BuildTarget.StandaloneOSX)
        {
            return;
        }

        var sourceBundlePath = Path.GetFullPath(SourceBundleRelativePath);
        if (!Directory.Exists(sourceBundlePath))
        {
            UnityEngine.Debug.LogWarning($"StandaloneFileBrowser bundle was not found at {sourceBundlePath}.");
            return;
        }

        var plugInsPath = Path.Combine(pathToBuiltProject, "Contents", "PlugIns");
        Directory.CreateDirectory(plugInsPath);

        var destinationBundlePath = Path.Combine(plugInsPath, "StandaloneFileBrowser.bundle");
        if (Directory.Exists(destinationBundlePath))
        {
            Directory.Delete(destinationBundlePath, true);
        }

        CopyDirectory(sourceBundlePath, destinationBundlePath);

        var executablePath = Path.Combine(destinationBundlePath, "Contents", "MacOS", "StandaloneFileBrowser");
        EnsureExecutablePermission(executablePath);

        UnityEngine.Debug.Log($"Copied StandaloneFileBrowser bundle to build output: {destinationBundlePath}");
#else
        _ = target;
        _ = pathToBuiltProject;
#endif
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        Directory.CreateDirectory(destinationDir);

        foreach (var filePath in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(filePath);
            var destinationFilePath = Path.Combine(destinationDir, fileName);
            File.Copy(filePath, destinationFilePath, true);
        }

        foreach (var directoryPath in Directory.GetDirectories(sourceDir))
        {
            var directoryName = Path.GetFileName(directoryPath);
            var destinationSubDirectory = Path.Combine(destinationDir, directoryName);
            CopyDirectory(directoryPath, destinationSubDirectory);
        }
    }

    private static void EnsureExecutablePermission(string executablePath)
    {
        if (!File.Exists(executablePath))
        {
            UnityEngine.Debug.LogWarning($"StandaloneFileBrowser executable was not found: {executablePath}");
            return;
        }

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/chmod",
                    Arguments = $"755 \"{executablePath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                UnityEngine.Debug.LogWarning($"chmod returned non-zero exit code for {executablePath}.");
            }
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogWarning($"Failed to set executable permission for {executablePath}: {ex.Message}");
        }
    }
}
#endif
