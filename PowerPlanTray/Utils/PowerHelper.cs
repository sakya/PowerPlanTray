using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using PowerPlanTray.Models;

namespace PowerPlanTray.Utils;

public static class PowerHelper
{
    private const uint ACCESS_SCHEME = 16;
    private const uint ACCESS_SUBGROUP = 17;
    private const uint ACCESS_INDIVIDUAL_SETTING = 18;

    public static Guid GUID_PROCESSOR_SETTINGS_SUBGROUP = new("54533251-82be-4824-96c1-47b60b740d00");
    public static Guid GUID_BOOST_MODE_SETTING = new("be337238-0d82-4146-a960-4f3749d470c7");

    public static Guid GUID_ACDC_POWER_SOURCE = new("5D3E9A59-E9D5-4B00-A6BD-FF34FF516548");
    public static Guid GUID_ENERGY_SAVER_STATUS = new("550E8400-E29B-41D4-A716-446655440000");
    public static Guid GUID_BATTERY_PERCENTAGE_REMAINING = new("A7AD8041-B45A-4CAE-87A3-EECBB468A9E1");
    public static Guid GUID_POWER_SAVING_STATUS = new("E00958C0-C213-4ACE-AC77-FECCED2EEEA5");
    public static Guid GUID_POWERSCHEME_PERSONALITY = new("245D8541-3943-4422-B025-13A784F679B7");

    public enum PowerStates
    {
        AC,
        DC
    }

    [Flags]
    public enum BatteryFlag : byte
    {
        High = 1,
        Low = 2,
        Critical = 4,
        Charging = 8,
        NoSystemBattery = 128,
        Unknown = 255
    }

    public enum ACLineStatus : byte
    {
        Offline = 0,
        Online = 1,
        Unknown = 255
    }

    [StructLayout(LayoutKind.Sequential)]
    public class PowerState
    {
        public ACLineStatus ACLineStatus = ACLineStatus.Unknown;
        public BatteryFlag BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public int BatteryLifeTime;
        public int BatteryFullLifeTime;

        public PowerStates AcDc =>
            ACLineStatus == ACLineStatus.Online
                ? PowerStates.AC
                : PowerStates.DC;
    }

    public enum DEVICE_PWR_NOTIFY
    {
        /// <summary>
        /// The Recipient parameter is a handle to a service.Use the CreateService or OpenService function to obtain this handle.
        /// </summary>
        DEVICE_NOTIFY_SERVICE_HANDLE = 1,

        /// <summary>The Recipient parameter is a pointer to a callback function to call when the power setting changes.</summary>
        DEVICE_NOTIFY_CALLBACK = 2,
    }

    public struct DEVICE_NOTIFY_SUBSCRIBE_PARAMETERS
    {
        /// <summary>Indicates the callback function that will be called when the application receives the notification.</summary>
        [MarshalAs(UnmanagedType.FunctionPtr)]
        public DeviceNotifyCallbackRoutine Callback;

        /// <summary>The context of the application registering for the notification.</summary>
        public IntPtr Context;
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    public delegate uint DeviceNotifyCallbackRoutine(IntPtr context, uint type, IntPtr setting);

    #region dll import
    [DllImport("Kernel32.dll", EntryPoint = "GetSystemPowerStatus")]
    public static extern bool GetSystemPowerStatus(
        PowerState sps
    );

    // https://learn.microsoft.com/en-us/windows/win32/api/powersetting/
    // https://learn.microsoft.com/en-us/windows/win32/api/powrprof/
    // https://github.com/dahall/Vanara/tree/master/PInvoke/PowrProf
    [DllImport("powrprof.dll", EntryPoint = "PowerEnumerate")]
    static extern uint PowerEnumerate(
        IntPtr rootPowerKey,
        IntPtr schemeGuid,
        IntPtr subGroupOfPowerSetting,
        uint accessFlags,
        uint index,
        byte[] buffer,
        ref uint bufferSize);

    [DllImport("powrprof.dll", EntryPoint = "PowerGetActiveScheme")]
    static extern uint PowerGetActiveScheme(IntPtr userPowerKey, out IntPtr activePolicyGuid);

    [DllImport("powrprof.dll", EntryPoint = "PowerSetActiveScheme")]
    static extern uint PowerSetActiveScheme(IntPtr rootPowerKey, IntPtr schemeGuid);

