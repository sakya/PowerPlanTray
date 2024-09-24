namespace PowerPlanTray.Models;

public class IdName
{
    public IdName(uint id, string friendlyName)
    {
        Id = id;
        FriendlyName = friendlyName;
    }

    public uint Id { get; init; }
    public string FriendlyName { get; init; }
    public string? Description { get; set; }
}