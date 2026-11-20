using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace SC4CleanitolAvalonia.Services {
    internal interface IFolderPickerService {
        Task<IStorageFolder?> PickFolderAsync();
    }
}