    [DllImport("powrprof.dll", EntryPoint = "PowerReadACValue")]
    static extern uint PowerReadACValue(
        IntPtr rootPowerKey,
        ref Guid schemeGuid,
        ref Guid subGroupOfPowerSettingGuid,
        ref Guid powerSettingGuid,
        ref int type,
        byte[] buffer,
        ref uint bufferSize
    );

    [DllImport("powrprof.dll", EntryPoint = "PowerWriteACValueIndex")]
    static extern uint PowerWriteACValueIndex(
        IntPtr rootPowerKey,
        ref Guid schemeGuid,
        ref Guid subGroupOfPowerSettingGuid,
        ref Guid powerSettingGuid,
        uint acValueIndex
    );

    [DllImport("powrprof.dll", EntryPoint = "PowerReadACDefaultIndex")]
    static extern uint PowerReadACDefaultIndex(
        IntPtr rootPowerKey,
        ref Guid schemeGuid,
        ref Guid subGroupOfPowerSettingGuid,
        ref Guid powerSettingGuid,
        ref uint acValueIndex
    );

    [DllImport("powrprof.dll", EntryPoint = "PowerReadDCValue")]
    static extern uint PowerReadDCValue(
        IntPtr rootPowerKey,
        ref Guid schemeGuid,
        ref Guid subGroupOfPowerSettingGuid,
        ref Guid powerSettingGuid,
        ref int type,
        byte[] buffer,
        ref uint bufferSize
    );

    [DllImport("powrprof.dll", EntryPoint = "PowerWriteDCValueIndex")]
    static extern uint PowerWriteDCValueIndex(
        IntPtr rootPowerKey,
        ref Guid schemeGuid,
        ref Guid subGroupOfPowerSettingGuid,
        ref Guid powerSettingGuid,
        uint acValueIndex
    );

    [DllImport("powrprof.dll", EntryPoint = "PowerReadDCDefaultIndex")]
    static extern uint PowerReadDCDefaultIndex(
        IntPtr rootPowerKey,
        ref Guid schemeGuid,
        ref Guid subGroupOfPowerSettingGuid,
        ref Guid powerSettingGuid,
        ref uint acValueIndex
    );

    [DllImport("powrprof.dll", CharSet = CharSet.Unicode, EntryPoint = "PowerReadFriendlyName")]
    static extern uint PowerReadFriendlyName(
        IntPtr rootPowerKey,
        ref Guid schemeGuid,
        IntPtr subGroupOfPowerSettingGuid,
        IntPtr powerSettingGuid,
        StringBuilder buffer,
        ref uint bufferSize
    );

    [DllImport("powrprof.dll", EntryPoint = "PowerReadValueMin")]
    static extern uint PowerReadValueMin(
        IntPtr rootPowerKey,
        ref Guid subGroupOfPowerSettingGuid,
        ref Guid powerSettingGuid,
        ref uint value
    );

    [DllImport("powrprof.dll", EntryPoint = "PowerReadValueMax")]
    static extern uint PowerReadValueMax(
        IntPtr rootPowerKey,
        ref Guid subGroupOfPowerSettingGuid,
        ref Guid powerSettingGuid,
        ref uint value
    );

    [DllImport("powrprof.dll", CharSet = CharSet.Unicode, EntryPoint = "PowerReadDescription")]
    static extern uint PowerReadDescription(
        IntPtr rootPowerKey,
        IntPtr schemeGuid,
        IntPtr subGroupOfPowerSettingGuid,
        IntPtr powerSettingGuid,
        StringBuilder buffer,
        ref uint bufferSize
    );

    [DllImport("powrprof.dll", CharSet = CharSet.Unicode, EntryPoint = "PowerReadPossibleFriendlyName")]
    static extern uint PowerReadPossibleFriendlyName(
        IntPtr rootPowerKey,
        ref Guid subGroupOfPowerSettingGuid,
        ref Guid powerSettingGuid,
        uint value,
        StringBuilder buffer,
        ref uint bufferSize
    );

    [DllImport("powrprof.dll", CharSet = CharSet.Unicode, EntryPoint = "PowerReadPossibleDescription")]
    static extern uint PowerReadPossibleDescription(
        IntPtr rootPowerKey,
        ref Guid subGroupOfPowerSettingGuid,
        ref Guid powerSettingGuid,
        uint value,
        StringBuilder buffer,
        ref uint bufferSize
    );

