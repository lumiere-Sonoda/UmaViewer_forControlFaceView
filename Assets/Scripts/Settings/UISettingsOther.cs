#if !UNITY_ANDROID || UNITY_EDITOR
using SFB;
#endif

using System.Collections;
using System.Diagnostics;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISettingsOther : MonoBehaviour
{
    public Button UpdateDBButton;
    public TMP_Dropdown RegionDropdown;
    public TMP_Dropdown WorkModeDropdown;
    public TMP_Dropdown LanguageDropdown;
    private IEnumerator _updateResVerCoroutine;

    public void ApplySettings()
    {
        WorkModeDropdown.SetValueWithoutNotify((int)Config.Instance.WorkMode);
        RegionDropdown.SetValueWithoutNotify((int)Config.Instance.Region);
        LanguageDropdown.SetValueWithoutNotify((int)Config.Instance.Language);
        UpdateDBButton.interactable = (Config.Instance.WorkMode == WorkMode.Standalone);
    }

    public void ChangeLanguage(int lang)
    {
        if ((int)Config.Instance.Language != lang)
        {
            Config.Instance.Language = (Language)lang;
            Config.Instance.UpdateConfig(true);
        }
    }

    public void ChangeRegion(int region)
    {
        if ((int)Config.Instance.Region != region)
        {
            Config.Instance.Region = (Region)region;
            Config.Instance.UpdateConfig(false);
            StartCoroutine(UmaViewerUI.Instance.ApplyGraphicsSettings());
        }
    }

    public void ChangeWorkMode(int mode)
    {
        if ((int)Config.Instance.WorkMode != mode)
        {
            Config.Instance.WorkMode = (WorkMode)mode;
            Config.Instance.UpdateConfig(true);
        }
    }

    public void UpdateGameDB()
    {
        if (_updateResVerCoroutine != null && Config.Instance.WorkMode != WorkMode.Standalone) return;
        Popup.Create($"Automatic database update is no longer supported until all issues with new files are resolved. Please run the game to obtain required files.", -1, 200,
            "Ok", null, "Ok");
    }

    public void ChangeDataPath()
    {
#if !UNITY_ANDROID || UNITY_EDITOR
        var browseStartPath = GetBrowseStartPath();
        var selected = StandaloneFileBrowser.OpenFolderPanel("Select Uma data folder", browseStartPath, false);
        if (selected != null && selected.Length > 0 && !string.IsNullOrEmpty(selected[0]))
        {
            var resolvedDataPath = ResolveUmaDataPath(selected[0]);
            if (string.IsNullOrEmpty(resolvedDataPath))
            {
                UmaViewerUI.Instance.ShowMessage("Selected folder is invalid. It must contain meta/master/dat.", UIMessageType.Error);
                return;
            }

            var currentPath = NormalizePath(Config.Instance.MainPath);
            if (resolvedDataPath != currentPath)
            {
                Config.Instance.MainPath = resolvedDataPath;
                Config.Instance.UpdateConfig(true);
                UmaViewerUI.Instance.ShowMessage($"DataPath changed: {resolvedDataPath}", UIMessageType.Success);
            }
        }
#else
        UmaViewerUI.Instance.ShowMessage("Not supported on this platform", UIMessageType.Warning);
#endif
    }

    private static string GetBrowseStartPath()
    {
        var currentPath = NormalizePath(Config.Instance.MainPath);
        if (Directory.Exists(currentPath))
        {
            return currentPath;
        }

        // macOS first-run config may still contain a Windows path; fall back to a valid local directory.
        var persistentParent = Directory.GetParent(Application.persistentDataPath);
        if (persistentParent != null && persistentParent.Exists)
        {
            return persistentParent.FullName;
        }

        return Application.persistentDataPath;
    }

    private static string ResolveUmaDataPath(string selectedPath)
    {
        var normalized = NormalizePath(selectedPath);
        if (IsValidUmaDataPath(normalized))
        {
            return normalized;
        }

        // Common layouts: .../Persistent or .../Umamusume_Data/Persistent
        var candidatePaths = new string[]
        {
            Path.Combine(normalized, "Persistent"),
            Path.Combine(normalized, "Umamusume_Data", "Persistent"),
            Path.Combine(normalized, "umamusume"),
        };

        foreach (var candidate in candidatePaths)
        {
            var candidatePath = NormalizePath(candidate);
            if (IsValidUmaDataPath(candidatePath))
            {
                return candidatePath;
            }
        }

        return string.Empty;
    }

    private static bool IsValidUmaDataPath(string path)
    {
        var hasDefaultMeta = File.Exists(Path.Combine(path, "meta"));
        var hasDefaultMaster = File.Exists(Path.Combine(path, "master", "master.mdb"));
        var hasStandaloneMeta = File.Exists(Path.Combine(path, "meta_umaviewer"));
        var hasStandaloneMaster = File.Exists(Path.Combine(path, "master", "master_umaviewer.mdb"));
        var hasDat = Directory.Exists(Path.Combine(path, "dat"));

        return hasDat && ((hasDefaultMeta && hasDefaultMaster) || (hasStandaloneMeta && hasStandaloneMaster));
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        try
        {
            var fullPath = Path.GetFullPath(path);
            return fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        catch
        {
            return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }

    public void OpenConfig()
    {
#if !UNITY_ANDROID || UNITY_EDITOR
        if (File.Exists(Config.configPath))
        {
            Process.Start(new ProcessStartInfo()
            {
                FileName = Config.configPath,
                UseShellExecute = true
            });
        }
#else
        UmaViewerUI.Instance.ShowMessage("Not supported on this platform", UIMessageType.Warning);
#endif
    }

    public void UnloadAllBundle() => UmaAssetManager.UnloadAllBundle(true);
}
