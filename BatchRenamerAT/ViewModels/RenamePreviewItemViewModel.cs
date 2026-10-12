using BatchRenamerAT.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BatchRenamerAT.ViewModels;

public sealed partial class RenamePreviewItemViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string OriginalName { get; set; } = "";

    [ObservableProperty]
    public partial string NewName { get; set; } = "";

    [ObservableProperty]
    public partial string StatusText { get; set; } = "";

    // Error details shown as a tooltip on the status, such as the reason a rename failed.
    [ObservableProperty]
    public partial string? DetailText { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsError))]
    [NotifyPropertyChangedFor(nameof(IsSuccess))]
    [NotifyPropertyChangedFor(nameof(IsNeutral))]
    [NotifyPropertyChangedFor(nameof(NewNameOpacity))]
    public partial RenamePreviewStatus Status { get; set; }

    public bool IsError => Status is not (RenamePreviewStatus.Changed or RenamePreviewStatus.Unchanged or RenamePreviewStatus.Renamed);

    public bool IsSuccess => Status == RenamePreviewStatus.Renamed;

    public bool IsNeutral => !IsError && !IsSuccess;

    public double NewNameOpacity => Status == RenamePreviewStatus.Unchanged ? 0.6 : 1;
}