    [DllImport("powrprof.dll", CharSet = CharSet.Unicode, EntryPoint = "PowerSettingRegisterNotification")]
    static extern uint PowerSettingRegisterNotification(
        ref Guid powerSettingGuid,
        DEVICE_PWR_NOTIFY flags,
        DEVICE_NOTIFY_SUBSCRIBE_PARAMETERS recipient,
        out IntPtr registrationHandle);
    #endregion

    public static bool RegisterNotification(Guid settingGuid, DeviceNotifyCallbackRoutine callback, out IntPtr handle)
    {
        var res = PowerSettingRegisterNotification(
            ref settingGuid,
            DEVICE_PWR_NOTIFY.DEVICE_NOTIFY_CALLBACK,
            new DEVICE_NOTIFY_SUBSCRIBE_PARAMETERS()
            {
                Callback = callback
            },
            out handle);
        return res == 0;
    }

    public static List<PowerScheme> GetPowerSchemes()
    {
        var res = new List<PowerScheme>();

        var activeGuid = GetActiveSchemeGuid();
        var schemes = Enumerate(Guid.Empty, Guid.Empty);
        foreach (var scheme in schemes) {
            res.Add(new PowerScheme(scheme.Guid, scheme.FriendlyName)
            {
                Description = scheme.Description,
                Active = scheme.Guid == activeGuid
            });
        }

        return res;
    }

    public static Guid GetActiveSchemeGuid()
    {
        if (PowerGetActiveScheme((IntPtr)null, out var ptr) == 0) {
            var guid = (Guid?)Marshal.PtrToStructure(ptr, typeof(Guid));
            Marshal.FreeHGlobal(ptr);
            if (guid != null)
                return guid.Value;
        }

        return Guid.Empty;
    }

    public static bool ActivateScheme(Guid schemaGuid)
    {
        var schemePtr = Marshal.AllocHGlobal(Marshal.SizeOf(schemaGuid));
        Marshal.StructureToPtr(schemaGuid, schemePtr, false);

        var tRes = PowerSetActiveScheme(IntPtr.Zero, schemePtr);
        Marshal.FreeHGlobal(schemePtr);
        return tRes == 0;
    }

    public static uint GetBoostModeIndex(Guid schemaGuid, PowerStates powerState)
    {
        if (powerState == PowerStates.AC)
            return GetAcValue(schemaGuid, GUID_PROCESSOR_SETTINGS_SUBGROUP, GUID_BOOST_MODE_SETTING);
        return GetDcValue(schemaGuid, GUID_PROCESSOR_SETTINGS_SUBGROUP, GUID_BOOST_MODE_SETTING);
    }

    public static bool SetBoostModeIndex(Guid schemaGuid, PowerStates powerState, uint index)
    {
        if (powerState == PowerStates.AC)
            return SetAcValueIndex(schemaGuid, GUID_PROCESSOR_SETTINGS_SUBGROUP, GUID_BOOST_MODE_SETTING, index);
        return SetDcValueIndex(schemaGuid, GUID_PROCESSOR_SETTINGS_SUBGROUP, GUID_BOOST_MODE_SETTING, index);
    }

