using System;
using Avalonia;
using Avalonia.Interactivity;
namespace PowerPlanTray.Controls
{
    public class CheckableMenuItem : NativeMenuItemExtended
    {
        public CheckableMenuItem()
        {
            if (!string.IsNullOrEmpty(Group)) {
                Icon = Utils.MaterialIconsHelper.GetBitmap("mdi-radiobox-blank");
            } else {
                Icon = null;
            }

            Click += OnClicked;
        }

        public new static readonly DirectProperty<CheckableMenuItem, bool> IsCheckedProperty =
            AvaloniaProperty.RegisterDirect<CheckableMenuItem, bool>(
                nameof(IsChecked),
                o => o.IsChecked,
                (o, v) => o.IsChecked = v);

        private bool _isChecked;
        private string _group = string.Empty;

        public event EventHandler<RoutedEventArgs>? IsCheckedChanged;

        public string Group {
            get => _group;
            set {
                if (_group != value) {
                    _group = value;
                    SetIcon();
                }
            }
        }

        public new bool IsChecked
        {
            get => _isChecked;
            set {
                if (SetAndRaise(IsCheckedProperty, ref _isChecked, value)) {
                    SetIcon();
                    OnIsCheckedChanged();
                }
            }
        }

        private bool HasGroup => !string.IsNullOrEmpty(Group);

        private void OnClicked(object? sender, EventArgs args)
        {
            if (!HasGroup) {
                IsChecked = !this.IsChecked;
            } else if (!this.IsChecked)
                IsChecked = true;
        }

        private void OnIsCheckedChanged()
        {
            if (HasGroup) {
                if (!this.IsChecked)
                    return;

                var pi = this.Parent;
                if (pi != null) {
                    foreach (var i in pi.Items) {
                        if (i is CheckableMenuItem mi && mi.Group == this.Group) {
                            mi.IsChecked = mi == this;
                        }
                    }
                }
                IsCheckedChanged?.Invoke(this, new RoutedEventArgs());
            } else {
                IsCheckedChanged?.Invoke(this, new RoutedEventArgs());
            }
        }

        private void SetIcon()
        {
            string icon;
            if (_isChecked)
                icon = HasGroup ? "mdi-radiobox-marked" : "mdi-check";
            else
                icon = HasGroup ? "mdi-radiobox-blank" : string.Empty;

            Icon = !string.IsNullOrEmpty(icon) ? Utils.MaterialIconsHelper.GetBitmap(icon) : null;
        }
    }
}