using BatchRenamerAT.Models;
using BatchRenamerAT.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Windows.Storage.Pickers;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.Json;

namespace BatchRenamerAT.ViewModels;

// Ordered trigger list. Triggers run top to bottom, and the list can be rearranged by dragging
// or with the move commands.
public sealed partial class TriggerListViewModel : BaseViewModel
{
    private readonly LocalizationService _localizationService;
    private readonly TriggerDescriptionService _triggerDescriptionService;
    private readonly TriggerPresetService _triggerPresetService;

    public TriggerListViewModel(LocalizationService localizationService, TriggerDescriptionService triggerDescriptionService, TriggerPresetService triggerPresetService)
    {
        _localizationService = localizationService;
        _triggerDescriptionService = triggerDescriptionService;
        _triggerPresetService = triggerPresetService;

        Items.CollectionChanged += OnItemsCollectionChanged;
        UpdateItemState();
    }

    // Raised whenever triggers are added, removed, reordered, edited, or switched on or off.
    public event EventHandler? TriggersChanged;

    public event EventHandler<TriggerEditorRequestedEventArgs>? TriggerEditorRequested;

    // Supplies the item used for the example line of the trigger editor.
    public Func<RenameSource?>? SampleSourceProvider { get; set; }

    public ObservableCollection<TriggerItemViewModel> Items { get; } = [];

    [ObservableProperty]
    public partial string ItemCountText { get; private set; } = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteAllCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveTriggersCommand))]
    public partial bool HasItems { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    [NotifyCanExecuteChangedFor(nameof(EditCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveUpCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveDownCommand))]
    public partial int SelectedIndex { get; set; } = -1;

    public bool HasSelection => SelectedIndex >= 0 && SelectedIndex < Items.Count;

    private bool CanMoveUp => SelectedIndex > 0 && SelectedIndex < Items.Count;

    private bool CanMoveDown => SelectedIndex >= 0 && SelectedIndex < Items.Count - 1;

    public List<RenameTrigger> GetTriggers() =>
    [.. Items.Select(item => item.Trigger)];

    public async Task EditItemAsync(TriggerItemViewModel item)
    {
        var editedTrigger = await RequestTriggerEditorAsync(item.Trigger);
        if (editedTrigger is not null) item.Trigger = editedTrigger;
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        var trigger = await RequestTriggerEditorAsync(null);
        if (trigger is null) return;

        Items.Add(new TriggerItemViewModel(trigger, _triggerDescriptionService));
        SelectedIndex = Items.Count - 1;
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task EditAsync()
    {
        if (HasSelection)
        {
            await EditItemAsync(Items[SelectedIndex]);
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void Delete()
    {
        if (!HasSelection) return;

        var deletedIndex = SelectedIndex;
        Items.RemoveAt(deletedIndex);
        SelectedIndex = Math.Min(deletedIndex, Items.Count - 1);
    }

    [RelayCommand(CanExecute = nameof(HasItems))]
    private void DeleteAll()
    {
        for (var itemIndex = Items.Count - 1; itemIndex >= 0; itemIndex--)
        {
            Items.RemoveAt(itemIndex);
        }
    }

    [RelayCommand(CanExecute = nameof(CanMoveUp))]
    private void MoveUp() => MoveSelectedItem(-1);

    [RelayCommand(CanExecute = nameof(CanMoveDown))]
    private void MoveDown() => MoveSelectedItem(1);

    [RelayCommand(CanExecute = nameof(HasItems))]
    private async Task SaveTriggersAsync()
    {
        var fileTypeChoices = new Dictionary<string, IReadOnlyList<string>> { [_localizationService.GetLocalizedString("TriggerFileTypeLabel")] = [TriggerPresetService.FileExtension] };
        var pickFileResult = await SaveFileAsync(new FileSavePickerParameters(fileTypeChoices, _localizationService.GetLocalizedString("TriggerFileSuggestedName"), TriggerPresetService.FileExtension, PickerLocationId.DocumentsLibrary));
        if (pickFileResult is null) return;

        try { await _triggerPresetService.SaveAsync(pickFileResult.Path, GetTriggers()); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { await ShowErrorAsync("TriggerSaveFailedFormat", exception.Message); }
    }

    [RelayCommand]
    private async Task LoadTriggersAsync()
    {
        var pickFileResult = await PickFileAsync(new FileOpenPickerParameters([TriggerPresetService.FileExtension], PickerLocationId.DocumentsLibrary));
        if (pickFileResult is null) return;

        List<RenameTrigger> triggers;
        try { triggers = await _triggerPresetService.LoadAsync(pickFileResult.Path); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            await ShowErrorAsync("TriggerLoadFailedFormat", exception.Message);
            return;
        }

        for (var itemIndex = Items.Count - 1; itemIndex >= 0; itemIndex--) Items.RemoveAt(itemIndex);
        foreach (var trigger in triggers) Items.Add(new TriggerItemViewModel(trigger, _triggerDescriptionService));
        SelectedIndex = Items.Count > 0 ? 0 : -1;
    }

    private async Task<RenameTrigger?> RequestTriggerEditorAsync(RenameTrigger? trigger)
    {
        var dialogViewModel = new TriggerEditorDialogViewModel(_localizationService, trigger, SampleSourceProvider?.Invoke());
        var args = new TriggerEditorRequestedEventArgs(dialogViewModel);
        TriggerEditorRequested?.Invoke(this, args);
        if (args.ResultTask is null || !await args.ResultTask) return null;

        return dialogViewModel.CreateTrigger();
    }

    private void MoveSelectedItem(int offset)
    {
        var sourceIndex = SelectedIndex;
        var targetIndex = sourceIndex + offset;
        if (sourceIndex < 0 || targetIndex < 0 || targetIndex >= Items.Count) return;

        Items.Move(sourceIndex, targetIndex);
        SelectedIndex = targetIndex;
    }

    private async Task ShowErrorAsync(string formatResourceName, string errorMessage) => await ShowMessageDialogAsync(new MessageDialogParameters(_localizationService.GetLocalizedString("ErrorTitle"), _localizationService.GetFormattedString(formatResourceName, errorMessage)));

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (var oldItem in e.OldItems.OfType<TriggerItemViewModel>())
            {
                oldItem.PropertyChanged -= OnTriggerItemPropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (var newItem in e.NewItems.OfType<TriggerItemViewModel>())
            {
                newItem.PropertyChanged += OnTriggerItemPropertyChanged;
            }
        }

        UpdateItemState();
        TriggersChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnTriggerItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TriggerItemViewModel.Trigger))
        {
            TriggersChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void UpdateItemState()
    {
        for (var itemIndex = 0; itemIndex < Items.Count; itemIndex++) Items[itemIndex].Number = itemIndex + 1;
        HasItems = Items.Count > 0;
        ItemCountText = _localizationService.GetFormattedString("TriggerCountFormat", Items.Count);
        MoveUpCommand.NotifyCanExecuteChanged();
        MoveDownCommand.NotifyCanExecuteChanged();
        EditCommand.NotifyCanExecuteChanged();
        DeleteCommand.NotifyCanExecuteChanged();
    }
}
