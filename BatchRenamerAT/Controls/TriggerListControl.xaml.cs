using BatchRenamerAT.Dialogs;
using BatchRenamerAT.Helpers;
using BatchRenamerAT.Models;
using BatchRenamerAT.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace BatchRenamerAT.Controls;

public sealed partial class TriggerListControl : BaseControl
{
    public TriggerListControl()
    {
        ViewModel = App.Services.GetRequiredService<TriggerListViewModel>();

        InitializeComponent();
    }

    public override TriggerListViewModel ViewModel { get; }

    protected override void SubscribeViewModelEvents()
    {
        base.SubscribeViewModelEvents();

        ViewModel.TriggerEditorRequested += OnViewModelTriggerEditorRequested;
    }

    protected override void UnsubscribeViewModelEvents()
    {
        ViewModel.TriggerEditorRequested -= OnViewModelTriggerEditorRequested;

        base.UnsubscribeViewModelEvents();
    }

    private void OnViewModelTriggerEditorRequested(object? _, TriggerEditorRequestedEventArgs args) => args.ResultTask = ShowTriggerEditorDialogAsync(args.DialogViewModel);

    private async Task<bool> ShowTriggerEditorDialogAsync(TriggerEditorDialogViewModel dialogViewModel) => await this.ShowContentDialogAsync(new TriggerEditorDialog(dialogViewModel)) == ContentDialogResult.Primary;

    private async void OnTriggerListViewDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if ((e.OriginalSource as FrameworkElement)?.DataContext is not TriggerItemViewModel triggerItemViewModel) return;

        e.Handled = true;
        await ViewModel.EditItemAsync(triggerItemViewModel);
    }

    private void OnDeleteKeyboardAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args) => ExecuteKeyboardCommand(ViewModel.DeleteCommand, args);

    private void OnEditKeyboardAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args) => ExecuteKeyboardCommand(ViewModel.EditCommand, args);

    private void OnMoveUpKeyboardAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args) => ExecuteKeyboardCommand(ViewModel.MoveUpCommand, args);

    private void OnMoveDownKeyboardAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args) => ExecuteKeyboardCommand(ViewModel.MoveDownCommand, args);

    private static void ExecuteKeyboardCommand(System.Windows.Input.ICommand command, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        if (command.CanExecute(null)) command.Execute(null);
    }
}
