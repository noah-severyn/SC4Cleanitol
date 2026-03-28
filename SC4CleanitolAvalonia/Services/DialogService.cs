using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace SC4CleanitolAvalonia.Services {
    internal class DialogService(Window window) : IDialogService {
        private readonly Window _window = window;

        public async Task ShowMessageBoxAsync(string title, string message, Icon icon) {
            var msgbox = MessageBoxManager.GetMessageBoxStandard(title, message, ButtonEnum.Ok, icon);
            await msgbox.ShowWindowDialogAsync(_window);
        }
    }
}
