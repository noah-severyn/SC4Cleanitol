using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace SC4CleanitolAvalonia.Services;

/// <summary>
/// Provides dialog operations for a specific owner window.
/// </summary>
/// <param name="window">Owner window used as the parent for displayed dialogs.</param>
internal class DialogService(Window window) : IDialogService {
    private readonly Window _window = window;

    public async Task<ButtonResult> ShowMessageBoxAsync(string title, string message, Icon icon) {
        var msgbox = MessageBoxManager.GetMessageBoxStandard(title, message, ButtonEnum.YesNo, icon);
        return await msgbox.ShowWindowDialogAsync(_window);
    }
}
