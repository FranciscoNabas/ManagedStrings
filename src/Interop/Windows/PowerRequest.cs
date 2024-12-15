// This file is part of the ManagedStrings project and repository.
// Project files are licensed under the MIT license.
// https://github.com/FranciscoNabas/ManagedStrings

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace ManagedStrings.Interop.Windows;

#region Enumerations

internal enum POWER_INFORMATION_LEVEL
{
    SystemPowerPolicyAc,
    SystemPowerPolicyDc,
    VerifySystemPolicyAc,
    VerifySystemPolicyDc,
    SystemPowerCapabilities,
    SystemBatteryState,
    SystemPowerStateHandler,
    ProcessorStateHandler,
    SystemPowerPolicyCurrent,
    AdministratorPowerPolicy,
    SystemReserveHiberFile,
    ProcessorInformation,
    SystemPowerInformation,
    ProcessorStateHandler2,
    LastWakeTime,                                   // Compare with KeQueryInterruptTime()
    LastSleepTime,                                  // Compare with KeQueryInterruptTime()
    SystemExecutionState,
    SystemPowerStateNotifyHandler,
    ProcessorPowerPolicyAc,
    ProcessorPowerPolicyDc,
    VerifyProcessorPowerPolicyAc,
    VerifyProcessorPowerPolicyDc,
    ProcessorPowerPolicyCurrent,
    SystemPowerStateLogging,
    SystemPowerLoggingEntry,
    SetPowerSettingValue,
    NotifyUserPowerSetting,
    PowerInformationLevelUnused0,
    SystemMonitorHiberBootPowerOff,
    SystemVideoState,
    TraceApplicationPowerMessage,
    TraceApplicationPowerMessageEnd,
    ProcessorPerfStates,
    ProcessorIdleStates,
    ProcessorCap,
    SystemWakeSource,
    SystemHiberFileInformation,
    TraceServicePowerMessage,
    ProcessorLoad,
    PowerShutdownNotification,
    MonitorCapabilities,
    SessionPowerInit,
    SessionDisplayState,
    PowerRequestCreate,
    PowerRequestAction,
    GetPowerRequestList,
    ProcessorInformationEx,
    NotifyUserModeLegacyPowerEvent,
    GroupPark,
    ProcessorIdleDomains,
    WakeTimerList,
    SystemHiberFileSize,
    ProcessorIdleStatesHv,
    ProcessorPerfStatesHv,
    ProcessorPerfCapHv,
    ProcessorSetIdle,
    LogicalProcessorIdling,
    UserPresence,                                   // Deprecated
    PowerSettingNotificationName,
    GetPowerSettingValue,
    IdleResiliency,
    SessionRITState,
    SessionConnectNotification,
    SessionPowerCleanup,
    SessionLockState,
    SystemHiberbootState,
    PlatformInformation,
    PdcInvocation,
    MonitorInvocation,
    FirmwareTableInformationRegistered,
    SetShutdownSelectedTime,
    SuspendResumeInvocation,                        // Deprecated
    PlmPowerRequestCreate,
    ScreenOff,
    CsDeviceNotification,
    PlatformRole,
    LastResumePerformance,
    DisplayBurst,
    ExitLatencySamplingPercentage,
    RegisterSpmPowerSettings,
    PlatformIdleStates,
    ProcessorIdleVeto,                              // Deprecated.
    PlatformIdleVeto,                               // Deprecated.
    SystemBatteryStatePrecise,
    ThermalEvent,
    PowerRequestActionInternal,
    BatteryDeviceState,
    PowerInformationInternal,
    ThermalStandby,
    SystemHiberFileType,
    PhysicalPowerButtonPress,
    QueryPotentialDripsConstraint,
    EnergyTrackerCreate,
    EnergyTrackerQuery,
    UpdateBlackBoxRecorder,
    SessionAllowExternalDmaDevices,
    SendSuspendResumeNotification,
    BlackBoxRecorderDirectAccessBuffer,
    PowerInformationLevelMaximum,
}

internal enum POWER_REQUEST_TYPE
{
    DisplayRequired,
    SystemRequired,
    AwayModeRequired,
    ExecutionRequired
}

internal enum POWER_REQUEST_TYPE_INTERNAL
{
    DisplayRequiredInternal,
    SystemRequiredInternal,
    AwayModeRequiredInternal,
    ExecutionRequiredInternal,  // Windows 8+
    PerfBoostRequiredInternal,  // Windows 8+
    ActiveLockScreenInternal,   // Windows 10 RS1+ (reserved on Windows 8)
    
    // Values 6 and 7 are reserved for Windows 8 only
    InternalInvalid,
    InternalUnknown,

    FullScreenVideoRequired     // Windows 8 only
}

#endregion

#region Structures

[StructLayout(LayoutKind.Explicit, Pack = 8, Size = 40)]
internal struct COUNTED_REASON_CONTEXT
{
    [FieldOffset(0x0)] internal uint Version;
    [FieldOffset(0x4)] internal uint Flags;
    [FieldOffset(0x8)] internal SafeUnicodeString ResourceFileName;
    [FieldOffset(0x8)] internal SafeUnicodeString SimpleString;
    [FieldOffset(0x18)] internal ushort ResourceReasonId;
    [FieldOffset(0x1D)] internal uint StringCount;
    [FieldOffset(0x20)] internal nint ReasonStrings; // PUNICODE_STRING.
}

