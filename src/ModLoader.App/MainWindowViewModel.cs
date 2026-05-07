using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using ModLoader.Core;

namespace ModLoader.App;

public enum ToastKind
{
    None,
    Informational,
    Warning,
    Confirmation
}

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private const double ProfileCommandPreviewHideWidthThreshold = 768d;
    private readonly ISourcePortLauncher _launcher;
    private readonly ILaunchInputsPersistence _persistence;
    private readonly LaunchInputsStore _store;
    private readonly List<ProfileConfig> _profiles = [];
    private bool _isFileLibraryViewActive;
    private bool _isIwadDropZoneDragActive;
    private bool _isProfileDragGhostVisible;
    private bool _isProfileDropIndicatorVisible;
    private bool _isIwadSectionCollapsed;
    private bool _isModSectionCollapsed;
    private bool _isModDropZoneDragActive;
    private ProfileRenameMode _profileRenameMode;
    private bool _isSourcePortDropZoneDragActive;
    private bool _isSourcePortSectionCollapsed;
    private string? _pendingDeleteProfileId;
    private string _profileDragGhostText = string.Empty;
    private double _profileDragGhostLeft;
    private double _profileDragGhostTop;
    private double _profileDropIndicatorLeft;
    private double _profileDropIndicatorTop;
    private double _profileDropIndicatorWidth;
    private double _windowWidth = double.PositiveInfinity;
    private string? _selectedIwadPath;
    private string? _selectedProfileId;
    private string _selectedProfileRenameText = string.Empty;
    private string? _selectedSourcePortPath;
    private bool _isInvalidLaunchToastVisible;
    private string? _toastMessageText;
    private int _toastSequence;
    private ToastKind _toastKind;

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainWindowViewModel()
        : this(
            new JsonLaunchInputsPersistence(Path.Combine(AppContext.BaseDirectory, "modloader.config.json")),
            new ProcessSourcePortLauncher())
    {
    }

    public MainWindowViewModel(ILaunchInputsPersistence persistence)
        : this(persistence, new ProcessSourcePortLauncher())
    {
    }

    public MainWindowViewModel(ILaunchInputsPersistence persistence, ISourcePortLauncher launcher)
    {
        _persistence = persistence;
        _launcher = launcher;

        var loadResult = _persistence.Load();
        _store = new LaunchInputsStore(loadResult.State);
        LoadProfilesFromConfig(loadResult.State);
        IsFileLibraryViewActive = loadResult.State.IsFileLibraryViewActive;
        IsSourcePortSectionCollapsed = loadResult.State.IsSourcePortSectionCollapsed;
        IsIwadSectionCollapsed = loadResult.State.IsIwadSectionCollapsed;
        IsModSectionCollapsed = loadResult.State.IsModSectionCollapsed;

        var storeSanitized = _store.RemoveMissingPaths();
        var selectedProfileSanitized = InitializeSelectedProfile(loadResult.State.SelectedProfileId);

        if (!string.IsNullOrWhiteSpace(loadResult.WarningMessage))
        {
            ShowToast(loadResult.WarningMessage, ToastKind.Warning);
        }

        RefreshFromStore();

        if (storeSanitized || selectedProfileSanitized)
        {
            PersistState();
        }
    }

    public ObservableCollection<ProfileListItem> ProfileRows { get; } = [];

    public ObservableCollection<string> SourcePorts { get; } = [];

    public ObservableCollection<string> Iwads { get; } = [];

    public ObservableCollection<string> Mods { get; } = [];

    public ObservableCollection<SelectablePathRow> SourcePortRows { get; } = [];

    public ObservableCollection<SelectablePathRow> IwadRows { get; } = [];

    public ObservableCollection<SelectablePathRow> ModRows { get; } = [];

    public ObservableCollection<string> SelectedModPaths { get; } = [];

    public bool HasProfiles => ProfileRows.Count > 0;

    public bool HasSourcePort => !string.IsNullOrWhiteSpace(SelectedSourcePortPath);

    public bool HasSourcePorts => SourcePorts.Count > 0;

    public bool HasIwads => Iwads.Count > 0;

    public bool HasMods => Mods.Count > 0;

    public bool HasSelectedProfile => !string.IsNullOrWhiteSpace(SelectedProfileId);

    public bool HasActiveProfileRename => _profileRenameMode != ProfileRenameMode.None;

    public string? RenamingProfileId => HasActiveProfileRename ? SelectedProfileId : null;

    public bool CanCreateProfile => true;

    public bool CanRenameSelectedProfile => HasSelectedProfile;

    public bool CanLaunch
    {
        get
        {
            var selectedProfile = GetSelectedProfile();
            if (selectedProfile is null)
            {
                return false;
            }

            return GetProfileValidity(selectedProfile).IsValid;
        }
    }

    public string? SelectedSourcePortPath
    {
        get => _selectedSourcePortPath;
        private set
        {
            if (string.Equals(_selectedSourcePortPath, value, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _selectedSourcePortPath = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSourcePort));
            OnPropertyChanged(nameof(CanCreateProfile));
            OnPropertyChanged(nameof(CommandPreviewArguments));
        }
    }

    public string? SelectedIwadPath
    {
        get => _selectedIwadPath;
        private set
        {
            if (string.Equals(_selectedIwadPath, value, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _selectedIwadPath = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanCreateProfile));
            OnPropertyChanged(nameof(CommandPreviewArguments));
        }
    }

    public string? SelectedProfileId
    {
        get => _selectedProfileId;
        private set
        {
            if (string.Equals(_selectedProfileId, value, StringComparison.Ordinal))
            {
                return;
            }

            _selectedProfileId = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedProfile));
            OnPropertyChanged(nameof(CanRenameSelectedProfile));
            OnPropertyChanged(nameof(CanLaunch));
            OnSelectedProfilePresentationChanged();

            if (!HasActiveProfileRename)
            {
                SelectedProfileRenameText = GetSelectedProfile()?.Name ?? string.Empty;
            }
        }
    }

    public bool HasPendingDeleteConfirmation => !string.IsNullOrWhiteSpace(_pendingDeleteProfileId);

    public string? ToastMessageText => _toastMessageText;

    public ToastKind CurrentToastKind => _toastKind;

    public bool HasToast => !string.IsNullOrWhiteSpace(_toastMessageText) && _toastKind != ToastKind.None;

    public bool HasToastActions => HasToast && CurrentToastKind == ToastKind.Confirmation;

    public bool IsPassiveToast => HasToast && !HasToastActions;

    public string ToastBackground => CurrentToastKind switch
    {
        ToastKind.Informational => "#172554",
        ToastKind.Warning => "#2b1a08",
        ToastKind.Confirmation => "#2b1a08",
        _ => "#00000000"
    };

    public string ToastBorderBrush => CurrentToastKind switch
    {
        ToastKind.Informational => "#60a5fa",
        ToastKind.Warning => "#f59e0b",
        ToastKind.Confirmation => "#f59e0b",
        _ => "#00000000"
    };

    public string ToastForeground => CurrentToastKind switch
    {
        ToastKind.Informational => "#dbeafe",
        ToastKind.Warning => "#fde68a",
        ToastKind.Confirmation => "#fde68a",
        _ => "#e5e7eb"
    };

    public int ToastSequence => _toastSequence;

    public bool IsFileLibraryViewActive
    {
        get => _isFileLibraryViewActive;
        private set
        {
            if (_isFileLibraryViewActive == value)
            {
                return;
            }

            _isFileLibraryViewActive = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsProfilesViewActive));
            OnPropertyChanged(nameof(OpenFileLibraryViewText));
            OnPropertyChanged(nameof(ReturnToProfilesViewText));
            OnPropertyChanged(nameof(AreProfileCommandPreviewsVisible));
            OnPropertyChanged(nameof(IsSelectedProfileHeaderRenameVisible));
            OnPropertyChanged(nameof(IsSelectedProfileRowRenameVisible));
        }
    }

    public bool IsProfilesViewActive => !IsFileLibraryViewActive;

    public string OpenFileLibraryViewText => "File Library";

    public string ReturnToProfilesViewText => "Profiles";

    public bool IsSourcePortSectionCollapsed
    {
        get => _isSourcePortSectionCollapsed;
        private set
        {
            if (_isSourcePortSectionCollapsed == value)
            {
                return;
            }

            _isSourcePortSectionCollapsed = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AreSourcePortRowsVisible));
            OnPropertyChanged(nameof(SourcePortSectionToggleText));
        }
    }

    public bool AreSourcePortRowsVisible => !IsSourcePortSectionCollapsed;

    public string SourcePortSectionToggleText => IsSourcePortSectionCollapsed ? "Expand" : "Collapse";

    public bool IsIwadSectionCollapsed
    {
        get => _isIwadSectionCollapsed;
        private set
        {
            if (_isIwadSectionCollapsed == value)
            {
                return;
            }

            _isIwadSectionCollapsed = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AreIwadRowsVisible));
            OnPropertyChanged(nameof(IwadSectionToggleText));
        }
    }

    public bool AreIwadRowsVisible => !IsIwadSectionCollapsed;

    public string IwadSectionToggleText => IsIwadSectionCollapsed ? "Expand" : "Collapse";

    public bool IsModSectionCollapsed
    {
        get => _isModSectionCollapsed;
        private set
        {
            if (_isModSectionCollapsed == value)
            {
                return;
            }

            _isModSectionCollapsed = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AreModRowsVisible));
            OnPropertyChanged(nameof(ModSectionToggleText));
        }
    }

    public bool AreModRowsVisible => !IsModSectionCollapsed;

    public string ModSectionToggleText => IsModSectionCollapsed ? "Expand" : "Collapse";

    public string SelectedProfileName => GetSelectedProfile()?.Name ?? "No Profile Selected";

    public string SelectedProfileCommandPreviewText
    {
        get
        {
            var selectedProfile = GetSelectedProfile();
            return selectedProfile is null ? string.Empty : BuildCommandPreviewArguments(selectedProfile);
        }
    }

    public bool HasSelectedProfileCommandPreview => !string.IsNullOrWhiteSpace(SelectedProfileCommandPreviewText);

    public string SelectedProfileRenameText
    {
        get => _selectedProfileRenameText;
        set
        {
            if (_selectedProfileRenameText == value)
            {
                return;
            }

            _selectedProfileRenameText = value;
            OnPropertyChanged();
        }
    }

    public bool IsSelectedProfileDisplayVisible => _profileRenameMode != ProfileRenameMode.Header;

    public bool IsSelectedProfileHeaderRenameVisible
    {
        get => _profileRenameMode == ProfileRenameMode.Header;
        private set
        {
            var newMode = value ? ProfileRenameMode.Header : ProfileRenameMode.None;
            if (_profileRenameMode == newMode)
            {
                return;
            }

            _profileRenameMode = newMode;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsSelectedProfileDisplayVisible));
            OnPropertyChanged(nameof(HasActiveProfileRename));
            OnPropertyChanged(nameof(RenamingProfileId));
            OnPropertyChanged(nameof(IsSelectedProfileHeaderRenameVisible));
            OnPropertyChanged(nameof(IsSelectedProfileRowRenameVisible));
        }
    }

    public bool IsSelectedProfileRowRenameVisible => _profileRenameMode == ProfileRenameMode.Row;

    public string SelectedProfileStatusText
    {
        get
        {
            var selectedProfile = GetSelectedProfile();
            if (selectedProfile is null)
            {
                return "Select a saved profile or create a new one.";
            }

            var validity = GetProfileValidity(selectedProfile);
            return validity.IsValid ? "Selected profile is ready to launch." : validity.Reason;
        }
    }

    public string SelectedProfileStatusForeground
    {
        get
        {
            var selectedProfile = GetSelectedProfile();
            if (selectedProfile is null)
            {
                return "#94a3b8";
            }

            return GetProfileValidity(selectedProfile).IsValid ? "#94a3b8" : "#f59e0b";
        }
    }

    public bool IsProfileDragGhostVisible
    {
        get => _isProfileDragGhostVisible;
        private set
        {
            if (_isProfileDragGhostVisible == value)
            {
                return;
            }

            _isProfileDragGhostVisible = value;
            OnPropertyChanged();
        }
    }

    public string ProfileDragGhostText
    {
        get => _profileDragGhostText;
        private set
        {
            if (_profileDragGhostText == value)
            {
                return;
            }

            _profileDragGhostText = value;
            OnPropertyChanged();
        }
    }

    public double ProfileDragGhostLeft
    {
        get => _profileDragGhostLeft;
        private set
        {
            if (_profileDragGhostLeft == value)
            {
                return;
            }

            _profileDragGhostLeft = value;
            OnPropertyChanged();
        }
    }

    public double ProfileDragGhostTop
    {
        get => _profileDragGhostTop;
        private set
        {
            if (_profileDragGhostTop == value)
            {
                return;
            }

            _profileDragGhostTop = value;
            OnPropertyChanged();
        }
    }

    public bool IsProfileDropIndicatorVisible
    {
        get => _isProfileDropIndicatorVisible;
        private set
        {
            if (_isProfileDropIndicatorVisible == value)
            {
                return;
            }

            _isProfileDropIndicatorVisible = value;
            OnPropertyChanged();
        }
    }

    public double ProfileDropIndicatorLeft
    {
        get => _profileDropIndicatorLeft;
        private set
        {
            if (_profileDropIndicatorLeft == value)
            {
                return;
            }

            _profileDropIndicatorLeft = value;
            OnPropertyChanged();
        }
    }

    public double ProfileDropIndicatorTop
    {
        get => _profileDropIndicatorTop;
        private set
        {
            if (_profileDropIndicatorTop == value)
            {
                return;
            }

            _profileDropIndicatorTop = value;
            OnPropertyChanged();
        }
    }

    public double ProfileDropIndicatorWidth
    {
        get => _profileDropIndicatorWidth;
        private set
        {
            if (_profileDropIndicatorWidth == value)
            {
                return;
            }

            _profileDropIndicatorWidth = value;
            OnPropertyChanged();
        }
    }

    public string CommandPreviewArguments => BuildCommandPreviewArguments();

    public bool AreProfileCommandPreviewsVisible => IsProfilesViewActive && _windowWidth > ProfileCommandPreviewHideWidthThreshold;

    public bool IsSourcePortDropZoneDragActive
    {
        get => _isSourcePortDropZoneDragActive;
        private set
        {
            if (_isSourcePortDropZoneDragActive == value)
            {
                return;
            }

            _isSourcePortDropZoneDragActive = value;
            OnPropertyChanged();
        }
    }

    public bool IsIwadDropZoneDragActive
    {
        get => _isIwadDropZoneDragActive;
        private set
        {
            if (_isIwadDropZoneDragActive == value)
            {
                return;
            }

            _isIwadDropZoneDragActive = value;
            OnPropertyChanged();
        }
    }

    public bool IsModDropZoneDragActive
    {
        get => _isModDropZoneDragActive;
        private set
        {
            if (_isModDropZoneDragActive == value)
            {
                return;
            }

            _isModDropZoneDragActive = value;
            OnPropertyChanged();
        }
    }

    public void SetWindowWidth(double width)
    {
        var normalizedWidth = width > 0d ? width : 0d;
        if (Math.Abs(_windowWidth - normalizedWidth) < 0.01d)
        {
            return;
        }

        _windowWidth = normalizedWidth;
        RefreshProfileRows();
    }

    public void ProcessSourcePortDrop(IEnumerable<string> droppedPaths)
    {
        ResetDropZoneDragStates();
        _store.ProcessSourcePortDrop(droppedPaths);
        ClearPendingDeleteConfirmation();
        RefreshFromStore();
        PersistState();
    }

    public void ProcessIwadDrop(IEnumerable<string> droppedPaths)
    {
        ResetDropZoneDragStates();
        _store.ProcessIwadDrop(droppedPaths);
        ClearPendingDeleteConfirmation();
        RefreshFromStore();
        PersistState();
    }

    public void ProcessModDrop(IEnumerable<string> droppedPaths)
    {
        ResetDropZoneDragStates();
        _store.ProcessModDrop(droppedPaths);
        ClearPendingDeleteConfirmation();
        RefreshFromStore();
        PersistState();
    }

    internal void SetDropZoneDragActive(DropZoneKind kind, bool isActive)
    {
        switch (kind)
        {
            case DropZoneKind.SourcePort:
                IsSourcePortDropZoneDragActive = isActive;
                break;
            case DropZoneKind.Iwad:
                IsIwadDropZoneDragActive = isActive;
                break;
            case DropZoneKind.Mod:
                IsModDropZoneDragActive = isActive;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }

    public void ToggleSourcePortSectionCollapsed()
    {
        IsSourcePortSectionCollapsed = !IsSourcePortSectionCollapsed;
        PersistState();
    }

    public void ShowFileLibraryView()
    {
        CancelRename();
        IsFileLibraryViewActive = true;
        RefreshProfileRows();
        PersistState();
    }

    public void ShowProfilesView()
    {
        CancelRename();
        IsFileLibraryViewActive = false;
        RefreshProfileRows();
        PersistState();
    }

    public void RemoveSourcePort(string path)
    {
        _store.RemoveSourcePort(path);
        ClearPendingDeleteConfirmation();
        RefreshFromStore();
        PersistState();
    }

    public void RemoveIwad(string path)
    {
        _store.RemoveIwad(path);
        ClearPendingDeleteConfirmation();
        RefreshFromStore();
        PersistState();
    }

    public void RemoveMod(string path)
    {
        _store.RemoveMod(path);
        ClearPendingDeleteConfirmation();
        RefreshFromStore();
        PersistState();
    }

    public void ToggleIwadSectionCollapsed()
    {
        IsIwadSectionCollapsed = !IsIwadSectionCollapsed;
        PersistState();
    }

    public void ToggleModSectionCollapsed()
    {
        IsModSectionCollapsed = !IsModSectionCollapsed;
        PersistState();
    }

    public void ToggleSourcePortSelection(string path)
    {
        if (!HasSelectedProfile)
        {
            return;
        }

        var normalizedPath = PathNormalizer.NormalizeAbsolutePath(path);

        if (string.Equals(SelectedSourcePortPath, normalizedPath, StringComparison.OrdinalIgnoreCase))
        {
            SelectedSourcePortPath = null;
        }
        else
        {
            SelectedSourcePortPath = normalizedPath;
        }

        ClearPendingDeleteConfirmation();
        SyncSelectedProfileFromSelections();
        RefreshRows();
        RefreshProfileRows();
        OnPropertyChanged(nameof(CanLaunch));
        OnSelectedProfilePresentationChanged();
        PersistState();
    }

    public void ToggleIwadSelection(string path)
    {
        if (!HasSelectedProfile)
        {
            return;
        }

        var normalizedPath = PathNormalizer.NormalizeAbsolutePath(path);

        if (string.Equals(SelectedIwadPath, normalizedPath, StringComparison.OrdinalIgnoreCase))
        {
            SelectedIwadPath = null;
        }
        else
        {
            SelectedIwadPath = normalizedPath;
        }

        ClearPendingDeleteConfirmation();
        SyncSelectedProfileFromSelections();
        RefreshRows();
        RefreshProfileRows();
        OnPropertyChanged(nameof(CanLaunch));
        OnSelectedProfilePresentationChanged();
        PersistState();
    }

    public void ToggleModSelection(string path)
    {
        if (!HasSelectedProfile)
        {
            return;
        }

        var normalizedPath = PathNormalizer.NormalizeAbsolutePath(path);
        var existingIndex = FindPathIndex(SelectedModPaths, normalizedPath);

        if (existingIndex >= 0)
        {
            SelectedModPaths.RemoveAt(existingIndex);
        }
        else
        {
            SelectedModPaths.Add(normalizedPath);
        }

        ClearPendingDeleteConfirmation();
        SyncSelectedProfileFromSelections();
        RefreshRows();
        RefreshProfileRows();
        OnPropertyChanged(nameof(CommandPreviewArguments));
        OnPropertyChanged(nameof(CanLaunch));
        OnSelectedProfilePresentationChanged();
        PersistState();
    }

    public void ToggleProfileSelection(string profileId)
    {
        CancelRename();

        if (string.Equals(SelectedProfileId, profileId, StringComparison.Ordinal))
        {
            SelectedProfileId = null;
            ClearCurrentSelections();
        }
        else if (TrySelectProfile(profileId))
        {
            HydrateSelectionsFromSelectedProfile();
        }

        ClearPendingDeleteConfirmation();
        RefreshRows();
        RefreshProfileRows();
        OnPropertyChanged(nameof(CanLaunch));
        OnSelectedProfilePresentationChanged();
        PersistState();
    }

    public bool SelectProfileAndOpenFileLibraryView(string profileId)
    {
        CancelRename();

        if (!TrySelectProfile(profileId))
        {
            return false;
        }

        HydrateSelectionsFromSelectedProfile();
        IsFileLibraryViewActive = true;
        ClearPendingDeleteConfirmation();
        RefreshRows();
        RefreshProfileRows();
        OnPropertyChanged(nameof(CanLaunch));
        OnSelectedProfilePresentationChanged();
        PersistState();
        return true;
    }

    public void CreateNewProfile()
    {
        CancelRename();
        ClearPendingDeleteConfirmation();

        var profile = new ProfileConfig
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = GenerateDefaultProfileName(),
            SourcePortPath = null,
            IwadPath = null,
            SelectedModPaths = []
        };

        _profiles.Add(profile);
        SelectedProfileId = profile.Id;
        HydrateSelectionsFromSelectedProfile();
        RefreshRows();
        RefreshProfileRows();
        OnPropertyChanged(nameof(HasProfiles));
        OnPropertyChanged(nameof(CanLaunch));
        OnSelectedProfilePresentationChanged();
        PersistState();
    }

    public bool BeginProfileDrag(string profileId, double ghostLeft, double ghostTop)
    {
        CancelRename();

        var row = FindProfileRow(profileId);
        if (row is null)
        {
            return false;
        }

        ProfileDragGhostText = row.Name;
        ProfileDragGhostLeft = ghostLeft;
        ProfileDragGhostTop = ghostTop;
        IsProfileDragGhostVisible = true;
        HideProfileDropIndicator();
        return true;
    }

    public void UpdateProfileDragGhostPosition(double ghostLeft, double ghostTop)
    {
        ProfileDragGhostLeft = ghostLeft;
        ProfileDragGhostTop = ghostTop;
    }

    public void ShowProfileDropIndicator(double left, double top, double width)
    {
        ProfileDropIndicatorLeft = left;
        ProfileDropIndicatorTop = top;
        ProfileDropIndicatorWidth = width;
        IsProfileDropIndicatorVisible = true;
    }

    public void HideProfileDropIndicator()
    {
        IsProfileDropIndicatorVisible = false;
    }

    public void HideProfileDragFeedback()
    {
        IsProfileDragGhostVisible = false;
        ProfileDragGhostText = string.Empty;
        HideProfileDropIndicator();
    }

    public void BeginRenameSelectedProfile()
    {
        var selectedProfileId = SelectedProfileId;
        if (string.IsNullOrWhiteSpace(selectedProfileId))
        {
            return;
        }

        CancelRename();
        ClearPendingDeleteConfirmation();

        if (!TrySelectProfile(selectedProfileId))
        {
            return;
        }

        HydrateSelectionsFromSelectedProfile();
        SelectedProfileRenameText = GetSelectedProfile()?.Name ?? string.Empty;
        ClearPassiveToast();
        IsSelectedProfileHeaderRenameVisible = true;
        RefreshRows();
        RefreshProfileRows();
        OnPropertyChanged(nameof(CanLaunch));
        OnSelectedProfilePresentationChanged();
        PersistState();
    }

    public void BeginRenameProfile(string profileId)
    {
        CancelRename();
        ClearPendingDeleteConfirmation();

        if (!TrySelectProfile(profileId))
        {
            return;
        }

        HydrateSelectionsFromSelectedProfile();
        RefreshRows();
        RefreshProfileRows();

        var row = FindProfileRow(profileId);
        if (row is null)
        {
            return;
        }

        _profileRenameMode = ProfileRenameMode.Row;
        OnPropertyChanged(nameof(HasActiveProfileRename));
        OnPropertyChanged(nameof(RenamingProfileId));
        OnPropertyChanged(nameof(IsSelectedProfileDisplayVisible));
        OnPropertyChanged(nameof(IsSelectedProfileHeaderRenameVisible));
        OnPropertyChanged(nameof(IsSelectedProfileRowRenameVisible));
        SelectedProfileRenameText = row.Name;
        RefreshProfileRows();
        ClearPassiveToast();
        OnPropertyChanged(nameof(CanLaunch));
        OnSelectedProfilePresentationChanged();
        PersistState();
    }

    public void UpdateRenameText(string profileId, string? text)
    {
        if (!HasActiveProfileRename
            || !string.Equals(profileId, SelectedProfileId, StringComparison.Ordinal))
        {
            return;
        }

        SelectedProfileRenameText = text ?? string.Empty;
    }

    public void CommitRename(string profileId)
    {
        var row = FindProfileRow(profileId);
        if (row is null
            || !HasActiveProfileRename
            || !string.Equals(profileId, SelectedProfileId, StringComparison.Ordinal))
        {
            return;
        }

        var proposedName = SelectedProfileRenameText.Trim();
        if (string.IsNullOrWhiteSpace(proposedName))
        {
            ShowToast("Profile name is required.", ToastKind.Warning);
            return;
        }

        if (_profiles.Any(
                profile => !string.Equals(profile.Id, profileId, StringComparison.Ordinal)
                    && string.Equals(profile.Name, proposedName, StringComparison.OrdinalIgnoreCase)))
        {
            ShowToast("Profile name must be unique.", ToastKind.Warning);
            return;
        }

        var profileIndex = FindProfileIndex(profileId);
        if (profileIndex < 0)
        {
            return;
        }

        var existingProfile = _profiles[profileIndex];
        _profiles[profileIndex] = new ProfileConfig
        {
            Id = existingProfile.Id,
            Name = proposedName,
            SourcePortPath = existingProfile.SourcePortPath,
            IwadPath = existingProfile.IwadPath,
            SelectedModPaths = [.. existingProfile.SelectedModPaths]
        };

        _profileRenameMode = ProfileRenameMode.None;
        OnPropertyChanged(nameof(HasActiveProfileRename));
        OnPropertyChanged(nameof(RenamingProfileId));
        OnPropertyChanged(nameof(IsSelectedProfileDisplayVisible));
        OnPropertyChanged(nameof(IsSelectedProfileHeaderRenameVisible));
        OnPropertyChanged(nameof(IsSelectedProfileRowRenameVisible));
        SelectedProfileRenameText = proposedName;
        ClearPassiveToast();
        RefreshProfileRows();
        OnSelectedProfilePresentationChanged();
        PersistState();
    }

    public bool CancelRename()
    {
        if (!HasActiveProfileRename)
        {
            return false;
        }

        _profileRenameMode = ProfileRenameMode.None;
        OnPropertyChanged(nameof(HasActiveProfileRename));
        OnPropertyChanged(nameof(RenamingProfileId));
        OnPropertyChanged(nameof(IsSelectedProfileDisplayVisible));
        OnPropertyChanged(nameof(IsSelectedProfileHeaderRenameVisible));
        OnPropertyChanged(nameof(IsSelectedProfileRowRenameVisible));
        SelectedProfileRenameText = GetSelectedProfile()?.Name ?? string.Empty;
        ClearPassiveToast();
        RefreshProfileRows();
        return true;
    }

    public void RequestDeleteProfile(string profileId)
    {
        CancelRename();

        var profile = _profiles.FirstOrDefault(candidate => string.Equals(candidate.Id, profileId, StringComparison.Ordinal));
        if (profile is null)
        {
            return;
        }

        _pendingDeleteProfileId = profileId;
        ShowToast($"Delete profile \"{profile.Name}\"?", ToastKind.Confirmation);
        OnPropertyChanged(nameof(HasPendingDeleteConfirmation));
    }

    public void RequestDeleteSelectedProfile()
    {
        var selectedProfileId = SelectedProfileId;
        if (string.IsNullOrWhiteSpace(selectedProfileId))
        {
            return;
        }

        RequestDeleteProfile(selectedProfileId);
    }

    public void ConfirmDeleteProfile()
    {
        if (string.IsNullOrWhiteSpace(_pendingDeleteProfileId))
        {
            return;
        }

        var deleteProfileId = _pendingDeleteProfileId;
        var removed = _profiles.RemoveAll(profile => string.Equals(profile.Id, deleteProfileId, StringComparison.Ordinal)) > 0;
        ClearPendingDeleteConfirmation();

        if (!removed)
        {
            return;
        }

        if (string.Equals(SelectedProfileId, deleteProfileId, StringComparison.Ordinal))
        {
            SelectedProfileId = null;
            ClearCurrentSelections();
        }

        RefreshRows();
        RefreshProfileRows();
        OnPropertyChanged(nameof(HasProfiles));
        OnPropertyChanged(nameof(CanLaunch));
        OnSelectedProfilePresentationChanged();
        PersistState();
    }

    public void CancelDeleteConfirmation()
    {
        ClearPendingDeleteConfirmation();
    }

    public void LaunchProfile(string profileId)
    {
        CancelRename();

        if (!TrySelectProfile(profileId))
        {
            return;
        }

        HydrateSelectionsFromSelectedProfile();
        RefreshRows();
        RefreshProfileRows();
        OnPropertyChanged(nameof(CanLaunch));
        OnSelectedProfilePresentationChanged();
        PersistState();

        var selectedProfile = GetSelectedProfile();
        if (selectedProfile is null)
        {
            return;
        }

        var validity = GetProfileValidity(selectedProfile);
        if (!validity.IsValid)
        {
            if (ShouldSuppressInvalidLaunchToast(validity.Reason))
            {
                return;
            }

            ClearPendingDeleteConfirmation();
            ShowInvalidLaunchToast(validity.Reason);
            return;
        }

        ClearPendingDeleteConfirmation();
        LaunchSourcePort();
    }

    public bool ReorderProfile(string profileId, int targetIndex)
    {
        CancelRename();

        if (string.IsNullOrWhiteSpace(profileId))
        {
            return false;
        }

        var currentIndex = FindProfileIndex(profileId);
        if (currentIndex < 0 || targetIndex < 0 || targetIndex > _profiles.Count)
        {
            return false;
        }

        var adjustedIndex = targetIndex > currentIndex ? targetIndex - 1 : targetIndex;
        if (adjustedIndex == currentIndex)
        {
            return false;
        }

        var movedProfile = _profiles[currentIndex];
        _profiles.RemoveAt(currentIndex);
        _profiles.Insert(adjustedIndex, movedProfile);

        ClearPendingDeleteConfirmation();
        RefreshProfileRows();
        OnPropertyChanged(nameof(CanLaunch));
        OnSelectedProfilePresentationChanged();
        PersistState();
        return true;
    }

    public void LaunchSourcePort()
    {
        var selectedProfile = GetSelectedProfile();
        if (selectedProfile is null)
        {
            return;
        }

        var validity = GetProfileValidity(selectedProfile);
        if (!validity.IsValid || string.IsNullOrWhiteSpace(selectedProfile.SourcePortPath) || string.IsNullOrWhiteSpace(selectedProfile.IwadPath))
        {
            return;
        }

        try
        {
            _launcher.Launch(selectedProfile.SourcePortPath, BuildLaunchArguments(selectedProfile));
        }
        catch (Exception ex)
        {
            ShowToast($"Launch failed: {ex.Message}", ToastKind.Warning);
        }
    }

    private void RefreshFromStore()
    {
        CopyCollection(_store.SourcePorts, SourcePorts);
        CopyCollection(_store.Iwads, Iwads);
        CopyCollection(_store.Mods, Mods);

        OnPropertyChanged(nameof(HasSourcePorts));
        OnPropertyChanged(nameof(HasIwads));
        OnPropertyChanged(nameof(HasMods));

        if (HasSelectedProfile)
        {
            HydrateSelectionsFromSelectedProfile();
        }
        else
        {
            NormalizeDetachedSelections();
        }

        RefreshRows();
        RefreshProfileRows();
        OnPropertyChanged(nameof(CanCreateProfile));
        OnPropertyChanged(nameof(CanLaunch));
        OnPropertyChanged(nameof(CommandPreviewArguments));
        OnSelectedProfilePresentationChanged();
    }

    private void OnSelectedProfilePresentationChanged()
    {
        OnPropertyChanged(nameof(SelectedProfileName));
        OnPropertyChanged(nameof(SelectedProfileStatusText));
        OnPropertyChanged(nameof(SelectedProfileStatusForeground));
        OnPropertyChanged(nameof(SelectedProfileCommandPreviewText));
        OnPropertyChanged(nameof(HasSelectedProfileCommandPreview));
        OnPropertyChanged(nameof(IsSelectedProfileDisplayVisible));
        OnPropertyChanged(nameof(IsSelectedProfileHeaderRenameVisible));
        OnPropertyChanged(nameof(IsSelectedProfileRowRenameVisible));
    }

    private void RefreshRows()
    {
        var isLibrarySelectionEnabled = HasSelectedProfile;

        CopyRows(
            SourcePorts,
            SourcePortRows,
            path => string.Equals(path, SelectedSourcePortPath, StringComparison.OrdinalIgnoreCase),
            isLibrarySelectionEnabled);

        CopyRows(
            Iwads,
            IwadRows,
            path => string.Equals(path, SelectedIwadPath, StringComparison.OrdinalIgnoreCase),
            isLibrarySelectionEnabled);

        CopyRows(
            GetOrderedModPaths(),
            ModRows,
            path => FindPathIndex(SelectedModPaths, path) >= 0,
            isLibrarySelectionEnabled);
    }

    private void RefreshProfileRows()
    {
        var existingById = ProfileRows.ToDictionary(row => row.Id, StringComparer.Ordinal);
        ProfileRows.Clear();

        foreach (var profile in _profiles)
        {
            if (!existingById.TryGetValue(profile.Id, out var row))
            {
                row = new ProfileListItem(profile.Id, profile.Name);
            }

            var validity = GetProfileValidity(profile);

            row.Name = profile.Name;
            row.IsSelected = string.Equals(profile.Id, SelectedProfileId, StringComparison.Ordinal);
            row.IsRenameVisible = IsSelectedProfileRowRenameVisible && row.IsSelected;
            row.IsInvalid = !validity.IsValid;
            row.CanLaunchProfile = row.IsDisplayVisible;
            row.ValidMessage = validity.IsValid ? "VALID" : string.Empty;
            row.InvalidReason = validity.Reason;
            row.IsInvalidReasonVisible = !validity.IsValid
                && IsProfilesViewActive
                && !string.IsNullOrWhiteSpace(validity.Reason);
            row.CommandPreviewText = BuildCommandPreviewArguments(profile);
            row.IsCommandPreviewVisible = AreProfileCommandPreviewsVisible && !string.IsNullOrWhiteSpace(row.CommandPreviewText);

            ProfileRows.Add(row);
        }

        OnPropertyChanged(nameof(HasProfiles));
    }

    private static void CopyCollection(IReadOnlyList<string> source, ObservableCollection<string> destination)
    {
        destination.Clear();
        foreach (var path in source)
        {
            destination.Add(path);
        }
    }

    private static void CopyRows(
        IReadOnlyList<string> paths,
        ObservableCollection<SelectablePathRow> destination,
        Func<string, bool> isSelected,
        bool isSelectionEnabled)
    {
        destination.Clear();
        foreach (var path in paths)
        {
            destination.Add(new SelectablePathRow(path, isSelected(path), isSelectionEnabled));
        }
    }

    private void LoadProfilesFromConfig(LaunchInputsConfig state)
    {
        _profiles.Clear();

        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var profile in state.Profiles ?? [])
        {
            if (string.IsNullOrWhiteSpace(profile.Id) || string.IsNullOrWhiteSpace(profile.Name))
            {
                continue;
            }

            if (!seenIds.Add(profile.Id))
            {
                continue;
            }

            var normalizedModPaths = new List<string>();
            var seenModPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var modPath in profile.SelectedModPaths ?? [])
            {
                if (string.IsNullOrWhiteSpace(modPath))
                {
                    continue;
                }

                var normalizedModPath = PathNormalizer.NormalizeAbsolutePath(modPath);
                if (seenModPaths.Add(normalizedModPath))
                {
                    normalizedModPaths.Add(normalizedModPath);
                }
            }

            _profiles.Add(new ProfileConfig
            {
                Id = profile.Id,
                Name = profile.Name,
                SourcePortPath = NormalizeNullablePath(profile.SourcePortPath),
                IwadPath = NormalizeNullablePath(profile.IwadPath),
                SelectedModPaths = normalizedModPaths
            });
        }
    }

    private bool InitializeSelectedProfile(string? selectedProfileId)
    {
        if (string.IsNullOrWhiteSpace(selectedProfileId))
        {
            SelectedProfileId = null;
            return false;
        }

        var matchingProfile = _profiles.FirstOrDefault(profile => string.Equals(profile.Id, selectedProfileId, StringComparison.Ordinal));
        if (matchingProfile is null)
        {
            SelectedProfileId = null;
            return true;
        }

        SelectedProfileId = matchingProfile.Id;
        return false;
    }

    private bool TrySelectProfile(string profileId)
    {
        var profile = _profiles.FirstOrDefault(candidate => string.Equals(candidate.Id, profileId, StringComparison.Ordinal));
        if (profile is null)
        {
            return false;
        }

        SelectedProfileId = profile.Id;
        return true;
    }

    private ProfileConfig? GetSelectedProfile()
    {
        if (string.IsNullOrWhiteSpace(SelectedProfileId))
        {
            return null;
        }

        return _profiles.FirstOrDefault(profile => string.Equals(profile.Id, SelectedProfileId, StringComparison.Ordinal));
    }

    private void HydrateSelectionsFromSelectedProfile()
    {
        var selectedProfile = GetSelectedProfile();
        if (selectedProfile is null)
        {
            ClearCurrentSelections();
            return;
        }

        SelectedSourcePortPath = ResolveSelectablePath(selectedProfile.SourcePortPath, SourcePorts);
        SelectedIwadPath = ResolveSelectablePath(selectedProfile.IwadPath, Iwads);

        SelectedModPaths.Clear();
        foreach (var modPath in selectedProfile.SelectedModPaths)
        {
            var resolvedPath = ResolveSelectablePath(modPath, Mods);
            if (resolvedPath is null || FindPathIndex(SelectedModPaths, resolvedPath) >= 0)
            {
                continue;
            }

            SelectedModPaths.Add(resolvedPath);
        }

        OnPropertyChanged(nameof(CommandPreviewArguments));
        OnPropertyChanged(nameof(CanCreateProfile));
    }

    private void NormalizeDetachedSelections()
    {
        if (!ContainsPath(SourcePorts, SelectedSourcePortPath) && !string.IsNullOrWhiteSpace(SelectedSourcePortPath))
        {
            SelectedSourcePortPath = null;
        }

        if (!ContainsPath(Iwads, SelectedIwadPath) && !string.IsNullOrWhiteSpace(SelectedIwadPath))
        {
            SelectedIwadPath = null;
        }

        var selectedModSnapshot = SelectedModPaths.ToArray();
        foreach (var selectedModPath in selectedModSnapshot)
        {
            if (!ContainsPath(Mods, selectedModPath))
            {
                var selectedIndex = FindPathIndex(SelectedModPaths, selectedModPath);
                if (selectedIndex >= 0)
                {
                    SelectedModPaths.RemoveAt(selectedIndex);
                }
            }
        }

        OnPropertyChanged(nameof(CommandPreviewArguments));
        OnPropertyChanged(nameof(CanCreateProfile));
    }

    private void ClearCurrentSelections()
    {
        SelectedSourcePortPath = null;
        SelectedIwadPath = null;
        SelectedModPaths.Clear();
        OnPropertyChanged(nameof(CommandPreviewArguments));
        OnPropertyChanged(nameof(CanCreateProfile));
    }

    private void SyncSelectedProfileFromSelections()
    {
        var profile = GetSelectedProfile();
        if (profile is null)
        {
            return;
        }

        var profileIndex = FindProfileIndex(profile.Id);
        if (profileIndex < 0)
        {
            return;
        }

        _profiles[profileIndex] = new ProfileConfig
        {
            Id = profile.Id,
            Name = profile.Name,
            SourcePortPath = SelectedSourcePortPath,
            IwadPath = SelectedIwadPath,
            SelectedModPaths = [.. SelectedModPaths]
        };
    }

    private int FindProfileIndex(string profileId)
    {
        for (var i = 0; i < _profiles.Count; i++)
        {
            if (string.Equals(_profiles[i].Id, profileId, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private ProfileListItem? FindProfileRow(string profileId)
    {
        return ProfileRows.FirstOrDefault(row => string.Equals(row.Id, profileId, StringComparison.Ordinal));
    }

    private ProfileValidity GetProfileValidity(ProfileConfig profile)
    {
        var reasons = new List<string>();

        if (string.IsNullOrWhiteSpace(profile.SourcePortPath))
        {
            reasons.Add("Source Port is required.");
        }
        else
        {
            if (!File.Exists(profile.SourcePortPath))
            {
                reasons.Add($"Source Port file is missing: {Path.GetFileName(profile.SourcePortPath)}");
            }
            else if (!ContainsPath(SourcePorts, profile.SourcePortPath))
            {
                reasons.Add($"Source Port is no longer in the library: {Path.GetFileName(profile.SourcePortPath)}");
            }
        }

        if (string.IsNullOrWhiteSpace(profile.IwadPath))
        {
            reasons.Add("IWAD is required.");
        }
        else
        {
            if (!File.Exists(profile.IwadPath))
            {
                reasons.Add($"IWAD file is missing: {Path.GetFileName(profile.IwadPath)}");
            }
            else if (!ContainsPath(Iwads, profile.IwadPath))
            {
                reasons.Add($"IWAD is no longer in the library: {Path.GetFileName(profile.IwadPath)}");
            }
        }

        return reasons.Count == 0
            ? new ProfileValidity(true, "Selected profile is ready to launch.")
            : new ProfileValidity(false, string.Join(" ", reasons));
    }

    private IReadOnlyList<string> GetOrderedModPaths()
    {
        if (!HasSelectedProfile)
        {
            return
            [
                .. Mods
                    .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
                    .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
            ];
        }

        var orderedSelectedPaths = new List<string>();
        var selectedPathSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var selectedModPath in SelectedModPaths)
        {
            if (!ContainsPath(Mods, selectedModPath))
            {
                continue;
            }

            if (selectedPathSet.Add(selectedModPath))
            {
                orderedSelectedPaths.Add(selectedModPath);
            }
        }

        var orderedUnselectedPaths = Mods
            .Where(path => !selectedPathSet.Contains(path))
            .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
            .ThenBy(path => path, StringComparer.OrdinalIgnoreCase);

        return [.. orderedSelectedPaths, .. orderedUnselectedPaths];
    }

    private static bool ContainsPath(IEnumerable<string> candidates, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        foreach (var candidate in candidates)
        {
            if (string.Equals(candidate, path, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static int FindPathIndex(IReadOnlyList<string> candidates, string path)
    {
        for (var i = 0; i < candidates.Count; i++)
        {
            if (string.Equals(candidates[i], path, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private static string? ResolveSelectablePath(string? path, IEnumerable<string> candidates)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        foreach (var candidate in candidates)
        {
            if (string.Equals(candidate, path, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        return null;
    }

    private static string? NormalizeNullablePath(string? path)
    {
        return string.IsNullOrWhiteSpace(path) ? null : PathNormalizer.NormalizeAbsolutePath(path);
    }

    private string GenerateDefaultProfileName()
    {
        var usedNames = new HashSet<string>(_profiles.Select(profile => profile.Name), StringComparer.OrdinalIgnoreCase);
        var suffix = 1;

        while (true)
        {
            var candidateName = $"Profile {suffix}";
            if (!usedNames.Contains(candidateName))
            {
                return candidateName;
            }

            suffix++;
        }
    }

    private string BuildCommandPreviewArguments()
    {
        return BuildCommandPreviewArguments(SelectedSourcePortPath, SelectedIwadPath, SelectedModPaths);
    }

    private static string BuildCommandPreviewArguments(ProfileConfig profile)
    {
        return BuildCommandPreviewArguments(profile.SourcePortPath, profile.IwadPath, profile.SelectedModPaths);
    }

    private static string BuildCommandPreviewArguments(string? sourcePortPath, string? iwadPath, IReadOnlyList<string> modPaths)
    {
        var arguments = new List<string>();

        if (!string.IsNullOrWhiteSpace(sourcePortPath))
        {
            arguments.Add(FormatPreviewFileToken(sourcePortPath));
        }

        if (!string.IsNullOrWhiteSpace(iwadPath))
        {
            arguments.Add("-iwad");
            arguments.Add(FormatPreviewFileToken(iwadPath));
        }

        if (modPaths.Count > 0)
        {
            arguments.Add("-file");
            foreach (var selectedModPath in modPaths)
            {
                arguments.Add(FormatPreviewFileToken(selectedModPath));
            }
        }

        return string.Join(" ", arguments);
    }

    private static List<string> BuildLaunchArguments(ProfileConfig profile)
    {
        var arguments = new List<string>
        {
            "-iwad",
            profile.IwadPath!
        };

        if (profile.SelectedModPaths.Count > 0)
        {
            arguments.Add("-file");
            foreach (var selectedModPath in profile.SelectedModPaths)
            {
                arguments.Add(selectedModPath);
            }
        }

        return arguments;
    }

    private static string FormatPreviewFileToken(string path)
    {
        var fileName = Path.GetFileName(path);
        var displayToken = string.IsNullOrWhiteSpace(fileName) ? path : fileName;

        return displayToken.Contains(' ', StringComparison.Ordinal)
            ? $"\"{displayToken}\""
            : displayToken;
    }

    private void ResetDropZoneDragStates()
    {
        IsSourcePortDropZoneDragActive = false;
        IsIwadDropZoneDragActive = false;
        IsModDropZoneDragActive = false;
    }

    private void PersistState()
    {
        var snapshot = _store.CreateSnapshot();
        var persistedConfig = new LaunchInputsConfig
        {
            SourcePorts = [.. snapshot.SourcePorts],
            Profiles = [.. _profiles.Select(CloneProfile)],
            SelectedProfileId = SelectedProfileId,
            IsFileLibraryViewActive = IsFileLibraryViewActive,
            IsSourcePortSectionCollapsed = IsSourcePortSectionCollapsed,
            SelectedSourcePortPath = null,
            Iwads = [.. snapshot.Iwads],
            IsIwadSectionCollapsed = IsIwadSectionCollapsed,
            Mods = [.. snapshot.Mods],
            IsModSectionCollapsed = IsModSectionCollapsed,
            SelectedIwadPath = null,
            SelectedModPaths = []
        };

        _persistence.Save(persistedConfig);
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

    public void DismissPassiveToast()
    {
        if (!IsPassiveToast)
        {
            return;
        }

        ClearToast();
    }

    private void ClearPassiveToast()
    {
        if (HasPendingDeleteConfirmation)
        {
            return;
        }

        ClearToast();
    }

    private void ClearPendingDeleteConfirmation()
    {
        var hadPendingDelete = HasPendingDeleteConfirmation;
        _pendingDeleteProfileId = null;
        ClearToast();

        if (hadPendingDelete)
        {
            OnPropertyChanged(nameof(HasPendingDeleteConfirmation));
        }
    }

    private void ShowToast(string? message, ToastKind kind)
    {
        SetToast(message, kind, isInvalidLaunchToast: false);
    }

    private void ShowInvalidLaunchToast(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        SetToast(message, ToastKind.Warning, isInvalidLaunchToast: true);
    }

    private bool ShouldSuppressInvalidLaunchToast(string? message)
    {
        return _isInvalidLaunchToastVisible
            && !string.IsNullOrWhiteSpace(message)
            && string.Equals(_toastMessageText, message, StringComparison.Ordinal);
    }

    private void SetToast(string? message, ToastKind kind, bool isInvalidLaunchToast)
    {
        _toastMessageText = string.IsNullOrWhiteSpace(message) ? null : message;
        _toastKind = _toastMessageText is null ? ToastKind.None : kind;
        _isInvalidLaunchToastVisible = isInvalidLaunchToast && _toastMessageText is not null;
        _toastSequence++;
        OnToastPresentationChanged();
    }

    private void ClearToast()
    {
        if (_toastKind == ToastKind.None && string.IsNullOrWhiteSpace(_toastMessageText))
        {
            return;
        }

        _toastMessageText = null;
        _toastKind = ToastKind.None;
        _isInvalidLaunchToastVisible = false;
        _toastSequence++;
        OnToastPresentationChanged();
    }

    private void OnToastPresentationChanged()
    {
        OnPropertyChanged(nameof(ToastMessageText));
        OnPropertyChanged(nameof(CurrentToastKind));
        OnPropertyChanged(nameof(HasToast));
        OnPropertyChanged(nameof(HasToastActions));
        OnPropertyChanged(nameof(IsPassiveToast));
        OnPropertyChanged(nameof(ToastBackground));
        OnPropertyChanged(nameof(ToastBorderBrush));
        OnPropertyChanged(nameof(ToastForeground));
        OnPropertyChanged(nameof(ToastSequence));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class SelectablePathRow
{
    public SelectablePathRow(string path, bool isSelected, bool isSelectionEnabled)
    {
        Path = path;
        IsSelected = isSelected;
        IsSelectionEnabled = isSelectionEnabled;
    }

    public string Path { get; }

    public bool IsSelected { get; }

    public bool IsSelectionEnabled { get; }

    public bool IsSelectionDisabled => !IsSelectionEnabled;
}

public sealed class ProfileListItem : INotifyPropertyChanged
{
    private bool _canLaunchProfile;
    private string _commandPreviewText;
    private bool _isInvalid;
    private bool _isInvalidReasonVisible;
    private bool _isCommandPreviewVisible;
    private bool _isRenameVisible;
    private bool _isSelected;
    private string _invalidReason;
    private string _name;
    private string _validMessage;

    public ProfileListItem(string id, string name)
    {
        Id = id;
        _commandPreviewText = string.Empty;
        _name = name;
        _invalidReason = string.Empty;
        _validMessage = string.Empty;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Id { get; }

    public string Name
    {
        get => _name;
        set
        {
            if (_name == value)
            {
                return;
            }

            _name = value;
            OnPropertyChanged();
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            OnPropertyChanged();
        }
    }

    public bool IsRenameVisible
    {
        get => _isRenameVisible;
        set
        {
            if (_isRenameVisible == value)
            {
                return;
            }

            _isRenameVisible = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsDisplayVisible));
        }
    }

    public bool IsDisplayVisible => !IsRenameVisible;

    public bool CanLaunchProfile
    {
        get => _canLaunchProfile;
        set
        {
            if (_canLaunchProfile == value)
            {
                return;
            }

            _canLaunchProfile = value;
            OnPropertyChanged();
        }
    }

    public string CommandPreviewText
    {
        get => _commandPreviewText;
        set
        {
            if (_commandPreviewText == value)
            {
                return;
            }

            _commandPreviewText = value;
            OnPropertyChanged();
        }
    }

    public bool IsCommandPreviewVisible
    {
        get => _isCommandPreviewVisible;
        set
        {
            if (_isCommandPreviewVisible == value)
            {
                return;
            }

            _isCommandPreviewVisible = value;
            OnPropertyChanged();
        }
    }

    public bool IsInvalid
    {
        get => _isInvalid;
        set
        {
            if (_isInvalid == value)
            {
                return;
            }

            _isInvalid = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasInvalidReason));
            OnPropertyChanged(nameof(HasValidMessage));
            OnPropertyChanged(nameof(HasStatusBadge));
            OnPropertyChanged(nameof(StatusBadgeText));
            OnPropertyChanged(nameof(StatusBadgeBackground));
            OnPropertyChanged(nameof(StatusBadgeForeground));
        }
    }

    public string InvalidReason
    {
        get => _invalidReason;
        set
        {
            if (_invalidReason == value)
            {
                return;
            }

            _invalidReason = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasInvalidReason));
        }
    }

    public bool IsInvalidReasonVisible
    {
        get => _isInvalidReasonVisible;
        set
        {
            if (_isInvalidReasonVisible == value)
            {
                return;
            }

            _isInvalidReasonVisible = value;
            OnPropertyChanged();
        }
    }

    public string ValidMessage
    {
        get => _validMessage;
        set
        {
            if (_validMessage == value)
            {
                return;
            }

            _validMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasValidMessage));
            OnPropertyChanged(nameof(HasStatusBadge));
            OnPropertyChanged(nameof(StatusBadgeText));
            OnPropertyChanged(nameof(StatusBadgeBackground));
            OnPropertyChanged(nameof(StatusBadgeForeground));
        }
    }

    public bool HasInvalidReason => IsInvalid && !string.IsNullOrWhiteSpace(InvalidReason);

    public bool HasValidMessage => !IsInvalid && !string.IsNullOrWhiteSpace(ValidMessage);

    public bool HasStatusBadge => IsInvalid || HasValidMessage;

    public string StatusBadgeText => IsInvalid ? "INVALID" : ValidMessage;

    public string StatusBadgeBackground => IsInvalid ? "#44311d" : "#113a2f";

    public string StatusBadgeForeground => IsInvalid ? "#f59e0b" : "#10b981";

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

internal readonly record struct ProfileValidity(bool IsValid, string Reason);

internal enum ProfileRenameMode
{
    None,
    Row,
    Header
}
