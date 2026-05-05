using System.Linq;
using Avalonia.Platform.Storage;
using ModLoader.App;
using ModLoader.Core;

namespace ModLoader.Core.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public void CreateNewProfile_UsesFirstAvailableDefaultNameAndSelectsNewProfile()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source],
                    Iwads = [iwad],
                    Profiles =
                    [
                        CreateProfile("p1", "Profile 1", source, iwad),
                        CreateProfile("p3", "Profile 3", source, iwad)
                    ],
                    SelectedProfileId = "p1"
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        viewModel.CreateNewProfile();

        Assert.Equal("Profile 2", viewModel.SelectedProfileName);
        Assert.Equal(3, viewModel.ProfileRows.Count);
        Assert.Contains(viewModel.ProfileRows, row => row.Name == "Profile 2");
        Assert.Equal(3, persistence.SavedStates.Last().Profiles.Count);
        Assert.Equal("Profile 2", persistence.SavedStates.Last().Profiles.Last().Name);
        Assert.Equal(persistence.SavedStates.Last().Profiles.Last().Id, persistence.SavedStates.Last().SelectedProfileId);
    }

    [Fact]
    public void ToggleProfileSelection_SelectsAndUnselectsHydratingAndClearingSelections()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");
        var mod = temp.CreateFile("mod-a.pk3");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source],
                    Iwads = [iwad],
                    Mods = [mod],
                    Profiles = [CreateProfile("p1", "Profile 1", source, iwad, mod)]
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        viewModel.ToggleProfileSelection("p1");

        Assert.Equal("Profile 1", viewModel.SelectedProfileName);
        Assert.Equal(Path.GetFullPath(source), viewModel.SelectedSourcePortPath);
        Assert.Equal(Path.GetFullPath(iwad), viewModel.SelectedIwadPath);
        Assert.Equal([Path.GetFullPath(mod)], viewModel.SelectedModPaths);
        Assert.True(viewModel.CanLaunch);

        viewModel.ToggleProfileSelection("p1");

        Assert.Equal("No Profile Selected", viewModel.SelectedProfileName);
        Assert.Null(viewModel.SelectedSourcePortPath);
        Assert.Null(viewModel.SelectedIwadPath);
        Assert.Empty(viewModel.SelectedModPaths);
        Assert.False(viewModel.CanLaunch);
        Assert.Null(persistence.SavedStates.Last().SelectedProfileId);
        Assert.Null(persistence.SavedStates.Last().SelectedSourcePortPath);
        Assert.Null(persistence.SavedStates.Last().SelectedIwadPath);
        Assert.Empty(persistence.SavedStates.Last().SelectedModPaths);
    }

    [Fact]
    public void CanCreateProfile_IsAlwaysTrue()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");

        var persistence = new RecordingPersistence();
        var viewModel = new MainWindowViewModel(persistence);

        Assert.True(viewModel.CanCreateProfile);

        viewModel.ProcessSourcePortDrop([source]);
        viewModel.ProcessIwadDrop([iwad]);
        Assert.True(viewModel.CanCreateProfile);

        viewModel.ToggleSourcePortSelection(source);
        Assert.True(viewModel.CanCreateProfile);

        viewModel.ToggleIwadSelection(iwad);
        Assert.True(viewModel.CanCreateProfile);
    }

    [Fact]
    public void CreateNewProfile_WithoutSelections_CreatesSelectedInvalidProfile()
    {
        var persistence = new RecordingPersistence();
        var viewModel = new MainWindowViewModel(persistence);

        viewModel.CreateNewProfile();

        Assert.True(viewModel.HasSelectedProfile);
        Assert.Equal("Profile 1", viewModel.SelectedProfileName);
        Assert.False(viewModel.CanLaunch);
        Assert.Single(viewModel.ProfileRows);
        Assert.True(viewModel.ProfileRows.Single().IsInvalid);
        Assert.Null(persistence.SavedStates.Last().Profiles.Single().SourcePortPath);
        Assert.Null(persistence.SavedStates.Last().Profiles.Single().IwadPath);
        Assert.Empty(persistence.SavedStates.Last().Profiles.Single().SelectedModPaths);
    }

    [Fact]
    public void CreateNewProfile_WithPartialSelections_CopiesCurrentSelections()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var mod = temp.CreateFile("mod-a.pk3");

        var persistence = new RecordingPersistence();
        var viewModel = new MainWindowViewModel(persistence);

        viewModel.ProcessSourcePortDrop([source]);
        viewModel.ProcessModDrop([mod]);
        viewModel.ToggleSourcePortSelection(source);
        viewModel.ToggleModSelection(mod);

        viewModel.CreateNewProfile();

        var savedProfile = persistence.SavedStates.Last().Profiles.Single();
        Assert.Equal(Path.GetFullPath(source), savedProfile.SourcePortPath);
        Assert.Null(savedProfile.IwadPath);
        Assert.Equal([Path.GetFullPath(mod)], savedProfile.SelectedModPaths);
        Assert.Equal(savedProfile.Id, persistence.SavedStates.Last().SelectedProfileId);
    }

    [Fact]
    public void CreateNewProfile_WhenFileLibraryPaneCollapsed_ExpandsPane()
    {
        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    IsFileLibraryPaneCollapsed = true
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        viewModel.CreateNewProfile();

        Assert.False(viewModel.IsFileLibraryPaneCollapsed);
        Assert.True(viewModel.IsFileLibraryPaneExpanded);
        Assert.False(persistence.SavedStates.Last().IsFileLibraryPaneCollapsed);
    }

    [Fact]
    public void ToggleSectionCollapse_PersistsStateAndUpdatesVisibility()
    {
        var persistence = new RecordingPersistence();
        var viewModel = new MainWindowViewModel(persistence);

        Assert.False(viewModel.IsFileLibraryPaneCollapsed);
        Assert.True(viewModel.IsFileLibraryPaneExpanded);
        Assert.Equal("Collapse", viewModel.FileLibraryPaneToggleText);
        Assert.Equal(380d, viewModel.ProfilePaneColumnWidth.Value);
        Assert.Equal(16d, viewModel.PaneSpacerColumnWidth.Value);
        Assert.Equal(1d, viewModel.FileLibraryPaneColumnWidth.Value);
        Assert.True(viewModel.AreSourcePortRowsVisible);
        Assert.Equal("Collapse", viewModel.SourcePortSectionToggleText);
        Assert.True(viewModel.AreIwadRowsVisible);
        Assert.Equal("Collapse", viewModel.IwadSectionToggleText);
        Assert.True(viewModel.AreModRowsVisible);
        Assert.Equal("Collapse", viewModel.ModSectionToggleText);

        viewModel.ToggleSourcePortSectionCollapsed();
        viewModel.ToggleIwadSectionCollapsed();
        viewModel.ToggleModSectionCollapsed();

        viewModel.ToggleFileLibraryPaneCollapsed();

        Assert.True(viewModel.IsFileLibraryPaneCollapsed);
        Assert.False(viewModel.IsFileLibraryPaneExpanded);
        Assert.Equal("Expand", viewModel.FileLibraryPaneToggleText);
        Assert.Equal(1d, viewModel.ProfilePaneColumnWidth.Value);
        Assert.Equal(0d, viewModel.PaneSpacerColumnWidth.Value);
        Assert.Equal(0d, viewModel.FileLibraryPaneColumnWidth.Value);
        Assert.True(viewModel.IsSourcePortSectionCollapsed);
        Assert.False(viewModel.AreSourcePortRowsVisible);
        Assert.Equal("Expand", viewModel.SourcePortSectionToggleText);
        Assert.True(viewModel.IsIwadSectionCollapsed);
        Assert.False(viewModel.AreIwadRowsVisible);
        Assert.Equal("Expand", viewModel.IwadSectionToggleText);
        Assert.True(viewModel.IsModSectionCollapsed);
        Assert.False(viewModel.AreModRowsVisible);
        Assert.Equal("Expand", viewModel.ModSectionToggleText);

        Assert.True(persistence.SavedStates.Last().IsFileLibraryPaneCollapsed);
        Assert.True(persistence.SavedStates.Last().IsSourcePortSectionCollapsed);
        Assert.True(persistence.SavedStates.Last().IsIwadSectionCollapsed);
        Assert.True(persistence.SavedStates.Last().IsModSectionCollapsed);
    }

    [Fact]
    public void Constructor_WithPersistedCollapseState_RestoresSectionVisibility()
    {
        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    IsFileLibraryPaneCollapsed = true,
                    IsSourcePortSectionCollapsed = true,
                    IsIwadSectionCollapsed = false,
                    IsModSectionCollapsed = true
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        Assert.True(viewModel.IsFileLibraryPaneCollapsed);
        Assert.False(viewModel.IsFileLibraryPaneExpanded);
        Assert.Equal("Expand", viewModel.FileLibraryPaneToggleText);
        Assert.Equal(0d, viewModel.PaneSpacerColumnWidth.Value);
        Assert.Equal(0d, viewModel.FileLibraryPaneColumnWidth.Value);
        Assert.True(viewModel.IsSourcePortSectionCollapsed);
        Assert.False(viewModel.AreSourcePortRowsVisible);
        Assert.Equal("Expand", viewModel.SourcePortSectionToggleText);
        Assert.False(viewModel.IsIwadSectionCollapsed);
        Assert.True(viewModel.AreIwadRowsVisible);
        Assert.Equal("Collapse", viewModel.IwadSectionToggleText);
        Assert.True(viewModel.IsModSectionCollapsed);
        Assert.False(viewModel.AreModRowsVisible);
        Assert.Equal("Expand", viewModel.ModSectionToggleText);
    }

    [Fact]
    public void ToggleFileLibraryPaneCollapse_DoesNotChangeProfileSelectionsOrLaunchState()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");
        var mod = temp.CreateFile("mod-a.pk3");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source],
                    Iwads = [iwad],
                    Mods = [mod],
                    Profiles = [CreateProfile("p1", "Profile 1", source, iwad, mod)],
                    SelectedProfileId = "p1"
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        viewModel.ToggleFileLibraryPaneCollapsed();

        Assert.Equal("Profile 1", viewModel.SelectedProfileName);
        Assert.Equal(Path.GetFullPath(source), viewModel.SelectedSourcePortPath);
        Assert.Equal(Path.GetFullPath(iwad), viewModel.SelectedIwadPath);
        Assert.Equal([Path.GetFullPath(mod)], viewModel.SelectedModPaths);
        Assert.True(viewModel.CanLaunch);
        Assert.True(viewModel.IsFileLibraryPaneCollapsed);
    }

    [Fact]
    public void DropZoneDragState_DefaultsToInactive()
    {
        var viewModel = new MainWindowViewModel(new RecordingPersistence());

        Assert.False(viewModel.IsSourcePortDropZoneDragActive);
        Assert.False(viewModel.IsIwadDropZoneDragActive);
        Assert.False(viewModel.IsModDropZoneDragActive);
    }

    [Fact]
    public void SetDropZoneDragActive_TogglesRequestedZoneOnly()
    {
        var viewModel = new MainWindowViewModel(new RecordingPersistence());

        viewModel.SetDropZoneDragActive(DropZoneKind.Iwad, true);

        Assert.False(viewModel.IsSourcePortDropZoneDragActive);
        Assert.True(viewModel.IsIwadDropZoneDragActive);
        Assert.False(viewModel.IsModDropZoneDragActive);

        viewModel.SetDropZoneDragActive(DropZoneKind.Iwad, false);

        Assert.False(viewModel.IsIwadDropZoneDragActive);
    }

    [Fact]
    public void ProcessDrop_ResetsDropZoneDragState()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");
        var mod = temp.CreateFile("mod-a.zip");
        var viewModel = new MainWindowViewModel(new RecordingPersistence());

        viewModel.SetDropZoneDragActive(DropZoneKind.SourcePort, true);
        viewModel.SetDropZoneDragActive(DropZoneKind.Iwad, true);
        viewModel.SetDropZoneDragActive(DropZoneKind.Mod, true);

        viewModel.ProcessSourcePortDrop([source]);
        Assert.False(viewModel.IsSourcePortDropZoneDragActive);

        viewModel.SetDropZoneDragActive(DropZoneKind.Iwad, true);
        viewModel.ProcessIwadDrop([iwad]);
        Assert.False(viewModel.IsIwadDropZoneDragActive);

        viewModel.SetDropZoneDragActive(DropZoneKind.Mod, true);
        viewModel.ProcessModDrop([mod]);
        Assert.False(viewModel.IsModDropZoneDragActive);
    }

    [Fact]
    public void ToggleSelections_WhenProfileSelected_AutoSavesProfileAndCanBecomeInvalid()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source],
                    Iwads = [iwad],
                    Profiles = [CreateProfile("p1", "Profile 1", source, iwad)],
                    SelectedProfileId = "p1"
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        viewModel.ToggleIwadSelection(iwad);

        Assert.False(viewModel.CanLaunch);
        Assert.Null(viewModel.SelectedIwadPath);
        Assert.Null(persistence.SavedStates.Last().Profiles.Single().IwadPath);
        Assert.True(viewModel.ProfileRows.Single().IsInvalid);
        Assert.False(viewModel.ProfileRows.Single().CanLaunchProfile);
    }

    [Fact]
    public void DetachedSelections_DoNotPersistAsCanonicalSelectionState()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");
        var mod = temp.CreateFile("mod-a.pk3");

        var persistence = new RecordingPersistence();
        var viewModel = new MainWindowViewModel(persistence);

        viewModel.ProcessSourcePortDrop([source]);
        viewModel.ProcessIwadDrop([iwad]);
        viewModel.ProcessModDrop([mod]);
        viewModel.ToggleSourcePortSelection(source);
        viewModel.ToggleIwadSelection(iwad);
        viewModel.ToggleModSelection(mod);

        Assert.Equal("gzdoom.exe -iwad doom2.wad -file mod-a.pk3", viewModel.CommandPreviewArguments);
        Assert.Null(persistence.SavedStates.Last().SelectedProfileId);
        Assert.Null(persistence.SavedStates.Last().SelectedSourcePortPath);
        Assert.Null(persistence.SavedStates.Last().SelectedIwadPath);
        Assert.Empty(persistence.SavedStates.Last().SelectedModPaths);
    }

    [Fact]
    public void SelectProfileAndExpandFileLibraryPane_WhenCollapsedAndUnselected_SelectsHydratesAndPersistsExpandedState()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");
        var mod = temp.CreateFile("mod-a.pk3");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source],
                    Iwads = [iwad],
                    Mods = [mod],
                    Profiles = [CreateProfile("p1", "Profile 1", source, iwad, mod)],
                    IsFileLibraryPaneCollapsed = true
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        var wasSelected = viewModel.SelectProfileAndExpandFileLibraryPane("p1");

        Assert.True(wasSelected);
        Assert.Equal("p1", viewModel.SelectedProfileId);
        Assert.Equal(Path.GetFullPath(source), viewModel.SelectedSourcePortPath);
        Assert.Equal(Path.GetFullPath(iwad), viewModel.SelectedIwadPath);
        Assert.Equal([Path.GetFullPath(mod)], viewModel.SelectedModPaths);
        Assert.False(viewModel.IsFileLibraryPaneCollapsed);
        Assert.True(viewModel.IsFileLibraryPaneExpanded);
        Assert.False(persistence.SavedStates.Last().IsFileLibraryPaneCollapsed);
        Assert.Equal("p1", persistence.SavedStates.Last().SelectedProfileId);
    }

    [Fact]
    public void SelectProfileAndExpandFileLibraryPane_WhenCollapsedAndAlreadySelected_KeepsSelectionAndSelectionsIntact()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");
        var mod = temp.CreateFile("mod-a.pk3");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source],
                    Iwads = [iwad],
                    Mods = [mod],
                    Profiles = [CreateProfile("p1", "Profile 1", source, iwad, mod)],
                    SelectedProfileId = "p1",
                    IsFileLibraryPaneCollapsed = true
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        var wasSelected = viewModel.SelectProfileAndExpandFileLibraryPane("p1");

        Assert.True(wasSelected);
        Assert.Equal("p1", viewModel.SelectedProfileId);
        Assert.Equal(Path.GetFullPath(source), viewModel.SelectedSourcePortPath);
        Assert.Equal(Path.GetFullPath(iwad), viewModel.SelectedIwadPath);
        Assert.Equal([Path.GetFullPath(mod)], viewModel.SelectedModPaths);
        Assert.False(viewModel.IsFileLibraryPaneCollapsed);
        Assert.True(viewModel.IsFileLibraryPaneExpanded);
        Assert.False(persistence.SavedStates.Last().IsFileLibraryPaneCollapsed);
        Assert.Equal("p1", persistence.SavedStates.Last().SelectedProfileId);
    }

    [Fact]
    public void ProfileRows_ShowSavedCommandPreviewTextFromProfileData()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");
        var modA = temp.CreateFile("mod-a.pk3");
        var modB = temp.CreateFile("mod-b.pk3");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source],
                    Iwads = [iwad],
                    Mods = [modA, modB],
                    Profiles = [CreateProfile("p1", "Profile 1", source, iwad, modA, modB)]
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        var row = viewModel.ProfileRows.Single();
        Assert.Equal("gzdoom.exe -iwad doom2.wad -file mod-a.pk3 mod-b.pk3", row.CommandPreviewText);
        Assert.False(row.IsCommandPreviewVisible);

        viewModel.ToggleFileLibraryPaneCollapsed();
        Assert.True(row.IsCommandPreviewVisible);
    }

    [Fact]
    public void ProfileRows_ShowPartialCommandPreviewForInvalidProfile()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var mod = temp.CreateFile("mod-a.pk3");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source],
                    Mods = [mod],
                    Profiles = [CreateProfile("p1", "Profile 1", source, null, mod)]
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        var row = viewModel.ProfileRows.Single();
        Assert.True(row.IsInvalid);
        Assert.Equal("gzdoom.exe -file mod-a.pk3", row.CommandPreviewText);
        Assert.False(row.IsCommandPreviewVisible);

        viewModel.ToggleFileLibraryPaneCollapsed();
        Assert.True(row.IsCommandPreviewVisible);
    }

    [Fact]
    public void ProfileRows_OmitCommandPreviewWhenProfileHasNoPreviewableTokens()
    {
        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    Profiles = [CreateProfile("p1", "Profile 1", null, null)]
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        var row = viewModel.ProfileRows.Single();
        Assert.Equal(string.Empty, row.CommandPreviewText);
        Assert.False(row.IsCommandPreviewVisible);
    }

    [Fact]
    public void ProfileRows_ShowAndHideCommandPreviewFromPaneStateAndWidth()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source],
                    Iwads = [iwad],
                    Profiles = [CreateProfile("p1", "Profile 1", source, iwad)]
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);
        var row = viewModel.ProfileRows.Single();

        Assert.False(row.IsCommandPreviewVisible);

        viewModel.ToggleFileLibraryPaneCollapsed();
        Assert.True(row.IsCommandPreviewVisible);

        viewModel.SetWindowWidth(768d);
        Assert.False(row.IsCommandPreviewVisible);

        viewModel.SetWindowWidth(700d);
        Assert.False(row.IsCommandPreviewVisible);

        viewModel.SetWindowWidth(769d);
        Assert.True(row.IsCommandPreviewVisible);

        viewModel.ToggleFileLibraryPaneCollapsed();
        Assert.False(row.IsCommandPreviewVisible);

        viewModel.ToggleFileLibraryPaneCollapsed();
        Assert.True(row.IsCommandPreviewVisible);
    }

    [Fact]
    public void DetachedModRows_DefaultToAlphabeticalFilenameOrder_AndTemporarilyReorderSelectedMods()
    {
        using var temp = new TempDirectory();
        var modBeta = temp.CreateFile("beta.pk3");
        var modGamma = temp.CreateFile("gamma.pk3");
        var modAlpha = temp.CreateFile("alpha.pk3");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    Mods = [modBeta, modGamma, modAlpha]
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        Assert.Equal(
            ["alpha.pk3", "beta.pk3", "gamma.pk3"],
            viewModel.ModRows.Select(row => Path.GetFileName(row.Path)).ToArray());

        viewModel.ToggleModSelection(modGamma);
        viewModel.ToggleModSelection(modAlpha);

        Assert.Equal(
            ["gamma.pk3", "alpha.pk3", "beta.pk3"],
            viewModel.ModRows.Select(row => Path.GetFileName(row.Path)).ToArray());
        Assert.Equal(
            [Path.GetFullPath(modBeta), Path.GetFullPath(modGamma), Path.GetFullPath(modAlpha)],
            persistence.SavedStates.Last().Mods);
    }

    [Fact]
    public void SelectedProfile_ModRowsUseProfileOrderFirst_AndAlphabeticalRemainder()
    {
        using var temp = new TempDirectory();
        var modCharlie = temp.CreateFile("charlie.pk3");
        var modAlpha = temp.CreateFile("alpha.pk3");
        var modDelta = temp.CreateFile("delta.pk3");
        var modBravo = temp.CreateFile("bravo.pk3");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    Mods = [modCharlie, modAlpha, modDelta, modBravo],
                    Profiles = [CreateProfile("p1", "Profile 1", null, null, modDelta, modBravo)],
                    SelectedProfileId = "p1"
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        Assert.Equal(
            ["delta.pk3", "bravo.pk3", "alpha.pk3", "charlie.pk3"],
            viewModel.ModRows.Select(row => Path.GetFileName(row.Path)).ToArray());
    }

    [Fact]
    public void SwitchingProfiles_RecomputesDisplayedModOrderPerProfile()
    {
        using var temp = new TempDirectory();
        var modCharlie = temp.CreateFile("charlie.pk3");
        var modAlpha = temp.CreateFile("alpha.pk3");
        var modDelta = temp.CreateFile("delta.pk3");
        var modBravo = temp.CreateFile("bravo.pk3");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    Mods = [modCharlie, modAlpha, modDelta, modBravo],
                    Profiles =
                    [
                        CreateProfile("p1", "Profile 1", null, null, modDelta, modBravo),
                        CreateProfile("p2", "Profile 2", null, null, modAlpha, modCharlie)
                    ]
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        viewModel.ToggleProfileSelection("p1");
        Assert.Equal(
            ["delta.pk3", "bravo.pk3", "alpha.pk3", "charlie.pk3"],
            viewModel.ModRows.Select(row => Path.GetFileName(row.Path)).ToArray());

        viewModel.ToggleProfileSelection("p2");
        Assert.Equal(
            ["alpha.pk3", "charlie.pk3", "bravo.pk3", "delta.pk3"],
            viewModel.ModRows.Select(row => Path.GetFileName(row.Path)).ToArray());
    }

    [Fact]
    public void ToggleModSelection_WhenProfileSelected_PersistsProfileOrderWithoutRewritingSharedMods()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");
        var modBeta = temp.CreateFile("beta.pk3");
        var modGamma = temp.CreateFile("gamma.pk3");
        var modAlpha = temp.CreateFile("alpha.pk3");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source],
                    Iwads = [iwad],
                    Mods = [modBeta, modGamma, modAlpha],
                    Profiles = [CreateProfile("p1", "Profile 1", source, iwad, modGamma)],
                    SelectedProfileId = "p1"
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        viewModel.ToggleModSelection(modAlpha);

        Assert.Equal(
            [Path.GetFullPath(modGamma), Path.GetFullPath(modAlpha)],
            persistence.SavedStates.Last().Profiles.Single().SelectedModPaths);
        Assert.Equal(
            [Path.GetFullPath(modBeta), Path.GetFullPath(modGamma), Path.GetFullPath(modAlpha)],
            persistence.SavedStates.Last().Mods);
        Assert.Equal(
            ["gamma.pk3", "alpha.pk3", "beta.pk3"],
            viewModel.ModRows.Select(row => Path.GetFileName(row.Path)).ToArray());
    }

    [Fact]
    public void DeleteSelectedProfile_ClearsSelectionAndCurrentSelections()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");
        var mod = temp.CreateFile("mod-a.pk3");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source],
                    Iwads = [iwad],
                    Mods = [mod],
                    Profiles = [CreateProfile("p1", "Profile 1", source, iwad, mod)],
                    SelectedProfileId = "p1"
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        viewModel.RequestDeleteProfile("p1");
        Assert.True(viewModel.HasPendingDeleteConfirmation);

        viewModel.ConfirmDeleteProfile();

        Assert.False(viewModel.HasProfiles);
        Assert.Equal("No Profile Selected", viewModel.SelectedProfileName);
        Assert.Null(viewModel.SelectedSourcePortPath);
        Assert.Null(viewModel.SelectedIwadPath);
        Assert.Empty(viewModel.SelectedModPaths);
        Assert.False(viewModel.CanLaunch);
        Assert.Empty(persistence.SavedStates.Last().Profiles);
        Assert.Null(persistence.SavedStates.Last().SelectedProfileId);
    }

    [Fact]
    public void RemoveSourcePortUsedBySelectedProfile_KeepsProfileSavedButInvalid()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source],
                    Iwads = [iwad],
                    Profiles = [CreateProfile("p1", "Profile 1", source, iwad)],
                    SelectedProfileId = "p1"
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        viewModel.RemoveSourcePort(source);

        var profileRow = viewModel.ProfileRows.Single();
        Assert.True(profileRow.IsInvalid);
        Assert.False(profileRow.HasValidMessage);
        Assert.True(profileRow.HasStatusBadge);
        Assert.Equal("INVALID", profileRow.StatusBadgeText);
        Assert.Contains("Source Port", profileRow.InvalidReason);
        Assert.False(viewModel.CanLaunch);
        Assert.Null(viewModel.SelectedSourcePortPath);
        Assert.Equal(Path.GetFullPath(source), persistence.SavedStates.Last().Profiles.Single().SourcePortPath);
    }

    [Fact]
    public void RemoveIwadUsedBySelectedProfile_KeepsProfileSavedButInvalid()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source],
                    Iwads = [iwad],
                    Profiles = [CreateProfile("p1", "Profile 1", source, iwad)],
                    SelectedProfileId = "p1"
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        viewModel.RemoveIwad(iwad);

        var profileRow = viewModel.ProfileRows.Single();
        Assert.True(profileRow.IsInvalid);
        Assert.False(profileRow.HasValidMessage);
        Assert.True(profileRow.HasStatusBadge);
        Assert.Equal("INVALID", profileRow.StatusBadgeText);
        Assert.Contains("IWAD", profileRow.InvalidReason);
        Assert.False(viewModel.CanLaunch);
        Assert.Null(viewModel.SelectedIwadPath);
        Assert.Equal(Path.GetFullPath(iwad), persistence.SavedStates.Last().Profiles.Single().IwadPath);
    }

    [Fact]
    public void RemoveModUsedBySelectedProfile_KeepsProfileSavedAndLaunchable()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");
        var mod = temp.CreateFile("mod-a.pk3");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source],
                    Iwads = [iwad],
                    Mods = [mod],
                    Profiles = [CreateProfile("p1", "Profile 1", source, iwad, mod)],
                    SelectedProfileId = "p1"
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        viewModel.RemoveMod(mod);

        var profileRow = viewModel.ProfileRows.Single();
        Assert.False(profileRow.IsInvalid);
        Assert.True(profileRow.CanLaunchProfile);
        Assert.True(profileRow.HasStatusBadge);
        Assert.Equal("VALID", profileRow.StatusBadgeText);
        Assert.Equal("Selected profile is ready to launch.", viewModel.SelectedProfileStatusText);
        Assert.True(viewModel.CanLaunch);
        Assert.Empty(viewModel.SelectedModPaths);
        Assert.Equal("gzdoom.exe -iwad doom2.wad -file mod-a.pk3", profileRow.CommandPreviewText);
        Assert.Equal([Path.GetFullPath(mod)], persistence.SavedStates.Last().Profiles.Single().SelectedModPaths);
    }

    [Fact]
    public void InvalidProfileRow_HidesInlineInvalidReasonWhileExpanded_AndShowsItWhenCollapsed()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var missingIwad = Path.Combine(temp.Path, "missing.wad");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source],
                    Profiles = [CreateProfile("p1", "Profile 1", source, missingIwad)],
                    SelectedProfileId = "p1"
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);
        var row = viewModel.ProfileRows.Single();

        Assert.True(row.IsInvalid);
        Assert.Equal("INVALID", row.StatusBadgeText);
        Assert.False(row.IsInvalidReasonVisible);
        Assert.Contains("IWAD file is missing", row.InvalidReason);
        Assert.Contains("IWAD file is missing", viewModel.SelectedProfileStatusText);

        viewModel.ToggleFileLibraryPaneCollapsed();

        Assert.True(row.IsInvalid);
        Assert.Equal("INVALID", row.StatusBadgeText);
        Assert.True(row.IsInvalidReasonVisible);
        Assert.Contains("IWAD file is missing", viewModel.SelectedProfileStatusText);
    }

    [Fact]
    public void SelectedProfileHeader_ShowsAmberInvalidStatusAndSavedCommandPreview()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var missingIwad = Path.Combine(temp.Path, "missing.wad");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source],
                    Profiles = [CreateProfile("p1", "Profile 1", source, missingIwad)],
                    SelectedProfileId = "p1"
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        Assert.Equal("IWAD file is missing: missing.wad", viewModel.SelectedProfileStatusText);
        Assert.Equal("#f59e0b", viewModel.SelectedProfileStatusForeground);
        Assert.Equal("gzdoom.exe -iwad missing.wad", viewModel.SelectedProfileCommandPreviewText);
        Assert.True(viewModel.HasSelectedProfileCommandPreview);
        Assert.Null(viewModel.SelectedIwadPath);
        Assert.Equal("gzdoom.exe", Path.GetFileName(viewModel.SelectedSourcePortPath));
    }

    [Fact]
    public void ValidProfileRow_ShowsExplicitValidBadge()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source],
                    Iwads = [iwad],
                    Profiles = [CreateProfile("p1", "Profile 1", source, iwad)]
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        var row = viewModel.ProfileRows.Single();

        Assert.False(row.IsInvalid);
        Assert.True(row.CanLaunchProfile);
        Assert.True(row.HasStatusBadge);
        Assert.Equal("VALID", row.StatusBadgeText);
        Assert.True(row.HasValidMessage);
        Assert.Equal("VALID", row.ValidMessage);
        Assert.False(row.HasInvalidReason);
    }

    [Fact]
    public void MissingSavedModPath_DoesNotInvalidateProfile()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");
        var missingMod = Path.Combine(temp.Path, "missing-mod.pk3");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source],
                    Iwads = [iwad],
                    Profiles = [CreateProfile("p1", "Profile 1", source, iwad, missingMod)],
                    SelectedProfileId = "p1"
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);
        var row = viewModel.ProfileRows.Single();

        Assert.False(row.IsInvalid);
        Assert.True(row.CanLaunchProfile);
        Assert.Equal("VALID", row.StatusBadgeText);
        Assert.Equal("Selected profile is ready to launch.", viewModel.SelectedProfileStatusText);
        Assert.True(viewModel.CanLaunch);
        Assert.Empty(viewModel.SelectedModPaths);
        Assert.Equal("gzdoom.exe -iwad doom2.wad -file missing-mod.pk3", row.CommandPreviewText);
    }

    [Fact]
    public void ReorderProfile_MovesProfileToFirstPosition_AndPersistsCanonicalOrder()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source],
                    Iwads = [iwad],
                    Profiles =
                    [
                        CreateProfile("p1", "Profile 1", source, iwad),
                        CreateProfile("p2", "Profile 2", source, iwad),
                        CreateProfile("p3", "Profile 3", source, iwad)
                    ]
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        var reordered = viewModel.ReorderProfile("p3", 0);

        Assert.True(reordered);
        Assert.Equal(["p3", "p1", "p2"], viewModel.ProfileRows.Select(row => row.Id).ToArray());
        Assert.Equal(["p3", "p1", "p2"], persistence.SavedStates.Last().Profiles.Select(profile => profile.Id).ToArray());
        Assert.All(persistence.SavedStates.Last().Profiles, profile =>
        {
            Assert.Equal(Path.GetFullPath(source), profile.SourcePortPath);
            Assert.Equal(Path.GetFullPath(iwad), profile.IwadPath);
        });
    }

    [Fact]
    public void ReorderProfile_PreservesSelectedProfileSelectionsAndLaunchState()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");
        var mod = temp.CreateFile("mod-a.pk3");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source],
                    Iwads = [iwad],
                    Mods = [mod],
                    Profiles =
                    [
                        CreateProfile("p1", "Profile 1", source, iwad, mod),
                        CreateProfile("p2", "Profile 2", source, iwad),
                        CreateProfile("p3", "Profile 3", source, iwad)
                    ],
                    SelectedProfileId = "p1"
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        var reordered = viewModel.ReorderProfile("p1", 3);

        Assert.True(reordered);
        Assert.Equal("p1", viewModel.SelectedProfileId);
        Assert.Equal("Profile 1", viewModel.SelectedProfileName);
        Assert.Equal(Path.GetFullPath(source), viewModel.SelectedSourcePortPath);
        Assert.Equal(Path.GetFullPath(iwad), viewModel.SelectedIwadPath);
        Assert.Equal([Path.GetFullPath(mod)], viewModel.SelectedModPaths);
        Assert.True(viewModel.CanLaunch);
        Assert.Equal(["p2", "p3", "p1"], persistence.SavedStates.Last().Profiles.Select(profile => profile.Id).ToArray());
        Assert.Equal("p1", persistence.SavedStates.Last().SelectedProfileId);
    }

    [Fact]
    public void ReorderProfile_SupportsMiddleInsertion()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source],
                    Iwads = [iwad],
                    Profiles =
                    [
                        CreateProfile("p1", "Profile 1", source, iwad),
                        CreateProfile("p2", "Profile 2", source, iwad),
                        CreateProfile("p3", "Profile 3", source, iwad),
                        CreateProfile("p4", "Profile 4", source, iwad)
                    ]
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        var reordered = viewModel.ReorderProfile("p1", 2);

        Assert.True(reordered);
        Assert.Equal(["p2", "p1", "p3", "p4"], viewModel.ProfileRows.Select(row => row.Id).ToArray());
        Assert.Equal(["p2", "p1", "p3", "p4"], persistence.SavedStates.Last().Profiles.Select(profile => profile.Id).ToArray());
    }

    [Fact]
    public void ReorderProfile_NoOpTargets_DoNotPersistNewOrder()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source],
                    Iwads = [iwad],
                    Profiles =
                    [
                        CreateProfile("p1", "Profile 1", source, iwad),
                        CreateProfile("p2", "Profile 2", source, iwad),
                        CreateProfile("p3", "Profile 3", source, iwad)
                    ]
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);
        var saveCountBeforeNoOps = persistence.SaveCallCount;

        Assert.False(viewModel.ReorderProfile("missing", 0));
        Assert.False(viewModel.ReorderProfile("p2", 1));
        Assert.False(viewModel.ReorderProfile("p2", 2));
        Assert.False(viewModel.ReorderProfile("p2", -1));
        Assert.False(viewModel.ReorderProfile("p2", 4));

        Assert.Equal(saveCountBeforeNoOps, persistence.SaveCallCount);
        Assert.Equal(["p1", "p2", "p3"], viewModel.ProfileRows.Select(row => row.Id).ToArray());
    }

    [Fact]
    public void BeginProfileDrag_UsesProfileNameOnlyGhost_AndHideClearsFeedback()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source],
                    Iwads = [iwad],
                    Profiles = [CreateProfile("p1", "Ultra-Violence", source, iwad)]
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        var began = viewModel.BeginProfileDrag("p1", 24d, 48d);
        viewModel.ShowProfileDropIndicator(8d, 16d, 120d);

        Assert.True(began);
        Assert.True(viewModel.IsProfileDragGhostVisible);
        Assert.Equal("Ultra-Violence", viewModel.ProfileDragGhostText);
        Assert.Equal(24d, viewModel.ProfileDragGhostLeft);
        Assert.Equal(48d, viewModel.ProfileDragGhostTop);
        Assert.True(viewModel.IsProfileDropIndicatorVisible);

        viewModel.HideProfileDragFeedback();

        Assert.False(viewModel.IsProfileDragGhostVisible);
        Assert.Equal(string.Empty, viewModel.ProfileDragGhostText);
        Assert.False(viewModel.IsProfileDropIndicatorVisible);
    }

    [Fact]
    public void BeginRenameProfile_CommitRename_PersistsNewUniqueName()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source],
                    Iwads = [iwad],
                    Profiles = [CreateProfile("p1", "Profile 1", source, iwad)]
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        viewModel.BeginRenameProfile("p1");
        Assert.True(viewModel.ProfileRows.Single().IsRenameVisible);
        viewModel.SelectedProfileRenameText = "Ultra-Violence";

        viewModel.CommitRename("p1");

        Assert.False(viewModel.IsSelectedProfileRenameVisible);
        Assert.False(viewModel.ProfileRows.Single().IsRenameVisible);
        Assert.Equal("Ultra-Violence", viewModel.SelectedProfileName);
        Assert.Equal("Ultra-Violence", persistence.SavedStates.Last().Profiles.Single().Name);
        Assert.False(viewModel.HasMessage);
    }

    [Fact]
    public void BeginRenameProfile_SelectsProfileAndHydratesSelections()
    {
        using var temp = new TempDirectory();
        var source1 = temp.CreateFile("gzdoom.exe");
        var source2 = temp.CreateFile("vkdoom.exe");
        var iwad1 = temp.CreateFile("doom.wad");
        var iwad2 = temp.CreateFile("doom2.wad");
        var mod1 = temp.CreateFile("mod-a.pk3");
        var mod2 = temp.CreateFile("mod-b.pk3");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source1, source2],
                    Iwads = [iwad1, iwad2],
                    Mods = [mod1, mod2],
                    Profiles =
                    [
                        CreateProfile("p1", "Profile 1", source1, iwad1, mod1),
                        CreateProfile("p2", "Profile 2", source2, iwad2, mod2)
                    ]
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        viewModel.BeginRenameProfile("p2");

        Assert.True(viewModel.IsSelectedProfileRenameVisible);
        Assert.Equal("Profile 2", viewModel.SelectedProfileRenameText);
        Assert.Equal("Profile 2", viewModel.SelectedProfileName);
        Assert.Equal(Path.GetFullPath(source2), viewModel.SelectedSourcePortPath);
        Assert.Equal(Path.GetFullPath(iwad2), viewModel.SelectedIwadPath);
        Assert.Equal([Path.GetFullPath(mod2)], viewModel.SelectedModPaths);
        Assert.False(viewModel.ProfileRows.Single(row => row.Id == "p1").IsRenameVisible);
        Assert.True(viewModel.ProfileRows.Single(row => row.Id == "p2").IsRenameVisible);
        Assert.Equal("p2", persistence.SavedStates.Last().SelectedProfileId);
    }

    [Fact]
    public void CommitRename_DuplicateName_ShowsValidationAndKeepsRenameOpen()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source],
                    Iwads = [iwad],
                    Profiles =
                    [
                        CreateProfile("p1", "Profile 1", source, iwad),
                        CreateProfile("p2", "Profile 2", source, iwad)
                    ]
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        viewModel.BeginRenameProfile("p1");
        viewModel.SelectedProfileRenameText = "profile 2";

        viewModel.CommitRename("p1");

        Assert.True(viewModel.IsSelectedProfileRenameVisible);
        Assert.True(viewModel.ProfileRows.Single(row => row.Id == "p1").IsRenameVisible);
        Assert.Equal("Profile 1", viewModel.SelectedProfileName);
        Assert.Equal("Profile name must be unique.", viewModel.MessageText);
    }

    [Fact]
    public void CancelRename_RestoresSavedName_AndDoesNotPersistRename()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source],
                    Iwads = [iwad],
                    Profiles = [CreateProfile("p1", "Profile 1", source, iwad)]
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        viewModel.BeginRenameProfile("p1");
        var saveCallCountBeforeCancel = persistence.SaveCallCount;
        viewModel.SelectedProfileRenameText = "Ultra-Violence";

        var canceled = viewModel.CancelRename();

        Assert.True(canceled);
        Assert.False(viewModel.IsSelectedProfileRenameVisible);
        Assert.False(viewModel.ProfileRows.Single().IsRenameVisible);
        Assert.Equal("Profile 1", viewModel.SelectedProfileName);
        Assert.Equal("Profile 1", viewModel.SelectedProfileRenameText);
        Assert.Equal(saveCallCountBeforeCancel, persistence.SaveCallCount);
        Assert.Equal("Profile 1", persistence.SavedStates.Last().Profiles.Single().Name);
    }

    [Fact]
    public void CanRenameSelectedProfile_IsFalse_WhenNoProfileIsSelected()
    {
        var persistence = new RecordingPersistence();
        var viewModel = new MainWindowViewModel(persistence);

        Assert.False(viewModel.CanRenameSelectedProfile);

        viewModel.BeginRenameSelectedProfile();

        Assert.False(viewModel.IsSelectedProfileRenameVisible);
    }

    [Fact]
    public void SelectProfileAndSetFileLibraryPaneCollapsed_WhenExpanded_SelectsHydratesAndPersistsCollapsedState()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");
        var mod = temp.CreateFile("mod-a.pk3");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source],
                    Iwads = [iwad],
                    Mods = [mod],
                    Profiles = [CreateProfile("p1", "Profile 1", source, iwad, mod)],
                    SelectedProfileId = "p1",
                    IsFileLibraryPaneCollapsed = false
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        var wasSelected = viewModel.SelectProfileAndSetFileLibraryPaneCollapsed("p1", true);

        Assert.True(wasSelected);
        Assert.Equal("p1", viewModel.SelectedProfileId);
        Assert.Equal(Path.GetFullPath(source), viewModel.SelectedSourcePortPath);
        Assert.Equal(Path.GetFullPath(iwad), viewModel.SelectedIwadPath);
        Assert.Equal([Path.GetFullPath(mod)], viewModel.SelectedModPaths);
        Assert.True(viewModel.IsFileLibraryPaneCollapsed);
        Assert.False(viewModel.IsFileLibraryPaneExpanded);
        Assert.True(persistence.SavedStates.Last().IsFileLibraryPaneCollapsed);
        Assert.Equal("p1", persistence.SavedStates.Last().SelectedProfileId);
    }

    [Fact]
    public void LaunchProfile_WhenValidAndUnselected_SelectsProfileAndUsesThatProfilesArguments()
    {
        using var temp = new TempDirectory();
        var source1 = temp.CreateFile("gzdoom.exe");
        var source2 = temp.CreateFile("vkdoom.exe");
        var iwad1 = temp.CreateFile("doom.wad");
        var iwad2 = temp.CreateFile("doom2.wad");
        var mod1 = temp.CreateFile("mod-a.pk3");
        var mod2 = temp.CreateFile("mod-b.pk3");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source1, source2],
                    Iwads = [iwad1, iwad2],
                    Mods = [mod1, mod2],
                    Profiles =
                    [
                        CreateProfile("p1", "Profile 1", source1, iwad1, mod1),
                        CreateProfile("p2", "Profile 2", source2, iwad2, mod2)
                    ],
                    SelectedProfileId = "p1"
                }
            }
        };

        var launcher = new RecordingLauncher();
        var viewModel = new MainWindowViewModel(persistence, launcher);

        viewModel.LaunchProfile("p2");

        Assert.Equal("p2", viewModel.SelectedProfileId);
        Assert.Equal("Profile 2", viewModel.SelectedProfileName);
        Assert.Equal(Path.GetFullPath(source2), viewModel.SelectedSourcePortPath);
        Assert.Equal(Path.GetFullPath(iwad2), viewModel.SelectedIwadPath);
        Assert.Equal([Path.GetFullPath(mod2)], viewModel.SelectedModPaths);
        Assert.Equal("p2", persistence.SavedStates.Last().SelectedProfileId);
        Assert.Equal(1, launcher.LaunchCallCount);
        Assert.Equal(Path.GetFullPath(source2), launcher.LastExecutablePath);
        Assert.Equal(
            [
                "-iwad",
                Path.GetFullPath(iwad2),
                "-file",
                Path.GetFullPath(mod2)
            ],
            launcher.LastArguments);
    }

    [Fact]
    public void LaunchSourcePort_WhenSelectedProfileValid_UsesProfileArguments()
    {
        using var temp = new TempDirectory();
        var source1 = temp.CreateFile("gzdoom.exe");
        var source2 = temp.CreateFile("vkdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");
        var mod1 = temp.CreateFile("mod-a.pk3");
        var mod2 = temp.CreateFile("mod-b.pk3");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source1, source2],
                    Iwads = [iwad],
                    Mods = [mod1, mod2],
                    Profiles = [CreateProfile("p1", "Profile 1", source2, iwad, mod2, mod1)],
                    SelectedProfileId = "p1"
                }
            }
        };

        var launcher = new RecordingLauncher();
        var viewModel = new MainWindowViewModel(persistence, launcher);

        viewModel.LaunchSourcePort();

        Assert.Equal(1, launcher.LaunchCallCount);
        Assert.Equal(Path.GetFullPath(source2), launcher.LastExecutablePath);
        Assert.Equal(
            [
                "-iwad",
                Path.GetFullPath(iwad),
                "-file",
                Path.GetFullPath(mod2),
                Path.GetFullPath(mod1)
            ],
            launcher.LastArguments);
    }

    [Fact]
    public void LaunchProfile_WhenTargetProfileInvalid_DoesNotInvokeLauncher()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var missingIwad = Path.Combine(temp.Path, "missing.wad");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source],
                    Profiles = [CreateProfile("p1", "Profile 1", source, missingIwad)]
                }
            }
        };

        var launcher = new RecordingLauncher();
        var viewModel = new MainWindowViewModel(persistence, launcher);

        var row = viewModel.ProfileRows.Single();
        Assert.True(row.IsInvalid);
        Assert.False(row.CanLaunchProfile);

        viewModel.LaunchProfile("p1");

        Assert.Equal("p1", viewModel.SelectedProfileId);
        Assert.Equal(0, launcher.LaunchCallCount);
    }

    [Fact]
    public void DropZonePickerOptions_UseZoneSpecificAllowlists()
    {
        var sourcePortOptions = DropZonePickerOptionsFactory.Create(DropZoneKind.SourcePort);
        var iwadOptions = DropZonePickerOptionsFactory.Create(DropZoneKind.Iwad);
        var modOptions = DropZonePickerOptionsFactory.Create(DropZoneKind.Mod);

        Assert.True(sourcePortOptions.AllowMultiple);
        Assert.Equal("Select Source Port Files", sourcePortOptions.Title);
        Assert.Equal(["*.exe"], GetPatterns(sourcePortOptions));

        Assert.True(iwadOptions.AllowMultiple);
        Assert.Equal("Select IWAD Files", iwadOptions.Title);
        Assert.Equal(["*.wad", "*.pk3", "*.iwad", "*.ipk3", "*.ipk7", "*.pk7"], GetPatterns(iwadOptions));

        Assert.True(modOptions.AllowMultiple);
        Assert.Equal("Select Mod Files", modOptions.Title);
        Assert.Equal(["*.wad", "*.pwad", "*.pk3", "*.pk7", "*.ipk3", "*.ipk7", "*.pkz", "*.zip"], GetPatterns(modOptions));
    }

    [Fact]
    public void MainWindowXaml_PlacesProfileActionsAndSharedStatusBadgeInCorrectSections()
    {
        var xamlPath = GetRepoFilePath("src", "ModLoader.App", "MainWindow.axaml");
        var xaml = File.ReadAllText(xamlPath);

        Assert.DoesNotContain("Click=\"OnLaunchClicked\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"OnLaunchProfileClicked\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding DataContext.SelectedProfileRenameText, RelativeSource={RelativeSource AncestorType=Window}, Mode=TwoWay}\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"{Binding RenameText, Mode=TwoWay}\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"{Binding SelectedProfileRenameText, Mode=TwoWay}\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("IsEnabled=\"{Binding CanCreateProfile}\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("IsEnabled=\"{Binding CanRenameSelectedProfile}\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("IsVisible=\"{Binding IsSelectedProfileRenameVisible}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding HasStatusBadge}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding StatusBadgeText}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Background=\"{Binding StatusBadgeBackground}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Foreground=\"{Binding StatusBadgeForeground}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding IsCommandPreviewVisible}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding CommandPreviewText}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("FontFamily=\"Consolas\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding IsInvalidReasonVisible}\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("IsVisible=\"{Binding HasInvalidReason}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Foreground=\"{Binding SelectedProfileStatusForeground}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding HasSelectedProfileCommandPreview}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding SelectedProfileCommandPreviewText}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("MaxLines=\"2\"", xaml, StringComparison.Ordinal);
        Assert.Contains("TextTrimming=\"CharacterEllipsis\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("IsVisible=\"{Binding HasValidMessage}\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("IsVisible=\"{Binding IsFileLibraryPaneCollapsed}\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Command Preview\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"{Binding CommandPreviewArguments}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("PointerMoved=\"OnProfileRowPointerMoved\"", xaml, StringComparison.Ordinal);
        Assert.Contains("PointerReleased=\"OnProfileRowPointerReleased\"", xaml, StringComparison.Ordinal);
        Assert.Contains("PointerCaptureLost=\"OnProfileRowPointerCaptureLost\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding IsProfileDragGhostVisible}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding ProfileDragGhostText}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding IsProfileDropIndicatorVisible}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Width=\"{Binding ProfileDropIndicatorWidth}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"Drag and drop here\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Drag files here or click to upload\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"+\"", xaml, StringComparison.Ordinal);
        Assert.Contains("PointerPressed=\"OnDropZonePointerPressed\"", xaml, StringComparison.Ordinal);
        Assert.Contains("KeyDown=\"OnDropZoneKeyDown\"", xaml, StringComparison.Ordinal);
        Assert.Contains("DragDrop.DragEnter=\"OnDropZoneDragEnter\"", xaml, StringComparison.Ordinal);
        Assert.Contains("DragDrop.DragLeave=\"OnDropZoneDragLeave\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Classes.dragover=\"{Binding IsSourcePortDropZoneDragActive}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Classes.dragover=\"{Binding IsIwadDropZoneDragActive}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Classes.dragover=\"{Binding IsModDropZoneDragActive}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("automation:AutomationProperties.Name=\"Source Port drop zone. Drag files here or click to upload.\"", xaml, StringComparison.Ordinal);
        Assert.Contains("automation:AutomationProperties.Name=\"IWAD drop zone. Drag files here or click to upload.\"", xaml, StringComparison.Ordinal);
        Assert.Contains("automation:AutomationProperties.Name=\"Mod drop zone. Drag files here or click to upload.\"", xaml, StringComparison.Ordinal);

        var profilesHeaderIndex = xaml.IndexOf("Text=\"Profiles\"", StringComparison.Ordinal);
        var newProfileIndex = xaml.IndexOf("Content=\"New Profile\"", StringComparison.Ordinal);
        var fileLibraryToggleIndex = xaml.IndexOf("Content=\"{Binding FileLibraryPaneToggleText}\"", StringComparison.Ordinal);
        var selectedProfileNameIndex = xaml.IndexOf("Text=\"{Binding SelectedProfileName}\"", StringComparison.Ordinal);
        var selectedProfilePreviewIndex = xaml.IndexOf("Text=\"{Binding SelectedProfileCommandPreviewText}\"", StringComparison.Ordinal);
        var launchIndex = xaml.LastIndexOf("Content=\"Launch\"", StringComparison.Ordinal);
        var renameIndex = xaml.LastIndexOf("Content=\"Rename\"", StringComparison.Ordinal);
        var deleteIndex = xaml.LastIndexOf("Content=\"Delete\"", StringComparison.Ordinal);

        Assert.True(profilesHeaderIndex >= 0);
        Assert.True(newProfileIndex > profilesHeaderIndex);
        Assert.True(fileLibraryToggleIndex > newProfileIndex);
        Assert.True(selectedProfileNameIndex > newProfileIndex);
        Assert.True(selectedProfileNameIndex > fileLibraryToggleIndex);
        Assert.True(selectedProfilePreviewIndex > selectedProfileNameIndex);
        Assert.True(launchIndex >= 0);
        Assert.True(renameIndex > launchIndex);
        Assert.True(deleteIndex > launchIndex);
        Assert.True(deleteIndex > renameIndex);
        Assert.Equal(fileLibraryToggleIndex, xaml.LastIndexOf("Content=\"{Binding FileLibraryPaneToggleText}\"", StringComparison.Ordinal));
    }

    [Fact]
    public void FeatureSpecs_ReflectProfileOrderingAndDragReorderSpecs()
    {
        var spec = File.ReadAllText(GetRepoFilePath("SPEC.md"));
        var feature002 = File.ReadAllText(GetRepoFilePath("Features", "002-border-drop-and-row-selection.md"));
        var feature008 = File.ReadAllText(GetRepoFilePath("Features", "008-profile-management.md"));
        var feature009 = File.ReadAllText(GetRepoFilePath("Features", "009-file-library-pane-collapse.md"));
        var feature010 = File.ReadAllText(GetRepoFilePath("Features", "010-profile-drag-reorder.md"));

        Assert.Contains("always-visible instructional card styling", spec, StringComparison.Ordinal);
        Assert.Contains("clickable keyboard-accessible file-picker fallback", spec, StringComparison.Ordinal);
        Assert.Contains("click / keyboard fallback to multi-select file pickers", spec, StringComparison.Ordinal);
        Assert.Contains("`Launch`, `Rename`, and `Delete` actions", spec, StringComparison.Ordinal);
        Assert.Contains("Double-clicking a profile row acts as a pane shortcut", spec, StringComparison.Ordinal);
        Assert.Contains("fixed `380 px` width", spec, StringComparison.Ordinal);
        Assert.Contains("profile names wrap up to two lines", spec, StringComparison.Ordinal);
        Assert.Contains("inline invalid-reason text stays hidden while shared status badges remain visible", spec, StringComparison.Ordinal);
        Assert.Contains("selected-profile header keeps the profile name, shows status text with the shared amber invalid color when invalid, and renders its own wrapped filename-only command preview", spec, StringComparison.Ordinal);
        Assert.Contains("require one source port plus one IWAD only for profile validity", spec, StringComparison.Ordinal);
        Assert.Contains("Each drop zone renders a visible default target treatment before any drag begins", feature002, StringComparison.Ordinal);
        Assert.DoesNotContain("an always-visible empty-state icon or badge treatment", feature002, StringComparison.Ordinal);
        Assert.DoesNotContain("`Drag files here or click to upload`", feature002, StringComparison.Ordinal);
        Assert.Contains("clicking the zone opens a multi-select file picker for that zone", feature002, StringComparison.Ordinal);
        Assert.Contains("`Enter` and `Space` trigger the same picker flow as click", feature002, StringComparison.Ordinal);
        Assert.Contains("file-library `Expand` / `Collapse` action to the right of `New Profile`", feature008, StringComparison.Ordinal);
        Assert.Contains("selected-profile command preview text below the status text when the selected profile has one or more previewable saved launch tokens", feature008, StringComparison.Ordinal);
        Assert.Contains("the Source Port, IWAD, and Mod drop zones inside that pane use the shared visible drop-zone affordance defined by Feature 002", feature008, StringComparison.Ordinal);
        Assert.Contains("File-picker fallback does not add folder selection support", feature008, StringComparison.Ordinal);
        Assert.Contains("valid rows show a `VALID` badge in that same slot", feature008, StringComparison.Ordinal);
        Assert.Contains("left profile pane uses a fixed width of `380 px`", feature008, StringComparison.Ordinal);
        Assert.Contains("Feature 010 becomes authoritative for how profile row ordering is changed by drag reordering", feature008, StringComparison.Ordinal);
        Assert.Contains("does not render a separate inline valid text line", feature008, StringComparison.Ordinal);
        Assert.Contains("Profile names in left-pane rows wrap within the row body and are capped at two rendered lines.", feature008, StringComparison.Ordinal);
        Assert.Contains("selected-profile status text in the right-pane header uses the same amber invalid text color", feature008, StringComparison.Ordinal);
        Assert.Contains("Each profile row renders its command preview in the non-interactive text area under the profile name", feature008, StringComparison.Ordinal);
        Assert.Contains("when the file library pane is expanded, row preview text is hidden for all profile rows regardless of width", feature008, StringComparison.Ordinal);
        Assert.Contains("when the file library pane is expanded, inline invalid-reason text is hidden for all profile rows regardless of width", feature008, StringComparison.Ordinal);
        Assert.Contains("selected profile's saved launch inputs rather than current hydrated live selections", feature008, StringComparison.Ordinal);
        Assert.Contains("when the overall window width is less than or equal to `768 px`, row preview text is hidden for all profile rows even while the file library pane is collapsed", feature008, StringComparison.Ordinal);
        Assert.Contains("While the file library pane is collapsed, double-clicking a profile row is a profile-open shortcut", feature008, StringComparison.Ordinal);
        Assert.Contains("While the file library pane is expanded, double-clicking a profile row is the inverse pane shortcut", feature008, StringComparison.Ordinal);
        Assert.Contains("the second click does not toggle the row back off", feature008, StringComparison.Ordinal);
        Assert.Contains("Each profile row exposes `Launch`, `Rename`, and `Delete` actions in that order.", feature008, StringComparison.Ordinal);
        Assert.Contains("The right-pane selected-profile header remains display-only", feature008, StringComparison.Ordinal);
        Assert.Contains("Removing a referenced Mod from the shared library does not invalidate the profile.", feature008, StringComparison.Ordinal);
        Assert.Contains("Missing referenced Mod files on disk do not invalidate the profile.", feature008, StringComparison.Ordinal);
        Assert.Contains("its saved Mod references remain preserved for preview text and launch argument construction", feature008, StringComparison.Ordinal);
        Assert.Contains("the right file-library pane is not rendered", feature009, StringComparison.Ordinal);
        Assert.Contains("the spacer gap between the profile pane and file-library pane is not rendered", feature009, StringComparison.Ordinal);
        Assert.Contains("when the file library pane is expanded, inline profile-row command preview is hidden for all profile rows", feature009, StringComparison.Ordinal);
        Assert.Contains("Double-clicking a profile row while the file library pane is collapsed is the other exception", feature009, StringComparison.Ordinal);
        Assert.Contains("Double-clicking a profile row while the file library pane is expanded is the inverse exception", feature009, StringComparison.Ordinal);
        Assert.Contains("The drag ghost renders only the dragged profile name.", feature010, StringComparison.Ordinal);
        Assert.Contains("`Launch`, `Rename`, and `Delete` buttons do not start drag.", feature010, StringComparison.Ordinal);
        Assert.Contains("A real drag does not also toggle profile row selection.", feature010, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_WithLegacySelectionStateAndNoProfiles_StartsDetachedAndDisabled()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");
        var mod = temp.CreateFile("mod-a.pk3");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source],
                    SelectedSourcePortPath = source,
                    Iwads = [iwad],
                    Mods = [mod],
                    SelectedIwadPath = iwad,
                    SelectedModPaths = [mod]
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        Assert.False(viewModel.HasProfiles);
        Assert.False(viewModel.HasSelectedProfile);
        Assert.Null(viewModel.SelectedSourcePortPath);
        Assert.Null(viewModel.SelectedIwadPath);
        Assert.Empty(viewModel.SelectedModPaths);
        Assert.False(viewModel.CanLaunch);
        Assert.Equal(string.Empty, viewModel.CommandPreviewArguments);
        Assert.Equal(string.Empty, viewModel.SelectedProfileCommandPreviewText);
        Assert.False(viewModel.HasSelectedProfileCommandPreview);
        Assert.Equal("#94a3b8", viewModel.SelectedProfileStatusForeground);
    }

    private static string GetRepoFilePath(params string[] relativeParts)
    {
        var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        while (currentDirectory is not null)
        {
            var specPath = Path.Combine(currentDirectory.FullName, "SPEC.md");
            var featuresPath = Path.Combine(currentDirectory.FullName, "Features");
            var srcPath = Path.Combine(currentDirectory.FullName, "src");
            if (File.Exists(specPath) && Directory.Exists(featuresPath) && Directory.Exists(srcPath))
            {
                return Path.GetFullPath(Path.Combine([currentDirectory.FullName, .. relativeParts]));
            }

            currentDirectory = currentDirectory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root from test output directory.");
    }

    private static ProfileConfig CreateProfile(string id, string name, string? sourcePortPath, string? iwadPath, params string[] selectedModPaths)
    {
        return new ProfileConfig
        {
            Id = id,
            Name = name,
            SourcePortPath = sourcePortPath,
            IwadPath = iwadPath,
            SelectedModPaths = [.. selectedModPaths]
        };
    }

    private static IReadOnlyList<string> GetPatterns(FilePickerOpenOptions options)
    {
        return options.FileTypeFilter?.Single().Patterns?.ToArray() ?? [];
    }
}

internal sealed class RecordingPersistence : ILaunchInputsPersistence
{
    public LaunchInputsLoadResult LoadResult { get; set; } = new()
    {
        State = LaunchInputsConfig.Empty
    };

    public int SaveCallCount { get; private set; }

    public List<LaunchInputsConfig> SavedStates { get; } = [];

    public LaunchInputsLoadResult Load()
    {
        return LoadResult;
    }

    public void Save(LaunchInputsConfig config)
    {
        SaveCallCount++;
        SavedStates.Add(new LaunchInputsConfig
        {
            SourcePorts = [.. config.SourcePorts],
            Profiles = [.. config.Profiles.Select(CloneProfile)],
            SelectedProfileId = config.SelectedProfileId,
            IsFileLibraryPaneCollapsed = config.IsFileLibraryPaneCollapsed,
            IsSourcePortSectionCollapsed = config.IsSourcePortSectionCollapsed,
            SelectedSourcePortPath = config.SelectedSourcePortPath,
            Iwads = [.. config.Iwads],
            IsIwadSectionCollapsed = config.IsIwadSectionCollapsed,
            Mods = [.. config.Mods],
            IsModSectionCollapsed = config.IsModSectionCollapsed,
            SelectedIwadPath = config.SelectedIwadPath,
            SelectedModPaths = [.. config.SelectedModPaths]
        });
    }

    private static ProfileConfig CloneProfile(ProfileConfig profile)
    {
        return new ProfileConfig
        {
            Id = profile.Id,
            Name = profile.Name,
            SourcePortPath = profile.SourcePortPath,
            IwadPath = profile.IwadPath,
            SelectedModPaths = [.. profile.SelectedModPaths]
        };
    }
}

internal sealed class RecordingLauncher : ISourcePortLauncher
{
    public Exception? ExceptionToThrow { get; set; }

    public int LaunchCallCount { get; private set; }

    public string? LastExecutablePath { get; private set; }

    public IReadOnlyList<string> LastArguments { get; private set; } = [];

    public void Launch(string executablePath, IReadOnlyList<string> arguments)
    {
        LaunchCallCount++;

        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        LastExecutablePath = executablePath;
        LastArguments = [.. arguments];
    }
}
