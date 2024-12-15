// This file is part of the ManagedStrings project and repository.
// Project files are licensed under the MIT license.
// https://github.com/FranciscoNabas/ManagedStrings

using System.Runtime.InteropServices;

namespace ManagedStrings.Interop.Windows;

#region x64

[StructLayout(LayoutKind.Sequential)]
internal readonly struct PEB
{
    internal readonly byte InheritedAddressSpace;
    internal readonly byte ReadImageFileExecOptions;
    internal readonly byte BeingDebugged;

    private readonly byte BitField;
    internal bool ImageUsesLargePages => (BitField & 0x1) > 0;           // :1
    internal bool IsProtectedProcess => (BitField & 0x2) > 0;            // :1
    internal bool IsImageDynamicallyRelocated => (BitField & 0x4) > 0;   // :1
    internal bool SkipPatchingUser32Forwarders => (BitField & 0x8) > 0;  // :1
    internal bool IsPackagedProcess => (BitField & 0x10) > 0;            // :1
    internal bool IsAppContainer => (BitField & 0x20) > 0;               // :1
    internal bool IsProtectedProcessLight => (BitField & 0x40) > 0;      // :1
    internal bool IsLongPathAwareProcess => (BitField & 0x80) > 0;       // :1

    internal readonly nint Mutant;
    internal readonly nint ImageBaseAddress;
    internal readonly nint Ldr;                // PPEB_LDR_DATA.
    internal readonly nint ProcessParameters;  // PRTL_USER_PROCESS_PARAMETERS.
    internal readonly nint SubSystemData;
    internal readonly nint ProcessHeap;
    internal readonly nint FastPebLock;        // PRTL_CRITICAL_SECTION.
    internal readonly nint AtlThunkSListPtr;   // PSLIST_HEADER.
    internal readonly nint IFEOKey;

    private readonly uint CrossProcessFlags;
    internal bool ProcessInJob => (CrossProcessFlags & 0x1) > 0;                 // :1
    internal bool ProcessInitializing => (CrossProcessFlags & 0x2) > 0;          // :1
    internal bool ProcessUsingVEH => (CrossProcessFlags & 0x4) > 0;              // :1
    internal bool ProcessUsingVCH => (CrossProcessFlags & 0x8) > 0;              // :1
    internal bool ProcessUsingFTH => (CrossProcessFlags & 0x10) > 0;             // :1
    internal bool ProcessPreviouslyThrottled => (CrossProcessFlags & 0x20) > 0;  // :1
    internal bool ProcessCurrentlyThrottled => (CrossProcessFlags & 0x40) > 0;   // :1
    internal bool ProcessImagesHotPatched => (CrossProcessFlags & 0x80) > 0;     // :1   // REDSTONE5.
    internal uint ReservedBits0 => CrossProcessFlags >> 8;                       // :24

    private readonly PEB_Union1 Union1;
    internal nint KernelCallbackTable => Union1.KernelCallbackTable;
    internal nint UserSharedInfoPtr => Union1.UserSharedInfoPtr;

    internal readonly uint SystemReserved;
    internal readonly uint AtlThunkSListPtr32;
    internal readonly nint ApiSetMap;                // PAPI_SET_NAMESPACE.
    internal readonly uint TlsExpansionCounter;
    internal readonly nint TlsBitmap;                //PRTL_BITMAP.

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
    internal readonly uint[] TlsBitmapBits;            // TLS_MINIMUM_AVAILABLE

    internal readonly nint ReadOnlySharedMemoryBase;
    internal readonly nint SharedData;               // HotpatchInformation. PSILO_USER_SHARED_DATA.
    internal readonly nint ReadOnlyStaticServerData; //PVOID*.
    internal readonly nint AnsiCodePageData;         // PCPTABLEINFO
    internal readonly nint OemCodePageData;          // PCPTABLEINFO
    internal readonly nint UnicodeCaseTableData;     // PNLSTABLEINFO
    internal readonly uint NumberOfProcessors;
    internal readonly uint NtGlobalFlag;
    internal readonly ULARGE_INTEGER CriticalSectionTimeout;
    internal readonly ulong HeapSegmentReserve;
    internal readonly ulong HeapSegmentCommit;
    internal readonly ulong HeapDeCommitTotalFreeThreshold;
    internal readonly ulong HeapDeCommitFreeBlockThreshold;
    internal readonly uint NumberOfHeaps;
    internal readonly uint MaximumNumberOfHeaps;
    internal readonly nint ProcessHeaps;             // PHEAP. PVOID*.
    internal readonly nint GdiSharedHandleTable;     // PGDI_SHARED_MEMORY
    internal readonly nint ProcessStarterHelper;
    internal readonly uint GdiDCAttributeList;
    internal readonly nint LoaderLock;               // PRTL_CRITICAL_SECTION.
    internal readonly uint OSMajorVersion;
    internal readonly uint OSMinorVersion;
    internal readonly ushort OSBuildNumber;
    internal readonly ushort OSCSDVersion;
    internal readonly uint OSPlatformId;
    internal readonly uint ImageSubsystem;
    internal readonly uint ImageSubsystemMajorVersion;
    internal readonly uint ImageSubsystemMinorVersion;
    internal readonly ulong ActiveProcessAffinityMask; // KAFFINITY (ULONG_PTR).

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 60)]
    internal readonly uint[] GdiHandleBuffer;          // GDI_HANDLE_BUFFER (ULONG[GDI_HANDLE_BUFFER_SIZE(60)]).

    internal readonly nint PostProcessInitRoutine;
    internal readonly nint TlsExpansionBitmap;       // PRTL_BITMAP.

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
    internal readonly uint[] TlsExpansionBitmapBits;   // TLS_EXPANSION_SLOTS

    internal readonly uint SessionId;
    internal readonly ULARGE_INTEGER AppCompatFlags;              // KACF_*
    internal readonly ULARGE_INTEGER AppCompatFlagsUser;
    internal readonly nint pShimData;
    internal readonly nint AppCompatInfo;                       // APPCOMPAT_EXE_DATA
    internal readonly UNICODE_STRING CSDVersion;
    internal readonly nint ActivationContextData;               // PACTIVATION_CONTEXT_DATA.
    internal readonly nint ProcessAssemblyStorageMap;           // PASSEMBLY_STORAGE_MAP.
    internal readonly nint SystemDefaultActivationContextData;  // PACTIVATION_CONTEXT_DATA.
    internal readonly nint SystemAssemblyStorageMap;            // PASSEMBLY_STORAGE_MAP.
    internal readonly ulong MinimumStackCommit;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
    internal readonly nint[] SparePointers;    // 19H1 (previously FlsCallback to FlsHighIndex)

    internal readonly nint PatchLoaderData;
    internal readonly nint ChpeV2ProcessInfo;  // _CHPEV2_PROCESS_INFO
    internal readonly uint AppModelFeatureState;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
    internal readonly uint[] SpareUlongs;

    internal readonly ushort ActiveCodePage;
    internal readonly ushort OemCodePage;
    internal readonly ushort UseCaseMapping;
    internal readonly ushort UnusedNlsField;
    internal readonly nint WerRegistrationData;
    internal readonly nint WerShipAssertPtr;

    private readonly PEB_Union2 Union2;
    internal nint ContextData => Union2.pContextData;   // WIN7.
    internal nint EcCodeBitMap => Union2.EcCodeBitMap;  // WIN11.

    internal readonly nint pImageHeaderHash;

    private readonly uint TracingFlags;
    internal bool HeapTracingEnabled => (TracingFlags & 0x1) > 0;       // :1
    internal bool CritSecTracingEnabled => (TracingFlags & 0x2) > 0;    // :1
    internal bool LibLoaderTracingEnabled => (TracingFlags & 0x4) > 0;  // :1
    internal uint SpareTracingBits => TracingFlags >> 3;                // :29

    internal readonly ulong CsrServerReadOnlySharedMemoryBase;
    internal readonly nint TppWorkerpListLock;        // PRTL_CRITICAL_SECTION.
    internal readonly LIST_ENTRY TppWorkerpList;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
    internal readonly nint[] WaitOnAddressHashTable;

    internal readonly nint TelemetryCoverageHeader;   // REDSTONE3. PTELEMETRY_COVERAGE_HEADER.
    internal readonly uint CloudFileFlags;
    internal readonly uint CloudFileDiagFlags;          // REDSTONE4
    internal readonly sbyte PlaceholderCompatibilityMode;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
    internal readonly sbyte[] PlaceholderCompatibilityModeReserved;

    internal readonly nint LeapSecondData;            // REDSTONE5. PLEAP_SECOND_DATA.

    private readonly uint LeapSecondFlags;
    internal bool SixtySecondEnabled => (LeapSecondFlags & 0x1) > 0;  // :1
    internal uint Reserved => LeapSecondFlags >> 1;                   // :31

    internal readonly uint NtGlobalFlag2;
    internal readonly ulong ExtendedFeatureDisableMask; // since WIN11
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal readonly struct TEB
{
    internal readonly NT_TIB NtTib;
    internal readonly nint EnvironmentPointer;
    internal readonly CLIENT_ID ClientId;
    internal readonly nint ActiveRpcHandle;
    internal readonly nint ThreadLocalStoragePointer;
    internal readonly nint ProcessEnvironmentBlock; // PPEB.
    internal readonly uint LastErrorValue;
    internal readonly uint CountOfOwnedCriticalSections;
    internal readonly nint CsrClientThread;
    internal readonly nint Win32ThreadInfo;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 26)]
    internal readonly uint[] User32Reserved;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
    internal readonly uint[] UserReserved;

    internal readonly nint WOW32Reserved;
    internal readonly uint CurrentLocale; // LCID.
    internal readonly uint FpSoftwareStatusRegister;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    internal readonly nint[] ReservedForDebuggerInstrumentation;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
    internal readonly nint[] SystemReserved1;

    internal readonly sbyte PlaceholderCompatibilityMode;
    internal readonly byte PlaceholderHydrationAlwaysExplicit;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
    internal readonly sbyte[] PlaceholderReserved;

    internal readonly uint ProxiedProcessId;
    internal readonly ACTIVATION_CONTEXT_STACK ActivationStack;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    internal readonly byte[] WorkingOnBehalfTicket;

    internal readonly int ExceptionCode; // NTSTATUS.
    internal readonly nint ActivationContextStackPointer; // PACTIVATION_CONTEXT_STACK.
    internal readonly ulong InstrumentationCallbackSp;
    internal readonly ulong InstrumentationCallbackPreviousPc;
    internal readonly ulong InstrumentationCallbackPreviousSp;
    internal readonly uint TxFsContext;
    internal readonly byte InstrumentationCallbackDisabled;
    internal readonly byte UnalignedLoadStoreExceptions;
    internal readonly GDI_TEB_BATCH GdiTebBatch;
    internal readonly CLIENT_ID RealClientId;
    internal readonly nint GdiCachedProcessHandle;
    internal readonly uint GdiClientPID;
    internal readonly uint GdiClientTID;
    internal readonly nint GdiThreadLocalInfo;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 62)]
    internal readonly ulong[] Win32ClientInfo;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 233)]
    internal readonly nint[] glDispatchTable;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 29)]
    internal readonly ulong[] glReserved1;

    internal readonly nint glReserved2;
    internal readonly nint glSectionInfo;
    internal readonly nint glSection;
    internal readonly nint glTable;
    internal readonly nint glCurrentRC;
    internal readonly nint glContext;
    internal readonly int LastStatusValue; // NTSTATUS
    internal readonly UNICODE_STRING StaticUnicodeString;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 261)]
    internal readonly char[] StaticUnicodeBuffer;

    internal readonly nint DeallocationStack;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
    internal readonly nint[] TlsSlots;

    internal readonly LIST_ENTRY TlsLinks;
    internal readonly nint Vdm;
    internal readonly nint ReservedForNtRpc;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
    internal readonly nint DbgSsReserved;

    internal readonly uint HardErrorMode;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 11)]
    internal readonly nint[] Instrumentation;

    internal readonly GUID ActivityId;
    internal readonly nint SubProcessTag;
    internal readonly nint PerflibData;
    internal readonly nint EtwTraceData;
    internal readonly nint WinSockData;
    internal readonly uint GdiBatchCount;

    private readonly TEB_Union1 Union1;
    internal PROCESSOR_NUMBER CurrentIdealProcessor => Union1.CurrentIdealProcessor;
    internal uint IdealProcessorValue => Union1.IdealProcessorValue;
    internal byte IdealProcessor => Union1.IdealProcessor;

    internal readonly uint GuaranteedStackBytes;
    internal readonly nint ReservedForPerf;
    internal readonly nint ReservedForOle; // tagSOleTlsData
    internal readonly uint WaitingOnLoaderLock;
    internal readonly nint SavedPriorityState;
    internal readonly ulong ReservedForCodeCoverage;
    internal readonly nint ThreadPoolData;
    internal readonly nint TlsExpansionSlots; // PVOID*.
    internal readonly nint DeallocationBStore;
    internal readonly nint BStoreLimit;
    internal readonly uint MuiGeneration;
    internal readonly uint IsImpersonating;
    internal readonly nint NlsCache;
    internal readonly nint pShimData;
    internal readonly uint HeapData;
    internal readonly nint CurrentTransactionHandle;
    internal readonly nint ActiveFrame; // PTEB_ACTIVE_FRAME.
    internal readonly nint FlsData;
    internal readonly nint PreferredLanguages;
    internal readonly nint UserPrefLanguages;
    internal readonly nint MergedPrefLanguages;
    internal readonly uint MuiImpersonation;

    private readonly TEB_Union2 Union2;
    internal ushort CrossTebFlags => Union2.CrossTebFlags;
    internal ushort SpareCrossTebBits => Union2.SpareCrossTebBits;

    private readonly ushort SameTebFlags;
    internal bool SafeThunkCall => (SameTebFlags & 0x1) > 0;            // :1
    internal bool InDebugPrint => (SameTebFlags & 0x2) > 0;             // :1
    internal bool HasFiberData => (SameTebFlags & 0x4) > 0;             // :1
    internal bool SkipThreadAttach => (SameTebFlags & 0x8) > 0;         // :1
    internal bool WerInShipAssertCode => (SameTebFlags & 0x10) > 0;     // :1
    internal bool RanProcessInit => (SameTebFlags & 0x20) > 0;          // :1
    internal bool ClonedThread => (SameTebFlags & 0x40) > 0;            // :1
    internal bool SuppressDebugMsg => (SameTebFlags & 0x80) > 0;        // :1
    internal bool DisableUserStackWalk => (SameTebFlags & 0x100) > 0;   // :1
    internal bool RtlExceptionAttached => (SameTebFlags & 0x200) > 0;   // :1
    internal bool InitialThread => (SameTebFlags & 0x400) > 0;          // :1
    internal bool SessionAware => (SameTebFlags & 0x800) > 0;           // :1
    internal bool LoadOwner => (SameTebFlags & 0x1000) > 0;             // :1
    internal bool LoaderWorker => (SameTebFlags & 0x2000) > 0;          // :1
    internal bool SkipLoaderInit => (SameTebFlags & 0x4000) > 0;        // :1
    internal bool SkipFileAPIBrokering => (SameTebFlags & 0x8000) > 0;  // :1

    internal readonly nint TxnScopeEnterCallback;
    internal readonly nint TxnScopeExitCallback;
    internal readonly nint TxnScopeContext;
    internal readonly uint LockCount;
    internal readonly int WowTebOffset;
    internal readonly nint ResourceRetValue;
    internal readonly nint ReservedForWdf;
    internal readonly ulong ReservedForCrt;
    internal readonly GUID EffectiveContainerId;
    internal readonly ulong LastSleepCounter; // Win11
    internal readonly uint SpinCallCount;
    internal readonly ulong ExtendedFeatureDisableMask;
}