[StructLayout(LayoutKind.Sequential)]
internal struct POWER_REQUEST_ACTION
{
    internal nint PowerRequestHandle;
    internal POWER_REQUEST_TYPE_INTERNAL RequestType;
    internal BOOLEAN SetAction;
    internal nint ProcessHandle;
}

#endregion

/// <summary>
/// Partial class containing unmanaged macros.
/// </summary>
internal static partial class Constants
{
    internal const uint POWER_REQUEST_CONTEXT_VERSION          = 0x00000000;
    internal const uint POWER_REQUEST_CONTEXT_SIMPLE_STRING    = 0x00000001;
    internal const uint POWER_REQUEST_CONTEXT_DETAILED_STRING  = 0x00000002;
}

/// <summary>
/// Contains methods to issue power requests.
/// </summary>
internal static partial class PowerRequest
{
    /// <seealso href="https://learn.microsoft.com/windows-hardware/drivers/ddi/wdm/nf-wdm-ntpowerinformation">NtPowerInformation function (wdm.h)</seealso>
    [LibraryImport("ntdll.dll")]
    private static unsafe partial int NtPowerInformation(
        POWER_INFORMATION_LEVEL InformationLevel,
        void* InputBuffer,
        int InputBufferLength,
        out nint OutputBuffer,
        int OutputBufferLength
    );

    /// <summary>
    /// Creates a Execution Required power request.
    /// </summary>
    /// <param name="hProcess">The process <see cref="SafeProcessHandle"/>.</param>
    /// <returns>A safe handle to the power request.</returns>
    /// <exception cref="NativeException">The call to 'NtPowerInformation' failed.</exception>
    internal static unsafe SafePowerRequestHandle CreateExecutionRequiredRequest(SafeProcessHandle hProcess)
    {
        // Creating the request.
        COUNTED_REASON_CONTEXT reason = new() {
            Version = Constants.POWER_REQUEST_CONTEXT_VERSION,
            Flags = Constants.POWER_REQUEST_CONTEXT_SIMPLE_STRING,
            SimpleString = new("QueryDebugInformation request")
        };

        int status = NtPowerInformation(
            POWER_INFORMATION_LEVEL.PlmPowerRequestCreate,
            &reason,
            Marshal.SizeOf<COUNTED_REASON_CONTEXT>(),
            out nint powerRequestHandle,
            nint.Size
        );

        if (status != ErrorCodes.STATUS_SUCCESS)
            throw new NativeException(status, true);

        // Enabling the execution required request.
        POWER_REQUEST_ACTION action = new() {
            PowerRequestHandle = powerRequestHandle,
            RequestType = POWER_REQUEST_TYPE_INTERNAL.ExecutionRequiredInternal,
            SetAction = BOOLEAN.TRUE,
            ProcessHandle = hProcess.DangerousGetHandle()
        };

        status = NtPowerInformation(
            POWER_INFORMATION_LEVEL.PowerRequestAction,
            &action,
            Marshal.SizeOf<POWER_REQUEST_ACTION>(),
            out _,
            0
        );

        if (status != ErrorCodes.STATUS_SUCCESS)
            throw new NativeException(status, true);

        return new(powerRequestHandle, true);
    }

    /// <summary>
    /// Destroys a Execution Required power request.
    /// </summary>
    /// <param name="hRequest">The power request.</param>
    /// <returns>True if the request was destroyed successfully.</returns>
    internal static unsafe bool DestroyExecutionRequiredRequest(SafePowerRequestHandle hRequest)
    {
        POWER_REQUEST_ACTION action = new() {
            PowerRequestHandle = hRequest.DangerousGetHandle(),
            RequestType = POWER_REQUEST_TYPE_INTERNAL.ExecutionRequiredInternal,
            SetAction = BOOLEAN.FALSE,
            ProcessHandle = nint.Zero
        };

        return NtPowerInformation(
            POWER_INFORMATION_LEVEL.PowerRequestAction,
            &action,
            Marshal.SizeOf<POWER_REQUEST_ACTION>(),
            out _,
            0
        ) == ErrorCodes.STATUS_SUCCESS;
    }
}

/// <summary>
/// Represents a safe handle to a power request.
/// </summary>
internal sealed class SafePowerRequestHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    private readonly bool m_destroyExecutionRequiredRequest;

    internal SafePowerRequestHandle(nint handle, bool destroyExecRequiredRequest)
        : base(true)
    {
        SetHandle(handle);
        m_destroyExecutionRequiredRequest = destroyExecRequiredRequest;
    }

    protected override bool ReleaseHandle()
    {
        bool requestDestroyed = true;
        if (m_destroyExecutionRequiredRequest)
            requestDestroyed = PowerRequest.DestroyExecutionRequiredRequest(this);

        return Common.CloseHandle(handle) && requestDestroyed;
    }
}