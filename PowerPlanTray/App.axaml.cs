using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using PowerPlanTray.Controls;
using PowerPlanTray.Models;
using PowerPlanTray.Utils;

namespace PowerPlanTray;

public class App : Application
{
    public CancellationTokenSource CancellationTokenSource { get; } = new();
    private TrayIcon _trayIcon = null!;

    private readonly Status _status = new();

    private PowerHelper.DeviceNotifyCallbackRoutine _registerNotification = null!;
    private readonly List<IntPtr> _registerNotificationHandles = [];

    private DateTime? _lastBatteryRemainingCapacityTime;
    private uint _lastBatteryRemainingCapacity;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public void Init()
    {
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 16299))
            EfficiencyModeHelper.SetEfficiencyMode(true);
        _registerNotification = OnSettingChange;

        _status.Theme = ActualThemeVariant;
        _status.TrayIcon = "mdi-power-plug-battery-outline";

        _status.Schemes = PowerHelper.GetPowerSchemes();
        _status.ActiveSchemeGuid = PowerHelper.GetActiveSchemeGuid();

        _status.BoostModeValues = PowerHelper.GetPossibleValues(PowerHelper.GUID_PROCESSOR_SETTINGS_SUBGROUP, PowerHelper.GUID_BOOST_MODE_SETTING);
        _status.BoostModeIndex = _status.ActiveScheme != null
            ? PowerHelper.GetBoostModeIndex(_status.ActiveSchemeGuid, _status.PowerState.AcDc)
            : 0;

        _trayIcon = new TrayIcon();
        _trayIcon.Icon = new WindowIcon(MaterialIconsHelper.GetBitmap(_status.TrayIcon));
        _trayIcon.ToolTipText = "Power Plan Tray";
        _trayIcon.Menu = BuildMenu();
        TrayIcon.SetIcons(this, [_trayIcon]);

        IntPtr ptr;
        if (!PowerHelper.RegisterNotification(PowerHelper.GUID_ACDC_POWER_SOURCE, _registerNotification, out ptr))
            Console.WriteLine("Error registering notification for GUID_ACDC_POWER_SOURCE");
        if (ptr != IntPtr.Zero)
            _registerNotificationHandles.Add(ptr);
        if (!PowerHelper.RegisterNotification(PowerHelper.GUID_ENERGY_SAVER_STATUS, _registerNotification, out ptr))
            Console.WriteLine("Error registering notification for GUID_ENERGY_SAVER_STATUS");
        if (ptr != IntPtr.Zero)
            _registerNotificationHandles.Add(ptr);
        if (!PowerHelper.RegisterNotification(PowerHelper.GUID_POWERSCHEME_PERSONALITY, _registerNotification, out ptr))
            Console.WriteLine("Error registering notification for GUID_POWERSCHEME_PERSONALITY");
        if (ptr != IntPtr.Zero)
            _registerNotificationHandles.Add(ptr);
        if (!PowerHelper.RegisterNotification(PowerHelper.GUID_BOOST_MODE_SETTING, _registerNotification, out ptr))
            Console.WriteLine("Error registering notification for GUID_BOOST_MODE_SETTING");
        if (ptr != IntPtr.Zero)
            _registerNotificationHandles.Add(ptr);

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
        } finally {
            foreach (var ptr in _registerNotificationHandles) {
                PowerHelper.UnregisterNotification(ptr);
            }
        }
    } // Run

    #region private operations
    private NativeMenu BuildMenu()
    {
        var res = new NativeMenu();
        if (_status.Schemes != null) {
            foreach (var scheme in _status.Schemes) {
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
        }

        res.Items.Add(new NativeMenuItemSeparator());
        var cmi = new NativeMenuItemExtended()
        {
            Header = "Boost mode",
            Tag = "BoostMode"
        };
        res.Items.Add(cmi);

        if (_status.BoostModeValues?.Count > 0) {
            cmi.Menu = new NativeMenu();
            foreach (var tbv in _status.BoostModeValues) {
                var tbm = new CheckableMenuItem()
                {
                    Header = tbv.FriendlyName,
                    ToolTip = tbv.Description,
                    Tag = tbv.Id,
                    IsChecked = tbv.Id == _status.BoostModeIndex
                };
                tbm.Click += OnBoostModeClick;
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

        _status.Schemes = PowerHelper.GetPowerSchemes();
        foreach (var menuItem in _trayIcon.Menu.Items.OfType<CheckableMenuItem>()) {
            if (menuItem.Tag is PowerScheme menuScheme) {
                var scheme = _status.Schemes.FirstOrDefault(p => p.Guid == menuScheme.Guid);
                menuItem.IsChecked = scheme?.Active ?? false;
            }
        }
    }

    private void UpdateBoostModeMenu(uint boostModeIndex)
    {
        var mi = _trayIcon.Menu?.Items.OfType<NativeMenuItemExtended>()
            .FirstOrDefault(m => m.Tag as string == "BoostMode");
        if (mi?.Menu != null) {
            foreach (var menuItem in mi.Menu.Items.OfType<CheckableMenuItem>()) {
                menuItem.IsChecked = menuItem.Tag is uint index && index == boostModeIndex;
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

    private void OnBoostModeClick(object? sender, EventArgs e)
    {
        if (sender is CheckableMenuItem { Tag: uint index }) {
            var schemeGuid = PowerHelper.GetActiveSchemeGuid();
            if (PowerHelper.SetBoostModeIndex(schemeGuid, _status.PowerState.AcDc, index))
                UpdateBoostModeMenu(index);
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

    private uint OnSettingChange(IntPtr context, uint type, IntPtr setting)
    {
        var guid = Marshal.PtrToStructure<Guid>(setting);
        if (guid == PowerHelper.GUID_ACDC_POWER_SOURCE || guid == PowerHelper.GUID_ENERGY_SAVER_STATUS) {
            PowerHelper.GetSystemPowerStatus(_status.PowerState);
            _lastBatteryRemainingCapacityTime = null;
            _lastBatteryRemainingCapacity = 0;
        } else if (guid == PowerHelper.GUID_POWERSCHEME_PERSONALITY) {
            var active = PowerHelper.GetActiveSchemeGuid();
            if (active != _status.ActiveSchemeGuid) {
                _status.ActiveSchemeGuid = active;
                Dispatcher.UIThread.Invoke(UpdateSchemesMenu);
            }
        } else if (guid == PowerHelper.GUID_BOOST_MODE_SETTING) {
            var active = PowerHelper.GetActiveSchemeGuid();
            var boostModeIndex = PowerHelper.GetBoostModeIndex(active, _status.PowerState.AcDc);
            if (_status.BoostModeIndex != boostModeIndex) {
                _status.BoostModeIndex = boostModeIndex;
                Dispatcher.UIThread.Invoke(() => UpdateBoostModeMenu(boostModeIndex));
            }
        }

        return 0;
    }

    private void OnTimer()
    {
        // Check the theme
        if (_status.Theme != ActualThemeVariant) {
            // Change tray icon
            _status.Theme = ActualThemeVariant;
            if (!string.IsNullOrEmpty(_status.TrayIcon))
                _trayIcon.Icon = new WindowIcon(MaterialIconsHelper.GetBitmap(_status.TrayIcon));
            _trayIcon.Menu = BuildMenu();
            return;
        }

        StringBuilder sb = new();
        var activeScheme = _status.ActiveScheme;
        if (activeScheme != null) {
            sb.AppendLine($"Scheme: {activeScheme.FriendlyName}");
        }
        var tbName = _status.BoostModeValues?.FirstOrDefault(t => t.Id == _status.BoostModeIndex);
        if (tbName != null) {
            sb.AppendLine($"Boost mode: {tbName.FriendlyName}");
        }

        // Battery icon
        if (_status.PowerState.BatteryFlag == PowerHelper.BatteryFlag.Unknown || (_status.PowerState.BatteryFlag & PowerHelper.BatteryFlag.NoSystemBattery) == PowerHelper.BatteryFlag.NoSystemBattery) {
            if (_status.TrayIcon != "mdi-power-plug-battery-outline") {
                _status.TrayIcon = "mdi-power-plug-battery-outline";
                _trayIcon.Icon = new WindowIcon(MaterialIconsHelper.GetBitmap(_status.TrayIcon));
                _trayIcon.ToolTipText = sb.ToString();
            }
        } else {
            PowerHelper.GetSystemPowerStatus(_status.PowerState);
            var isAc = _status.PowerState.AcDc == PowerHelper.PowerStates.AC;
            var isCharging = (_status.PowerState.BatteryFlag & PowerHelper.BatteryFlag.Charging) == PowerHelper.BatteryFlag.Charging;
            var icon = _status.PowerState.BatteryLifePercent switch
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

            if (!string.IsNullOrEmpty(icon) && icon != _status.TrayIcon) {
                _status.TrayIcon = icon;
                _trayIcon.Icon = new WindowIcon(MaterialIconsHelper.GetBitmap(_status.TrayIcon));
            }

            BatteryHelper.GetInfo(_status.BatteryInfo);
            sb.Append($"Battery info: {_status.PowerState.BatteryLifePercent}% (");
            if (isCharging)
                sb.Append("Charging");
            else if (isAc)
                sb.Append("Plugged in");
            else
                sb.Append("Discharging");

            if (_status.BatteryInfo.ChargingTime != TimeSpan.Zero) {
                sb.Append($" - Remaining: {_status.BatteryInfo.ChargingTime.ToString(@"hh\:mm")}");
            } else if (_status.BatteryInfo.RemainingTime != TimeSpan.Zero) {
                sb.Append($" - Remaining: {_status.BatteryInfo.RemainingTime.ToString(@"hh\:mm")}");
            } else if (!isAc && !isCharging) {
                // Estimate remaining time
                if (_lastBatteryRemainingCapacityTime != null) {
                    var passedTime = DateTime.UtcNow - _lastBatteryRemainingCapacityTime.Value;
                    var capacityLoss = (_lastBatteryRemainingCapacity - _status.BatteryInfo.RemainingCapacity) / passedTime.TotalSeconds;
                    if (capacityLoss > 0) {
                        var secondsLeft = _status.BatteryInfo.RemainingCapacity / capacityLoss;
                        sb.Append($" - Remaining: {TimeSpan.FromSeconds(secondsLeft).ToString(@"hh\:mm")}");
                    }
                }
            }

            sb.Append(')');

            _trayIcon.ToolTipText = sb.ToString();

            if (_lastBatteryRemainingCapacityTime == null) {
                _lastBatteryRemainingCapacity = _status.BatteryInfo.RemainingCapacity;
                _lastBatteryRemainingCapacityTime = DateTime.UtcNow;
            }
        }
    }
}