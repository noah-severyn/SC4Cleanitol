using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SC4CleanitolAvalonia.Views.Components {
    public partial class ExpanderHeader : UserControl {
        public static readonly StyledProperty<string?> IconProperty = AvaloniaProperty.Register<ExpanderHeader, string?>(nameof(Icon));
        public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<ExpanderHeader, string?>(nameof(Title));

        public static readonly StyledProperty<int?> CountProperty = AvaloniaProperty.Register<ExpanderHeader, int?>(nameof(Count));

        public int? Count {
            get {
                return GetValue(CountProperty);
            }
            set {
                SetValue(CountProperty, value);
            }
        }

        public string? Icon {
            get {
                return GetValue(IconProperty);
            }
            set {
                SetValue(IconProperty, value);
            }
        }
        public string? Title {
            get {
                return GetValue(TitleProperty); 
            }
            set {
                SetValue(TitleProperty, value);
            }
        }

        public ExpanderHeader() {
            InitializeComponent();

            if (Design.IsDesignMode) {
                Icon = "\ue2d2";
                Title = "Sample header text";
                Count = 5;
            }
        }
    }
}