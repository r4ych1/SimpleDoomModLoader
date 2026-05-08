using System.Collections.Generic;

namespace ModLoader.Core;

public sealed class LaunchInputsConfig
{
    public List<string> SourcePorts { get; init; } = [];

    public List<ProfileConfig> Profiles { get; init; } = [];

    public string? SelectedProfileId { get; init; }

    public bool IsFileLibraryViewActive { get; init; }

    public bool IsSourcePortSectionCollapsed { get; init; }

    public string? SelectedSourcePortPath { get; init; }

    public List<string> Iwads { get; init; } = [];

    public bool IsIwadSectionCollapsed { get; init; }

    public List<string> Mods { get; init; } = [];

    public bool IsModSectionCollapsed { get; init; }

    public string? SelectedIwadPath { get; init; }

    public List<string> SelectedModPaths { get; init; } = [];

    public static LaunchInputsConfig Empty { get; } = new();
}
