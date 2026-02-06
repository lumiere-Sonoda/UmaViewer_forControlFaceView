# StandaloneFileBrowser macOS bundle

This folder contains the source and build script for the macOS native dialog plugin used by Unity StandaloneFileBrowser.

## Build universal binary

Run from repository root:

```bash
./Tools/StandaloneFileBrowserMac/build_macos_bundle.sh
```

The script writes a universal (`x86_64` + `arm64`) bundle executable to:

`Assets/Plugins/StandaloneFileBrowser/Plugins/StandaloneFileBrowser.bundle/Contents/MacOS/StandaloneFileBrowser`