[StructLayout(LayoutKind.Sequential)]
internal struct PROCESSOR_NUMBER
{
    internal ushort Group;
    internal byte Number;
    internal byte Reserved;
}

[StructLayout(LayoutKind.Explicit)]
internal struct NT_TIB
{
    [FieldOffset(0)] internal nint ExceptionList; // EXCEPTION_REGISTRATION_RECORD*.
    [FieldOffset(8)] internal nint StackBase;
    [FieldOffset(16)] internal nint StackLimit;
    [FieldOffset(24)] internal nint SubSystemTib;
    [FieldOffset(32)] internal nint FiberData;
    [FieldOffset(32)] internal uint Version;
    [FieldOffset(40)] internal nint ArbitraryUserPointer;
    [FieldOffset(48)] internal nint Self; // NT_TIB*.
}

[StructLayout(LayoutKind.Sequential)]
internal struct CLIENT_ID
{
    internal nint UniqueProcess;
    internal nint UniqueThread;
}

[StructLayout(LayoutKind.Sequential)]
internal struct ACTIVATION_CONTEXT_STACK
{
    internal nint ActiveFrame; // PRTL_ACTIVATION_CONTEXT_STACK_FRAME.
    internal LIST_ENTRY FrameListCache;
    internal uint Flags; // ACTIVATION_CONTEXT_STACK_FLAG_*
    internal uint NextCookieSequenceNumber;
    internal uint StackId;
}