    public static List<GuidName> Enumerate(Guid schemaGuid, Guid subgroupGuid)
    {
        var res = new List<GuidName>();

        var accessFlag = ACCESS_INDIVIDUAL_SETTING;
        IntPtr subgroupPtr;
        if (subgroupGuid != Guid.Empty) {
            subgroupPtr = Marshal.AllocHGlobal(Marshal.SizeOf(subgroupGuid));
            Marshal.StructureToPtr(subgroupGuid, subgroupPtr, false);
        } else {
            accessFlag = ACCESS_SUBGROUP;
            subgroupPtr = IntPtr.Zero;
        }

        IntPtr schemaPtr;
        if (schemaGuid != Guid.Empty) {
            schemaPtr = Marshal.AllocHGlobal(Marshal.SizeOf(schemaGuid));
            Marshal.StructureToPtr(schemaGuid, schemaPtr, false);
        } else {
            accessFlag = ACCESS_SCHEME;
            schemaPtr = IntPtr.Zero;
        }

        var guidPtr = Marshal.AllocHGlobal(Marshal.SizeOf(Guid.Empty));

        uint index = 0;
        while (true) {
            uint bufferSize = 0;
            var buffer = new byte[bufferSize];
            var tRes = PowerEnumerate(IntPtr.Zero, schemaPtr, subgroupPtr, accessFlag, index, buffer, ref bufferSize);
            if (tRes != 234)
                break;

            buffer = new byte[bufferSize];
            tRes = PowerEnumerate(IntPtr.Zero, schemaPtr, subgroupPtr, accessFlag, index, buffer, ref bufferSize);
            if (tRes != 0)
                break;

            var guid = new Guid(buffer);
            if (guid == Guid.Empty)
                break;

            var name = accessFlag switch
            {
                ACCESS_SCHEME => GetFriendlyName(guid, Guid.Empty, Guid.Empty),
                ACCESS_SUBGROUP => GetFriendlyName(schemaGuid, guid, Guid.Empty),
                ACCESS_INDIVIDUAL_SETTING => GetFriendlyName(schemaGuid, subgroupGuid, guid),
                _ => throw new ArgumentOutOfRangeException()
            };

            var guidName = new GuidName(guid, name);

            Marshal.StructureToPtr(guid, guidPtr, false);
            bufferSize = 0;
            var strBuffer = new StringBuilder();
            tRes = accessFlag switch
            {
                ACCESS_SCHEME => PowerReadDescription(IntPtr.Zero, guidPtr, IntPtr.Zero, IntPtr.Zero, strBuffer, ref bufferSize),
                ACCESS_SUBGROUP => PowerReadDescription(IntPtr.Zero, schemaPtr, guidPtr, IntPtr.Zero, strBuffer, ref bufferSize),
                ACCESS_INDIVIDUAL_SETTING => PowerReadDescription(IntPtr.Zero, schemaPtr, subgroupPtr, guidPtr, strBuffer, ref bufferSize),
                _ => throw new ArgumentOutOfRangeException()
            };
            if (tRes != 234 && tRes != 0)
                break;

            if (tRes > 0) {
                strBuffer.Capacity = (int)bufferSize;
                tRes = accessFlag switch
                {
                    ACCESS_SCHEME => PowerReadDescription(IntPtr.Zero, guidPtr, IntPtr.Zero, IntPtr.Zero, strBuffer,
                        ref bufferSize),
                    ACCESS_SUBGROUP => PowerReadDescription(IntPtr.Zero, schemaPtr, guidPtr, IntPtr.Zero, strBuffer,
                        ref bufferSize),
                    ACCESS_INDIVIDUAL_SETTING => PowerReadDescription(IntPtr.Zero, schemaPtr, subgroupPtr, guidPtr,
                        strBuffer, ref bufferSize),
                    _ => throw new ArgumentOutOfRangeException()
                };
                if (tRes != 0)
                    break;
            }

            guidName.Description = strBuffer.Length > 0 ? strBuffer.ToString() : null;
            res.Add(guidName);
            index++;
        }

        Marshal.FreeHGlobal(guidPtr);
        if (schemaPtr != IntPtr.Zero)
            Marshal.FreeHGlobal(schemaPtr);
        if (subgroupPtr != IntPtr.Zero)
            Marshal.FreeHGlobal(subgroupPtr);

        return res;
    }

    public static List<IdName> GetPossibleValues(Guid subgroupGuid, Guid settingGuid)
    {
        if (subgroupGuid == Guid.Empty)
            throw new ArgumentException("subgroupGuid cannot be empty");
        if (settingGuid == Guid.Empty)
            throw new ArgumentException("settingGuid cannot be empty");

        var res = new List<IdName>();

        uint index = 0;
        StringBuilder buffer = new();
        while (true) {
            buffer.Clear();
            uint bufferSize = 0;
            if (PowerReadPossibleFriendlyName(IntPtr.Zero, ref subgroupGuid, ref settingGuid, index, buffer, ref bufferSize) != 234)
                break;
            buffer.Capacity = (int)bufferSize;
            if (PowerReadPossibleFriendlyName(IntPtr.Zero, ref subgroupGuid, ref settingGuid, index, buffer, ref bufferSize) != 0)
                break;

            var idName = new IdName(index, buffer.ToString());
            buffer.Clear();
            bufferSize = 0;
            if (PowerReadPossibleDescription(IntPtr.Zero, ref subgroupGuid, ref settingGuid, index, buffer, ref bufferSize) != 234)
                break;
            buffer.Capacity = (int)bufferSize;
            if (PowerReadPossibleDescription(IntPtr.Zero, ref subgroupGuid, ref settingGuid, index, buffer, ref bufferSize) != 0)
                break;
            idName.Description = buffer.ToString();

            res.Add(idName);
            index++;
        }

        return res;
    }

