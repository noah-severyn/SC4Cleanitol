using Microsoft.Maui.Storage;

namespace SC4Cleanitol;

public partial class MainPage : ContentPage {
	int count = 0;

	public MainPage() {
		InitializeComponent();

		//https://learn.microsoft.com/en-us/dotnet/maui/user-interface/system-theme-changes?view=net-maui-7.0
		AppTheme currentTheme = Application.Current.RequestedTheme;
		Application.Current.UserAppTheme = AppTheme.Dark;
	}


	private async void OnCounterClicked(object sender, EventArgs e) {
		count++;

		if (count == 1)
			SelectScript.Text = $"Clicked {count} time";
		else
			SelectScript.Text = $"Clicked {count} times";

		SemanticScreenReader.Announce(SelectScript.Text);

		PickOptions po = new PickOptions();
		_ = await PickAndShowAsync(po);
	}



	//https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/storage/file-picker?view=net-maui-7.0&tabs=windows
	private async Task<FileResult> PickAndShowAsync(PickOptions options) {
		try {
			FileResult result = await FilePicker.Default.PickAsync(options);
			if (result != null) {
				if (result.FileName.EndsWith("txt", StringComparison.OrdinalIgnoreCase)) {
					//https://learn.microsoft.com/en-us/dotnet/standard/io/how-to-read-text-from-a-file
					using var stream = new StreamReader(result.FullPath);
					ScriptOutput.Text = await stream.ReadToEndAsync();
				}
			}

			return result;
		}
		catch (Exception ex) {
			// The user canceled or something went wrong
		}

		return null;
	}


	private async void RunScriptAsync(object sender, EventArgs e) {
		//https://learn.microsoft.com/en-us/dotnet/maui/user-interface/pop-ups?view=net-maui-7.0
		await DisplayAlert("Alert", "Script will be run.", "OK");
	}

	private async void BackupFilesAsync(object sender, EventArgs e) {
		await DisplayAlert("Alert", "Files will be backed up.", "OK");
	}



	/// <summary>
	/// Quit the program.
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	private void QuitProgram(object sender, EventArgs e) {
		Application.Current.Quit();
	}
}