[StructLayout(LayoutKind.Sequential)]
internal struct GDI_TEB_BATCH
{
    internal uint Offset;
    internal ulong HDC;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 310)]
    internal uint[] Buffer;
}

[StructLayout(LayoutKind.Explicit)]
internal struct PEB_Union1
{
    [FieldOffset(0)] internal nint KernelCallbackTable;
    [FieldOffset(0)] internal nint UserSharedInfoPtr;
}

[StructLayout(LayoutKind.Explicit)]
internal struct PEB_Union2
{
    [FieldOffset(0)] internal nint pContextData; // WIN7
    [FieldOffset(0)] internal nint pUnused; // WIN10
    [FieldOffset(0)] internal nint EcCodeBitMap; // WIN11
}

[StructLayout(LayoutKind.Explicit)]
internal struct TEB_Union1
{
    [FieldOffset(0)] internal PROCESSOR_NUMBER CurrentIdealProcessor;
    [FieldOffset(4)] internal uint IdealProcessorValue;
    [FieldOffset(0)] internal byte ReservedPad0;
    [FieldOffset(1)] internal byte ReservedPad1;
    [FieldOffset(2)] internal byte ReservedPad2;
    [FieldOffset(3)] internal byte IdealProcessor;
}

[StructLayout(LayoutKind.Explicit)]
internal struct TEB_Union2
{
    [FieldOffset(0)] internal ushort CrossTebFlags;
    [FieldOffset(0)] internal ushort SpareCrossTebBits;
}