    private static uint GetAcValue(Guid schemaGuid, Guid subGroupGuid, Guid settingGuid)
    {
        var type = 0;
        var buffer = new byte[4];
        uint size = 4;
        var tRes = PowerReadACValue(IntPtr.Zero, ref schemaGuid, ref subGroupGuid, ref settingGuid, ref type, buffer, ref size);

        return tRes == 0 ? BitConverter.ToUInt32(buffer, 0) : 0;
    }

    private static uint GetDcValue(Guid schemaGuid, Guid subGroupGuid, Guid settingGuid)
    {
        var type = 0;
        var buffer = new byte[4];
        uint size = 4;
        var tRes = PowerReadDCValue(IntPtr.Zero, ref schemaGuid, ref subGroupGuid, ref settingGuid, ref type, buffer, ref size);

        return tRes == 0 ? BitConverter.ToUInt32(buffer, 0) : 0;
    }

    private static bool SetAcValueIndex(Guid schemaGuid, Guid subGroupGuid, Guid settingGuid, uint value)
    {
        var schemePtr = Marshal.AllocHGlobal(Marshal.SizeOf(schemaGuid));
        Marshal.StructureToPtr(schemaGuid, schemePtr, false);

        var subGroupGuidPtr = Marshal.AllocHGlobal(Marshal.SizeOf(subGroupGuid));
        Marshal.StructureToPtr(subGroupGuid, subGroupGuidPtr, false);

        var settingGuidPtr = Marshal.AllocHGlobal(Marshal.SizeOf(settingGuid));
        Marshal.StructureToPtr(settingGuid, settingGuidPtr, false);

        var tRes = PowerWriteACValueIndex(IntPtr.Zero, ref schemaGuid, ref subGroupGuid, ref settingGuid, value);

        Marshal.FreeHGlobal(schemePtr);
        Marshal.FreeHGlobal(subGroupGuidPtr);
        Marshal.FreeHGlobal(settingGuidPtr);
        return tRes == 0;
    }

    private static bool SetDcValueIndex(Guid schemaGuid, Guid subGroupGuid, Guid settingGuid, uint value)
    {
        var tRes = PowerWriteDCValueIndex(IntPtr.Zero, ref schemaGuid, ref subGroupGuid, ref settingGuid, value);
        return tRes == 0;
    }

    private static string GetFriendlyName(Guid schemaGuid, Guid subgroupGuid, Guid settingGuid)
    {
        if (schemaGuid == Guid.Empty)
            throw new ArgumentException("schemaGuid cannot be empty");

        uint bufferSize = 0;

        IntPtr subgroupPtr;
        if (subgroupGuid != Guid.Empty) {
            subgroupPtr = Marshal.AllocHGlobal(Marshal.SizeOf(subgroupGuid));
            Marshal.StructureToPtr(subgroupGuid, subgroupPtr, false);
        } else {
            subgroupPtr = IntPtr.Zero;
        }

        IntPtr settingPtr;
        if (settingGuid != Guid.Empty) {
            settingPtr = Marshal.AllocHGlobal(Marshal.SizeOf(settingGuid));
            Marshal.StructureToPtr(settingGuid, settingPtr, false);
        } else {
            settingPtr = IntPtr.Zero;
        }

        var res = string.Empty;
        var strBuffer = new StringBuilder();
        var tRes = PowerReadFriendlyName(IntPtr.Zero, ref schemaGuid, subgroupPtr, settingPtr, strBuffer, ref bufferSize);
        if (tRes == 234) {
            strBuffer.Capacity = (int)bufferSize;
            tRes = PowerReadFriendlyName(IntPtr.Zero, ref schemaGuid, subgroupPtr, settingPtr, strBuffer, ref bufferSize);
            if (tRes == 0)
                res = strBuffer.ToString();
        }

        Marshal.FreeHGlobal(subgroupPtr);
        Marshal.FreeHGlobal(settingPtr);

        return res;
    }
}