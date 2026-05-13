using System;
using System.Collections.Generic;
using System.IO;

namespace ModLoader.Core;

public sealed class LaunchInputsStore
{
    private static readonly HashSet<string> SourcePortExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe"
    };

    private static readonly HashSet<string> IwadExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".wad",
        ".pk3",
        ".iwad",
        ".ipk3",
        ".ipk7",
        ".pk7"
    };

    private static readonly HashSet<string> ModExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".wad",
        ".pwad",
        ".pk3",
        ".pk7",
        ".ipk3",
        ".ipk7",
        ".pkz",
        ".zip"
    };

    private readonly List<string> _sourcePorts = [];
    private readonly List<string> _iwads = [];
    private readonly List<string> _mods = [];
    private readonly HashSet<string> _sourcePortPathSet = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _iwadPathSet = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _modPathSet = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<string> SourcePorts => _sourcePorts;

    public IReadOnlyList<string> Iwads => _iwads;

    public IReadOnlyList<string> Mods => _mods;

    public LaunchInputsStore()
    {
    }

    public LaunchInputsStore(LaunchInputsConfig initialState)
    {
        LoadFromConfig(initialState);
    }

    public void ProcessSourcePortDrop(IEnumerable<string> droppedPaths)
    {
        foreach (var filePath in DropPathExpander.ExpandFiles(droppedPaths, includeTopLevelDirectoryFiles: false))
        {
            if (HasAllowedExtension(filePath, SourcePortExtensions))
            {
                AddUniquePath(filePath, _sourcePorts, _sourcePortPathSet);
            }
        }
    }

    public void ProcessIwadDrop(IEnumerable<string> droppedPaths)
    {
        foreach (var filePath in DropPathExpander.ExpandFiles(droppedPaths, includeTopLevelDirectoryFiles: true))
        {
            if (!HasAllowedExtension(filePath, IwadExtensions))
            {
                continue;
            }

            AddUniquePath(filePath, _iwads, _iwadPathSet);
        }
    }

    public void ProcessModDrop(IEnumerable<string> droppedPaths)
    {
        foreach (var filePath in DropPathExpander.ExpandFiles(droppedPaths, includeTopLevelDirectoryFiles: true))
        {
            if (!HasAllowedExtension(filePath, ModExtensions))
            {
                continue;
            }

            AddUniquePath(filePath, _mods, _modPathSet);
        }
    }

    public void ClearSourcePorts()
    {
        _sourcePorts.Clear();
        _sourcePortPathSet.Clear();
    }

    public void ClearIwads()
    {
        _iwads.Clear();
        _iwadPathSet.Clear();
    }

    public void ClearMods()
    {
        _mods.Clear();
        _modPathSet.Clear();
    }

    public void RemoveIwad(string path)
    {
        RemovePath(path, _iwads, _iwadPathSet);
    }

    public void RemoveSourcePort(string path)
    {
        RemovePath(path, _sourcePorts, _sourcePortPathSet);
    }

    public void RemoveMod(string path)
    {
        RemovePath(path, _mods, _modPathSet);
    }

    public bool ReorderSourcePort(string path, int targetIndex)
    {
        return ReorderPath(path, targetIndex, _sourcePorts);
    }

    public bool ReorderIwad(string path, int targetIndex)
    {
        return ReorderPath(path, targetIndex, _iwads);
    }

    public bool ReorderMod(string path, int targetIndex)
    {
        return ReorderPath(path, targetIndex, _mods);
    }

    public void LoadFromConfig(LaunchInputsConfig state)
    {
        _sourcePorts.Clear();
        _iwads.Clear();
        _mods.Clear();
        _sourcePortPathSet.Clear();
        _iwadPathSet.Clear();
        _modPathSet.Clear();

        foreach (var sourcePortPath in state.SourcePorts ?? [])
        {
            if (string.IsNullOrWhiteSpace(sourcePortPath))
            {
                continue;
            }

            AddUniquePath(sourcePortPath, _sourcePorts, _sourcePortPathSet);
        }

        foreach (var iwadPath in state.Iwads ?? [])
        {
            if (string.IsNullOrWhiteSpace(iwadPath))
            {
                continue;
            }

            AddUniquePath(iwadPath, _iwads, _iwadPathSet);
        }

        foreach (var modPath in state.Mods ?? [])
        {
            if (string.IsNullOrWhiteSpace(modPath))
            {
                continue;
            }

            AddUniquePath(modPath, _mods, _modPathSet);
        }
    }

    public bool RemoveMissingPaths()
    {
        var changed = false;

        changed |= RemoveMissingEntries(_sourcePorts, _sourcePortPathSet);
        changed |= RemoveMissingEntries(_iwads, _iwadPathSet);
        changed |= RemoveMissingEntries(_mods, _modPathSet);

        return changed;
    }

    public LaunchInputsConfig CreateSnapshot()
    {
        return new LaunchInputsConfig
        {
            SourcePorts = [.. _sourcePorts],
            Iwads = [.. _iwads],
            Mods = [.. _mods]
        };
    }

    private static bool HasAllowedExtension(string fullPath, HashSet<string> allowedExtensions)
    {
        // Validation is based on the full dropped path, not filename-only tokens.
        var extension = Path.GetExtension(fullPath);
        return allowedExtensions.Contains(extension);
    }

    private static void AddUniquePath(string path, List<string> orderedPaths, HashSet<string> dedupeSet)
    {
        var normalizedPath = PathNormalizer.NormalizeAbsolutePath(path);
        if (!dedupeSet.Add(normalizedPath))
        {
            return;
        }

        orderedPaths.Add(normalizedPath);
    }

    private static void RemovePath(string path, List<string> orderedPaths, HashSet<string> dedupeSet)
    {
        var normalizedPath = PathNormalizer.NormalizeAbsolutePath(path);
        if (!dedupeSet.Remove(normalizedPath))
        {
            return;
        }

        orderedPaths.RemoveAll(existingPath => string.Equals(existingPath, normalizedPath, StringComparison.OrdinalIgnoreCase));
    }

    private static bool RemoveMissingEntries(List<string> orderedPaths, HashSet<string> dedupeSet)
    {
        var changed = false;

        for (var i = orderedPaths.Count - 1; i >= 0; i--)
        {
            var path = orderedPaths[i];
            if (File.Exists(path))
            {
                continue;
            }

            orderedPaths.RemoveAt(i);
            dedupeSet.Remove(path);
            changed = true;
        }

        return changed;
    }

    private static bool ReorderPath(string path, int targetIndex, List<string> orderedPaths)
    {
        if (string.IsNullOrWhiteSpace(path) || targetIndex < 0 || targetIndex > orderedPaths.Count)
        {
            return false;
        }

        var normalizedPath = PathNormalizer.NormalizeAbsolutePath(path);
        var currentIndex = orderedPaths.FindIndex(existingPath => string.Equals(existingPath, normalizedPath, StringComparison.OrdinalIgnoreCase));
        if (currentIndex < 0)
        {
            return false;
        }

        var adjustedIndex = targetIndex > currentIndex ? targetIndex - 1 : targetIndex;
        if (adjustedIndex == currentIndex)
        {
            return false;
        }

        var movedPath = orderedPaths[currentIndex];
        orderedPaths.RemoveAt(currentIndex);
        orderedPaths.Insert(adjustedIndex, movedPath);
        return true;
    }
}
