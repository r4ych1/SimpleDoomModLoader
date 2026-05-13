using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ModLoader.Core;

namespace ModLoader.App;

public partial class MainWindow : Window
{
    private const double ProfileDragStartThreshold = 6d;
    private static readonly TimeSpan PassiveToastDuration = TimeSpan.FromSeconds(5);
    private const double ScrollAffordanceVisibilityEpsilon = 0.5d;
    private const double ScrollAffordanceVisibleOpacity = 0.72d;
    private static readonly TimeSpan CollapsedSelectedProfileToggleDelay = TimeSpan.FromMilliseconds(275);
    private readonly MainWindowViewModel _viewModel;
    private DispatcherTimer? _pendingProfileToggleTimer;
    private DispatcherTimer? _toastDismissTimer;
    private string? _pendingToggleProfileId;
    private Grid? _iwadListHost;
    private Grid? _modListHost;
    private Grid? _profileListHost;
    private Grid? _sourcePortListHost;
    private ScrollViewer? _profileListScrollViewer;
    private ScrollViewer? _fileLibraryScrollViewer;
    private PathIcon? _profileListScrollAffordance;
    private PathIcon? _fileLibraryScrollAffordance;
    private bool _isIwadDragActive;
    private bool _isProfileDragActive;
    private bool _isSourcePortDragActive;
    private bool _isModDragActive;
    private int? _iwadDropIndex;
    private int? _modDropIndex;
    private int? _profileDropIndex;
    private int? _sourcePortDropIndex;
    private string? _pressedIwadPath;
    private string? _pressedModPath;
    private string? _pressedProfileId;
    private string? _pressedSourcePortPath;
    private Point _pressedIwadPointInHost;
    private Point _pressedModPointInHost;
    private Point _pressedProfilePointInHost;
    private Point _pressedSourcePortPointInHost;
    private Border? _pressedIwadRow;
    private Border? _pressedModRow;
    private Border? _pressedProfileRow;
    private Border? _pressedSourcePortRow;

