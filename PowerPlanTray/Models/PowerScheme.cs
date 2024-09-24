using System;

namespace PowerPlanTray.Models;

public class PowerScheme : GuidName
{
    public PowerScheme(Guid guid, string friendlyName) : base(guid, friendlyName)
    {

    }

    public bool Active { get; set; }
}