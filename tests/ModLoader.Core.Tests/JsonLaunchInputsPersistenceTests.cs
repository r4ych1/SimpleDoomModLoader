using System.Text.Json;
using ModLoader.Core;

namespace ModLoader.Core.Tests;

public sealed class JsonLaunchInputsPersistenceTests
{
    [Fact]
    public void Load_MissingConfigFile_ReturnsEmptyStateWithoutWarning()
    {
        using var temp = new TempDirectory();
        var configPath = Path.Combine(temp.Path, "modloader.config.json");
        var persistence = new JsonLaunchInputsPersistence(configPath);

        var result = persistence.Load();

        Assert.False(result.HadLoadWarning);
        Assert.False(result.HadRemediationAction);
        Assert.Null(result.WarningMessage);
        Assert.Empty(result.State.SourcePorts);
        Assert.Empty(result.State.Profiles);
        Assert.Null(result.State.SelectedProfileId);
        Assert.False(result.State.IsFileLibraryPaneCollapsed);
        Assert.Null(result.State.LastExpandedWindowWidth);
        Assert.False(result.State.IsSourcePortSectionCollapsed);
        Assert.Null(result.State.SelectedSourcePortPath);
        Assert.Empty(result.State.Iwads);
        Assert.False(result.State.IsIwadSectionCollapsed);
        Assert.Empty(result.State.Mods);
        Assert.False(result.State.IsModSectionCollapsed);
        Assert.Null(result.State.SelectedIwadPath);
        Assert.Empty(result.State.SelectedModPaths);
    }

    [Fact]
    public void Save_ThenLoad_PreservesLibraryProfilesAndSelectionState()
    {
        using var temp = new TempDirectory();
        var configPath = Path.Combine(temp.Path, "modloader.config.json");
        var persistence = new JsonLaunchInputsPersistence(configPath);

        var state = new LaunchInputsConfig
        {
            SourcePorts = [Path.Combine(temp.Path, "gzdoom.exe"), Path.Combine(temp.Path, "vkdoom.exe")],
            Profiles =
            [
                new ProfileConfig
                {
                    Id = "p1",
                    Name = "Profile 1",
                    SourcePortPath = Path.Combine(temp.Path, "gzdoom.exe"),
                    IwadPath = Path.Combine(temp.Path, "doom2.wad"),
                    SelectedModPaths = [Path.Combine(temp.Path, "mod-b.pk3"), Path.Combine(temp.Path, "mod-a.pk3")]
                }
            ],
            SelectedProfileId = "p1",
            IsFileLibraryPaneCollapsed = true,
            LastExpandedWindowWidth = 1366d,
            IsSourcePortSectionCollapsed = true,
            SelectedSourcePortPath = null,
            Iwads = [Path.Combine(temp.Path, "doom.wad"), Path.Combine(temp.Path, "doom2.wad")],
            IsIwadSectionCollapsed = false,
            Mods = [Path.Combine(temp.Path, "mod-a.pk3"), Path.Combine(temp.Path, "mod-b.pk3")],
            IsModSectionCollapsed = true,
            SelectedIwadPath = null,
            SelectedModPaths = []
        };

        persistence.Save(state);
        var result = persistence.Load();

        Assert.Equal(state.SourcePorts, result.State.SourcePorts);
        Assert.Equal(state.SelectedProfileId, result.State.SelectedProfileId);
        Assert.Equal(state.IsFileLibraryPaneCollapsed, result.State.IsFileLibraryPaneCollapsed);
        Assert.Equal(state.LastExpandedWindowWidth, result.State.LastExpandedWindowWidth);
        Assert.Equal(state.IsSourcePortSectionCollapsed, result.State.IsSourcePortSectionCollapsed);
        Assert.Equal(state.Iwads, result.State.Iwads);
        Assert.Equal(state.IsIwadSectionCollapsed, result.State.IsIwadSectionCollapsed);
        Assert.Equal(state.Mods, result.State.Mods);
        Assert.Equal(state.IsModSectionCollapsed, result.State.IsModSectionCollapsed);
        Assert.Single(result.State.Profiles);
        Assert.Equal("p1", result.State.Profiles[0].Id);
        Assert.Equal("Profile 1", result.State.Profiles[0].Name);
        Assert.Equal(Path.Combine(temp.Path, "gzdoom.exe"), result.State.Profiles[0].SourcePortPath);
        Assert.Equal(Path.Combine(temp.Path, "doom2.wad"), result.State.Profiles[0].IwadPath);
        Assert.Equal(
            [Path.Combine(temp.Path, "mod-b.pk3"), Path.Combine(temp.Path, "mod-a.pk3")],
            result.State.Profiles[0].SelectedModPaths);
    }

