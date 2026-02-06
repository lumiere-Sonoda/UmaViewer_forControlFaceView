#if UNITY_STANDALONE_OSX

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace SFB {
    public class StandaloneFileBrowserMac : IStandaloneFileBrowser {
        private const string PluginName = "StandaloneFileBrowser";
        private const char PathsSeparator = (char) 28;

        [DllImport(PluginName)]
        private static extern IntPtr DialogOpenFilePanel(string title, string directory, string filters, bool multiselect);

        [DllImport(PluginName)]
        private static extern IntPtr DialogOpenFolderPanel(string title, string directory, bool multiselect);

        [DllImport(PluginName)]
        private static extern IntPtr DialogSaveFilePanel(string title, string directory, string defaultName, string filters);

        public string[] OpenFilePanel(string title, string directory, ExtensionFilter[] extensions, bool multiselect) {
            var filters = GetFilterFromFileExtensionList(extensions);
            var paths = MarshalDialogResult(DialogOpenFilePanel(title, directory, filters, multiselect));
            return SplitPaths(paths);
        }

        public void OpenFilePanelAsync(string title, string directory, ExtensionFilter[] extensions, bool multiselect, Action<string[]> cb) {
            cb?.Invoke(OpenFilePanel(title, directory, extensions, multiselect));
        }

        public string[] OpenFolderPanel(string title, string directory, bool multiselect) {
            var paths = MarshalDialogResult(DialogOpenFolderPanel(title, directory, multiselect));
            return SplitPaths(paths);
        }

        public void OpenFolderPanelAsync(string title, string directory, bool multiselect, Action<string[]> cb) {
            cb?.Invoke(OpenFolderPanel(title, directory, multiselect));
        }

        public string SaveFilePanel(string title, string directory, string defaultName, ExtensionFilter[] extensions) {
            var filters = GetFilterFromFileExtensionList(extensions);
            return MarshalDialogResult(DialogSaveFilePanel(title, directory, defaultName, filters));
        }

        public void SaveFilePanelAsync(string title, string directory, string defaultName, ExtensionFilter[] extensions, Action<string> cb) {
            cb?.Invoke(SaveFilePanel(title, directory, defaultName, extensions));
        }

        private static string MarshalDialogResult(IntPtr nativeString) {
            if (nativeString == IntPtr.Zero) return string.Empty;
            return Marshal.PtrToStringAnsi(nativeString) ?? string.Empty;
        }

        private static string[] SplitPaths(string paths) {
            if (string.IsNullOrEmpty(paths)) return Array.Empty<string>();
            return paths.Split(new[] { PathsSeparator }, StringSplitOptions.RemoveEmptyEntries);
        }

        // Native mac plugin expects: "Name|ext1,ext2;Name2|ext3"
        private static string GetFilterFromFileExtensionList(ExtensionFilter[] extensions) {
            if (extensions == null || extensions.Length == 0) return string.Empty;

            var builder = new StringBuilder();

            for (int i = 0; i < extensions.Length; i++) {
                var filter = extensions[i];
                if (filter.Extensions == null || filter.Extensions.Length == 0) continue;

                var addedExtension = false;
                var filterName = string.IsNullOrEmpty(filter.Name) ? "Files" : filter.Name;

                builder.Append(filterName);
                builder.Append('|');

                for (int j = 0; j < filter.Extensions.Length; j++) {
                    var ext = filter.Extensions[j];
                    if (string.IsNullOrEmpty(ext)) continue;

                    if (ext[0] == '.') ext = ext.Substring(1);
                    if (ext.Length == 0) continue;

                    if (addedExtension) builder.Append(',');
                    builder.Append(ext);
                    addedExtension = true;
                }

                // Remove dangling "Name|" if this filter had no valid extensions.
                if (!addedExtension) {
                    builder.Length -= filterName.Length + 1;
                    continue;
                }

                builder.Append(';');
            }

            if (builder.Length > 0 && builder[builder.Length - 1] == ';') {
                builder.Length -= 1;
            }

            return builder.ToString();
        }
    }
}

#endif