    public MainWindow()
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "modloader.config.json");
        var persistence = new JsonLaunchInputsPersistence(configPath);
        _viewModel = new MainWindowViewModel(persistence);

        InitializeComponent();
        AddHandler(InputElement.PointerPressedEvent, OnWindowPointerPressed, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        Opened += OnWindowOpened;
        Closed += OnWindowClosed;
        SizeChanged += OnWindowSizeChanged;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _viewModel.SetWindowWidth(Width);
        DataContext = _viewModel;
        UpdateToastDismissTimer();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _sourcePortListHost = this.FindControl<Grid>("SourcePortListHost");
        _iwadListHost = this.FindControl<Grid>("IwadListHost");
        _modListHost = this.FindControl<Grid>("ModListHost");
        _profileListHost = this.FindControl<Grid>("ProfileListHost");
        _profileListScrollViewer = this.FindControl<ScrollViewer>("ProfileListScrollViewer");
        _fileLibraryScrollViewer = this.FindControl<ScrollViewer>("FileLibraryScrollViewer");
        _profileListScrollAffordance = this.FindControl<PathIcon>("ProfileListScrollAffordance");
        _fileLibraryScrollAffordance = this.FindControl<PathIcon>("FileLibraryScrollAffordance");

        if (_profileListScrollViewer is not null)
        {
            _profileListScrollViewer.PropertyChanged += OnScrollViewerPropertyChanged;
        }

        if (_fileLibraryScrollViewer is not null)
        {
            _fileLibraryScrollViewer.PropertyChanged += OnScrollViewerPropertyChanged;
        }
    }

    private void OnDropZoneDragEnter(object? sender, DragEventArgs e)
    {
        UpdateDropZoneDragState(sender, e);
    }

    private void OnDropZoneDragOver(object? sender, DragEventArgs e)
    {
        UpdateDropZoneDragState(sender, e);
    }

    private void OnDropZoneDragLeave(object? sender, RoutedEventArgs e)
    {
        if (TryGetDropZoneKind(sender, out var kind))
        {
            _viewModel.SetDropZoneDragActive(kind, false);
        }

        e.Handled = true;
    }

    private void OnSourcePortDrop(object? sender, DragEventArgs e)
    {
        _viewModel.SetDropZoneDragActive(DropZoneKind.SourcePort, false);
        _viewModel.ProcessSourcePortDrop(ExtractDroppedPaths(e));
        e.Handled = true;
    }

    private void OnIwadDrop(object? sender, DragEventArgs e)
    {
        _viewModel.SetDropZoneDragActive(DropZoneKind.Iwad, false);
        _viewModel.ProcessIwadDrop(ExtractDroppedPaths(e));
        e.Handled = true;
    }

    private void OnModDrop(object? sender, DragEventArgs e)
    {
        _viewModel.SetDropZoneDragActive(DropZoneKind.Mod, false);
        _viewModel.ProcessModDrop(ExtractDroppedPaths(e));
        e.Handled = true;
    }

    private void OnNewProfileClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        CancelPendingProfileToggle();
        _viewModel.CreateNewProfile();
        ScheduleScrollAffordanceUpdate();
    }

    private void OnEditSelectedProfileClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        CancelPendingProfileToggle();
        _viewModel.BeginRenameSelectedProfile();
    }

    private void OnDeleteSelectedProfileClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        CancelPendingProfileToggle();
        _viewModel.RequestDeleteSelectedProfile();
    }

    private void OnDeleteProfileClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        CancelPendingProfileToggle();
        if (sender is Button button && button.Tag is string profileId)
        {
            _viewModel.RequestDeleteProfile(profileId);
        }
    }

    private void OnRenameProfileClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        CancelPendingProfileToggle();
        if (sender is Button button && button.Tag is string profileId)
        {
            _viewModel.BeginRenameProfile(profileId);
        }
    }

    private void OnLaunchProfileClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        CancelPendingProfileToggle();
        if (sender is Button button && button.Tag is string profileId)
        {
            _viewModel.LaunchProfile(profileId);
        }
    }

    private void OnConfirmDeleteProfileClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        CancelPendingProfileToggle();
        _viewModel.ConfirmDeleteProfile();
        ScheduleScrollAffordanceUpdate();
    }

    private void OnCancelDeleteProfileClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        CancelPendingProfileToggle();
        _viewModel.CancelDeleteConfirmation();
    }

    private void OnOpenFileLibraryViewClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        CancelPendingProfileToggle();
        _viewModel.ShowFileLibraryView();
        ScheduleScrollAffordanceUpdate();
    }

    private void OnReturnToProfilesViewClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        CancelPendingProfileToggle();
        _viewModel.ShowProfilesView();
        ScheduleScrollAffordanceUpdate();
    }

    private void OnToggleSourcePortSectionCollapsedClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _viewModel.ToggleSourcePortSectionCollapsed();
        ScheduleScrollAffordanceUpdate();
    }

    private void OnRemoveSourcePortClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string path)
        {
            _viewModel.RemoveSourcePort(path);
        }
    }

    private void OnRemoveIwadClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string path)
        {
            _viewModel.RemoveIwad(path);
        }
    }

    private void OnRemoveModClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string path)
        {
            _viewModel.RemoveMod(path);
        }
    }

    private void OnToggleIwadSectionCollapsedClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _viewModel.ToggleIwadSectionCollapsed();
        ScheduleScrollAffordanceUpdate();
    }

    private void OnToggleModSectionCollapsedClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _viewModel.ToggleModSectionCollapsed();
        ScheduleScrollAffordanceUpdate();
    }

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        ScheduleScrollAffordanceUpdate();
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        if (_fileLibraryScrollViewer is not null)
        {
            _fileLibraryScrollViewer.PropertyChanged -= OnScrollViewerPropertyChanged;
        }

        if (_profileListScrollViewer is not null)
        {
            _profileListScrollViewer.PropertyChanged -= OnScrollViewerPropertyChanged;
        }

        if (_toastDismissTimer is not null)
        {
            _toastDismissTimer.Stop();
            _toastDismissTimer.Tick -= OnToastDismissTimerTick;
        }
    }

    private void OnWindowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!_viewModel.HasActiveProfileRename)
        {
            return;
        }

        if (IsWithinActiveRenameTextBox(e.Source))
        {
            return;
        }

        if (_viewModel.CancelRename())
        {
            e.Handled = true;
        }
    }

    private void OnProfileRowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (IsFromInteractiveChild(e.Source))
        {
            return;
        }

        CancelPendingProfileToggle();

        if (e.ClickCount > 1)
        {
            if (sender is Border doubleClickedBorder
                && doubleClickedBorder.Tag is string doubleClickedProfileId
                && _viewModel.SelectProfileAndOpenFileLibraryView(doubleClickedProfileId))
            {
                ScheduleScrollAffordanceUpdate();
                e.Handled = true;
            }

            return;
        }

        if (sender is Border border
            && border.Tag is string profileId
            && TryGetProfileListPoint(e, out var pointInHost))
        {
            _pressedProfileRow = border;
            _pressedProfileId = profileId;
            _pressedProfilePointInHost = pointInHost;
            _isProfileDragActive = false;
            _profileDropIndex = null;
            e.Pointer.Capture(border);
            e.Handled = true;
        }
    }

    private void OnProfileRowPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!IsActivePressedProfileRow(sender) || _pressedProfileId is null || !TryGetProfileListPoint(e, out var pointInHost))
        {
            return;
        }

        if (!_isProfileDragActive)
        {
            if (!HasExceededDragThreshold(_pressedProfilePointInHost, pointInHost))
            {
                return;
            }

            if (!_viewModel.BeginProfileDrag(_pressedProfileId, pointInHost.X + 12d, pointInHost.Y + 12d))
            {
                ClearProfilePointerInteraction();
                e.Pointer.Capture(null);
                return;
            }

            _isProfileDragActive = true;
        }

        UpdateProfileDrag(pointInHost);
        e.Handled = true;
    }

    private void OnProfileRowPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!IsActivePressedProfileRow(sender))
        {
            return;
        }

        var releasedProfileId = _pressedProfileId;
        var wasDragActive = _isProfileDragActive;
        var dropIndex = _profileDropIndex;

        ClearProfilePointerInteraction();
        e.Pointer.Capture(null);

        if (releasedProfileId is null)
        {
            return;
        }

        if (wasDragActive)
        {
            if (dropIndex.HasValue)
            {
                _viewModel.ReorderProfile(releasedProfileId, dropIndex.Value);
            }

            _viewModel.HideProfileDragFeedback();
        }
        else
        {
            if (string.Equals(_viewModel.SelectedProfileId, releasedProfileId, StringComparison.Ordinal))
            {
                SchedulePendingProfileToggle(releasedProfileId);
            }
            else
            {
                _viewModel.ToggleProfileSelection(releasedProfileId);
            }
        }

        e.Handled = true;
    }

    private void OnProfileRowPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (!IsActivePressedProfileRow(sender))
        {
            return;
        }

        if (_isProfileDragActive)
        {
            _viewModel.HideProfileDragFeedback();
        }

        ClearProfilePointerInteraction();
    }

    private void CancelPendingProfileToggle()
    {
        if (_pendingProfileToggleTimer is not null)
        {
            _pendingProfileToggleTimer.Stop();
        }

        _pendingToggleProfileId = null;
    }

    private void SchedulePendingProfileToggle(string profileId)
    {
        if (_pendingProfileToggleTimer is null)
        {
            _pendingProfileToggleTimer = new DispatcherTimer
            {
                Interval = CollapsedSelectedProfileToggleDelay
            };
            _pendingProfileToggleTimer.Tick += OnPendingProfileToggleTick;
        }

        _pendingToggleProfileId = profileId;
        _pendingProfileToggleTimer.Stop();
        _pendingProfileToggleTimer.Start();
    }

    private void OnPendingProfileToggleTick(object? sender, EventArgs e)
    {
        _pendingProfileToggleTimer?.Stop();

        var pendingProfileId = _pendingToggleProfileId;
        _pendingToggleProfileId = null;

        if (pendingProfileId is null
            || !string.Equals(_viewModel.SelectedProfileId, pendingProfileId, StringComparison.Ordinal))
        {
            return;
        }

        _viewModel.ToggleProfileSelection(pendingProfileId);
    }

    private void OnProfileRenameKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox textBox || textBox.Tag is not string profileId)
        {
            return;
        }

        if (e.Key == Key.Enter)
        {
            _viewModel.CommitRename(profileId);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            _viewModel.CancelRename();
            e.Handled = true;
        }
    }

    private void OnProfileRenameLostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is TextBox textBox && textBox.Tag is string profileId && string.Equals(_viewModel.RenamingProfileId, profileId, StringComparison.Ordinal))
        {
            _viewModel.CancelRename();
        }
    }

    private void OnProfileRenameTextBoxLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            return;
        }

        textBox.Focus();
        textBox.SelectAll();
    }

    private void OnSourcePortRowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (IsFromInteractiveChild(e.Source))
        {
            return;
        }

        if (sender is Border border
            && border.Tag is string path
            && TryGetSourcePortListPoint(e, out var pointInHost))
        {
            _pressedSourcePortRow = border;
            _pressedSourcePortPath = path;
            _pressedSourcePortPointInHost = pointInHost;
            _isSourcePortDragActive = false;
            _sourcePortDropIndex = null;
            e.Pointer.Capture(border);
            e.Handled = true;
        }
    }

    private void OnSourcePortRowPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!IsActivePressedSourcePortRow(sender) || _pressedSourcePortPath is null || !TryGetSourcePortListPoint(e, out var pointInHost))
        {
            return;
        }

        if (!_isSourcePortDragActive)
        {
            if (!HasExceededDragThreshold(_pressedSourcePortPointInHost, pointInHost))
            {
                return;
            }

            if (!_viewModel.BeginSourcePortDrag(_pressedSourcePortPath, pointInHost.X + 12d, pointInHost.Y + 12d))
            {
                ClearSourcePortPointerInteraction();
                e.Pointer.Capture(null);
                return;
            }

            _isSourcePortDragActive = true;
        }

        UpdateSourcePortDrag(pointInHost);
        e.Handled = true;
    }

    private void OnSourcePortRowPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!IsActivePressedSourcePortRow(sender))
        {
            return;
        }

        var releasedPath = _pressedSourcePortPath;
        var wasDragActive = _isSourcePortDragActive;
        var dropIndex = _sourcePortDropIndex;

        ClearSourcePortPointerInteraction();
        e.Pointer.Capture(null);

        if (releasedPath is null)
        {
            return;
        }

        if (wasDragActive)
        {
            if (dropIndex.HasValue)
            {
                _viewModel.ReorderSourcePort(releasedPath, dropIndex.Value);
            }

            _viewModel.HideSourcePortDragFeedback();
        }
        else
        {
            _viewModel.ToggleSourcePortSelection(releasedPath);
        }

        e.Handled = true;
    }

    private void OnSourcePortRowPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (!IsActivePressedSourcePortRow(sender))
        {
            return;
        }

        if (_isSourcePortDragActive)
        {
            _viewModel.HideSourcePortDragFeedback();
        }

        ClearSourcePortPointerInteraction();
    }

    private void OnIwadRowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (IsFromInteractiveChild(e.Source))
        {
            return;
        }

        if (sender is Border border
            && border.Tag is string path
            && TryGetIwadListPoint(e, out var pointInHost))
        {
            _pressedIwadRow = border;
            _pressedIwadPath = path;
            _pressedIwadPointInHost = pointInHost;
            _isIwadDragActive = false;
            _iwadDropIndex = null;
            e.Pointer.Capture(border);
            e.Handled = true;
        }
    }

    private void OnIwadRowPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!IsActivePressedIwadRow(sender) || _pressedIwadPath is null || !TryGetIwadListPoint(e, out var pointInHost))
        {
            return;
        }

        if (!_isIwadDragActive)
        {
            if (!HasExceededDragThreshold(_pressedIwadPointInHost, pointInHost))
            {
                return;
            }

            if (!_viewModel.BeginIwadDrag(_pressedIwadPath, pointInHost.X + 12d, pointInHost.Y + 12d))
            {
                ClearIwadPointerInteraction();
                e.Pointer.Capture(null);
                return;
            }

            _isIwadDragActive = true;
        }

        UpdateIwadDrag(pointInHost);
        e.Handled = true;
    }

    private void OnIwadRowPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!IsActivePressedIwadRow(sender))
        {
            return;
        }

        var releasedPath = _pressedIwadPath;
        var wasDragActive = _isIwadDragActive;
        var dropIndex = _iwadDropIndex;

        ClearIwadPointerInteraction();
        e.Pointer.Capture(null);

        if (releasedPath is null)
        {
            return;
        }

        if (wasDragActive)
        {
            if (dropIndex.HasValue)
            {
                _viewModel.ReorderIwad(releasedPath, dropIndex.Value);
            }

            _viewModel.HideIwadDragFeedback();
        }
        else
        {
            _viewModel.ToggleIwadSelection(releasedPath);
        }

        e.Handled = true;
    }

    private void OnIwadRowPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (!IsActivePressedIwadRow(sender))
        {
            return;
        }

        if (_isIwadDragActive)
        {
            _viewModel.HideIwadDragFeedback();
        }

        ClearIwadPointerInteraction();
    }

    private void OnModRowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (IsFromInteractiveChild(e.Source))
        {
            return;
        }

        if (sender is Border border
            && border.Tag is string path
            && TryGetModListPoint(e, out var pointInHost))
        {
            _pressedModRow = border;
            _pressedModPath = path;
            _pressedModPointInHost = pointInHost;
            _isModDragActive = false;
            _modDropIndex = null;
            e.Pointer.Capture(border);
            e.Handled = true;
        }
    }

    private void OnModRowPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!IsActivePressedModRow(sender) || _pressedModPath is null || !TryGetModListPoint(e, out var pointInHost))
        {
            return;
        }

        if (!_isModDragActive)
        {
            if (!HasExceededDragThreshold(_pressedModPointInHost, pointInHost))
            {
                return;
            }

            if (!_viewModel.BeginModDrag(_pressedModPath, pointInHost.X + 12d, pointInHost.Y + 12d))
            {
                ClearModPointerInteraction();
                e.Pointer.Capture(null);
                return;
            }

            _isModDragActive = true;
        }

        UpdateModDrag(pointInHost);
        e.Handled = true;
    }

    private void OnModRowPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!IsActivePressedModRow(sender))
        {
            return;
        }

        var releasedModPath = _pressedModPath;
        var wasDragActive = _isModDragActive;
        var dropIndex = _modDropIndex;

        ClearModPointerInteraction();
        e.Pointer.Capture(null);

        if (releasedModPath is null)
        {
            return;
        }

        if (wasDragActive)
        {
            if (dropIndex.HasValue)
            {
                _viewModel.ReorderMod(releasedModPath, dropIndex.Value);
            }

            _viewModel.HideModDragFeedback();
        }
        else
        {
            _viewModel.ToggleModSelection(releasedModPath);
        }

        e.Handled = true;
    }

    private void OnModRowPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (!IsActivePressedModRow(sender))
        {
            return;
        }

        if (_isModDragActive)
        {
            _viewModel.HideModDragFeedback();
        }

        ClearModPointerInteraction();
    }

    private static bool HasFilePayload(DragEventArgs e)
    {
        return e.Data.Contains(DataFormats.Files);
    }

    private static IReadOnlyList<string> ExtractDroppedPaths(DragEventArgs e)
    {
        var droppedItems = e.Data.GetFiles();
        if (droppedItems is null)
        {
            return Array.Empty<string>();
        }

        var localPaths = new List<string>();
        foreach (var droppedItem in droppedItems)
        {
            var localPath = droppedItem.Path.LocalPath;
            if (!string.IsNullOrWhiteSpace(localPath))
            {
                localPaths.Add(localPath);
            }
        }

        return localPaths;
    }

    private static bool IsFromInteractiveChild(object? source)
    {
        if (source is Button or TextBox)
        {
            return true;
        }

        if (source is not Avalonia.Visual visual)
        {
            return false;
        }

        foreach (var ancestor in visual.GetVisualAncestors())
        {
            if (ancestor is Button or TextBox)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsWithinActiveRenameTextBox(object? source)
    {
        var activeRenameProfileId = _viewModel.RenamingProfileId;
        if (string.IsNullOrWhiteSpace(activeRenameProfileId))
        {
            return false;
        }

        return TryGetAncestorTextBox(source, out var textBox)
            && textBox.Tag is string profileId
            && string.Equals(profileId, activeRenameProfileId, StringComparison.Ordinal);
    }

    private static bool TryGetAncestorTextBox(object? source, out TextBox textBox)
    {
        if (source is TextBox directTextBox)
        {
            textBox = directTextBox;
            return true;
        }

        if (source is Avalonia.Visual visual)
        {
            foreach (var ancestor in visual.GetVisualAncestors())
            {
                if (ancestor is TextBox ancestorTextBox)
                {
                    textBox = ancestorTextBox;
                    return true;
                }
            }
        }

        textBox = null!;
        return false;
    }

    private void UpdateProfileDrag(Point pointInHost)
    {
        _viewModel.UpdateProfileDragGhostPosition(pointInHost.X + 12d, pointInHost.Y + 12d);

        if (TryGetProfileDropTarget(pointInHost, out var dropTarget))
        {
            _profileDropIndex = dropTarget.Index;
            _viewModel.ShowProfileDropIndicator(dropTarget.Left, dropTarget.Top, dropTarget.Width);
        }
        else
        {
            _profileDropIndex = null;
            _viewModel.HideProfileDropIndicator();
        }
    }

    private void UpdateSourcePortDrag(Point pointInHost)
    {
        _viewModel.UpdateSourcePortDragGhostPosition(pointInHost.X + 12d, pointInHost.Y + 12d);

        if (TryGetSourcePortDropTarget(pointInHost, out var dropTarget))
        {
            _sourcePortDropIndex = dropTarget.Index;
            _viewModel.ShowSourcePortDropIndicator(dropTarget.Left, dropTarget.Top, dropTarget.Width);
        }
        else
        {
            _sourcePortDropIndex = null;
            _viewModel.HideSourcePortDropIndicator();
        }
    }

    private void UpdateIwadDrag(Point pointInHost)
    {
        _viewModel.UpdateIwadDragGhostPosition(pointInHost.X + 12d, pointInHost.Y + 12d);

        if (TryGetIwadDropTarget(pointInHost, out var dropTarget))
        {
            _iwadDropIndex = dropTarget.Index;
            _viewModel.ShowIwadDropIndicator(dropTarget.Left, dropTarget.Top, dropTarget.Width);
        }
        else
        {
            _iwadDropIndex = null;
            _viewModel.HideIwadDropIndicator();
        }
    }

    private void UpdateModDrag(Point pointInHost)
    {
        _viewModel.UpdateModDragGhostPosition(pointInHost.X + 12d, pointInHost.Y + 12d);

        if (TryGetModDropTarget(pointInHost, out var dropTarget))
        {
            _modDropIndex = dropTarget.Index;
            _viewModel.ShowModDropIndicator(dropTarget.Left, dropTarget.Top, dropTarget.Width);
        }
        else
        {
            _modDropIndex = null;
            _viewModel.HideModDropIndicator();
        }
    }

    private bool TryGetProfileListPoint(PointerEventArgs e, out Point pointInHost)
    {
        if (_profileListHost is null)
        {
            pointInHost = default;
            return false;
        }

        pointInHost = e.GetPosition(_profileListHost);
        return true;
    }

    private bool TryGetSourcePortListPoint(PointerEventArgs e, out Point pointInHost)
    {
        if (_sourcePortListHost is null)
        {
            pointInHost = default;
            return false;
        }

        pointInHost = e.GetPosition(_sourcePortListHost);
        return true;
    }

    private bool TryGetIwadListPoint(PointerEventArgs e, out Point pointInHost)
    {
        if (_iwadListHost is null)
        {
            pointInHost = default;
            return false;
        }

        pointInHost = e.GetPosition(_iwadListHost);
        return true;
    }

    private bool TryGetModListPoint(PointerEventArgs e, out Point pointInHost)
    {
        if (_modListHost is null)
        {
            pointInHost = default;
            return false;
        }

        pointInHost = e.GetPosition(_modListHost);
        return true;
    }

    private bool TryGetProfileDropTarget(Point pointInHost, out ProfileDropTarget dropTarget)
    {
        dropTarget = default;

        if (_profileListHost is null
            || pointInHost.X < 0
            || pointInHost.Y < 0
            || pointInHost.X > _profileListHost.Bounds.Width
            || pointInHost.Y > _profileListHost.Bounds.Height)
        {
            return false;
        }

        var rowBounds = GetVisibleProfileRowBounds();
        if (rowBounds.Count == 0)
        {
            return false;
        }

        var firstRow = rowBounds[0];
        if (pointInHost.Y <= firstRow.Top)
        {
            dropTarget = new ProfileDropTarget(0, firstRow.Left, firstRow.Top, firstRow.Width);
            return true;
        }

        for (var i = 0; i < rowBounds.Count; i++)
        {
            var row = rowBounds[i];
            if (pointInHost.Y > row.Bottom)
            {
                continue;
            }

            var beforeRow = pointInHost.Y < row.Top + (row.Height / 2d);
            var targetIndex = beforeRow ? i : i + 1;
            var markerTop = beforeRow
                ? row.Top
                : i == rowBounds.Count - 1
                    ? row.Bottom
                    : rowBounds[i + 1].Top;
            var markerRow = beforeRow || i == rowBounds.Count - 1 ? row : rowBounds[i + 1];

            dropTarget = new ProfileDropTarget(targetIndex, markerRow.Left, markerTop, markerRow.Width);
            return true;
        }

        var lastRow = rowBounds[^1];
        dropTarget = new ProfileDropTarget(rowBounds.Count, lastRow.Left, lastRow.Bottom, lastRow.Width);
        return true;
    }

    private bool TryGetSourcePortDropTarget(Point pointInHost, out SourcePortDropTarget dropTarget)
    {
        dropTarget = default;

        if (_sourcePortListHost is null
            || pointInHost.X < 0
            || pointInHost.Y < 0
            || pointInHost.X > _sourcePortListHost.Bounds.Width
            || pointInHost.Y > _sourcePortListHost.Bounds.Height)
        {
            return false;
        }

        var rowBounds = GetVisibleSourcePortRowBounds();
        if (rowBounds.Count == 0)
        {
            return false;
        }

        var firstRow = rowBounds[0];
        if (pointInHost.Y <= firstRow.Top)
        {
            dropTarget = new SourcePortDropTarget(0, firstRow.Left, firstRow.Top, firstRow.Width);
            return true;
        }

        for (var i = 0; i < rowBounds.Count; i++)
        {
            var row = rowBounds[i];
            if (pointInHost.Y > row.Bottom)
            {
                continue;
            }

            var beforeRow = pointInHost.Y < row.Top + (row.Height / 2d);
            var targetIndex = beforeRow ? i : i + 1;
            var markerTop = beforeRow
                ? row.Top
                : i == rowBounds.Count - 1
                    ? row.Bottom
                    : rowBounds[i + 1].Top;
            var markerRow = beforeRow || i == rowBounds.Count - 1 ? row : rowBounds[i + 1];

            dropTarget = new SourcePortDropTarget(targetIndex, markerRow.Left, markerTop, markerRow.Width);
            return true;
        }

        var lastRow = rowBounds[^1];
        dropTarget = new SourcePortDropTarget(rowBounds.Count, lastRow.Left, lastRow.Bottom, lastRow.Width);
        return true;
    }

    private bool TryGetIwadDropTarget(Point pointInHost, out IwadDropTarget dropTarget)
    {
        dropTarget = default;

        if (_iwadListHost is null
            || pointInHost.X < 0
            || pointInHost.Y < 0
            || pointInHost.X > _iwadListHost.Bounds.Width
            || pointInHost.Y > _iwadListHost.Bounds.Height)
        {
            return false;
        }

        var rowBounds = GetVisibleIwadRowBounds();
        if (rowBounds.Count == 0)
        {
            return false;
        }

        var firstRow = rowBounds[0];
        if (pointInHost.Y <= firstRow.Top)
        {
            dropTarget = new IwadDropTarget(0, firstRow.Left, firstRow.Top, firstRow.Width);
            return true;
        }

        for (var i = 0; i < rowBounds.Count; i++)
        {
            var row = rowBounds[i];
            if (pointInHost.Y > row.Bottom)
            {
                continue;
            }

            var beforeRow = pointInHost.Y < row.Top + (row.Height / 2d);
            var targetIndex = beforeRow ? i : i + 1;
            var markerTop = beforeRow
                ? row.Top
                : i == rowBounds.Count - 1
                    ? row.Bottom
                    : rowBounds[i + 1].Top;
            var markerRow = beforeRow || i == rowBounds.Count - 1 ? row : rowBounds[i + 1];

            dropTarget = new IwadDropTarget(targetIndex, markerRow.Left, markerTop, markerRow.Width);
            return true;
        }

        var lastRow = rowBounds[^1];
        dropTarget = new IwadDropTarget(rowBounds.Count, lastRow.Left, lastRow.Bottom, lastRow.Width);
        return true;
    }

    private bool TryGetModDropTarget(Point pointInHost, out ModDropTarget dropTarget)
    {
        dropTarget = default;

        if (_modListHost is null
            || pointInHost.X < 0
            || pointInHost.Y < 0
            || pointInHost.X > _modListHost.Bounds.Width
            || pointInHost.Y > _modListHost.Bounds.Height)
        {
            return false;
        }

        var rowBounds = GetVisibleModRowBounds();
        if (rowBounds.Count == 0)
        {
            return false;
        }

        var firstRow = rowBounds[0];
        if (pointInHost.Y <= firstRow.Top)
        {
            dropTarget = new ModDropTarget(0, firstRow.Left, firstRow.Top, firstRow.Width);
            return true;
        }

        for (var i = 0; i < rowBounds.Count; i++)
        {
            var row = rowBounds[i];
            if (pointInHost.Y > row.Bottom)
            {
                continue;
            }

            var beforeRow = pointInHost.Y < row.Top + (row.Height / 2d);
            var targetIndex = beforeRow ? i : i + 1;
            var markerTop = beforeRow
                ? row.Top
                : i == rowBounds.Count - 1
                    ? row.Bottom
                    : rowBounds[i + 1].Top;
            var markerRow = beforeRow || i == rowBounds.Count - 1 ? row : rowBounds[i + 1];

            dropTarget = new ModDropTarget(targetIndex, markerRow.Left, markerTop, markerRow.Width);
            return true;
        }

        var lastRow = rowBounds[^1];
        dropTarget = new ModDropTarget(rowBounds.Count, lastRow.Left, lastRow.Bottom, lastRow.Width);
        return true;
    }

    private List<ProfileRowBounds> GetVisibleProfileRowBounds()
    {
        if (_profileListHost is null || _profileListScrollViewer is null)
        {
            return [];
        }

        return
        [
            .. _profileListScrollViewer.GetVisualDescendants()
                .OfType<Border>()
                .Where(border => border.DataContext is ProfileListItem && border.Classes.Contains("InputRow"))
                .Select(border => new { Border = border, TopLeft = border.TranslatePoint(new Point(0, 0), _profileListHost) })
                .Where(item => item.TopLeft.HasValue)
                .Select(item => new ProfileRowBounds(
                    item.TopLeft!.Value.X,
                    item.TopLeft.Value.Y,
                    item.Border.Bounds.Width,
                    item.Border.Bounds.Height))
                .OrderBy(row => row.Top)
        ];
    }

    private List<SourcePortRowBounds> GetVisibleSourcePortRowBounds()
    {
        if (_sourcePortListHost is null)
        {
            return [];
        }

        return
        [
            .. _sourcePortListHost.GetVisualDescendants()
                .OfType<Border>()
                .Where(border => border.DataContext is SelectablePathRow && border.Classes.Contains("InputRow"))
                .Select(border => new { Border = border, TopLeft = border.TranslatePoint(new Point(0, 0), _sourcePortListHost) })
                .Where(item => item.TopLeft.HasValue)
                .Select(item => new SourcePortRowBounds(
                    item.TopLeft!.Value.X,
                    item.TopLeft.Value.Y,
                    item.Border.Bounds.Width,
                    item.Border.Bounds.Height))
                .OrderBy(row => row.Top)
        ];
    }

    private List<IwadRowBounds> GetVisibleIwadRowBounds()
    {
        if (_iwadListHost is null)
        {
            return [];
        }

        return
        [
            .. _iwadListHost.GetVisualDescendants()
                .OfType<Border>()
                .Where(border => border.DataContext is SelectablePathRow && border.Classes.Contains("InputRow"))
                .Select(border => new { Border = border, TopLeft = border.TranslatePoint(new Point(0, 0), _iwadListHost) })
                .Where(item => item.TopLeft.HasValue)
                .Select(item => new IwadRowBounds(
                    item.TopLeft!.Value.X,
                    item.TopLeft.Value.Y,
                    item.Border.Bounds.Width,
                    item.Border.Bounds.Height))
                .OrderBy(row => row.Top)
        ];
    }

    private List<ModRowBounds> GetVisibleModRowBounds()
    {
        if (_modListHost is null)
        {
            return [];
        }

        return
        [
            .. _modListHost.GetVisualDescendants()
                .OfType<Border>()
                .Where(border => border.DataContext is SelectablePathRow && border.Classes.Contains("InputRow"))
                .Select(border => new { Border = border, TopLeft = border.TranslatePoint(new Point(0, 0), _modListHost) })
                .Where(item => item.TopLeft.HasValue)
                .Select(item => new ModRowBounds(
                    item.TopLeft!.Value.X,
                    item.TopLeft.Value.Y,
                    item.Border.Bounds.Width,
                    item.Border.Bounds.Height))
                .OrderBy(row => row.Top)
        ];
    }

    private bool IsActivePressedProfileRow(object? sender)
    {
        return sender is Border border
            && _pressedProfileRow is not null
            && ReferenceEquals(border, _pressedProfileRow);
    }

    private bool IsActivePressedSourcePortRow(object? sender)
    {
        return sender is Border border
            && _pressedSourcePortRow is not null
            && ReferenceEquals(border, _pressedSourcePortRow);
    }

    private bool IsActivePressedIwadRow(object? sender)
    {
        return sender is Border border
            && _pressedIwadRow is not null
            && ReferenceEquals(border, _pressedIwadRow);
    }

    private bool IsActivePressedModRow(object? sender)
    {
        return sender is Border border
            && _pressedModRow is not null
            && ReferenceEquals(border, _pressedModRow);
    }

    private static bool HasExceededDragThreshold(Point startPoint, Point currentPoint)
    {
        return Math.Abs(currentPoint.X - startPoint.X) >= ProfileDragStartThreshold
            || Math.Abs(currentPoint.Y - startPoint.Y) >= ProfileDragStartThreshold;
    }

    private void UpdateDropZoneDragState(object? sender, DragEventArgs e)
    {
        var hasFilePayload = HasFilePayload(e);
        e.DragEffects = hasFilePayload ? DragDropEffects.Copy : DragDropEffects.None;

        if (TryGetDropZoneKind(sender, out var kind))
        {
            _viewModel.SetDropZoneDragActive(kind, hasFilePayload);
        }

        e.Handled = true;
    }

    private static bool TryGetDropZoneKind(object? sender, out DropZoneKind kind)
    {
        if (sender is Border border
            && border.Tag is string tag
            && Enum.TryParse(tag, ignoreCase: false, out kind))
        {
            return true;
        }

        kind = default;
        return false;
    }

    private void OnWindowSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        _viewModel.SetWindowWidth(e.NewSize.Width);
        ScheduleScrollAffordanceUpdate();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.Equals(e.PropertyName, nameof(MainWindowViewModel.ToastSequence), StringComparison.Ordinal))
        {
            UpdateToastDismissTimer();
        }
    }

    private void OnScrollViewerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ScrollViewer.OffsetProperty
            || e.Property == ScrollViewer.ExtentProperty
            || e.Property == ScrollViewer.ViewportProperty)
        {
            ScheduleScrollAffordanceUpdate();
        }
    }

    private void ScheduleScrollAffordanceUpdate()
    {
        Dispatcher.UIThread.Post(UpdateScrollAffordances, DispatcherPriority.Background);
    }

    private void UpdateToastDismissTimer()
    {
        if (!_viewModel.IsPassiveToast)
        {
            _toastDismissTimer?.Stop();
            return;
        }

        if (_toastDismissTimer is null)
        {
            _toastDismissTimer = new DispatcherTimer
            {
                Interval = PassiveToastDuration
            };
            _toastDismissTimer.Tick += OnToastDismissTimerTick;
        }

        _toastDismissTimer.Stop();
        _toastDismissTimer.Start();
    }

    private void OnToastDismissTimerTick(object? sender, EventArgs e)
    {
        _toastDismissTimer?.Stop();
        _viewModel.DismissPassiveToast();
    }

    private void UpdateScrollAffordances()
    {
        UpdateScrollAffordance(_profileListScrollViewer, _profileListScrollAffordance, requireExpandedFileLibraryPane: false);
        UpdateScrollAffordance(_fileLibraryScrollViewer, _fileLibraryScrollAffordance, requireExpandedFileLibraryPane: true);
    }

    private void UpdateScrollAffordance(
        ScrollViewer? scrollViewer,
        PathIcon? affordance,
        bool requireExpandedFileLibraryPane)
    {
        if (scrollViewer is null || affordance is null)
        {
            return;
        }

        var overflowBelow = scrollViewer.Extent.Height - (scrollViewer.Offset.Y + scrollViewer.Viewport.Height);
        var hasOverflowBelow = overflowBelow > ScrollAffordanceVisibilityEpsilon;
        var shouldShow = scrollViewer.IsVisible
            && hasOverflowBelow
            && (!requireExpandedFileLibraryPane || _viewModel.IsFileLibraryViewActive);

        affordance.Opacity = shouldShow
            ? ScrollAffordanceVisibleOpacity
            : 0d;
    }

    private void ClearProfilePointerInteraction()
    {
        _pressedProfileRow = null;
        _pressedProfileId = null;
        _profileDropIndex = null;
        _isProfileDragActive = false;
    }

    private void ClearSourcePortPointerInteraction()
    {
        _pressedSourcePortRow = null;
        _pressedSourcePortPath = null;
        _sourcePortDropIndex = null;
        _isSourcePortDragActive = false;
    }

    private void ClearIwadPointerInteraction()
    {
        _pressedIwadRow = null;
        _pressedIwadPath = null;
        _iwadDropIndex = null;
        _isIwadDragActive = false;
    }

    private void ClearModPointerInteraction()
    {
        _pressedModRow = null;
        _pressedModPath = null;
        _modDropIndex = null;
        _isModDragActive = false;
    }

    private readonly record struct SourcePortDropTarget(int Index, double Left, double Top, double Width);

    private readonly record struct IwadDropTarget(int Index, double Left, double Top, double Width);

    private readonly record struct ModDropTarget(int Index, double Left, double Top, double Width);

    private readonly record struct ProfileDropTarget(int Index, double Left, double Top, double Width);

    private readonly record struct SourcePortRowBounds(double Left, double Top, double Width, double Height)
    {
        public double Bottom => Top + Height;
    }

    private readonly record struct IwadRowBounds(double Left, double Top, double Width, double Height)
    {
        public double Bottom => Top + Height;
    }

    private readonly record struct ModRowBounds(double Left, double Top, double Width, double Height)
    {
        public double Bottom => Top + Height;
    }

    private readonly record struct ProfileRowBounds(double Left, double Top, double Width, double Height)
    {
        public double Bottom => Top + Height;
    }
}
