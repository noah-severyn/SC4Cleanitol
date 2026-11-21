using System.Collections.Generic;
using SC4CleanitolEngine;
using static SC4CleanitolEngine.CleanitolEngine;

namespace SC4CleanitolAvalonia.ViewModels;

/// <summary>
/// Sample data for the XAML previewer, shared by <see cref="MainViewModel"/>'s design-time constructor and <c>d:DataContext</c> in component views. Not used at runtime.
/// </summary>
internal static class DesignData {
    /// <summary>
    /// Multiple results, for previewing the full list in <c>MainView</c>.
    /// </summary>
    public static IReadOnlyList<MainViewModel.ScriptResult> ScriptResults { get; } = [
        new("nam-team:network-addon-mod", [
            new Finding { Severity = FindingSeverity.Warning, Script = "nam-team:network-addon-mod", Item = "NetworkAddonMod_Locale_pt.dat", Message = "was found in C:\\Plugins\\075-my-plugins\\NAM\\@4^1 Core\\locale" },
            new Finding { Severity = FindingSeverity.Warning, Script = "nam-team:network-addon-mod", Item = "NetworkAddonMod_Locale_de.dat", Message = "was found in C:\\Plugins\\075-my-plugins\\NAM\\@4^1 Core\\locale" },
            new Finding { Severity = FindingSeverity.Info, Script = "nam-team:network-addon-mod", Item = "NetworkAddonMod_Controller.dat", Message = "is not present." },
        ]),
        new("b62:safeway-60s-retro-grocery", [
            new Finding { Severity = FindingSeverity.Warning, Script = "b62:safeway-60s-retro-grocery", Item = "B62-CS$$_Safeway Retro 1960s v 2.0-0x6534284a-0xd3a3e650-0x5639d97a.SC4Desc", Message = "was found in C:\\Plugins\\B62-Safeway 60's Retro" },
            new Finding { Severity = FindingSeverity.Warning, Script = "b62:safeway-60s-retro-grocery", Item = "CS$$2_6x6_B62-CS$$_Safeway Retro 1960s v 2.0_8639d990.SC4Lot", Message = "was found in C:\\Plugins\\B62-Safeway 60's Retro" },
            new Finding { Severity = FindingSeverity.Warning, Script = "b62:safeway-60s-retro-grocery", Item = "b62 1960s Safeway Retro v 2.0-0x5ad0e817_0xb2a42fa9_0x30000.SC4Model", Message = "was found in C:\\Plugins\\B62-Safeway 60's Retro\\B62-Safeway 60's Retro" },
        ]),
    ];

    /// <summary>
    /// Single result, for previewing a <c>ScriptResultView</c>.
    /// </summary>
    public static MainViewModel.ScriptResult ScriptResult { get; } = ScriptResults[0];
}