    [Fact]
    public void Load_LegacySourcePortPath_MigratesToSourcePortsAndSelectedSourcePort()
    {
        using var temp = new TempDirectory();
        var configPath = Path.Combine(temp.Path, "modloader.config.json");
        var legacySourcePort = Path.Combine(temp.Path, "gzdoom.exe");

        var legacyConfigJson = JsonSerializer.Serialize(new
        {
            SourcePortPath = legacySourcePort,
            Iwads = Array.Empty<string>(),
            Mods = Array.Empty<string>(),
            SelectedIwadPath = (string?)null,
            SelectedModPaths = Array.Empty<string>()
        });
        File.WriteAllText(configPath, legacyConfigJson);

        var persistence = new JsonLaunchInputsPersistence(configPath);
        var result = persistence.Load();

        Assert.Equal([legacySourcePort], result.State.SourcePorts);
        Assert.Equal(legacySourcePort, result.State.SelectedSourcePortPath);
        Assert.Empty(result.State.Profiles);
        Assert.Null(result.State.SelectedProfileId);
        Assert.False(result.State.IsFileLibraryPaneCollapsed);
        Assert.Null(result.State.LastExpandedWindowWidth);
        Assert.False(result.State.IsSourcePortSectionCollapsed);
        Assert.False(result.State.IsIwadSectionCollapsed);
        Assert.False(result.State.IsModSectionCollapsed);
    }

    [Fact]
    public void Load_ConfigWithoutCollapseFields_DefaultsAllSectionsToExpanded()
    {
        using var temp = new TempDirectory();
        var configPath = Path.Combine(temp.Path, "modloader.config.json");
        var configJson = JsonSerializer.Serialize(new
        {
            SourcePorts = new[] { Path.Combine(temp.Path, "gzdoom.exe") },
            Profiles = Array.Empty<object>(),
            SelectedProfileId = (string?)null,
            SelectedSourcePortPath = (string?)null,
            Iwads = Array.Empty<string>(),
            Mods = Array.Empty<string>(),
            SelectedIwadPath = (string?)null,
            SelectedModPaths = Array.Empty<string>()
        });
        File.WriteAllText(configPath, configJson);

        var persistence = new JsonLaunchInputsPersistence(configPath);
        var result = persistence.Load();

        Assert.False(result.State.IsFileLibraryPaneCollapsed);
        Assert.Null(result.State.LastExpandedWindowWidth);
        Assert.False(result.State.IsSourcePortSectionCollapsed);
        Assert.False(result.State.IsIwadSectionCollapsed);
        Assert.False(result.State.IsModSectionCollapsed);
    }

    [Fact]
    public void Load_InvalidJson_RenamesBrokenFile_AndWritesFreshEmptyConfig()
    {
        using var temp = new TempDirectory();
        var configPath = Path.Combine(temp.Path, "modloader.config.json");
        File.WriteAllText(configPath, "{ this is not valid json");

        var persistence = new JsonLaunchInputsPersistence(configPath);
        var result = persistence.Load();

        Assert.True(result.HadLoadWarning);
        Assert.True(result.HadRemediationAction);
        Assert.NotNull(result.WarningMessage);
        Assert.Empty(result.State.SourcePorts);
        Assert.Empty(result.State.Profiles);
        Assert.Null(result.State.SelectedProfileId);
        Assert.False(result.State.IsFileLibraryPaneCollapsed);
        Assert.Null(result.State.LastExpandedWindowWidth);
        Assert.False(result.State.IsSourcePortSectionCollapsed);
        Assert.Null(result.State.SelectedSourcePortPath);
        Assert.Empty(result.State.Iwads);
        Assert.False(result.State.IsIwadSectionCollapsed);
        Assert.Empty(result.State.Mods);
        Assert.False(result.State.IsModSectionCollapsed);
        Assert.Null(result.State.SelectedIwadPath);
        Assert.Empty(result.State.SelectedModPaths);

        var backupPath = Directory.EnumerateFiles(temp.Path, "modloader.config.json.broken.*").Single();
        Assert.True(File.Exists(backupPath));
        Assert.True(File.Exists(configPath));

        var replacementJson = File.ReadAllText(configPath);
        var replacementState = JsonSerializer.Deserialize<LaunchInputsConfig>(replacementJson);
        Assert.NotNull(replacementState);
        Assert.Empty(replacementState.SourcePorts);
        Assert.Empty(replacementState.Profiles);
        Assert.Null(replacementState.SelectedProfileId);
        Assert.False(replacementState.IsFileLibraryPaneCollapsed);
        Assert.Null(replacementState.LastExpandedWindowWidth);
        Assert.False(replacementState.IsSourcePortSectionCollapsed);
        Assert.Null(replacementState.SelectedSourcePortPath);
        Assert.Empty(replacementState.Iwads);
        Assert.False(replacementState.IsIwadSectionCollapsed);
        Assert.Empty(replacementState.Mods);
        Assert.False(replacementState.IsModSectionCollapsed);
        Assert.Null(replacementState.SelectedIwadPath);
        Assert.Empty(replacementState.SelectedModPaths);
    }
}
