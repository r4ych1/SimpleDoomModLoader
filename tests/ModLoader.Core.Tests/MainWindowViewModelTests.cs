using System.Linq;
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
    public void Constructor_WithLoadWarning_ShowsWarningToast()
    {
        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig(),
                WarningMessage = "Config file was invalid and was reset to an empty state."
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        Assert.True(viewModel.HasToast);
        Assert.True(viewModel.IsPassiveToast);
        Assert.Equal(ToastKind.Warning, viewModel.CurrentToastKind);
        Assert.Equal("Config file was invalid and was reset to an empty state.", viewModel.ToastMessageText);
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
    public void CreateNewProfile_AfterIgnoredNoProfileSelectionAttempts_CreatesEmptyProfileAndKeepsSelectionsCleared()
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
        Assert.Null(viewModel.SelectedSourcePortPath);
        Assert.Null(viewModel.SelectedIwadPath);
        Assert.Empty(viewModel.SelectedModPaths);
        Assert.Null(savedProfile.SourcePortPath);
        Assert.Null(savedProfile.IwadPath);
        Assert.Empty(savedProfile.SelectedModPaths);
        Assert.Equal(savedProfile.Id, persistence.SavedStates.Last().SelectedProfileId);
        Assert.Null(persistence.SavedStates.Last().SelectedSourcePortPath);
        Assert.Null(persistence.SavedStates.Last().SelectedIwadPath);
        Assert.Empty(persistence.SavedStates.Last().SelectedModPaths);
    }

    [Fact]
    public void CreateNewProfile_FromProfilesView_KeepsProfilesViewActive()
    {
        var persistence = new RecordingPersistence();
        var viewModel = new MainWindowViewModel(persistence);

        viewModel.CreateNewProfile();

        Assert.True(viewModel.IsProfilesViewActive);
        Assert.False(viewModel.IsFileLibraryViewActive);
        Assert.False(persistence.SavedStates.Last().IsFileLibraryViewActive);
    }

    [Fact]
    public void ViewSwapAndSectionCollapse_PersistStateAndUpdateVisibility()
    {
        var persistence = new RecordingPersistence();
        var viewModel = new MainWindowViewModel(persistence);

        Assert.True(viewModel.IsProfilesViewActive);
        Assert.False(viewModel.IsFileLibraryViewActive);
        Assert.Equal("File Library", viewModel.OpenFileLibraryViewText);
        Assert.Equal("Profiles", viewModel.ReturnToProfilesViewText);
        Assert.True(viewModel.AreSourcePortRowsVisible);
        Assert.True(viewModel.AreIwadRowsVisible);
        Assert.True(viewModel.AreModRowsVisible);

        viewModel.ToggleSourcePortSectionCollapsed();
        viewModel.ToggleIwadSectionCollapsed();
        viewModel.ToggleModSectionCollapsed();
        viewModel.ShowFileLibraryView();

        Assert.True(viewModel.IsFileLibraryViewActive);
        Assert.False(viewModel.IsProfilesViewActive);
        Assert.True(viewModel.IsSourcePortSectionCollapsed);
        Assert.False(viewModel.AreSourcePortRowsVisible);
        Assert.True(viewModel.IsIwadSectionCollapsed);
        Assert.False(viewModel.AreIwadRowsVisible);
        Assert.True(viewModel.IsModSectionCollapsed);
        Assert.False(viewModel.AreModRowsVisible);
        Assert.True(persistence.SavedStates.Last().IsFileLibraryViewActive);
        Assert.True(persistence.SavedStates.Last().IsSourcePortSectionCollapsed);
        Assert.True(persistence.SavedStates.Last().IsIwadSectionCollapsed);
        Assert.True(persistence.SavedStates.Last().IsModSectionCollapsed);

        viewModel.ShowProfilesView();

        Assert.True(viewModel.IsProfilesViewActive);
        Assert.False(viewModel.IsFileLibraryViewActive);
        Assert.False(persistence.SavedStates.Last().IsFileLibraryViewActive);
    }

    [Fact]
    public void Constructor_WithPersistedViewState_RestoresViewAndSectionVisibility()
    {
        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    IsFileLibraryViewActive = true,
                    IsSourcePortSectionCollapsed = true,
                    IsIwadSectionCollapsed = false,
                    IsModSectionCollapsed = true
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        Assert.True(viewModel.IsFileLibraryViewActive);
        Assert.False(viewModel.IsProfilesViewActive);
        Assert.True(viewModel.IsSourcePortSectionCollapsed);
        Assert.False(viewModel.AreSourcePortRowsVisible);
        Assert.False(viewModel.IsIwadSectionCollapsed);
        Assert.True(viewModel.AreIwadRowsVisible);
        Assert.True(viewModel.IsModSectionCollapsed);
        Assert.False(viewModel.AreModRowsVisible);
    }

    [Fact]
    public void SwitchingViews_DoesNotChangeProfileSelectionsOrLaunchState()
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

        viewModel.ShowFileLibraryView();

        Assert.Equal("Profile 1", viewModel.SelectedProfileName);
        Assert.Equal(Path.GetFullPath(source), viewModel.SelectedSourcePortPath);
        Assert.Equal(Path.GetFullPath(iwad), viewModel.SelectedIwadPath);
        Assert.Equal([Path.GetFullPath(mod)], viewModel.SelectedModPaths);
        Assert.True(viewModel.CanLaunch);
        Assert.True(viewModel.IsFileLibraryViewActive);

        viewModel.ShowProfilesView();

        Assert.Equal("Profile 1", viewModel.SelectedProfileName);
        Assert.Equal(Path.GetFullPath(source), viewModel.SelectedSourcePortPath);
        Assert.Equal(Path.GetFullPath(iwad), viewModel.SelectedIwadPath);
        Assert.Equal([Path.GetFullPath(mod)], viewModel.SelectedModPaths);
        Assert.True(viewModel.CanLaunch);
        Assert.True(viewModel.IsProfilesViewActive);
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
        Assert.True(viewModel.ProfileRows.Single().CanLaunchProfile);
    }

    [Fact]
    public void ToggleSelections_WhenNoProfileSelected_DoNothingAndDoNotPersistSelectionState()
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

        Assert.Null(viewModel.SelectedSourcePortPath);
        Assert.Null(viewModel.SelectedIwadPath);
        Assert.Empty(viewModel.SelectedModPaths);
        Assert.Equal(string.Empty, viewModel.CommandPreviewArguments);
        Assert.Null(persistence.SavedStates.Last().SelectedProfileId);
        Assert.Null(persistence.SavedStates.Last().SelectedSourcePortPath);
        Assert.Null(persistence.SavedStates.Last().SelectedIwadPath);
        Assert.Empty(persistence.SavedStates.Last().SelectedModPaths);
    }

    [Fact]
    public void SelectProfileAndOpenFileLibraryView_WhenProfilesViewAndUnselected_SelectsHydratesAndPersistsViewState()
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

        var wasSelected = viewModel.SelectProfileAndOpenFileLibraryView("p1");

        Assert.True(wasSelected);
        Assert.Equal("p1", viewModel.SelectedProfileId);
        Assert.Equal(Path.GetFullPath(source), viewModel.SelectedSourcePortPath);
        Assert.Equal(Path.GetFullPath(iwad), viewModel.SelectedIwadPath);
        Assert.Equal([Path.GetFullPath(mod)], viewModel.SelectedModPaths);
        Assert.True(viewModel.IsFileLibraryViewActive);
        Assert.True(persistence.SavedStates.Last().IsFileLibraryViewActive);
        Assert.Equal("p1", persistence.SavedStates.Last().SelectedProfileId);
    }

    [Fact]
    public void SelectProfileAndOpenFileLibraryView_WhenAlreadySelected_KeepsSelectionAndSelectionsIntact()
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

        var wasSelected = viewModel.SelectProfileAndOpenFileLibraryView("p1");

        Assert.True(wasSelected);
        Assert.Equal("p1", viewModel.SelectedProfileId);
        Assert.Equal(Path.GetFullPath(source), viewModel.SelectedSourcePortPath);
        Assert.Equal(Path.GetFullPath(iwad), viewModel.SelectedIwadPath);
        Assert.Equal([Path.GetFullPath(mod)], viewModel.SelectedModPaths);
        Assert.True(viewModel.IsFileLibraryViewActive);
        Assert.True(persistence.SavedStates.Last().IsFileLibraryViewActive);
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

        Assert.True(row.IsCommandPreviewVisible);
        Assert.True(viewModel.IsProfilesViewSubtitleVisible);
        Assert.False(viewModel.IsFileLibraryViewSubtitleVisible);

        viewModel.ShowFileLibraryView();
        Assert.False(row.IsCommandPreviewVisible);
        Assert.False(viewModel.IsProfilesViewSubtitleVisible);
        Assert.True(viewModel.IsFileLibraryViewSubtitleVisible);

        viewModel.SetWindowWidth(640d);
        Assert.False(row.IsCommandPreviewVisible);
        Assert.False(viewModel.IsProfilesViewSubtitleVisible);
        Assert.False(viewModel.IsFileLibraryViewSubtitleVisible);

        viewModel.SetWindowWidth(639d);
        Assert.False(row.IsCommandPreviewVisible);
        Assert.False(viewModel.IsProfilesViewSubtitleVisible);
        Assert.False(viewModel.IsFileLibraryViewSubtitleVisible);

        viewModel.ShowProfilesView();
        Assert.False(row.IsCommandPreviewVisible);
        Assert.False(viewModel.IsProfilesViewSubtitleVisible);
        Assert.False(viewModel.IsFileLibraryViewSubtitleVisible);

        viewModel.SetWindowWidth(641d);
        Assert.True(row.IsCommandPreviewVisible);
        Assert.True(viewModel.IsProfilesViewSubtitleVisible);
        Assert.False(viewModel.IsFileLibraryViewSubtitleVisible);
    }

    [Fact]
    public void ModRows_WhenNoProfileSelected_StayInSharedLibraryOrderAfterIgnoredToggleAttempts()
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
            ["beta.pk3", "gamma.pk3", "alpha.pk3"],
            viewModel.ModRows.Select(row => Path.GetFileName(row.Path)).ToArray());

        viewModel.ToggleModSelection(modGamma);
        viewModel.ToggleModSelection(modAlpha);

        Assert.Equal(
            ["beta.pk3", "gamma.pk3", "alpha.pk3"],
            viewModel.ModRows.Select(row => Path.GetFileName(row.Path)).ToArray());
        Assert.Empty(viewModel.SelectedModPaths);
        Assert.Equal(0, persistence.SaveCallCount);
    }

    [Fact]
    public void LibraryRows_RequireSelectedProfileForSelectionAndReflectDisabledStateOtherwise()
    {
        using var temp = new TempDirectory();
        var source1 = temp.CreateFile("gzdoom.exe");
        var source2 = temp.CreateFile("vkdoom.exe");
        var iwad1 = temp.CreateFile("doom.wad");
        var iwad2 = temp.CreateFile("doom2.wad");
        var mod1 = temp.CreateFile("alpha.pk3");
        var mod2 = temp.CreateFile("beta.pk3");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source1, source2],
                    Iwads = [iwad1, iwad2],
                    Mods = [mod2, mod1],
                    Profiles = [CreateProfile("p1", "Profile 1", source1, iwad1, mod1)]
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        Assert.All(viewModel.SourcePortRows, row => Assert.True(row.IsSelectionDisabled));
        Assert.All(viewModel.IwadRows, row => Assert.True(row.IsSelectionDisabled));
        Assert.All(viewModel.ModRows, row => Assert.True(row.IsSelectionDisabled));

        viewModel.ToggleSourcePortSelection(source2);
        viewModel.ToggleIwadSelection(iwad2);
        viewModel.ToggleModSelection(mod2);

        Assert.Null(viewModel.SelectedSourcePortPath);
        Assert.Null(viewModel.SelectedIwadPath);
        Assert.Empty(viewModel.SelectedModPaths);

        viewModel.ToggleProfileSelection("p1");

        Assert.All(viewModel.SourcePortRows, row => Assert.True(row.IsSelectionEnabled));
        Assert.All(viewModel.IwadRows, row => Assert.True(row.IsSelectionEnabled));
        Assert.All(viewModel.ModRows, row => Assert.True(row.IsSelectionEnabled));

        viewModel.ToggleProfileSelection("p1");

        Assert.Null(viewModel.SelectedSourcePortPath);
        Assert.Null(viewModel.SelectedIwadPath);
        Assert.Empty(viewModel.SelectedModPaths);
        Assert.All(viewModel.SourcePortRows, row => Assert.True(row.IsSelectionDisabled));
        Assert.All(viewModel.IwadRows, row => Assert.True(row.IsSelectionDisabled));
        Assert.All(viewModel.ModRows, row => Assert.True(row.IsSelectionDisabled));

        viewModel.ToggleSourcePortSelection(source2);
        viewModel.ToggleIwadSelection(iwad2);
        viewModel.ToggleModSelection(mod2);

        Assert.Null(viewModel.SelectedSourcePortPath);
        Assert.Null(viewModel.SelectedIwadPath);
        Assert.Empty(viewModel.SelectedModPaths);
    }

    [Fact]
    public void SelectedProfile_ModRowsUseSharedLibraryOrder()
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
            ["charlie.pk3", "alpha.pk3", "delta.pk3", "bravo.pk3"],
            viewModel.ModRows.Select(row => Path.GetFileName(row.Path)).ToArray());
    }

    [Fact]
    public void SwitchingProfiles_KeepsDisplayedModRowsInSharedLibraryOrder()
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
            ["charlie.pk3", "alpha.pk3", "delta.pk3", "bravo.pk3"],
            viewModel.ModRows.Select(row => Path.GetFileName(row.Path)).ToArray());

        viewModel.ToggleProfileSelection("p2");
        Assert.Equal(
            ["charlie.pk3", "alpha.pk3", "delta.pk3", "bravo.pk3"],
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
            ["beta.pk3", "gamma.pk3", "alpha.pk3"],
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
        Assert.False(viewModel.HasToast);
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
        Assert.Equal("#10b981", viewModel.SelectedProfileStatusForeground);
        Assert.True(viewModel.CanLaunch);
        Assert.Empty(viewModel.SelectedModPaths);
        Assert.Equal("gzdoom.exe -iwad doom2.wad -file mod-a.pk3", profileRow.CommandPreviewText);
        Assert.Equal([Path.GetFullPath(mod)], persistence.SavedStates.Last().Profiles.Single().SelectedModPaths);
    }

    [Fact]
    public void InvalidProfileRow_InlineInvalidReasonRespectsProfilesViewAndWidthGate()
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
        Assert.True(row.IsInvalidReasonVisible);
        Assert.Contains("IWAD file is missing", row.InvalidReason);
        Assert.Contains("IWAD file is missing", viewModel.SelectedProfileStatusText);

        viewModel.SetWindowWidth(640d);
        Assert.False(row.IsInvalidReasonVisible);

        viewModel.SetWindowWidth(639d);
        Assert.False(row.IsInvalidReasonVisible);

        viewModel.SetWindowWidth(641d);
        Assert.True(row.IsInvalidReasonVisible);

        viewModel.ShowFileLibraryView();

        Assert.True(row.IsInvalid);
        Assert.Equal("INVALID", row.StatusBadgeText);
        Assert.False(row.IsInvalidReasonVisible);

        viewModel.SetWindowWidth(641d);
        Assert.False(row.IsInvalidReasonVisible);
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
        Assert.Equal("#10b981", viewModel.SelectedProfileStatusForeground);
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
    public void ReorderMod_WhenNoProfileSelected_UpdatesAndPersistsSharedLibraryOrder()
    {
        using var temp = new TempDirectory();
        var modAlpha = temp.CreateFile("alpha.pk3");
        var modBravo = temp.CreateFile("bravo.pk3");
        var modCharlie = temp.CreateFile("charlie.pk3");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    Mods = [modAlpha, modBravo, modCharlie]
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        var reordered = viewModel.ReorderMod(modCharlie, 0);

        Assert.True(reordered);
        Assert.Equal(
            ["charlie.pk3", "alpha.pk3", "bravo.pk3"],
            viewModel.ModRows.Select(row => Path.GetFileName(row.Path)).ToArray());
        Assert.Equal(
            [Path.GetFullPath(modCharlie), Path.GetFullPath(modAlpha), Path.GetFullPath(modBravo)],
            persistence.SavedStates.Last().Mods);
    }

    [Fact]
    public void ReorderMod_PreservesSelectionStateAndSelectedSequenceOrder()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");
        var modAlpha = temp.CreateFile("alpha.pk3");
        var modBravo = temp.CreateFile("bravo.pk3");
        var modCharlie = temp.CreateFile("charlie.pk3");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source],
                    Iwads = [iwad],
                    Mods = [modAlpha, modBravo, modCharlie],
                    Profiles = [CreateProfile("p1", "Profile 1", source, iwad, modBravo, modAlpha)],
                    SelectedProfileId = "p1"
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        var reordered = viewModel.ReorderMod(modCharlie, 0);

        Assert.True(reordered);
        Assert.Equal(
            ["charlie.pk3", "alpha.pk3", "bravo.pk3"],
            viewModel.ModRows.Select(row => Path.GetFileName(row.Path)).ToArray());
        Assert.Equal(
            [Path.GetFullPath(modBravo), Path.GetFullPath(modAlpha)],
            viewModel.SelectedModPaths.ToArray());
        Assert.Equal(
            [Path.GetFullPath(modBravo), Path.GetFullPath(modAlpha)],
            persistence.SavedStates.Last().Profiles.Single().SelectedModPaths);
    }

    [Fact]
    public void ReorderMod_NoOpTargets_DoNotPersistNewOrder()
    {
        using var temp = new TempDirectory();
        var modAlpha = temp.CreateFile("alpha.pk3");
        var modBravo = temp.CreateFile("bravo.pk3");
        var modCharlie = temp.CreateFile("charlie.pk3");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    Mods = [modAlpha, modBravo, modCharlie]
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);
        var saveCountBeforeNoOps = persistence.SaveCallCount;

        Assert.False(viewModel.ReorderMod("missing", 0));
        Assert.False(viewModel.ReorderMod(modBravo, 1));
        Assert.False(viewModel.ReorderMod(modBravo, 2));
        Assert.False(viewModel.ReorderMod(modBravo, -1));
        Assert.False(viewModel.ReorderMod(modBravo, 4));

        Assert.Equal(saveCountBeforeNoOps, persistence.SaveCallCount);
        Assert.Equal(
            ["alpha.pk3", "bravo.pk3", "charlie.pk3"],
            viewModel.ModRows.Select(row => Path.GetFileName(row.Path)).ToArray());
    }

    [Fact]
    public void ReorderSourcePort_WhenNoProfileSelected_UpdatesAndPersistsSharedLibraryOrder()
    {
        using var temp = new TempDirectory();
        var sourceAlpha = temp.CreateFile("alpha.exe");
        var sourceBravo = temp.CreateFile("bravo.exe");
        var sourceCharlie = temp.CreateFile("charlie.exe");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [sourceAlpha, sourceBravo, sourceCharlie]
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        var reordered = viewModel.ReorderSourcePort(sourceCharlie, 0);

        Assert.True(reordered);
        Assert.Equal(
            ["charlie.exe", "alpha.exe", "bravo.exe"],
            viewModel.SourcePortRows.Select(row => Path.GetFileName(row.Path)).ToArray());
        Assert.Equal(
            [Path.GetFullPath(sourceCharlie), Path.GetFullPath(sourceAlpha), Path.GetFullPath(sourceBravo)],
            persistence.SavedStates.Last().SourcePorts);
    }

    [Fact]
    public void ReorderIwad_WhenNoProfileSelected_UpdatesAndPersistsSharedLibraryOrder()
    {
        using var temp = new TempDirectory();
        var iwadAlpha = temp.CreateFile("alpha.wad");
        var iwadBravo = temp.CreateFile("bravo.wad");
        var iwadCharlie = temp.CreateFile("charlie.wad");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    Iwads = [iwadAlpha, iwadBravo, iwadCharlie]
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        var reordered = viewModel.ReorderIwad(iwadCharlie, 0);

        Assert.True(reordered);
        Assert.Equal(
            ["charlie.wad", "alpha.wad", "bravo.wad"],
            viewModel.IwadRows.Select(row => Path.GetFileName(row.Path)).ToArray());
        Assert.Equal(
            [Path.GetFullPath(iwadCharlie), Path.GetFullPath(iwadAlpha), Path.GetFullPath(iwadBravo)],
            persistence.SavedStates.Last().Iwads);
    }

    [Fact]
    public void ReorderSourcePort_PreservesSelectionState()
    {
        using var temp = new TempDirectory();
        var sourceAlpha = temp.CreateFile("alpha.exe");
        var sourceBravo = temp.CreateFile("bravo.exe");
        var sourceCharlie = temp.CreateFile("charlie.exe");
        var iwad = temp.CreateFile("doom2.wad");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [sourceAlpha, sourceBravo, sourceCharlie],
                    Iwads = [iwad],
                    Profiles = [CreateProfile("p1", "Profile 1", sourceBravo, iwad)],
                    SelectedProfileId = "p1"
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        var reordered = viewModel.ReorderSourcePort(sourceCharlie, 0);

        Assert.True(reordered);
        Assert.Equal(Path.GetFullPath(sourceBravo), viewModel.SelectedSourcePortPath);
        Assert.Equal(Path.GetFullPath(sourceBravo), persistence.SavedStates.Last().Profiles.Single().SourcePortPath);
    }

    [Fact]
    public void ReorderIwad_PreservesSelectionState()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwadAlpha = temp.CreateFile("alpha.wad");
        var iwadBravo = temp.CreateFile("bravo.wad");
        var iwadCharlie = temp.CreateFile("charlie.wad");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source],
                    Iwads = [iwadAlpha, iwadBravo, iwadCharlie],
                    Profiles = [CreateProfile("p1", "Profile 1", source, iwadBravo)],
                    SelectedProfileId = "p1"
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        var reordered = viewModel.ReorderIwad(iwadCharlie, 0);

        Assert.True(reordered);
        Assert.Equal(Path.GetFullPath(iwadBravo), viewModel.SelectedIwadPath);
        Assert.Equal(Path.GetFullPath(iwadBravo), persistence.SavedStates.Last().Profiles.Single().IwadPath);
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
    public void BeginModDrag_UsesModPathGhost_AndHideClearsFeedback()
    {
        using var temp = new TempDirectory();
        var mod = temp.CreateFile("mod-a.pk3");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    Mods = [mod]
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        var began = viewModel.BeginModDrag(mod, 18d, 36d);
        viewModel.ShowModDropIndicator(4d, 10d, 90d);

        Assert.True(began);
        Assert.True(viewModel.IsModDragGhostVisible);
        Assert.Equal(Path.GetFullPath(mod), viewModel.ModDragGhostText);
        Assert.Equal(18d, viewModel.ModDragGhostLeft);
        Assert.Equal(36d, viewModel.ModDragGhostTop);
        Assert.True(viewModel.IsModDropIndicatorVisible);

        viewModel.HideModDragFeedback();

        Assert.False(viewModel.IsModDragGhostVisible);
        Assert.Equal(string.Empty, viewModel.ModDragGhostText);
        Assert.False(viewModel.IsModDropIndicatorVisible);
    }

    [Fact]
    public void BeginSourcePortDrag_UsesPathGhost_AndHideClearsFeedback()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source]
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        var began = viewModel.BeginSourcePortDrag(source, 18d, 36d);
        viewModel.ShowSourcePortDropIndicator(4d, 10d, 90d);

        Assert.True(began);
        Assert.True(viewModel.IsSourcePortDragGhostVisible);
        Assert.Equal(Path.GetFullPath(source), viewModel.SourcePortDragGhostText);
        Assert.Equal(18d, viewModel.SourcePortDragGhostLeft);
        Assert.Equal(36d, viewModel.SourcePortDragGhostTop);
        Assert.True(viewModel.IsSourcePortDropIndicatorVisible);

        viewModel.HideSourcePortDragFeedback();

        Assert.False(viewModel.IsSourcePortDragGhostVisible);
        Assert.Equal(string.Empty, viewModel.SourcePortDragGhostText);
        Assert.False(viewModel.IsSourcePortDropIndicatorVisible);
    }

    [Fact]
    public void BeginIwadDrag_UsesPathGhost_AndHideClearsFeedback()
    {
        using var temp = new TempDirectory();
        var iwad = temp.CreateFile("doom2.wad");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    Iwads = [iwad]
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        var began = viewModel.BeginIwadDrag(iwad, 18d, 36d);
        viewModel.ShowIwadDropIndicator(4d, 10d, 90d);

        Assert.True(began);
        Assert.True(viewModel.IsIwadDragGhostVisible);
        Assert.Equal(Path.GetFullPath(iwad), viewModel.IwadDragGhostText);
        Assert.Equal(18d, viewModel.IwadDragGhostLeft);
        Assert.Equal(36d, viewModel.IwadDragGhostTop);
        Assert.True(viewModel.IsIwadDropIndicatorVisible);

        viewModel.HideIwadDragFeedback();

        Assert.False(viewModel.IsIwadDragGhostVisible);
        Assert.Equal(string.Empty, viewModel.IwadDragGhostText);
        Assert.False(viewModel.IsIwadDropIndicatorVisible);
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

        Assert.False(viewModel.HasActiveProfileRename);
        Assert.False(viewModel.ProfileRows.Single().IsRenameVisible);
        Assert.Equal("Ultra-Violence", viewModel.SelectedProfileName);
        Assert.Equal("Ultra-Violence", persistence.SavedStates.Last().Profiles.Single().Name);
        Assert.False(viewModel.HasToast);
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

        Assert.True(viewModel.HasActiveProfileRename);
        Assert.True(viewModel.IsSelectedProfileRowRenameVisible);
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

        Assert.True(viewModel.HasActiveProfileRename);
        Assert.True(viewModel.ProfileRows.Single(row => row.Id == "p1").IsRenameVisible);
        Assert.Equal("Profile 1", viewModel.SelectedProfileName);
        Assert.True(viewModel.HasToast);
        Assert.True(viewModel.IsPassiveToast);
        Assert.Equal(ToastKind.Warning, viewModel.CurrentToastKind);
        Assert.Equal("Profile name must be unique.", viewModel.ToastMessageText);
    }

    [Fact]
    public void CommitRename_EmptyName_ShowsWarningToastAndKeepsRenameOpen()
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
        viewModel.SelectedProfileRenameText = "   ";

        viewModel.CommitRename("p1");

        Assert.True(viewModel.HasActiveProfileRename);
        Assert.True(viewModel.ProfileRows.Single().IsRenameVisible);
        Assert.True(viewModel.HasToast);
        Assert.True(viewModel.IsPassiveToast);
        Assert.Equal(ToastKind.Warning, viewModel.CurrentToastKind);
        Assert.Equal("Profile name is required.", viewModel.ToastMessageText);
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
        Assert.False(viewModel.HasActiveProfileRename);
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

        Assert.False(viewModel.HasActiveProfileRename);
    }

    [Fact]
    public void BeginRenameSelectedProfile_WhenProfileIsSelected_UsesHeaderRenameFlow()
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

        viewModel.ShowFileLibraryView();
        viewModel.BeginRenameSelectedProfile();

        Assert.True(viewModel.HasActiveProfileRename);
        Assert.True(viewModel.IsSelectedProfileHeaderRenameVisible);
        Assert.Equal("Profile 1", viewModel.SelectedProfileRenameText);
        Assert.False(viewModel.ProfileRows.Single().IsRenameVisible);
    }

    [Fact]
    public void RequestDeleteSelectedProfile_WhenProfileIsSelected_UsesExistingConfirmationFlow()
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

        viewModel.RequestDeleteSelectedProfile();

        Assert.True(viewModel.HasPendingDeleteConfirmation);
        Assert.True(viewModel.HasToast);
        Assert.True(viewModel.HasToastActions);
        Assert.Equal(ToastKind.Confirmation, viewModel.CurrentToastKind);
        Assert.Equal("Delete profile \"Profile 1\"?", viewModel.ToastMessageText);
        Assert.Equal("p1", viewModel.SelectedProfileId);
    }

    [Fact]
    public void CancelDeleteConfirmation_DismissesConfirmationToastWithoutDeleting()
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

        viewModel.RequestDeleteSelectedProfile();
        viewModel.CancelDeleteConfirmation();

        Assert.False(viewModel.HasPendingDeleteConfirmation);
        Assert.False(viewModel.HasToast);
        Assert.Single(viewModel.ProfileRows);
        Assert.Equal("p1", viewModel.SelectedProfileId);
    }

    [Fact]
    public void ShowProfilesView_WhenLibraryViewIsActive_PersistsProfilesViewState()
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
                    IsFileLibraryViewActive = true
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        viewModel.ShowProfilesView();

        Assert.Equal("p1", viewModel.SelectedProfileId);
        Assert.Equal(Path.GetFullPath(source), viewModel.SelectedSourcePortPath);
        Assert.Equal(Path.GetFullPath(iwad), viewModel.SelectedIwadPath);
        Assert.Equal([Path.GetFullPath(mod)], viewModel.SelectedModPaths);
        Assert.True(viewModel.IsProfilesViewActive);
        Assert.False(viewModel.IsFileLibraryViewActive);
        Assert.False(persistence.SavedStates.Last().IsFileLibraryViewActive);
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
    public void LaunchSourcePort_WhenLauncherFails_ShowsWarningToast()
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

        var launcher = new RecordingLauncher
        {
            ExceptionToThrow = new InvalidOperationException("boom")
        };
        var viewModel = new MainWindowViewModel(persistence, launcher);

        viewModel.LaunchSourcePort();

        Assert.True(viewModel.HasToast);
        Assert.True(viewModel.IsPassiveToast);
        Assert.Equal(ToastKind.Warning, viewModel.CurrentToastKind);
        Assert.Equal("Launch failed: boom", viewModel.ToastMessageText);
    }

    [Fact]
    public void LaunchProfile_WhenTargetProfileInvalid_ShowsWarningToastWithoutInvokingLauncher()
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
        Assert.True(row.CanLaunchProfile);

        viewModel.LaunchProfile("p1");

        Assert.Equal("p1", viewModel.SelectedProfileId);
        Assert.Equal(0, launcher.LaunchCallCount);
        Assert.True(viewModel.HasToast);
        Assert.True(viewModel.IsPassiveToast);
        Assert.Equal(ToastKind.Warning, viewModel.CurrentToastKind);
        Assert.Equal("IWAD file is missing: missing.wad", viewModel.ToastMessageText);
    }

    [Fact]
    public void LaunchProfile_WhenSameInvalidWarningAlreadyVisible_DoesNotReplaceToast()
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

        viewModel.LaunchProfile("p1");
        var toastSequence = viewModel.ToastSequence;

        viewModel.LaunchProfile("p1");

        Assert.Equal(toastSequence, viewModel.ToastSequence);
        Assert.Equal(0, launcher.LaunchCallCount);
        Assert.Equal("IWAD file is missing: missing.wad", viewModel.ToastMessageText);
    }

    [Fact]
    public void LaunchProfile_WhenInvalidWarningDismissed_CanShowSameWarningAgain()
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

        viewModel.LaunchProfile("p1");
        viewModel.DismissPassiveToast();
        var toastSequenceAfterDismiss = viewModel.ToastSequence;

        viewModel.LaunchProfile("p1");

        Assert.True(viewModel.HasToast);
        Assert.True(viewModel.ToastSequence > toastSequenceAfterDismiss);
        Assert.Equal(0, launcher.LaunchCallCount);
        Assert.Equal("IWAD file is missing: missing.wad", viewModel.ToastMessageText);
    }

    [Fact]
    public void LaunchProfile_WhenInvalidReasonChanges_ShowsUpdatedWarningToast()
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

        viewModel.LaunchProfile("p1");
        var firstToastSequence = viewModel.ToastSequence;

        viewModel.ToggleSourcePortSelection(source);
        viewModel.LaunchProfile("p1");

        Assert.True(viewModel.ToastSequence > firstToastSequence);
        Assert.Equal(0, launcher.LaunchCallCount);
        Assert.Equal("Source Port is required. IWAD is required.", viewModel.ToastMessageText);
    }

    [Fact]
    public void MainWindowXaml_PlacesProfileActionsAndDragOnlyDropZonesInCorrectSections()
    {
        var xamlPath = GetRepoFilePath("src", "ModLoader.App", "MainWindow.axaml");
        var xaml = File.ReadAllText(xamlPath);

        Assert.DoesNotContain("Click=\"OnLaunchClicked\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"OnLaunchProfileClicked\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("IsEnabled=\"{Binding CanLaunchProfile}\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("IsVisible=\"{Binding HasMessage}\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"{Binding MessageText}\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("DockPanel.Dock=\"Top\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ToastHost\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding HasToast}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding ToastMessageText}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding HasToastActions}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Background=\"{Binding ToastBackground}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("BorderBrush=\"{Binding ToastBorderBrush}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Foreground=\"{Binding ToastForeground}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding DataContext.SelectedProfileRenameText, RelativeSource={RelativeSource AncestorType=Window}, Mode=TwoWay}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding SelectedProfileRenameText, Mode=TwoWay}\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"{Binding RenameText, Mode=TwoWay}\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("IsEnabled=\"{Binding CanCreateProfile}\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("IsEnabled=\"{Binding CanRenameSelectedProfile}\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("IsVisible=\"{Binding IsSelectedProfileRenameVisible}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding IsSelectedProfileHeaderRenameVisible}\"", xaml, StringComparison.Ordinal);
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
        Assert.DoesNotContain("Text=\"Command Preview\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"{Binding CommandPreviewArguments}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"File Library\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"Create, launch, rename, or delete saved profiles.\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding IsProfilesViewSubtitleVisible}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"Select Source Port, IWAD, and Mods for the selected profile.\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding IsFileLibraryViewSubtitleVisible}\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Select, launch, or delete a launch profile from the saved profile list.", xaml, StringComparison.Ordinal);
        Assert.Contains("ToolTip.Tip=\"New Profile\"", xaml, StringComparison.Ordinal);
        Assert.Contains("automation:AutomationProperties.Name=\"New Profile\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ToolTip.Tip=\"{Binding OpenFileLibraryViewText}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("automation:AutomationProperties.Name=\"{Binding OpenFileLibraryViewText}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ToolTip.Tip=\"{Binding ReturnToProfilesViewText}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("automation:AutomationProperties.Name=\"{Binding ReturnToProfilesViewText}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ToolTip.Tip=\"Create Profile\"", xaml, StringComparison.Ordinal);
        Assert.Contains("automation:AutomationProperties.Name=\"Create Profile\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding IsSelectedProfileCreateVisible}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ToolTip.Tip=\"Edit\"", xaml, StringComparison.Ordinal);
        Assert.Contains("automation:AutomationProperties.Name=\"Edit\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ToolTip.Tip=\"Launch\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ToolTip.Tip=\"Rename\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ToolTip.Tip=\"Delete\"", xaml, StringComparison.Ordinal);
        Assert.Equal(5, xaml.Split("ToolTip.Tip=\"Delete\"", StringSplitOptions.None).Length - 1);
        Assert.Contains("automation:AutomationProperties.Name=\"Delete\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("ToolTip.Tip=\"Remove\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("automation:AutomationProperties.Name=\"Remove\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Data=\"{StaticResource add_square_regular}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Data=\"{StaticResource arrow_left_regular}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Data=\"{StaticResource arrow_right_regular}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Data=\"{StaticResource play_regular}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Data=\"{StaticResource edit_regular}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Data=\"{StaticResource delete_regular}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Data=\"{StaticResource arrow_down_regular}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Data=\"{StaticResource chevron_up_regular}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Data=\"{StaticResource chevron_down_regular}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Selector=\"ScrollViewer.HiddenScrollbars\"", xaml, StringComparison.Ordinal);
        Assert.Contains("VerticalScrollBarVisibility\" Value=\"Hidden\"", xaml, StringComparison.Ordinal);
        Assert.Contains("HorizontalScrollBarVisibility\" Value=\"Disabled\"", xaml, StringComparison.Ordinal);
        Assert.Equal(2, xaml.Split("Classes=\"HiddenScrollbars\"", StringSplitOptions.None).Length - 1);
        Assert.Contains("x:Name=\"ProfileListScrollAffordance\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"FileLibraryScrollViewer\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"FileLibraryScrollAffordance\"", xaml, StringComparison.Ordinal);
        Assert.Equal(2, xaml.Split("Data=\"{StaticResource arrow_down_regular}\"", StringSplitOptions.None).Length - 1);
        Assert.Contains("IsHitTestVisible=\"False\"", xaml, StringComparison.Ordinal);
        Assert.Contains("HorizontalAlignment=\"Center\"", xaml, StringComparison.Ordinal);
        Assert.Contains("VerticalAlignment=\"Bottom\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Grid RowDefinitions=\"Auto,Auto,*\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Grid RowDefinitions=\"Auto,Auto,Auto,*\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<DoubleTransition Property=\"Opacity\" Duration=\"0:0:0.18\" />", xaml, StringComparison.Ordinal);
        Assert.Contains("PointerMoved=\"OnProfileRowPointerMoved\"", xaml, StringComparison.Ordinal);
        Assert.Contains("PointerReleased=\"OnProfileRowPointerReleased\"", xaml, StringComparison.Ordinal);
        Assert.Contains("PointerCaptureLost=\"OnProfileRowPointerCaptureLost\"", xaml, StringComparison.Ordinal);
        Assert.Contains("PointerMoved=\"OnSourcePortRowPointerMoved\"", xaml, StringComparison.Ordinal);
        Assert.Contains("PointerReleased=\"OnSourcePortRowPointerReleased\"", xaml, StringComparison.Ordinal);
        Assert.Contains("PointerCaptureLost=\"OnSourcePortRowPointerCaptureLost\"", xaml, StringComparison.Ordinal);
        Assert.Contains("PointerMoved=\"OnIwadRowPointerMoved\"", xaml, StringComparison.Ordinal);
        Assert.Contains("PointerReleased=\"OnIwadRowPointerReleased\"", xaml, StringComparison.Ordinal);
        Assert.Contains("PointerCaptureLost=\"OnIwadRowPointerCaptureLost\"", xaml, StringComparison.Ordinal);
        Assert.Contains("PointerMoved=\"OnModRowPointerMoved\"", xaml, StringComparison.Ordinal);
        Assert.Contains("PointerReleased=\"OnModRowPointerReleased\"", xaml, StringComparison.Ordinal);
        Assert.Contains("PointerCaptureLost=\"OnModRowPointerCaptureLost\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding IsProfileDragGhostVisible}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding ProfileDragGhostText}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding IsProfileDropIndicatorVisible}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Width=\"{Binding ProfileDropIndicatorWidth}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding IsModDragGhostVisible}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding ModDragGhostText}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding IsModDropIndicatorVisible}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Width=\"{Binding ModDropIndicatorWidth}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding IsSourcePortDragGhostVisible}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding SourcePortDragGhostText}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding IsSourcePortDropIndicatorVisible}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Width=\"{Binding SourcePortDropIndicatorWidth}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding IsIwadDragGhostVisible}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding IwadDragGhostText}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding IsIwadDropIndicatorVisible}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Width=\"{Binding IwadDropIndicatorWidth}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"SourcePortListHost\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"IwadListHost\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ModListHost\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"Drag and drop here\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"Allowed: .exe. Directories are ignored.\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Allowed: .exe. Drop one or more files. Directories are ignored.\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Drag files here or click to upload\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"+\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("PointerPressed=\"OnDropZonePointerPressed\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("KeyDown=\"OnDropZoneKeyDown\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Focusable=\"True\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("IsTabStop=\"True\"", xaml, StringComparison.Ordinal);
        Assert.Contains("DragDrop.DragEnter=\"OnDropZoneDragEnter\"", xaml, StringComparison.Ordinal);
        Assert.Contains("DragDrop.DragLeave=\"OnDropZoneDragLeave\"", xaml, StringComparison.Ordinal);
        Assert.Contains("DragDrop.DragOver=\"OnDropZoneDragOver\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Classes.dragover=\"{Binding IsSourcePortDropZoneDragActive}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Classes.dragover=\"{Binding IsIwadDropZoneDragActive}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Classes.dragover=\"{Binding IsModDropZoneDragActive}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("automation:AutomationProperties.Name=\"Source Port drop zone. Drag and drop files here.\"", xaml, StringComparison.Ordinal);
        Assert.Contains("automation:AutomationProperties.HelpText=\"Allowed: .exe. Directories are ignored.\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("automation:AutomationProperties.HelpText=\"Allowed: .exe. Drop one or more files. Directories are ignored.\"", xaml, StringComparison.Ordinal);
        Assert.Contains("automation:AutomationProperties.Name=\"IWAD drop zone. Drag and drop files here.\"", xaml, StringComparison.Ordinal);
        Assert.Contains("automation:AutomationProperties.Name=\"Mod drop zone. Drag and drop files here.\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Feature 010: Profile Drag Reorder\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Profiles are the only launchable unit. Select a profile on the left, then manage Source Port, IWAD, and Mods from the shared library on the right.", xaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"OnEditSelectedProfileClicked\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"OnDeleteSelectedProfileClicked\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding HasSelectedProfile}\"", xaml, StringComparison.Ordinal);

        var toastHostIndex = xaml.IndexOf("x:Name=\"ToastHost\"", StringComparison.Ordinal);
        Assert.True(toastHostIndex >= 0);

        var toastHostEndIndex = xaml.IndexOf("</Border>", toastHostIndex, StringComparison.Ordinal);
        Assert.True(toastHostEndIndex > toastHostIndex);

        var toastHostBlock = xaml.Substring(toastHostIndex, toastHostEndIndex - toastHostIndex);
        Assert.Contains("HorizontalAlignment=\"Center\"", toastHostBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("HorizontalAlignment=\"Right\"", toastHostBlock, StringComparison.Ordinal);
        Assert.Contains("VerticalAlignment=\"Top\"", toastHostBlock, StringComparison.Ordinal);

        var profilesHeaderIndex = xaml.IndexOf("Text=\"Profiles\"", StringComparison.Ordinal);
        var newProfileIndex = xaml.IndexOf("ToolTip.Tip=\"New Profile\"", StringComparison.Ordinal);
        var fileLibraryToggleIndex = xaml.IndexOf("ToolTip.Tip=\"{Binding OpenFileLibraryViewText}\"", StringComparison.Ordinal);
        var profilesToggleIndex = xaml.IndexOf("ToolTip.Tip=\"{Binding ReturnToProfilesViewText}\"", StringComparison.Ordinal);
        var fileLibraryTitleIndex = xaml.IndexOf("Text=\"File Library\"", StringComparison.Ordinal);
        var fileLibraryScrollViewerIndex = xaml.IndexOf("x:Name=\"FileLibraryScrollViewer\"", StringComparison.Ordinal);
        var selectedProfileNameIndex = xaml.IndexOf("Text=\"{Binding SelectedProfileName}\"", StringComparison.Ordinal);
        var selectedProfileEditIndex = xaml.IndexOf("ToolTip.Tip=\"Edit\"", StringComparison.Ordinal);
        var selectedProfilePreviewIndex = xaml.IndexOf("Text=\"{Binding SelectedProfileCommandPreviewText}\"", StringComparison.Ordinal);
        var launchIndex = xaml.LastIndexOf("ToolTip.Tip=\"Launch\"", StringComparison.Ordinal);
        var renameIndex = xaml.LastIndexOf("ToolTip.Tip=\"Rename\"", StringComparison.Ordinal);
        var deleteIndex = xaml.LastIndexOf("ToolTip.Tip=\"Delete\"", StringComparison.Ordinal);

        Assert.True(profilesHeaderIndex >= 0);
        Assert.True(newProfileIndex > profilesHeaderIndex);
        Assert.True(fileLibraryToggleIndex > newProfileIndex);
        Assert.True(fileLibraryTitleIndex > profilesToggleIndex);
        Assert.True(fileLibraryTitleIndex < selectedProfileNameIndex);
        Assert.True(profilesToggleIndex < selectedProfileNameIndex);
        Assert.True(selectedProfileNameIndex > newProfileIndex);
        Assert.True(selectedProfileNameIndex > fileLibraryToggleIndex);
        Assert.True(fileLibraryScrollViewerIndex > selectedProfileNameIndex);
        Assert.True(fileLibraryScrollViewerIndex > selectedProfilePreviewIndex);
        Assert.True(selectedProfileEditIndex > selectedProfileNameIndex);
        Assert.True(selectedProfilePreviewIndex > selectedProfileNameIndex);
        Assert.True(launchIndex >= 0);
        Assert.True(renameIndex > launchIndex);
        Assert.True(deleteIndex > launchIndex);
        Assert.True(deleteIndex > renameIndex);
        Assert.Equal(fileLibraryToggleIndex, xaml.LastIndexOf("ToolTip.Tip=\"{Binding OpenFileLibraryViewText}\"", StringComparison.Ordinal));
        Assert.Equal(profilesToggleIndex, xaml.LastIndexOf("ToolTip.Tip=\"{Binding ReturnToProfilesViewText}\"", StringComparison.Ordinal));
    }

    [Fact]
    public void MainWindowCodeBehind_CommitsRenameOnOutsideClickAndLostFocus()
    {
        var codeBehindPath = GetRepoFilePath("src", "ModLoader.App", "MainWindow.axaml.cs");
        var codeBehind = File.ReadAllText(codeBehindPath);

        var pointerPressedStart = codeBehind.IndexOf("private void OnWindowPointerPressed", StringComparison.Ordinal);
        var pointerPressedEnd = codeBehind.IndexOf("private void OnProfileRowPointerPressed", pointerPressedStart, StringComparison.Ordinal);
        var pointerPressedBlock = codeBehind.Substring(pointerPressedStart, pointerPressedEnd - pointerPressedStart);

        Assert.Contains("if (_viewModel.RenamingProfileId is string profileId)", pointerPressedBlock, StringComparison.Ordinal);
        Assert.Contains("_viewModel.CommitRename(profileId);", pointerPressedBlock, StringComparison.Ordinal);
        Assert.Contains("e.Handled = true;", pointerPressedBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("_viewModel.CancelRename();", pointerPressedBlock, StringComparison.Ordinal);

        var lostFocusStart = codeBehind.IndexOf("private void OnProfileRenameLostFocus", StringComparison.Ordinal);
        var lostFocusEnd = codeBehind.IndexOf("private void OnProfileRenameTextBoxLoaded", lostFocusStart, StringComparison.Ordinal);
        var lostFocusBlock = codeBehind.Substring(lostFocusStart, lostFocusEnd - lostFocusStart);

        Assert.Contains("_viewModel.CommitRename(profileId);", lostFocusBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("_viewModel.CancelRename();", lostFocusBlock, StringComparison.Ordinal);
    }

    [Fact]
    public void FeatureSpecs_ReflectProfileOrderingAndDragReorderSpecs()
    {
        var spec = File.ReadAllText(GetRepoFilePath("SPEC.md"));
        var feature008 = File.ReadAllText(GetRepoFilePath("Features", "008-profile-management.md"));
        var feature009 = File.ReadAllText(GetRepoFilePath("Features", "009-file-library-pane-collapse.md"));
        var feature011 = File.ReadAllText(GetRepoFilePath("Features", "011-icon-based-action-controls.md"));
        var feature012 = File.ReadAllText(GetRepoFilePath("Features", "012-profile-only-collapse-mode-and-header-removal.md"));
        var feature013 = File.ReadAllText(GetRepoFilePath("Features", "013-toast-message-overlay.md"));
        var feature014 = File.ReadAllText(GetRepoFilePath("Features", "014-single-view-profile-library-swap.md"));
        var feature015 = File.ReadAllText(GetRepoFilePath("Features", "015-mod-selection-stability-and-drag-reorder.md"));

        Assert.Contains("Feature 014: Single-view profile/library workspace swap.", spec, StringComparison.Ordinal);
        Assert.Contains("single shared workspace", spec, StringComparison.Ordinal);
        Assert.Contains("adds a `File Library` title in File Library view", spec, StringComparison.Ordinal);
        Assert.Contains("removes the old pane-collapse model and fixed default window sizes", spec, StringComparison.Ordinal);
        Assert.Contains("shared top-centered toast overlay", spec, StringComparison.Ordinal);
        Assert.Contains("saved profiles the only launchable unit", spec, StringComparison.Ordinal);
        Assert.Contains("Selected-profile status text uses themed green `#10b981`", spec, StringComparison.Ordinal);
        Assert.Contains("Profile rename interaction model (Feature 008):", spec, StringComparison.Ordinal);
        Assert.Contains("save on `Enter` or outside click", spec, StringComparison.Ordinal);
        Assert.Contains("Profiles-view helper subtitle text is visible only in `Profiles` view", spec, StringComparison.Ordinal);
        Assert.Contains("File-Library-view helper subtitle text is visible only in `File Library` view", spec, StringComparison.Ordinal);
        Assert.Contains("Feature 015: Shared-library selection stability and manual drag reorder.", spec, StringComparison.Ordinal);
        Assert.Contains("Selecting or deselecting Source Port, IWAD, or Mod rows does not reorder rows.", spec, StringComparison.Ordinal);

        Assert.Contains("single shared workspace", feature008, StringComparison.Ordinal);
        Assert.Contains("Profiles view", feature008, StringComparison.Ordinal);
        Assert.Contains("File Library view", feature008, StringComparison.Ordinal);
        Assert.Contains("When no profile is selected, Source Port, IWAD, and Mod rows remain visible but are not selectable.", feature008, StringComparison.Ordinal);
        Assert.Contains("The selected-profile header `Edit` action starts rename inside the File Library view header.", feature008, StringComparison.Ordinal);
        Assert.Contains("Each profile row exposes launch, rename, and delete actions in that order.", feature008, StringComparison.Ordinal);
        Assert.Contains("outside click saves a valid unique non-empty name", feature008, StringComparison.Ordinal);
        Assert.Contains("outside click with an invalid rename keeps rename mode open and shows a Feature 013 passive warning toast.", feature008, StringComparison.Ordinal);
        Assert.Contains("header rename uses the same `Enter`, outside-click, and `Escape` commit/cancel behavior as profile-row rename.", feature008, StringComparison.Ordinal);
        Assert.Contains("shared themed green `#10b981`", feature008, StringComparison.Ordinal);
        Assert.Contains("shared amber invalid `#f59e0b`", feature008, StringComparison.Ordinal);
        Assert.DoesNotContain("And outside click or `Escape` restores the previous saved name.", feature008, StringComparison.Ordinal);

        Assert.Contains("superseded by Feature 014", feature009, StringComparison.Ordinal);
        Assert.DoesNotContain("the right file-library pane is not rendered", feature009, StringComparison.Ordinal);

        Assert.Contains("Profiles-header view-swap action exposes `File Library`", feature011, StringComparison.Ordinal);
        Assert.Contains("File-Library top-left back-to-profiles action exposes `Profiles`", feature011, StringComparison.Ordinal);
        Assert.Contains("`arrow_right_regular`", feature011, StringComparison.Ordinal);
        Assert.Contains("`arrow_left_regular`", feature011, StringComparison.Ordinal);

        Assert.Contains("The top fixed header is not rendered.", feature012, StringComparison.Ordinal);
        Assert.Contains("Feature 014 is authoritative for workspace view swapping.", feature012, StringComparison.Ordinal);
        Assert.DoesNotContain("restores the remembered expanded normal-window width", feature012, StringComparison.Ordinal);

        Assert.Contains("Replace the two-pane workspace with one shared workspace that shows either `Profiles` or `File Library`.", feature014, StringComparison.Ordinal);
        Assert.Contains("Add a `File Library` title to File Library view.", feature014, StringComparison.Ordinal);
        Assert.Contains("Add simple helper subtitle text to Profiles and File Library view headers.", feature014, StringComparison.Ordinal);
        Assert.Contains("the helper subtitle text `Create, launch, rename, or delete saved profiles.`", feature014, StringComparison.Ordinal);
        Assert.Contains("the helper subtitle text `Select Source Port, IWAD, and Mods for the selected profile.`", feature014, StringComparison.Ordinal);
        Assert.Contains("Profiles-view helper subtitle text is visible only in Profiles view and only when the overall window width is greater than `640 px`.", feature014, StringComparison.Ordinal);
        Assert.Contains("File-Library-view helper subtitle text is visible only in File Library view and only when the overall window width is greater than `640 px`.", feature014, StringComparison.Ordinal);
        Assert.Contains("Remove persisted pane-collapse and remembered-width state.", feature014, StringComparison.Ordinal);
        Assert.Contains("Creating a new profile selects it and keeps Profiles view active.", feature014, StringComparison.Ordinal);
        Assert.Contains("Double-clicking a profile row selects that profile and opens File Library view.", feature014, StringComparison.Ordinal);
        Assert.Contains("Given File Library view is visible", feature014, StringComparison.Ordinal);
        Assert.Contains("Then the top-row `File Library` title is visible.", feature014, StringComparison.Ordinal);
        Assert.Contains("The File Library view exposes a top-left `Profiles` back action above the selected-profile header.", feature014, StringComparison.Ordinal);
        Assert.Contains("The selected-profile header `Edit` action opens rename inline in that header.", feature014, StringComparison.Ordinal);
        Assert.Contains("Stop shared-library row reordering during selection changes", feature015, StringComparison.Ordinal);
        Assert.Contains("Selection and deselection do not reorder Source Port, IWAD, or Mod rows.", feature015, StringComparison.Ordinal);
        Assert.Contains("Cross-list movement between Source Port, IWAD, and Mod lists.", feature015, StringComparison.Ordinal);
        Assert.Contains("Reordered list order persists immediately", feature015, StringComparison.Ordinal);
        Assert.Contains("Drag reorder availability both with and without a selected profile.", feature015, StringComparison.Ordinal);

        Assert.Contains("Feature 013 is the authoritative source for toast behavior.", feature013, StringComparison.Ordinal);
        Assert.Contains("Only one toast is visible at a time.", feature013, StringComparison.Ordinal);
        Assert.Contains("Passive warning toasts auto-dismiss after `5` seconds.", feature013, StringComparison.Ordinal);
        Assert.Contains("Confirmation toasts expose text `Delete` and `Cancel` actions.", feature013, StringComparison.Ordinal);
        Assert.Contains("do not replace or restart that toast", feature013, StringComparison.Ordinal);
        Assert.Contains("top-center of the window above the workspace content", feature013, StringComparison.Ordinal);
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
            IsFileLibraryViewActive = config.IsFileLibraryViewActive,
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
