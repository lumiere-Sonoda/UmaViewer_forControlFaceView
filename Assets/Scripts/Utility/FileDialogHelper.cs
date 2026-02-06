using System;
using System.IO;
#if !UNITY_ANDROID || UNITY_EDITOR
using SFB;
#endif
using UnityEngine;

public static class FileDialogHelper
{
    /// <summary>
    /// Returns an existing directory to start a file dialog from. Falls back to a
    /// platform-specific export directory and finally Unity's persistent data path.
    /// </summary>
    public static string GetSafeStartDirectory(string preferredDirectory)
    {
        if (!string.IsNullOrWhiteSpace(preferredDirectory) && Directory.Exists(preferredDirectory))
        {
            return preferredDirectory;
        }

        var exportRoot = GetPlatformExportRoot();
        if (Directory.Exists(exportRoot))
        {
            return exportRoot;
        }

        var persistentParent = Directory.GetParent(Application.persistentDataPath);
        if (persistentParent != null && persistentParent.Exists)
        {
            return persistentParent.FullName;
        }

        return Application.persistentDataPath;
    }

    /// <summary>
    /// Platform-specific default root for exported files.
    /// </summary>
    public static string GetPlatformExportRoot()
    {
        string baseDir;
        switch (Application.platform)
        {
            case RuntimePlatform.OSXPlayer:
            case RuntimePlatform.OSXEditor:
                baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Documents", "UmaViewer", "Exports");
                break;
            case RuntimePlatform.WindowsPlayer:
            case RuntimePlatform.WindowsEditor:
                baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "UmaViewer", "Exports");
                break;
            default:
                baseDir = Path.Combine(Application.persistentDataPath, "Exports");
                break;
        }

        Directory.CreateDirectory(baseDir);
        return baseDir;
    }

    public static string SaveFile(string title, string preferredDirectory, string defaultName, string extension)
    {
        var startDir = GetSafeStartDirectory(preferredDirectory);

#if !UNITY_ANDROID || UNITY_EDITOR
        try
        {
            return StandaloneFileBrowser.SaveFilePanel(title, startDir, defaultName, extension);
        }
        catch (DllNotFoundException)
        {
            return FallbackSavePath(startDir, defaultName, extension, "Native file dialog plug-in is missing for this build. Using a platform-specific export folder instead.");
        }
        catch (EntryPointNotFoundException)
        {
            return FallbackSavePath(startDir, defaultName, extension, "Native file dialog entry point was not found. Using a platform-specific export folder instead.");
        }
#else
        return FallbackSavePath(startDir, defaultName, extension, "Native file dialog is not supported on this platform. Using a platform-specific export folder instead.");
#endif
    }

    public static string[] OpenFolder(string title, string preferredDirectory, bool multiselect = false)
    {
        var startDir = GetSafeStartDirectory(preferredDirectory);

        try
        {
#if !UNITY_ANDROID || UNITY_EDITOR
            return StandaloneFileBrowser.OpenFolderPanel(title, startDir, multiselect);
#else
            WarnMissingDialogPlugin();
            return Array.Empty<string>();
#endif
        }
#if !UNITY_ANDROID || UNITY_EDITOR
        catch (DllNotFoundException)
        {
            WarnMissingDialogPlugin();
            return Array.Empty<string>();
        }
        catch (EntryPointNotFoundException)
        {
            WarnMissingDialogPlugin();
            return Array.Empty<string>();
        }
#endif
    }

    private static string FallbackSavePath(string startDir, string defaultName, string extension, string warning)
    {
        var exportRoot = GetPlatformExportRoot();
        var safeName = defaultName;
        var normalizedExtension = string.IsNullOrEmpty(extension) ? string.Empty : extension.TrimStart('.');

        if (!string.IsNullOrWhiteSpace(normalizedExtension) && !safeName.EndsWith($".{normalizedExtension}", StringComparison.OrdinalIgnoreCase))
        {
            safeName += $".{normalizedExtension}";
        }

        var fallbackPath = Path.Combine(exportRoot, safeName);
        UmaViewerUI.Instance?.ShowMessage($"{warning}\nFallback path: {fallbackPath}", UIMessageType.Warning);
        return fallbackPath;
    }

    private static void WarnMissingDialogPlugin()
    {
        var message = "Native file dialog plug-in could not be loaded.";
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
        var expectedBundlePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "PlugIns", "StandaloneFileBrowser.bundle"));
        message += $"\nExpected bundle: {expectedBundlePath}";
#endif
        message += "\nRebuild the macOS app after importing this project update.";
        UmaViewerUI.Instance?.ShowMessage(message, UIMessageType.Error);
    }
}
