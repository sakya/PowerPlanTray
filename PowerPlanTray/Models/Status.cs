using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Styling;
using PowerPlanTray.Utils;

namespace PowerPlanTray.Models;

public class Status
{
    public ThemeVariant? Theme { get; set; }
    public string? TrayIcon { get; set; }

    public PowerHelper.PowerState PowerState { get; set; } = new();

    public List<PowerScheme>? Schemes { get; set; }
    public Guid ActiveSchemeGuid { get; set; } = Guid.Empty;
    public PowerScheme? ActiveScheme
    {
        get
        {
            if (ActiveSchemeGuid == Guid.Empty)
                return null;

            return Schemes?.FirstOrDefault(s => s.Guid == ActiveSchemeGuid);
        }
    }

    public List<IdName>? BoostModeValues;
    public uint? BoostModeIndex { get; set; }
}