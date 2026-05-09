# Simple Doom Mod Loader

Simple Doom Mod Loader is a lightweight Windows-first desktop app for managing Doom launch inputs with saved profiles.

## What It Does

- Creates and manages saved launch profiles.
- Uses shared libraries for Source Ports, IWADs, and Mods.
- Builds launch arguments from profile selections.
- Launches your selected source port with the chosen IWAD and mods.

## Screenshot
<img width="856" height="822" alt="image" src="https://github.com/user-attachments/assets/3e956d24-efd4-4a80-bf5f-e5a66b962910" />

## Requirements

- Windows 64-bit.
- .NET 9 Runtime (x64).

.NET downloads: <https://dotnet.microsoft.com/en-us/download/dotnet/9.0>

## Quick Start (Release ZIP)

1. Download and extract the release ZIP.
2. Run `ModLoader.App.exe`.
3. Add Source Ports, IWADs, and Mods in the File Library.
4. Create a profile and assign one source port, one IWAD, and optional mods.
5. Launch from the saved profile.

Notes:
- The app stores config in `modloader.config.json` next to the executable.