#endregion

#region x86

[StructLayout(LayoutKind.Sequential)]
internal readonly struct PEB32
{
    internal readonly byte InheritedAddressSpace;
    internal readonly byte ReadImageFileExecOptions;
    internal readonly byte BeingDebugged;

    internal readonly byte BitField;
    internal bool ImageUsesLargePages => (BitField & 0x1) > 0;           // :1.
    internal bool IsProtectedProcess => (BitField & 0x2) > 0;            // :1.
    internal bool IsImageDynamicallyRelocated => (BitField & 0x4) > 0;   // :1.
    internal bool SkipPatchingUser32Forwarders => (BitField & 0x8) > 0;  // :1.
    internal bool IsPackagedProcess => (BitField & 0x10) > 0;            // :1.
    internal bool IsAppContainer => (BitField & 0x20) > 0;               // :1.
    internal bool IsProtectedProcessLight => (BitField & 0x40) > 0;      // :1.
    internal bool IsLongPathAwareProcess => (BitField & 0x80) > 0;       // :1.

    internal readonly uint Mutant;             // HANDLE.
    internal readonly uint ImageBaseAddress;   // PVOID.
    internal readonly uint Ldr;                // PPEB_LDR_DATA.
    internal readonly uint ProcessParameters;  // PRTL_USER_PROCESS_PARAMETERS.
    internal readonly uint SubSystemData;      // PVOID.
    internal readonly uint ProcessHeap;        // PVOID.
    internal readonly uint FastPebLock;        // PRTL_CRITICAL_SECTION.
    internal readonly uint AtlThunkSListPtr;   // PVOID.
    internal readonly uint IFEOKey;            // PVOID.

    internal readonly uint CrossProcessFlags;
    internal bool ProcessInJob => (CrossProcessFlags & 0x1) > 0;
    internal bool ProcessInitializing => (CrossProcessFlags & 0x2) > 0;
    internal bool ProcessUsingVEH => (CrossProcessFlags & 0x4) > 0;
    internal bool ProcessUsingVCH => (CrossProcessFlags & 0x8) > 0;
    internal bool ProcessUsingFTH => (CrossProcessFlags & 0x10) > 0;
    internal uint ReservedBits0 => CrossProcessFlags >> 5;

    private readonly PEB32_Union1 Union1;
    internal uint KernelCallbackTable => Union1.KernelCallbackTable;
    internal uint UserSharedInfoPtr => Union1.UserSharedInfoPtr;

    internal readonly uint SystemReserved;
    internal readonly uint AtlThunkSListPtr32;
    internal readonly uint ApiSetMap; // PVOID.
    internal readonly uint TlsExpansionCounter;
    internal readonly uint TlsBitmap; // PVOID.

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
    internal readonly uint[] TlsBitmapBits;

    internal readonly uint ReadOnlySharedMemoryBase; // PVOID.
    internal readonly uint SharedData; // PVOID.
    internal readonly uint ReadOnlyStaticServerData; // PVOID*.
    internal readonly uint AnsiCodePageData; // PVOID.
    internal readonly uint OemCodePageData; // PVOID.
    internal readonly uint UnicodeCaseTableData; // PVOID.
    internal readonly uint NumberOfProcessors;
    internal readonly uint NtGlobalFlag;
    internal readonly LARGE_INTEGER CriticalSectionTimeout;
    internal readonly uint HeapSegmentReserve;
    internal readonly uint HeapSegmentCommit;
    internal readonly uint HeapDeCommitTotalFreeThreshold;
    internal readonly uint HeapDeCommitFreeBlockThreshold;
    internal readonly uint NumberOfHeaps;
    internal readonly uint MaximumNumberOfHeaps;
    internal readonly uint ProcessHeaps; // PVOID*.
    internal readonly uint GdiSharedHandleTable; // PVOID.
    internal readonly uint ProcessStarterHelper; // PVOID.
    internal readonly uint GdiDCAttributeList;
    internal readonly uint LoaderLock; // PRTL_CRITICAL_SECTION.
    internal readonly uint OSMajorVersion;
    internal readonly uint OSMinorVersion;
    internal readonly ushort OSBuildNumber;
    internal readonly ushort OSCSDVersion;
    internal readonly uint OSPlatformId;
    internal readonly uint ImageSubsystem;
    internal readonly uint ImageSubsystemMajorVersion;
    internal readonly uint ImageSubsystemMinorVersion;
    internal readonly uint ActiveProcessAffinityMask; // ULONG_PTR.

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 34)]
    internal readonly uint[] GdiHandleBuffer; // GDI_HANDLE_BUFFER32.

    internal readonly uint PostProcessInitRoutine; // PVOID.
    internal readonly uint TlsExpansionBitmap; // PVOID.

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
    internal readonly uint[] TlsExpansionBitmapBits;

    internal readonly uint SessionId;
    internal readonly ULARGE_INTEGER AppCompatFlags;
    internal readonly ULARGE_INTEGER AppCompatFlagsUser;
    internal readonly uint pShimData; // PVOID.
    internal readonly uint AppCompatInfo; // PVOID.
    internal readonly UNICODE_STRING32 CSDVersion;
    internal readonly uint ActivationContextData; // PACTIVATION_CONTEXT_DATA.
    internal readonly uint ProcessAssemblyStorageMap; // PVOID.
    internal readonly uint SystemDefaultActivationContextData; // PACTIVATION_CONTEXT_DATA.
    internal readonly uint SystemAssemblyStorageMap; // PVOID.
    internal readonly uint MinimumStackCommit;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
    internal readonly uint[] SparePointers; // 19H1 (previously FlsCallback to FlsHighIndex)

    internal readonly uint PatchLoaderData; // PVOID.
    internal readonly uint ChpeV2ProcessInfo; // _CHPEV2_PROCESS_INFO
    internal readonly uint AppModelFeatureState;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
    internal readonly uint[] SpareUlongs;

    internal readonly ushort ActiveCodePage;
    internal readonly ushort OemCodePage;
    internal readonly ushort UseCaseMapping;
    internal readonly ushort UnusedNlsField;
    internal readonly uint WerRegistrationData; // PVOID.
    internal readonly uint WerShipAssertPtr; // PVOID.

    private readonly PEB32_Union2 Union2;
    internal uint ContextData => Union2.pContextData;   // WIN7.
    internal uint EcCodeBitMap => Union2.EcCodeBitMap;  // WIN11.

    internal readonly uint pImageHeaderHash; // PVOID.

    private readonly uint TracingFlags;
    internal bool HeapTracingEnabled => (TracingFlags & 0x1) > 0;       // :1
    internal bool CritSecTracingEnabled => (TracingFlags & 0x2) > 0;    // :1
    internal bool LibLoaderTracingEnabled => (TracingFlags & 0x4) > 0;  // :1
    internal uint SpareTracingBits => TracingFlags >> 3;                // :29

    internal readonly ulong CsrServerReadOnlySharedMemoryBase;
    internal readonly uint TppWorkerpListLock;        // PRTL_CRITICAL_SECTION.
    internal readonly LIST_ENTRY32 TppWorkerpList;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
    internal readonly uint[] WaitOnAddressHashTable; // PVOID.

    internal readonly uint TelemetryCoverageHeader; // REDSTONE3. PVOID.
    internal readonly uint CloudFileFlags;
    internal readonly uint CloudFileDiagFlags; // REDSTONE4
    internal readonly sbyte PlaceholderCompatibilityMode;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
    internal readonly sbyte[] PlaceholderCompatibilityModeReserved;

    internal readonly uint LeapSecondData; // REDSTONE5. PLEAP_SECOND_DATA.

    private readonly uint LeapSecondFlags;
    internal bool SixtySecondEnabled => (LeapSecondFlags & 0x1) > 0;  // :1
    internal uint Reserved => LeapSecondFlags >> 1;                   // :31

    internal readonly uint NtGlobalFlag2;
    internal readonly ulong ExtendedFeatureDisableMask; // since WIN11
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal readonly struct TEB32
{
    internal readonly NT_TIB32 NtTib;
    internal readonly uint EnvironmentPointer; // PVOID.
    internal readonly CLIENT_ID32 ClientId;
    internal readonly uint ActiveRpcHandle; // PVOID.
    internal readonly uint ThreadLocalStoragePointer; // PVOID.
    internal readonly uint ProcessEnvironmentBlock; // PPEB.
    internal readonly uint LastErrorValue;
    internal readonly uint CountOfOwnedCriticalSections;
    internal readonly uint CsrClientThread; // PVOID.
    internal readonly uint Win32ThreadInfo; // PVOID.

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 26)]
    internal readonly uint[] User32Reserved;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
    internal readonly uint[] UserReserved;

    internal readonly uint WOW32Reserved; // PVOID.
    internal readonly uint CurrentLocale; // LCID.
    internal readonly uint FpSoftwareStatusRegister;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    internal readonly uint[] ReservedForDebuggerInstrumentation; // PVOID.

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 36)]
    internal readonly uint[] SystemReserved1; // PVOID.

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    internal readonly byte[] WorkingOnBehalfTicket;

    internal readonly int ExceptionCode; // NTSTATUS.

    internal readonly uint ActivationContextStackPointer; // PVOID.
    internal readonly uint InstrumentationCallbackSp; // ULONG_PTR.
    internal readonly uint InstrumentationCallbackPreviousPc; // ULONG_PTR.
    internal readonly uint InstrumentationCallbackPreviousSp; // ULONG_PTR.
    internal readonly byte InstrumentationCallbackDisabled;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 23)]
    internal readonly byte[] SpareBytes;

    internal readonly uint TxFsContext;
    internal readonly GDI_TEB_BATCH32 GdiTebBatch;
    internal readonly CLIENT_ID32 RealClientId;
    internal readonly uint GdiCachedProcessHandle;
    internal readonly uint GdiClientPID;
    internal readonly uint GdiClientTID;
    internal readonly uint GdiThreadLocalInfo; // PVOID.

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 62)]
    internal readonly uint[] Win32ClientInfo;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 233)]
    internal readonly uint[] glDispatchTable;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 29)]
    internal readonly uint[] glReserved1;

    internal readonly uint glReserved2; // PVOID.
    internal readonly uint glSectionInfo; // PVOID.
    internal readonly uint glSection; // PVOID.
    internal readonly uint glTable; // PVOID.
    internal readonly uint glCurrentRC; // PVOID.
    internal readonly uint glContext; // PVOID.

    internal readonly int LastStatusValue; // NTSTATUS.
    internal readonly UNICODE_STRING32 StaticUnicodeString;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 261)]
    internal readonly char StaticUnicodeBuffer;

    internal readonly uint DeallocationStack; // PVOID.

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
    internal readonly uint[] TlsSlots; // PVOID.

    internal readonly LIST_ENTRY32 TlsLinks;
    internal readonly uint Vdm; // PVOID.
    internal readonly uint ReservedForNtRpc; // PVOID.

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
    internal readonly uint[] DbgSsReserved; // PVOID.

    internal readonly uint HardErrorMode;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
    internal readonly uint[] Instrumentation; // PVOID

    internal readonly GUID ActivityId;

    internal readonly uint SubProcessTag; // PVOID.
    internal readonly uint PerflibData; // PVOID.
    internal readonly uint EtwTraceData; // PVOID.
    internal readonly uint WinSockData; // PVOID.
    internal readonly uint GdiBatchCount;

    private readonly TEB32_Union1 Union1;
    internal PROCESSOR_NUMBER CurrentIdealProcessor => Union1.CurrentIdealProcessor;
    internal uint IdealProcessorValue => Union1.IdealProcessorValue;
    internal byte IdealProcessor => Union1.IdealProcessor;

    internal readonly uint GuaranteedStackBytes;
    internal readonly uint ReservedForPerf; // PVOID.
    internal readonly uint ReservedForOle; // PVOID.
    internal readonly uint WaitingOnLoaderLock;
    internal readonly uint SavedPriorityState; // PVOID.
    internal readonly uint ReservedForCodeCoverage; // ULONG_PTR.
    internal readonly uint ThreadPoolData; // PVOID.
    internal readonly uint TlsExpansionSlots; // PVOID*.

    internal readonly uint MuiGeneration;
    internal readonly uint IsImpersonating;
    internal readonly uint NlsCache; // PVOID.
    internal readonly uint pShimData; // PVOID.
    internal readonly ushort HeapVirtualAffinity;
    internal readonly ushort LowFragHeapDataSlot;
    internal readonly uint CurrentTransactionHandle;
    internal readonly uint ActiveFrame; // PTEB_ACTIVE_FRAME.
    internal readonly uint FlsData; // PVOID.
    internal readonly uint PreferredLanguages; // PVOID.
    internal readonly uint UserPrefLanguages; // PVOID.
    internal readonly uint MergedPrefLanguages; // PVOID.
    internal readonly uint MuiImpersonation;

    private readonly TEB_Union2 Union2;
    internal ushort CrossTebFlags => Union2.CrossTebFlags;
    internal ushort SpareCrossTebBits => Union2.SpareCrossTebBits;

    private readonly ushort SameTebFlags;
    internal bool SafeThunkCall => (SameTebFlags & 0x1) > 0;            // :1
    internal bool InDebugPrint => (SameTebFlags & 0x2) > 0;             // :1
    internal bool HasFiberData => (SameTebFlags & 0x4) > 0;             // :1
    internal bool SkipThreadAttach => (SameTebFlags & 0x8) > 0;         // :1
    internal bool WerInShipAssertCode => (SameTebFlags & 0x10) > 0;     // :1
    internal bool RanProcessInit => (SameTebFlags & 0x20) > 0;          // :1
    internal bool ClonedThread => (SameTebFlags & 0x40) > 0;            // :1
    internal bool SuppressDebugMsg => (SameTebFlags & 0x80) > 0;        // :1
    internal bool DisableUserStackWalk => (SameTebFlags & 0x100) > 0;   // :1
    internal bool RtlExceptionAttached => (SameTebFlags & 0x200) > 0;   // :1
    internal bool InitialThread => (SameTebFlags & 0x400) > 0;          // :1
    internal bool SessionAware => (SameTebFlags & 0x800) > 0;           // :1
    internal bool LoadOwner => (SameTebFlags & 0x1000) > 0;             // :1
    internal bool LoaderWorker => (SameTebFlags & 0x2000) > 0;          // :1
    internal byte SpareSameTebBits => (byte)(SameTebFlags >> 14);       // :2

    internal readonly uint TxnScopeEnterCallback; // PVOID.
    internal readonly uint TxnScopeExitCallback; // PVOID.
    internal readonly uint TxnScopeContext; // PVOID.
    internal readonly uint LockCount;
    internal readonly int WowTebOffset;
    internal readonly uint ResourceRetValue; // PVOID.
    internal readonly uint ReservedForWdf; // PVOID.
    internal readonly ulong ReservedForCrt;
    internal readonly GUID EffectiveContainerId;
}

