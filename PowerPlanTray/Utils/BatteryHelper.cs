using System;
using System.Management;

namespace PowerPlanTray.Utils;

public static class BatteryHelper
{
    private static readonly ManagementScope Scope = new(@"\\.\root\wmi");
    private static readonly ManagementObjectSearcher FullChargeSearcher = new(Scope, new ObjectQuery("Select * from BatteryFullChargedCapacity"));
    private static readonly ManagementObjectSearcher StatusSearcher = new(Scope, new ObjectQuery("Select * from BatteryStatus"));

    public class BatteryInfo
    {
        public bool IsCharging { get; set; }
        public uint FullChargedCapacity { get; set; }
        public int ChargeRate { get; set; }
        public int DischargeRate { get; set; }
        public uint RemainingCapacity { get; set; }
        public uint Voltage { get; set; }

        public TimeSpan RemainingTime
        {
            get
            {
                if (IsCharging || DischargeRate <= 0)
                    return TimeSpan.Zero;

                var left = RemainingCapacity / (double)DischargeRate;
                return TimeSpan.FromHours(left);
            }
        }

        public TimeSpan ChargingTime
        {
            get
            {
                if (!IsCharging || ChargeRate <= 0)
                    return TimeSpan.Zero;

                var left = (FullChargedCapacity - RemainingCapacity) / (double)ChargeRate;
                return TimeSpan.FromHours(left);
            }
        }

    }

    public static void GetInfo(BatteryInfo batteryInfo, int index = 0)
    {
        var i = 0;
        using var fullChargeSearcherObjects = FullChargeSearcher.Get();
        foreach (var mo in fullChargeSearcherObjects) {
            try {
                if (i++ == index) {
                    batteryInfo.FullChargedCapacity = (uint)mo["FullChargedCapacity"];
                }
            } finally {
                mo.Dispose();
            }
        }

        i = 0;
        using var statusSearcherObjects = StatusSearcher.Get();
        foreach (var mo in statusSearcherObjects) {
            try {
                if (i++ == index) {
                    batteryInfo.IsCharging = (bool)mo["Charging"];
                    batteryInfo.RemainingCapacity = (uint)mo["RemainingCapacity"];
                    batteryInfo.ChargeRate = (int)mo["ChargeRate"];
                    batteryInfo.DischargeRate = (int)mo["DischargeRate"];
                    batteryInfo.Voltage = (uint)mo["Voltage"];
                }
            } finally {
                mo.Dispose();
            }
        }
    }
}