using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Threading;
using PowerPlanTray.Controls;
using PowerPlanTray.Models;
using PowerPlanTray.Utils;

namespace PowerPlanTray;

public class App : Application
{
    public CancellationTokenSource CancellationTokenSource { get; } = new();
    private TrayIcon _trayIcon = null!;
    private readonly PowerHelper.PowerState _powerState = new();

    private List<PowerScheme>? _schemes;
    private List<IdName>? _turboBoostValues;

    private ThemeVariant? _lastTheme;
    private Guid _lastActiveScheme = Guid.Empty;
    private uint _lastTurboBoostIndex;
    private string _lastIcon = string.Empty;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        DataContext = this;
    }

    public void Init()
    {
        _lastTheme = ActualThemeVariant;
        _trayIcon = new TrayIcon();
        _lastIcon = "mdi-power-plug-battery-outline";
        _trayIcon.Icon = new WindowIcon(MaterialIconsHelper.GetBitmap(_lastIcon));

        _trayIcon.ToolTipText = "Power Plan Tray";
        _trayIcon.Menu = BuildMenu();
        TrayIcon.SetIcons(this, [_trayIcon]);

        DispatcherTimer.Run(() =>
        {
            OnTimer();
            return true;
        }, TimeSpan.FromSeconds(1), DispatcherPriority.Background);
    }

    public void Run(CancellationToken ct)
    {
        try {
            Dispatcher.UIThread.MainLoop(ct);
        } catch (Exception ex) {
            var dEx = ex;
            while (dEx != null) {
                Console.WriteLine($"{ex.Message}");
                if (!string.IsNullOrEmpty(ex.StackTrace))
                    Console.WriteLine($"{ex.StackTrace}");
                dEx = ex.InnerException;
            }
        }
    } // Run

    #region private operations
    private NativeMenu BuildMenu()
    {
        var ps = new PowerHelper.PowerState();
        PowerHelper.GetSystemPowerStatus(ps);

        _schemes = PowerHelper.GetPowerSchemes();
        var activeScheme = _schemes.FirstOrDefault(p => p.Active);
        _lastActiveScheme = activeScheme?.Guid ?? Guid.Empty;

        var turboBoostIndex = activeScheme != null
            ? PowerHelper.GetTurboBoostIndex(activeScheme.Guid, ps.AcDc)
            : 0;
        _lastTurboBoostIndex = turboBoostIndex;

        var res = new NativeMenu();
        foreach (var scheme in _schemes) {
            var pmi = new CheckableMenuItem()
            {
                Header = scheme.FriendlyName,
                ToolTip = scheme.Description,
                IsChecked = scheme.Active,
                Tag = scheme,
            };
            pmi.Click += OnSchemeClick;
            res.Items.Add(pmi);
        }
        res.Items.Add(new NativeMenuItemSeparator());
        var cmi = new NativeMenuItemExtended()
        {
            Header = "Boost mode",
            Tag = "TurboBoost"
        };
        res.Items.Add(cmi);

        _turboBoostValues = PowerHelper.GetPossibleValues(PowerHelper.GUID_PROCESSOR_SETTINGS_SUBGROUP, PowerHelper.GUID_TURBO_BOOST_SETTING);
        if (_turboBoostValues.Count > 0) {
            cmi.Menu = new NativeMenu();
            foreach (var tbv in _turboBoostValues) {
                var tbm = new CheckableMenuItem()
                {
                    Header = tbv.FriendlyName,
                    ToolTip = tbv.Description,
                    Tag = tbv.Id,
                    IsChecked = tbv.Id == turboBoostIndex
                };
                tbm.Click += OnTurboBoostClick;
                cmi.Menu.Items.Add(tbm);
            }
        }

        res.Items.Add(new NativeMenuItemSeparator());
        var mi = new NativeMenuItem
        {
            Header = "Settings",
            Menu = new NativeMenu(),
            Icon = MaterialIconsHelper.GetBitmap("mdi-cogs")
        };
        var smi = new CheckableMenuItem()
        {
            Header = "Start with Windows",
            IsChecked = AutoStartHelper.IsEnabled()
        };
        smi.Click += OnAutoStartClick;
        mi.Menu.Items.Add(smi);
        res.Items.Add(mi);

        mi = new NativeMenuItem()
        {
            Header = "Quit",
            Icon = MaterialIconsHelper.GetBitmap("mdi-logout")
        };
        mi.Click += OnQuitClick;
        res.Items.Add(mi);

        return res;
    }

    private void UpdateSchemesMenu()
    {
        if (_trayIcon.Menu == null)
            return;

        _schemes = PowerHelper.GetPowerSchemes();
        foreach (var menuItem in _trayIcon.Menu.Items) {
            if (menuItem is CheckableMenuItem { Tag: PowerScheme menuScheme } cmi) {
                var scheme = _schemes.FirstOrDefault(p => p.Guid == menuScheme.Guid);
                if (scheme != null) {
                    cmi.IsChecked = scheme.Active;
                }
            }
        }
    }

    private void UpdateTurboBoostMenu(uint turboBoostIndex)
    {
        var mi = _trayIcon.Menu?.Items.OfType<NativeMenuItemExtended>()
            .FirstOrDefault(m => m.Tag as string == "TurboBoost");
        if (mi?.Menu != null) {
            foreach (var menuItem in mi.Menu.Items.OfType<CheckableMenuItem>()) {
                menuItem.IsChecked = menuItem.Tag is uint index && index == turboBoostIndex;
            }
        }
    }
    #endregion

    #region menu item clicks
    private void OnSchemeClick(object? sender, EventArgs e)
    {
        if (sender is CheckableMenuItem { Tag: PowerScheme scheme }) {
            PowerHelper.ActivateScheme(scheme.Guid);
        }
    }

    private void OnTurboBoostClick(object? sender, EventArgs e)
    {
        if (sender is CheckableMenuItem { Tag: uint index }) {
            var ps = new PowerHelper.PowerState();
            PowerHelper.GetSystemPowerStatus(ps);

            var schemeGuid = PowerHelper.GetActiveSchemeGuid();
            if (PowerHelper.SetTurboBoostIndex(schemeGuid, ps.AcDc, index))
                UpdateTurboBoostMenu(index);
        }
    }

    private void OnAutoStartClick(object? sender, EventArgs e)
    {
        if (sender is CheckableMenuItem menuItem) {
            AutoStartHelper.SetEnabled(menuItem.IsChecked);
        }
    }

    private void OnQuitClick(object? sender, EventArgs e)
    {
        CancellationTokenSource.Cancel();
    }
    #endregion

    private void OnTimer()
    {
        // Check the theme
        if (_lastTheme != ActualThemeVariant) {
            _lastTheme = ActualThemeVariant;
            _trayIcon.Icon = new WindowIcon(MaterialIconsHelper.GetBitmap(_lastIcon));
            _trayIcon.Menu = BuildMenu();
            return;
        }

        // Check the active scheme
        var active = PowerHelper.GetActiveSchemeGuid();
        if (active != _lastActiveScheme) {
            _lastActiveScheme = active;
            UpdateSchemesMenu();
        }

        // Check turbo boost index
        PowerHelper.GetSystemPowerStatus(_powerState);

        var turboBoostIndex = PowerHelper.GetTurboBoostIndex(active, _powerState.AcDc);
        if (_lastTurboBoostIndex != turboBoostIndex) {
            _lastTurboBoostIndex = turboBoostIndex;
            UpdateTurboBoostMenu(turboBoostIndex);
        }

        StringBuilder sb = new();
        var activeScheme = _schemes?.FirstOrDefault(s => s.Guid == active);
        if (activeScheme != null) {
            sb.AppendLine($"Scheme: {activeScheme.FriendlyName}");
        }
        var tbName = _turboBoostValues?.FirstOrDefault(t => t.Id == turboBoostIndex);
        if (tbName != null) {
            sb.AppendLine($"Boost mode: {tbName.FriendlyName}");
        }

        // Battery icon
        if (_powerState.BatteryFlag == PowerHelper.BatteryFlag.Unknown || (_powerState.BatteryFlag & PowerHelper.BatteryFlag.NoSystemBattery) == PowerHelper.BatteryFlag.NoSystemBattery) {
            if (_lastIcon != "mdi-power-plug-battery-outline") {
                _lastIcon = "mdi-power-plug-battery-outline";
                _trayIcon.Icon = new WindowIcon(MaterialIconsHelper.GetBitmap(_lastIcon));
                _trayIcon.ToolTipText = sb.ToString();
            }
        } else {
            var isAc = _powerState.AcDc == PowerHelper.PowerStates.AC;
            var isCharging = (_powerState.BatteryFlag & PowerHelper.BatteryFlag.Charging) == PowerHelper.BatteryFlag.Charging;
            var icon = _powerState.BatteryLifePercent switch
            {
                100 => isAc ? "mdi-battery-charging-100" : "mdi-battery",
                >= 90 => isAc ? "mdi-battery-charging-90" : "mdi-battery-90",
                >= 80 => isAc ? "mdi-battery-charging-80" : "mdi-battery-80",
                >= 70 => isAc ? "mdi-battery-charging-70" : "mdi-battery-70",
                >= 60 => isAc ? "mdi-battery-charging-60" : "mdi-battery-60",
                >= 50 => isAc ? "mdi-battery-charging-50" : "mdi-battery-50",
                >= 40 => isAc ? "mdi-battery-charging-40" : "mdi-battery-40",
                >= 30 => isAc ? "mdi-battery-charging-30" : "mdi-battery-30",
                >= 20 => isAc ? "mdi-battery-charging-20" : "mdi-battery-20",
                _ => isAc ? "mdi-battery-charging-10" : "mdi-battery-10"
            };

            if (!string.IsNullOrEmpty(icon) && icon != _lastIcon) {
                _lastIcon = icon;
                _trayIcon.Icon = new WindowIcon(MaterialIconsHelper.GetBitmap(icon));
            }

            if (_powerState.BatteryLifeTime > 0) {
                sb.AppendLine($"{(isCharging ? "Charging - " : string.Empty)}Remaining: {TimeSpan.FromSeconds(_powerState.BatteryLifeTime).ToString(@"hh\:mm")}");
            } else if (isCharging) {
                sb.AppendLine("Charging");
            }

            sb.Append($"{_powerState.BatteryLifePercent}%");
            _trayIcon.ToolTipText = sb.ToString();
        }
    }
}