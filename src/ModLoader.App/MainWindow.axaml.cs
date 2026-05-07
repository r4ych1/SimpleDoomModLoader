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
    private Grid? _profileListHost;
    private ScrollViewer? _profileListScrollViewer;
    private ScrollViewer? _fileLibraryScrollViewer;
    private PathIcon? _profileListScrollAffordance;
    private PathIcon? _fileLibraryScrollAffordance;
    private bool _isProfileDragActive;
    private int? _profileDropIndex;
    private string? _pressedProfileId;
    private Point _pressedProfilePointInHost;
    private Border? _pressedProfileRow;

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
        PropertyChanged += OnWindowPropertyChanged;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _viewModel.SetWindowWidth(Width);
        DataContext = _viewModel;
        UpdateToastDismissTimer();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
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
        var wasCollapsed = _viewModel.IsFileLibraryPaneCollapsed;
        _viewModel.CreateNewProfile();
        ApplyWindowWidthForPaneStateTransition(wasCollapsed);
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

    private void OnToggleFileLibraryPaneCollapsedClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        CancelPendingProfileToggle();
        var wasCollapsed = _viewModel.IsFileLibraryPaneCollapsed;
        _viewModel.ToggleFileLibraryPaneCollapsed();
        ApplyWindowWidthForPaneStateTransition(wasCollapsed);
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
        if (_viewModel.IsFileLibraryPaneCollapsed)
        {
            ApplyWindowWidthForCurrentPaneState();
        }

        ScheduleScrollAffordanceUpdate();
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        PropertyChanged -= OnWindowPropertyChanged;
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
            var wasCollapsed = _viewModel.IsFileLibraryPaneCollapsed;
            if (sender is Border doubleClickedBorder
                && doubleClickedBorder.Tag is string doubleClickedProfileId
                && _viewModel.SelectProfileAndSetFileLibraryPaneCollapsed(
                    doubleClickedProfileId,
                    !wasCollapsed))
            {
                ApplyWindowWidthForPaneStateTransition(wasCollapsed);
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
            if (!HasExceededProfileDragThreshold(_pressedProfilePointInHost, pointInHost))
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

        if (sender is Border border && border.Tag is string path)
        {
            _viewModel.ToggleSourcePortSelection(path);
            e.Handled = true;
        }
    }

    private void OnIwadRowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (IsFromInteractiveChild(e.Source))
        {
            return;
        }

        if (sender is Border border && border.Tag is string path)
        {
            _viewModel.ToggleIwadSelection(path);
            e.Handled = true;
        }
    }

    private void OnModRowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (IsFromInteractiveChild(e.Source))
        {
            return;
        }

        if (sender is Border border && border.Tag is string path)
        {
            _viewModel.ToggleModSelection(path);
            e.Handled = true;
        }
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

    private bool IsActivePressedProfileRow(object? sender)
    {
        return sender is Border border
            && _pressedProfileRow is not null
            && ReferenceEquals(border, _pressedProfileRow);
    }

    private static bool HasExceededProfileDragThreshold(Point startPoint, Point currentPoint)
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
        _viewModel.RememberExpandedWindowWidth(e.NewSize.Width, WindowState == WindowState.Normal);
        ScheduleScrollAffordanceUpdate();
    }

    private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Window.WindowStateProperty && WindowState == WindowState.Normal)
        {
            ApplyWindowWidthForCurrentPaneState();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.Equals(e.PropertyName, nameof(MainWindowViewModel.ToastSequence), StringComparison.Ordinal))
        {
            UpdateToastDismissTimer();
        }
    }

    private void ApplyWindowWidthForPaneStateTransition(bool wasCollapsed)
    {
        if (WindowState != WindowState.Normal || wasCollapsed == _viewModel.IsFileLibraryPaneCollapsed)
        {
            return;
        }

        ApplyWindowWidthForCurrentPaneState();
    }

    private void ApplyWindowWidthForCurrentPaneState()
    {
        if (WindowState != WindowState.Normal)
        {
            return;
        }

        var targetWidth = _viewModel.IsFileLibraryPaneCollapsed
            ? _viewModel.PreferredCollapsedWindowWidth
            : _viewModel.PreferredExpandedWindowWidth;

        if (Math.Abs(Width - targetWidth) < 0.01d)
        {
            return;
        }

        Width = targetWidth;
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
            && (!requireExpandedFileLibraryPane || _viewModel.IsFileLibraryPaneExpanded);

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

    private readonly record struct ProfileDropTarget(int Index, double Left, double Top, double Width);

    private readonly record struct ProfileRowBounds(double Left, double Top, double Width, double Height)
    {
        public double Bottom => Top + Height;
    }
}
