using System;
using System.Collections.Generic;
using System.Linq;
using MsBox.Avalonia.Enums;
using System.Threading.Tasks;

namespace SC4CleanitolAvalonia.Services {
    internal interface IDialogService {
        Task<ButtonResult> ShowMessageBoxAsync(string title, string message, Icon icon);
    }
}
