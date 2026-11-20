using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC4CleanitolAvalonia.Services {
    internal interface IChecksumService {
        string? Compute(List<string> folders);
    }
}
