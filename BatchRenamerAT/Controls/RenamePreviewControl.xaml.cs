using BatchRenamerAT.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BatchRenamerAT.Controls;

public sealed partial class RenamePreviewControl : BaseControl
{
    public RenamePreviewControl()
    {
        ViewModel = App.Services.GetRequiredService<RenamePreviewViewModel>();

        InitializeComponent();
    }

    public override RenamePreviewViewModel ViewModel { get; }
}