[StructLayout(LayoutKind.Explicit)]
internal struct NT_TIB32
{
    [FieldOffset(0)] internal uint ExceptionList; // EXCEPTION_REGISTRATION_RECORD*.
    [FieldOffset(8)] internal uint StackBase;
    [FieldOffset(16)] internal uint StackLimit;
    [FieldOffset(24)] internal uint SubSystemTib;
    [FieldOffset(32)] internal uint FiberData;
    [FieldOffset(32)] internal uint Version;
    [FieldOffset(40)] internal uint ArbitraryUserPointer;
    [FieldOffset(48)] internal uint Self; // NT_TIB*.
}

[StructLayout(LayoutKind.Sequential)]
internal struct CLIENT_ID32
{
    internal uint UniqueProcess;
    internal uint UniqueThread;
}

[StructLayout(LayoutKind.Sequential)]
internal struct GDI_TEB_BATCH32
{
    internal uint Offset;
    internal uint HDC;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 310)]
    internal uint[] Buffer;
}

[StructLayout(LayoutKind.Explicit)]
internal readonly struct PEB32_Union1
{
    [FieldOffset(0)] internal readonly uint KernelCallbackTable;
    [FieldOffset(0)] internal readonly uint UserSharedInfoPtr;
}

[StructLayout(LayoutKind.Explicit)]
internal readonly struct PEB32_Union2
{
    [FieldOffset(0)] internal readonly uint pContextData;  // WIN7
    [FieldOffset(0)] internal readonly uint pUnused;       // WIN10
    [FieldOffset(0)] internal readonly uint EcCodeBitMap;  // WIN11
}

[StructLayout(LayoutKind.Explicit)]
internal struct TEB32_Union1
{
    [FieldOffset(0)] internal PROCESSOR_NUMBER CurrentIdealProcessor;
    [FieldOffset(4)] internal uint IdealProcessorValue;
    [FieldOffset(0)] internal byte ReservedPad0;
    [FieldOffset(1)] internal byte ReservedPad1;
    [FieldOffset(2)] internal byte ReservedPad2;
    [FieldOffset(3)] internal byte IdealProcessor;
}

[StructLayout(LayoutKind.Explicit)]
internal struct TEB32_Union2
{
    [FieldOffset(0)] internal ushort CrossTebFlags;
    [FieldOffset(0)] internal ushort SpareCrossTebBits;
}

#endregion