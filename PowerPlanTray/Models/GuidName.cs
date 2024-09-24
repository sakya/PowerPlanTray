using System;

namespace PowerPlanTray.Models;

public class GuidName
{
    public GuidName(Guid guid, string friendlyName)
    {
        Guid = guid;
        FriendlyName = friendlyName;
    }

    public Guid Guid { get; init; }
    public string FriendlyName { get; init; }
    public string? Description { get; set; }
}