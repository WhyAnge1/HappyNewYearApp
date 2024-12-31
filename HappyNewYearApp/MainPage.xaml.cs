using HappyNewYearApp.Telegram;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace HappyNewYearApp;

public partial class MainPage : ContentPage
{
    private readonly TelegramManager _telegramManager = new();
    private TaskCompletionSource _verificationTCS;
    private IEnumerable<Dialog> _filteredDialogs = new List<Dialog>();
    private Dictionary<string, List<long>> _savedSelections = new();

    public ObservableCollection<Dialog> Dialogs { get; private set; } = new();

    public int UsersCount { get; private set; }

    public int ChatsCount { get; private set; }

    public MainPage()
    {
        InitializeComponent();

        FilterPicker.SelectedIndex = 0;
    }

    private async void LoginButtonClicked(object sender, EventArgs e)
    {
        _verificationTCS?.TrySetCanceled();
        _verificationTCS = new();
        
        var result = await _telegramManager.DoLogin(OnVerificationStep);

        if (result)
        {
            await DisplayAlert("Success", "You logged in into account", "Ok");
            
            ShowLoader();

            LoginFieldsStack.IsVisible = false;
            VerificationFieldsStack.IsVisible = false;

            var dialogs = await _telegramManager.GetDialogs();
            if (dialogs != null)
            {
                Dialogs = new ObservableCollection<Dialog>(dialogs);
                _filteredDialogs = Dialogs;
                ChatsCountLabel.Text = $"Groups: {Dialogs.Count(x => !x.IsUser).ToString()}";
                UsersCountLabel.Text = $"Users: {Dialogs.Count(x => x.IsUser).ToString()}";
                DialogsGrid.IsVisible = true;
                DialogsListView.ItemsSource = Dialogs;
                LoadSelections();
            }

            HideLoader();
        }
        else
        {
            await DisplayAlert("Fail", "You failed to login into account", "Ok");
            LoginFieldsStack.IsVisible = true;
            VerificationFieldsStack.IsVisible = false;
        }
    }

    private async Task<string> OnVerificationStep(string verificationStepCode)
    {
        VerificationFieldsStack.IsVisible = true;
        LoginFieldsStack.IsVisible = false;
        VerificationInfoLabel.Text = verificationStepCode;
        VerificationEntry.Text = string.Empty;
        VerificationEntry.IsPassword = verificationStepCode == "password";

        await _verificationTCS.Task;
        _verificationTCS = new();

        return VerificationEntry.Text;
    }

    private void VerificationButtonClicked(object sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(VerificationEntry.Text))
        {
            _verificationTCS.TrySetResult();
        }
    }

    private async void MessageButtonClicked(object sender, EventArgs e)
    {
        var selectedDialogs = Dialogs.Where(x => x.IsSelected).ToList();
        var message = await DisplayPromptAsync("Send", $"Enter message to send to {selectedDialogs.Count} chats", "Send", "Cancel");

        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        var sendResult = await _telegramManager.SendMessages(message, selectedDialogs);

        if (sendResult)
        {
            await DisplayAlert("Success", $"All {selectedDialogs.Count} were send!", "Ok");
        }
    }

    private void DialogsSearchBarSearchButtonPressed(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(DialogsSearchBar.Text))
        {
            DialogsListView.ItemsSource = _filteredDialogs;
            return;
        }

        DialogsListView.ItemsSource = _filteredDialogs.Where(x => (x.PublicName?.Contains(DialogsSearchBar.Text, StringComparison.InvariantCultureIgnoreCase) ?? false) || (x.Username?.Contains(DialogsSearchBar.Text, StringComparison.InvariantCultureIgnoreCase) ?? false));
    }

    private void FilterPickerSelectedIndexChanged(object sender, EventArgs e)
    {
        if (FilterPicker.SelectedItem is not null)
        {
            var selectedFilter = FilterPicker.SelectedItem.ToString();

            _filteredDialogs = selectedFilter switch
            {
                "All" => Dialogs,
                "Users" => Dialogs.Where(x => x.IsUser),
                "Groups" => Dialogs.Where(x => !x.IsUser),
                "Selected" => Dialogs.Where(x => x.IsSelected),
                "Unselected" => Dialogs.Where(x => !x.IsSelected),
                _ => Dialogs,
            };

            DialogsListView.ItemsSource = _filteredDialogs;
        }
    }

    private async void SaveConfigButtonClicked(object sender, EventArgs e)
    {
        var selectedDialogs = Dialogs.Where(x => x.IsSelected).ToList();
        var configName = await DisplayPromptAsync("Save", $"You selected {selectedDialogs.Count} chats. Enter a config name that will be saved", "Save", "Cancel");

        if (string.IsNullOrEmpty(configName))
        {
            return;
        }

        var selectedIds = Dialogs.Where(x => x.IsSelected).Select(x => x.Id).ToList();
        
        if (_savedSelections.ContainsKey(configName))
        {
            _savedSelections[configName] = selectedIds;
        }
        else
        {
            _savedSelections.Add(configName, selectedIds);
        }
        
        SaveSelections();
    }

    private async void RestoreConfigButtonClicked(object sender, EventArgs e)
    {
        var selectedConfigKey = await DisplayActionSheet("Restore config", "Cancel", null, FlowDirection.LeftToRight, _savedSelections.Keys.ToArray());
        if (selectedConfigKey == "Cancel" || selectedConfigKey is null)
        {
            return;
        }

        if (_savedSelections.TryGetValue(selectedConfigKey.ToString(), out var selectedIds))
        {
            ClearSelection();

            foreach (var dialog in Dialogs)
            {
                dialog.IsSelected = selectedIds.Contains(dialog.Id);
            }

            await DisplayAlert("Success", $"Restored {selectedIds.Count} selected chats!", "Ok");
        }
    }

    private void ClearSelection()
    {
        foreach (var dialog in Dialogs)
        {
            dialog.IsSelected = false;
        }
    }

    private void SaveSelections()
    {
        var json = JsonSerializer.Serialize(_savedSelections);
        Preferences.Set("SavedSelections", json);
    }

    private void LoadSelections()
    {
        var json = Preferences.Get("SavedSelections", string.Empty);
        if (!string.IsNullOrEmpty(json))
        {
            _savedSelections = JsonSerializer.Deserialize<Dictionary<string, List<long>>>(json) ?? new Dictionary<string, List<long>>();
        }
    }

    private void ClearButtonClicked(object sender, EventArgs e)
    {
        ClearSelection();
    }

    private void ShowLoader()
    {
        LoadingIndicator.IsVisible = true;
    }

    private void HideLoader()
    {
        LoadingIndicator.IsVisible = false;
    }
}
