// This file is part of the ManagedStrings project and repository.
// Project files are licensed under the MIT license.
// https://github.com/FranciscoNabas/ManagedStrings

using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using ManagedStrings.Engine;

namespace ManagedStrings.Interop.Windows;

#region Enumerations

/// <summary>
/// Process information class.
/// </summary>
/// <remarks>
/// Not the entire enumeration.
/// Used by <see cref="NativeProcess.NtQueryInformationProcess(SafeProcessHandle, ManagedStrings.Interop.Windows.PROCESSINFOCLASS, nint, int, out int)"/>.
/// </remarks>
internal enum PROCESSINFOCLASS
{
    ProcessBasicInformation,
    ProcessWow64Information = 26,
}

/// <summary>
/// Process access flags.
/// </summary>
[Flags]
internal enum ProcessAccess : uint
{
    TERMINATE                  = 0x0001,
    CREATE_THREAD              = 0x0002,
    SET_SESSIONID              = 0x0004,
    VM_OPERATION               = 0x0008,
    VM_READ                    = 0x0010,
    VM_WRITE                   = 0x0020,
    DUP_HANDLE                 = 0x0040,
    CREATE_PROCESS             = 0x0080,
    SET_QUOTA                  = 0x0100,
    SET_INFORMATION            = 0x0200,
    QUERY_INFORMATION          = 0x0400,
    SUSPEND_RESUME             = 0x0800,
    QUERY_LIMITED_INFORMATION  = 0x1000,
    SET_LIMITED_INFORMATION    = 0x2000,
    ALL_ACCESS                 = AccessType.STANDARD_RIGHTS_REQUIRED | AccessType.SYNCHRONIZE | 0xFFFF,
}

/// <summary>
/// Memory information class.
/// </summary>
/// <remarks>
/// Used by <see cref="NativeProcess.NtQueryVirtualMemory(SafeProcessHandle, nint, MEMORY_INFORMATION_CLASS, nint, long, out long)"/>.
/// </remarks>
internal enum MEMORY_INFORMATION_CLASS
{
    BasicInformation,               // MEMORY_BASIC_INFORMATION.
    WorkingSetInformation,          // MEMORY_WORKING_SET_INFORMATION.
    MappedFilenameInformation,      // UNICODE_STRING.
    RegionInformation,              // MEMORY_REGION_INFORMATION.
    WorkingSetExInformation,        // MEMORY_WORKING_SET_EX_INFORMATION. Since VISTA.
    SharedCommitInformation,        // MEMORY_SHARED_COMMIT_INFORMATION. Since WIN8.
    ImageInformation,               // MEMORY_IMAGE_INFORMATION.
    RegionInformationEx,            // MEMORY_REGION_INFORMATION.
    PrivilegedBasicInformation,     // MEMORY_BASIC_INFORMATION.
    EnclaveImageInformation,        // MEMORY_ENCLAVE_IMAGE_INFORMATION Since REDSTONE3.
    BasicInformationCapped,         // 10.
    PhysicalContiguityInformation,  // MEMORY_PHYSICAL_CONTIGUITY_INFORMATION. Since 20H1.
    BadInformation,                 // Since WIN11.
    BadInformationAllProcesses,     // Since 22H1.
}

/// <summary>
/// Virtual memory page protection flags.
/// </summary>
/// <remarks>
/// winnt.h
/// </remarks>
[Flags]
internal enum PageProtection : uint
{
    NOACCESS                    = 0x01,
    READONLY                    = 0x02,
    READWRITE                   = 0x04,
    WRITECOPY                   = 0x08,
    EXECUTE                     = 0x10,
    EXECUTE_READ                = 0x20,
    EXECUTE_READWRITE           = 0x40,
    EXECUTE_WRITECOPY           = 0x80,
    GUARD                       = 0x100,
    NOCACHE                     = 0x200,
    WRITECOMBINE                = 0x400,
    GRAPHICS_NOACCESS           = 0x800,
    GRAPHICS_READONLY           = 0x1000,
    GRAPHICS_READWRITE          = 0x2000,
    GRAPHICS_EXECUTE            = 0x4000,
    GRAPHICS_EXECUTE_READ       = 0x8000,
    GRAPHICS_EXECUTE_READWRITE  = 0x10000,
    GRAPHICS_COHERENT           = 0x20000,
    GRAPHICS_NOCACHE            = 0x40000,
    ENCLAVE_UNVALIDATED         = 0x20000000,
    ENCLAVE_THREAD_CONTROL      = 0x80000000,
    ENCLAVE_DECOMMIT            = 0x10000000 | 0,
    ENCLAVE_SS_FIRST            = 0x10000000 | 1,
    ENCLAVE_SS_REST             = 0x10000000 | 2,
}

/// <summary>
/// Virtual memory state.
/// </summary>
/// <remarks>
/// winnt.h
/// </remarks>
internal enum MemoryState : uint
{
    COMMIT                      = 0x00001000,
    RESERVE                     = 0x00002000,
    RESET                       = 0x00080000,
    TOP_DOWN                    = 0x00100000,
    WRITE_WATCH                 = 0x00200000,
    PHYSICAL                    = 0x00400000,
    ROTATE                      = 0x00800000,
    RESET_UNDO                  = 0x01000000,
    LARGE_PAGES                 = 0x20000000,
    FOURMB_PAGES                = 0x80000000,
    SIXTYFOURK_PAGES            = LARGE_PAGES | PHYSICAL,
    UNMAP_WITH_TRANSIENT_BOOST  = 0x00000001,
    DECOMMIT                    = 0x00004000,
    RELEASE                     = 0x00008000,
    FREE                        = 0x00010000,
}

/// <summary>
/// Virtual memory type.
/// </summary>
/// <remarks>
/// winnt.h
/// </remarks>
internal enum MemoryType : uint
{
    PRIVATE  = 0x00020000,
    MAPPED   = 0x00040000,
    IMAGE    = 0x01000000,
}

internal enum KTHREAD_STATE
{
    Initialized,
    Ready,
    Running,
    Standby,
    Terminated,
    Waiting,
    Transition,
    DeferredReady,
    GateWaitObsolete,
    WaitingForProcessInSwap,
    MaximumThreadState
}

internal enum KWAIT_REASON
{
    Executive,
    FreePage,
    PageIn,
    PoolAllocation,
    DelayExecution,
    Suspended,
    UserRequest,
    WrExecutive,
    WrFreePage,
    WrPageIn,
    WrPoolAllocation,
    WrDelayExecution,
    WrSuspended,
    WrUserRequest,
    WrEventPair,
    WrQueue,
    WrLpcReceive,
    WrLpcReply,
    WrVirtualMemory,
    WrPageOut,
    WrRendezvous,
    WrKeyedEvent,
    WrTerminated,
    WrProcessInSwap,
    WrCpuRateControl,
    WrCalloutStack,
    WrKernel,
    WrResource,
    WrPushLock,
    WrMutex,
    WrQuantumEnd,
    WrDispatchInt,
    WrPreempted,
    WrYieldExecution,
    WrFastMutex,
    WrGuardedMutex,
    WrRundown,
    WrAlertByThreadId,
    WrDeferredPreempt,
    WrPhysicalFault,
    WrIoRing,
    WrMdlCache,
    MaximumWaitReason
}

#endregion

#region Structures

[StructLayout(LayoutKind.Sequential)]
internal struct SYSTEM_THREAD_INFORMATION
{
    internal LARGE_INTEGER KernelTime;
    internal LARGE_INTEGER UserTime;
    internal LARGE_INTEGER CreateTime;
    internal uint WaitTime;
    internal ulong StartAddress;
    internal CLIENT_ID ClientId;
    internal int Priority; // KPRIORITY.'
    internal int BasePriority; // KPRIORITY.'
    internal uint ContextSwitches;
    internal KTHREAD_STATE ThreadState;
    internal KWAIT_REASON WaitReason;
}

[StructLayout(LayoutKind.Sequential)]
internal struct SYSTEM_EXTENDED_THREAD_INFORMATION
{
    internal SYSTEM_THREAD_INFORMATION ThreadInfo;
    internal nint StackBase;
    internal nint StackLimit;
    internal nint Win32StartAddress;
    internal nint TebBase; // PTEB since VISTA
    internal ulong Reserved2;
    internal ulong Reserved3;
    internal ulong Reserved4;
}

[StructLayout(LayoutKind.Sequential)]
internal struct SYSTEM_PROCESS_INFORMATION
{
    internal uint NextEntryOffset;
    internal uint NumberOfThreads;
    internal LARGE_INTEGER WorkingSetPrivateSize; // since VISTA
    internal uint HardFaultCount; // since WIN7
    internal uint NumberOfThreadsHighWatermark; // since WIN7
    internal ulong CycleTime; // since WIN7
    internal LARGE_INTEGER CreateTime;
    internal LARGE_INTEGER UserTime;
    internal LARGE_INTEGER KernelTime;
    internal UNICODE_STRING ImageName;
    internal int BasePriority; // KPRIORITY.
    internal nint UniqueProcessId;
    internal nint InheritedFromUniqueProcessId;
    internal uint HandleCount;
    internal uint SessionId;
    internal ulong UniqueProcessKey; // since VISTA (requires SystemExtendedProcessInformation)
    internal ulong PeakVirtualSize;
    internal ulong VirtualSize;
    internal uint PageFaultCount;
    internal ulong PeakWorkingSetSize;
    internal ulong WorkingSetSize;
    internal ulong QuotaPeakPagedPoolUsage;
    internal ulong QuotaPagedPoolUsage;
    internal ulong QuotaPeakNonPagedPoolUsage;
    internal ulong QuotaNonPagedPoolUsage;
    internal ulong PagefileUsage;
    internal ulong PeakPagefileUsage;
    internal ulong PrivatePageCount;
    internal LARGE_INTEGER ReadOperationCount;
    internal LARGE_INTEGER WriteOperationCount;
    internal LARGE_INTEGER OtherOperationCount;
    internal LARGE_INTEGER ReadTransferCount;
    internal LARGE_INTEGER WriteTransferCount;
    internal LARGE_INTEGER OtherTransferCount;
    // SYSTEM_THREAD_INFORMATION Threads[1]; // SystemProcessInformation
    private SYSTEM_EXTENDED_THREAD_INFORMATION _threads; // SystemExtendedProcessinformation
                                                         // SYSTEM_EXTENDED_THREAD_INFORMATION + SYSTEM_PROCESS_INFORMATION_EXTENSION // SystemFullProcessInformation

    internal SYSTEM_EXTENDED_THREAD_INFORMATION[] Threads => GetThreadInfo();

    private unsafe SYSTEM_EXTENDED_THREAD_INFORMATION[] GetThreadInfo()
    {
        SYSTEM_EXTENDED_THREAD_INFORMATION[] output = new SYSTEM_EXTENDED_THREAD_INFORMATION[NumberOfThreads];
        fixed (SYSTEM_EXTENDED_THREAD_INFORMATION* threadsPtr = &_threads) {
            for (uint i = 0; i < NumberOfThreads; i++)
                output[i] = threadsPtr[i];
        }

        return output;
    }
}

[StructLayout(LayoutKind.Explicit)]
internal readonly struct MEMORY_WORKING_SET_EX_BLOCK
{
    [FieldOffset(0)] internal readonly Valid ValidData;
    [FieldOffset(0)] internal readonly Invalid InvalidData;

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct Valid
    {
        private readonly ulong ValidFlags;
        internal bool IsValid => (ValidFlags & 0x1) > 0;                                 // :1
        internal byte ShareCount => (byte)((ValidFlags >> 0x1) & 0x7);                   // :3
        internal short Win32Protection => (short)((ValidFlags >> 0x4) & 0x7FF);          // :11
        internal bool Shared => ((ValidFlags >> 0xF) & 0x1) > 0;                         // :1
        internal byte Node => (byte)((ValidFlags >> 0x10) & 0x3F);                       // :6
        internal bool Locked => ((ValidFlags >> 0x16) & 0x1) > 0;                        // :1
        internal bool LargePage => ((ValidFlags >> 0x17) & 0x1) > 0;                     // :1
        internal byte Priority => (byte)((ValidFlags >> 0x18) & 0x7);                    // :3
        internal byte Reserved => (byte)((ValidFlags >> 0x1B) & 0x7);                    // :3
        internal bool SharedOriginal => ((ValidFlags >> 0x1E) & 0x1) > 0;                // :1
        internal bool Bad => ((ValidFlags >> 0x1F) & 0x1) > 0;                           // :1
        internal byte Win32GraphicsProtection => (byte)((ValidFlags >> 0x20) & 0xF);     // :4
        internal uint ReservedULong => (uint)((ValidFlags >> 0x24) & 0xFFFFFFF);         // :28
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct Invalid
    {
        private readonly ulong InvalidFlags;
        internal bool IsValid => (InvalidFlags & 0x1) > 0;                                // :1
        internal ushort Reserved0 => (ushort)((InvalidFlags >> 0x1) & 0x7FFF);            // :14
        internal bool Shared => ((InvalidFlags >> 0xF) & 0x1) > 0;                        // :1
        internal byte Reserved1 => (byte)((InvalidFlags >> 0x10) & 0x1F);                 // :5
        internal bool PageTable => ((InvalidFlags >> 0x15) & 0x1) > 0;                    // :1
        internal byte Location => (byte)((InvalidFlags >> 0x16) & 0x3);                   // :2
        internal byte Priority => (byte)((InvalidFlags >> 0x18) & 0x7);                   // :3
        internal bool ModifiedList => ((InvalidFlags >> 0x19) & 0x1) > 0;                 // :1
        internal byte Reserved2 => (byte)((InvalidFlags >> 0x1A) & 0x3);                  // :2
        internal bool SharedOriginal => ((InvalidFlags >> 0x1E) & 0x1) > 0;               // :1
        internal bool Bad => ((InvalidFlags >> 0x1F) & 0x1) > 0;                          // :1
        internal uint ReservedULong => (uint)((InvalidFlags >> 0x24) & 0xFFFFFFFF);       // :28
    }
}

[StructLayout(LayoutKind.Explicit)]
internal struct MEMORY_WORKING_SET_EX_INFORMATION
{
    [FieldOffset(0)] internal nint VirtualAddress;
    [FieldOffset(8)] internal MEMORY_WORKING_SET_EX_BLOCK VirtualAttributes;
    [FieldOffset(8)] internal ulong Long;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MEMORY_BASIC_INFORMATION
{
    internal nint BaseAddress;
    internal nint AllocationBase;
    internal PageProtection AllocationProtect;
    internal ushort PartitionId;
    internal long RegionSize;
    internal MemoryState State;
    internal PageProtection Protect;
    internal MemoryType Type;
}

[StructLayout(LayoutKind.Sequential)]
internal struct SYSTEM_HYPERVISOR_SHARED_PAGE_INFORMATION
{
    internal nint HypervisorSharedUserVa;
}

[StructLayout(LayoutKind.Sequential)]
internal struct PROCESS_BASIC_INFORMATION
{
    internal int ExitStatus; // NTSTATUS.
    internal nint PebBaseAddress; // PPEB.
    internal ulong AffinityMask; // KAFFINITY.
    internal int BasePriority; // KPRIORITY.
    internal nint UniqueProcessId;
    internal nint InheritedFromUniqueProcessId;
}

[StructLayout(LayoutKind.Sequential)]
internal struct ACTIVATION_CONTEXT_DATA
{
    internal uint Magic;
    internal uint HeaderSize;
    internal uint FormatVersion;
    internal uint TotalSize;
    internal uint DefaultTocOffset; // to ACTIVATION_CONTEXT_DATA_TOC_HEADER
    internal uint ExtendedTocOffset; // to ACTIVATION_CONTEXT_DATA_EXTENDED_TOC_HEADER
    internal uint AssemblyRosterOffset; // to ACTIVATION_CONTEXT_DATA_ASSEMBLY_ROSTER_HEADER
    internal uint Flags; // ACTIVATION_CONTEXT_FLAG_*
}

#endregion

/// <summary>
/// Partial class containing unmanaged macros.
/// </summary>
internal static partial class Constants
{
    internal const int ACTIVATION_CONTEXT_DATA_MAGIC = 0x41637478;
    internal static readonly nint USER_SHARED_DATA = new(0x7FFE0000);
}

/// <summary>
/// Contains methods to manage processes.
/// </summary>
internal static partial class NativeProcess
{
    /// <seealso href="https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-openprocess">OpenProcess function (processthreadsapi.h)</seealso>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial SafeProcessHandle OpenProcess(
        ProcessAccess dwDesiredAccess,
        [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
        uint dwProcessId
    );

    /// <seealso href="https://learn.microsoft.com/windows/win32/api/winbase/nf-winbase-queryfullprocessimagenamew">QueryFullProcessImageNameW function (winbase.h)</seealso>
    [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16, EntryPoint = "QueryFullProcessImageNameW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static unsafe partial bool QueryFullProcessImageName(
        SafeProcessHandle hProcess,
        int dwFlags,
        byte* lpExeName,
        ref int lpdwSize
    );

    // We have to be very careful when marshaling functions using the new 'LibraryImport' attribute.
    // the original signature asks for a SIZE_T (ulong) on 'MemoryInformationLength' and PSIZE_T (ulong*) on
    // 'ReturnLength'. With 'DllImport' is common to use integers on these cases because it's easier to integrate
    // with the 'Marshal.*' APIs.
    // The problem we had was the JIT optimizing these parameters to 32-bit numbers and the OS trying to read
    // them as ulongs, causing an access violation.
    // The only way we caught this was disabling the JIT optimization suppression during debugging, and an
    // epiphany when someone mistakenly created this method signature on C++ with ULONG instead of SIZE_T, which
    // works on 32-bit applications (for C++).

    /// <seealso href="https://learn.microsoft.com/windows-hardware/drivers/ddi/ntifs/nf-ntifs-ntqueryvirtualmemory">NtQueryVirtualMemory function (ntifs.h)</seealso>
    [LibraryImport("ntdll.dll")]
    private static partial int NtQueryVirtualMemory(
        SafeProcessHandle ProcessHandle,
        nint BaseAddress,
        MEMORY_INFORMATION_CLASS MemoryInformationClass,
        nint MemoryInformation,
        long MemoryInformationLength,
        out long ReturnLength
    );

    /// <seealso href="https://learn.microsoft.com/windows/win32/api/memoryapi/nf-memoryapi-readprocessmemory">ReadProcessMemory function (memoryapi.h)</seealso>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static unsafe partial bool ReadProcessMemory(
        SafeProcessHandle hProcess,
        nint lpBaseAddress,
        byte* lpBuffer,
        long nSize,
        out long lpNumberOfBytesRead
    );

    /// <seealso href="https://learn.microsoft.com/windows/win32/api/memoryapi/nf-memoryapi-readprocessmemory">ReadProcessMemory function (memoryapi.h)</seealso>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ReadProcessMemory(
        SafeProcessHandle hProcess,
        nint lpBaseAddress,
        nint lpBuffer,
        long nSize,
        out long lpNumberOfBytesRead
    );

    /// <seealso href="https://learn.microsoft.com/windows/win32/api/winternl/nf-winternl-ntqueryinformationprocess">NtQueryInformationProcess function (winternl.h)</seealso>
    [LibraryImport("ntdll.dll")]
    private static partial int NtQueryInformationProcess(
        SafeProcessHandle ProcessHandle,
        PROCESSINFOCLASS ProcessInformationClass,
        nint ProcessInformation,
        int ProcessInformationLength,
        out int ReturnLength
    );

    /// <seealso href="https://learn.microsoft.com/windows/win32/api/heapapi/nf-heapapi-getprocessheap">GetProcessHeap function (heapapi.h)</seealso>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    internal static partial nint GetProcessHeap();

    /// <summary>
    /// Checks if the process ID is a running process.
    /// </summary>
    /// <param name="processId">The process ID.</param>
    /// <returns>True if it's a running process.</returns>
    internal static bool IsProcess(uint processId)
    {
        using ScopedBuffer buffer = new(0x4000);
        return TryGetProcessInformation(processId, buffer, out _);
    }

    /// <summary>
    /// Opens a handle to a process.
    /// </summary>
    /// <param name="processId">The process ID.</param>
    /// <param name="desiredAccess">The desired access.</param>
    /// <param name="inheritHandle">True to inherit the handle.</param>
    /// <returns>A <see cref="SafeProcessHandle"/> to the process.</returns>
    /// <exception cref="NativeException">The call to 'OpenProcess' failed.</exception>
    internal static SafeProcessHandle OpenProcess(uint processId, ProcessAccess desiredAccess, bool inheritHandle)
    {
        SafeProcessHandle hProcess = OpenProcess(desiredAccess, inheritHandle, processId);
        if (hProcess.IsInvalid)
            throw new NativeException(Marshal.GetLastWin32Error());

        return hProcess;
    }

    /// <summary>
    /// Gets the process image path.
    /// </summary>
    /// <param name="hProcess">The handle to the process.</param>
    /// <returns>The process image path.</returns>
    /// <exception cref="NativeException">The call to 'QueryFullProcessImageName' failed.</exception>
    internal static unsafe string QueryProcessImagePath(SafeProcessHandle hProcess)
    {
        int bufferSize = Constants.MAX_PATH;
        ScopedBuffer buffer = new(bufferSize);
        if (!QueryFullProcessImageName(hProcess, 0, (byte*)buffer, ref bufferSize))
            throw new NativeException(Marshal.GetLastWin32Error());

        string? output = Marshal.PtrToStringUni(buffer);
        return output is null ? string.Empty : output.TrimEnd('\0');
    }

    /// <summary>
    /// Reads from the process virtual memory space.
    /// </summary>
    /// <param name="hProcess">The handle to the process.</param>
    /// <param name="baseAddress">The base address to start reading from.</param>
    /// <param name="buffer">The buffer to read into.</param>
    /// <param name="count">The number of bytes to read.</param>
    /// <returns>The number of bytes read.</returns>
    /// <exception cref="NativeException">The call to 'ReadProcessMemory' failed.</exception>
    internal static unsafe long ReadProcessMemory(SafeProcessHandle hProcess, nint baseAddress, ScopedBuffer buffer, long count)
    {
        if (!ReadProcessMemory(hProcess, baseAddress, buffer, count, out long bytesRead)) {
            int lastError = Marshal.GetLastWin32Error();
            return lastError switch {
                ErrorCodes.ERROR_PARTIAL_COPY => bytesRead, // It will be zero anyways, but to have some consistency.
                ErrorCodes.ERROR_NOACCESS => 0,
                _ => throw new NativeException(lastError)
            };
        }

        return bytesRead;
    }

    /// <summary>
    /// Attempts to read from a process virtual memory space.
    /// </summary>
    /// <param name="hProcess">The handle to the process.</param>
    /// <param name="baseAddress">The base addres to start reading from.</param>
    /// <param name="buffer">The buffer to read into.</param>
    /// <param name="count">The number of bytes to read.</param>
    /// <returns>True if successfully read the requested number of bytes.</returns>
    internal static unsafe bool TryReadProcessMemory(SafeProcessHandle hProcess, nint baseAddress, Span<byte> buffer, long count)
    {
        fixed (byte* bufferPtr = &MemoryMarshal.GetReference(buffer)) {
            if (ReadProcessMemory(hProcess, baseAddress, bufferPtr, count, out long bytesRead) && bytesRead == count)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to read from a process virtual memory space.
    /// </summary>
    /// <param name="hProcess">The handle to the process.</param>
    /// <param name="baseAddress">The base addres to start reading from.</param>
    /// <param name="buffer">The buffer to read into.</param>
    /// <param name="count">The number of bytes to read.</param>
    /// <returns>True if successfully read the requested number of bytes.</returns>
    internal static bool TryReadProcessMemory(SafeProcessHandle hProcess, nint baseAddress, ScopedBuffer buffer, long count)
    {
        if (ReadProcessMemory(hProcess, baseAddress, buffer, count, out long bytesRead) && bytesRead == count)
            return true;

        return false;
    }

    /// <summary>
    /// Gets process virtual memory information.
    /// </summary>
    /// <param name="processId">The process ID.</param>
    /// <param name="hProcess">The handle to the process.</param>
    /// <param name="regionsToMap">The type of regions to map.</param>
    /// <returns>A list containing the process memory region information.</returns>
    public static unsafe List<ProcessMemoryRegion> GetProcessMemoryInformation(uint processId, SafeProcessHandle hProcess, ReadMemoryFlags regionsToMap)
    {
        List<ProcessMemoryRegion> output = [];

        // Attempting to get the process image path.
        string imagePath = QueryProcessImagePath(hProcess);

        // Listing heaps with the debug functions.
        ProcessDebugInformation? debugInfo = null;
        if (regionsToMap.HasMemoryFlag(ReadMemoryFlags.Heap))
            debugInfo = new(processId, QueryProcessFlags.HEAP_SUMMARY | QueryProcessFlags.HEAP_ENTRIES | QueryProcessFlags.NONINVASIVE);

        // Listing virtual addresses.
        int basicInfoSize = Marshal.SizeOf<MEMORY_BASIC_INFORMATION>();
        int workingSetInfoSize = Marshal.SizeOf<MEMORY_WORKING_SET_EX_INFORMATION>();

        nint address = 0;
        using ScopedBuffer buffer = new(basicInfoSize);
        while (NtQueryVirtualMemory(hProcess, address, MEMORY_INFORMATION_CLASS.BasicInformation, buffer, basicInfoSize, out _) == ErrorCodes.STATUS_SUCCESS) {
            MEMORY_BASIC_INFORMATION mbi = Marshal.PtrToStructure<MEMORY_BASIC_INFORMATION>(buffer);

            // We only query commited memory because reading from other types doesn't make sense.
            if (mbi.State == MemoryState.COMMIT && (mbi.Protect & (PageProtection.NOACCESS | PageProtection.GUARD)) == 0) {
                ProcessMemoryRegion currentRegion;
                if (debugInfo is not null) {
                    
                    // Checking if it's a heap.
                    ProcessHeap? possibleHeap = debugInfo.Heaps.FirstOrDefault(h => (long)mbi.BaseAddress >= (long)h.Base && (long)mbi.BaseAddress < (long)h.End);
                    if (possibleHeap is not null) {
                        currentRegion = new(processId, imagePath, ref mbi) {
                            Type = possibleHeap.RegionType,
                            HeapInformation = possibleHeap,
                        };
                    }
                    else {
                        currentRegion = new(processId, imagePath, ref mbi);
                    }
                }
                else {
                    currentRegion = new(processId, imagePath, ref mbi);
                }

                // Setting the allocation base.
                ProcessMemoryRegion? allocationBaseItem = null;
                if (mbi.AllocationBase == mbi.BaseAddress)
                    allocationBaseItem = currentRegion;
                else
                    allocationBaseItem = output.FirstOrDefault(i => i.BaseAddress == mbi.AllocationBase);

                if (allocationBaseItem is not null && mbi.AllocationBase == allocationBaseItem.BaseAddress)
                    currentRegion.AllocationBaseItem = allocationBaseItem;

                // Querying working set information.
                MEMORY_WORKING_SET_EX_INFORMATION mwsi = new() { VirtualAddress = address };
                if (NtQueryVirtualMemory(hProcess, nint.Zero, MEMORY_INFORMATION_CLASS.WorkingSetExInformation, new nint(&mwsi), workingSetInfoSize, out _) == ErrorCodes.STATUS_SUCCESS)
                    currentRegion.IsValid = mwsi.VirtualAttributes.ValidData.IsValid && !mwsi.VirtualAttributes.ValidData.Bad;

                output.Add(currentRegion);
            }

            address = checked((nint)(address + mbi.RegionSize));
        }

        // Categorizing the memory regions.
        UpdateMemoryRegionTypes(processId, hProcess, output);

        return [.. output.Where(r => r.Type.HasMemoryFlag(regionsToMap)).OrderBy(r => r.BaseLong)];
    }

    /// <summary>
    /// Categorizes a list of <see cref="ProcessMemoryRegion"/>.
    /// </summary>
    /// <param name="processId">The process ID.</param>
    /// <param name="hProcess">The handle to the process.</param>
    /// <param name="regionList">The process memory region list.</param>
    /// <remarks>
    /// This method was almost entirelly based on the SystemInformer verison.
    /// </remarks>
    private static unsafe void UpdateMemoryRegionTypes(uint processId, SafeProcessHandle hProcess, List<ProcessMemoryRegion> regionList)
    {
        // 1 - Checks if the process exists and collect information about it.
        using ScopedBuffer procInfoBuffer = new(0x4000);
        if (!TryGetProcessInformation(processId, procInfoBuffer, out int procInfoOffset))
            return;

        // 2 - Update user shared data.
        SetMemoryRegionType(regionList, Constants.USER_SHARED_DATA, true, MemoryRegionType.UserSharedData);

        // 3 - Update hipervisor shared data.
        if (WinVer.CurrentVersion >= WinVer.WINDOWS_10_RS4) {
            nint hypervisorSUVa = GetSystemHypervisorSharedPageInformation();
            if (hypervisorSUVa != nint.Zero)
                SetMemoryRegionType(regionList, hypervisorSUVa, true, MemoryRegionType.HypervisorSharedData);
        }

        // 4 - PEB, Heap.
        if (TryGetProcessPeb(hProcess, out PEB peb, out nint pebBase)) {
            ProcessPebInformation(hProcess, regionList, ref peb, pebBase);
        }

        if (TryGetProcessPeb32(hProcess, out PEB32 peb32, out nint peb32Base)) {
            ProcessPebInformation32(hProcess, regionList, ref peb32, peb32Base);
        }

        // 5 - TEB, Stack.
        SYSTEM_PROCESS_INFORMATION* processInfo = (SYSTEM_PROCESS_INFORMATION*)nint.Add(procInfoBuffer, procInfoOffset);
        foreach (var thread in processInfo->Threads) {
            if (thread.TebBase != nint.Zero) {

                // 5.1 - Set the region type.
                ProcessMemoryRegion? tebRegion = SetMemoryRegionType(regionList, thread.TebBase, WinVer.CurrentVersion < WinVer.WINDOWS_10_RS2, MemoryRegionType.Teb);
                if (tebRegion is not null)
                    tebRegion.ThreadId = (uint)thread.ThreadInfo.ClientId.UniqueThread;

                // 5.2 - Reading the NT_TIB.
                int tibSize = Marshal.SizeOf<NT_TIB>();
                using ScopedBuffer tibBuffer = new(tibSize);
                if (TryReadProcessMemory(hProcess, thread.TebBase, tibBuffer, tibSize)) {
                    NT_TIB* tib = (NT_TIB*)tibBuffer;
                    if ((ulong)tib->StackLimit < (ulong)tib->StackBase) {
                        tebRegion = SetMemoryRegionType(regionList, tib->StackLimit, true, MemoryRegionType.Stack);
                        if (tebRegion is not null)
                            tebRegion.ThreadId = (uint)thread.ThreadInfo.ClientId.UniqueThread;
                    }
                }
            }
        }

        // 6 - Mapped file, heap segment, unusable.
        foreach (var item in regionList) {
            if (item.Type != MemoryRegionType.Unknown)
                continue;

            if ((item.BasicType & (MemoryType.MAPPED | MemoryType.IMAGE)) > 0 && item.AllocationBaseItem == item) {
                item.MappedFilePath = GetMappedFileName(hProcess, item.BaseAddress);
                if ((item.BasicType & MemoryType.IMAGE) > 0)
                    item.Type = MemoryRegionType.Image;
                else {
                    if (string.IsNullOrEmpty(item.MappedFilePath))
                        item.Type = MemoryRegionType.Shareable;
                    else
                        item.Type = MemoryRegionType.MappedFile;
                }

                continue;
            }

            // Heap segment.
            if ((item.State & MemoryState.COMMIT) > 0 && item.IsValid) {
                int segSize = Marshal.SizeOf<HEAP_SEGMENT>();
                using ScopedBuffer buffer = new(segSize);
                if (TryReadProcessMemory(hProcess, item.BaseAddress, buffer, segSize)) {
                    HEAP_SEGMENT* segment = (HEAP_SEGMENT*)buffer;
                    HEAP_SEGMENT32* segment32 = (HEAP_SEGMENT32*)buffer;

                    nint candidateHeap = nint.Zero;
                    uint candidateHeap32 = 0;

                    // 0xFFEEFFEE: HEAP_SEGMENT_SIGNATURE.
                    if (segment->SegmentSignature == 0xFFEEFFEE)
                        candidateHeap = segment->Heap;
                    if (segment32->SegmentSignature == 0xFFEEFFEE)
                        candidateHeap32 = segment32->Heap;

                    if (candidateHeap != nint.Zero) {
                        ProcessMemoryRegion? heapRegion = regionList.FirstOrDefault(i => i.BaseAddress == candidateHeap);
                        if (heapRegion is not null) {
                            if (heapRegion.Type == MemoryRegionType.NtHeap)
                                heapRegion.Type = MemoryRegionType.NtHeapSegment;
                            else if (heapRegion.Type == MemoryRegionType.SegmentHeap)
                                heapRegion.Type = MemoryRegionType.SegmentHeapSegment;
                        }
                    }
                    else if (candidateHeap32 > 0) {
                        ProcessMemoryRegion? heapRegion = regionList.FirstOrDefault(i => i.BaseAddress == (nint)candidateHeap32);
                        if (heapRegion is not null) {
                            if (heapRegion.Type == MemoryRegionType.NtHeap)
                                heapRegion.Type = MemoryRegionType.NtHeapSegment;
                            else if (heapRegion.Type == MemoryRegionType.SegmentHeap)
                                heapRegion.Type = MemoryRegionType.SegmentHeapSegment;
                        }
                    }
                }
            }

            // Activation context data.
            if ((item.BasicType & MemoryType.MAPPED) > 0 && item.AllocationProtection == PageProtection.READONLY && item.AllocationBaseItem == item) {
                int buffSize = Marshal.SizeOf<ACTIVATION_CONTEXT_DATA>();
                using ScopedBuffer buffer = new(buffSize);
                if (TryReadProcessMemory(hProcess, item.BaseAddress, buffer, buffSize)) {
                    ACTIVATION_CONTEXT_DATA* acd = (ACTIVATION_CONTEXT_DATA*)buffer;

                    // 'xtcA'.
                    if (acd->Magic == Constants.ACTIVATION_CONTEXT_DATA_MAGIC) {
                        item.Type = MemoryRegionType.ActivationContextData;
                        continue;
                    }
                }
            }

            // Catch all.
            if (item.Type == MemoryRegionType.Unknown) {
                if (item.AllocationBaseItem is not null) {
                    if (item.AllocationBaseItem.Type == MemoryRegionType.Unknown) {
                        item.Type = item.BasicType switch {
                            MemoryType.PRIVATE => MemoryRegionType.PrivateData,
                            MemoryType.IMAGE => MemoryRegionType.Image,
                            MemoryType.MAPPED => string.IsNullOrEmpty(item.MappedFilePath) ? MemoryRegionType.Shareable : MemoryRegionType.MappedFile,
                            _ => MemoryRegionType.Unknown
                        };

                        item.AllocationBaseItem.Type = item.Type;
                    }
                    else {
                        item.Type = item.AllocationBaseItem.Type;
                        if ((item.BasicType & (MemoryType.MAPPED | MemoryType.IMAGE)) > 0 && !string.IsNullOrEmpty(item.AllocationBaseItem.MappedFilePath))
                            item.MappedFilePath = item.AllocationBaseItem.MappedFilePath;

                        if ((item.Type & (MemoryRegionType)ReadMemoryFlags.Heap) > 0 && item.AllocationBaseItem.HeapInformation is not null) {
                            item.HeapInformation ??= new(item.AllocationBaseItem.HeapInformation, item.BaseAddress, item.Size);
                        }
                    }
                }
                else {
                    item.Type = item.BasicType switch {
                        MemoryType.PRIVATE => MemoryRegionType.PrivateData,
                        MemoryType.IMAGE => MemoryRegionType.Image,
                        MemoryType.MAPPED => string.IsNullOrEmpty(item.MappedFilePath) ? MemoryRegionType.Shareable : MemoryRegionType.MappedFile,
                        _ => MemoryRegionType.Unknown
                    };
                }
            }
        }
    }

    /// <summary>
    /// Attempts to get process information.
    /// </summary>
    /// <param name="processId">The process ID.</param>
    /// <param name="buffer">The buffer to return the process information into.</param>
    /// <param name="offset">The process information offset.</param>
    /// <returns>True if successfully retrieved process information for the process ID.</returns>
    /// <exception cref="NativeException">A call to a native API failed.</exception>
    private static unsafe bool TryGetProcessInformation(uint processId, ScopedBuffer buffer, out int offset)
    {
        int status;
        int bufferSize = (int)buffer.Size;
        do {
            status = Common.NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS.SystemExtendedProcessInformation, buffer, bufferSize, out bufferSize);
            if (status == ErrorCodes.STATUS_SUCCESS)
                break;

            if (status != ErrorCodes.STATUS_BUFFER_TOO_SMALL && status != ErrorCodes.STATUS_INFO_LENGTH_MISMATCH)
                throw new NativeException(status, true);

            buffer.Resize((ulong)bufferSize);

        } while (status == ErrorCodes.STATUS_BUFFER_TOO_SMALL || status == ErrorCodes.STATUS_INFO_LENGTH_MISMATCH);

        offset = 0;
        SYSTEM_PROCESS_INFORMATION* processInfo = (SYSTEM_PROCESS_INFORMATION*)buffer;
        do {
            if ((uint)processInfo->UniqueProcessId == processId) {
                offset = (int)((byte*)processInfo - (byte*)buffer);

                return true;
            }

            if (processInfo->NextEntryOffset == 0)
                break;

            processInfo = (SYSTEM_PROCESS_INFORMATION*)((byte*)processInfo + processInfo->NextEntryOffset);

        } while (processInfo->NextEntryOffset != 0);

        offset = 0;

        return false;
    }

    /// <summary>
    /// Gets process hypervisor shared page information.
    /// </summary>
    /// <returns>The address of the hypervisor shared page</returns>
    private static unsafe nint GetSystemHypervisorSharedPageInformation()
    {
        int bufferSize = Marshal.SizeOf<SYSTEM_HYPERVISOR_SHARED_PAGE_INFORMATION>();
        using ScopedBuffer buffer = new(bufferSize);
        int status = Common.NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS.SystemHypervisorSharedPageInformation, buffer, bufferSize, out _);
        if (status == 0)
            return ((SYSTEM_HYPERVISOR_SHARED_PAGE_INFORMATION*)(nint)buffer)->HypervisorSharedUserVa;

        return nint.Zero;
    }

    /// <summary>
    /// Attempts to get the process PEB.
    /// </summary>
    /// <param name="hProcess">The handle to the process.</param>
    /// <param name="peb">The output PEB.</param>
    /// <param name="pebBase">The base address for the PEB.</param>
    /// <returns>True if successfully got the PEB.</returns>
    private static unsafe bool TryGetProcessPeb(SafeProcessHandle hProcess, out PEB peb, out nint pebBase)
    {
        peb = default;
        pebBase = nint.Zero;
        int bufferSize = Marshal.SizeOf<PROCESS_BASIC_INFORMATION>();
        using ScopedBuffer pbiBuffer = new(bufferSize);

        int status = NtQueryInformationProcess(hProcess, PROCESSINFOCLASS.ProcessBasicInformation, pbiBuffer, bufferSize, out _);
        if (status == ErrorCodes.STATUS_SUCCESS) {
            PROCESS_BASIC_INFORMATION* pbi = (PROCESS_BASIC_INFORMATION*)pbiBuffer;
            if (pbi->PebBaseAddress == nint.Zero)
                return false;

            int pebSize = Marshal.SizeOf<PEB>();
            byte[] pebBuffer = new byte[pebSize];
            if (TryReadProcessMemory(hProcess, pbi->PebBaseAddress, new Span<byte>(pebBuffer), pebSize)) {
                fixed (byte* pebPtr = pebBuffer) {
                    peb = Marshal.PtrToStructure<PEB>(new nint(pebPtr));
                    pebBase = pbi->PebBaseAddress;

                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Attempts to get the process 32-bit PEB.
    /// </summary>
    /// <param name="hProcess">The handle to the process.</param>
    /// <param name="peb">The output PEB.</param>
    /// <param name="pebBase">The base address for the PEB.</param>
    /// <returns>True if successfully got the PEB.</returns>
    private static unsafe bool TryGetProcessPeb32(SafeProcessHandle hProcess, out PEB32 peb, out nint pebBase)
    {
        peb = default;
        pebBase = nint.Zero;
        int bufferSize = Marshal.SizeOf<ulong>();
        using ScopedBuffer pebPtrBuffer = new(bufferSize);

        int status = NtQueryInformationProcess(hProcess, PROCESSINFOCLASS.ProcessWow64Information, pebPtrBuffer, bufferSize, out _);
        if (status == ErrorCodes.STATUS_SUCCESS) {
            ulong wowPebBase = Marshal.PtrToStructure<ulong>(pebPtrBuffer);
            if (wowPebBase != 0) {
                int pebSize = Marshal.SizeOf<PEB32>();
                using ScopedBuffer pebBuffer = new(pebSize);
                if (TryReadProcessMemory(hProcess, (nint)wowPebBase, pebBuffer, pebSize)) {
                    peb = Marshal.PtrToStructure<PEB32>(pebBuffer);
                    pebBase = pebPtrBuffer;

                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Set the <see cref="MemoryRegionType"/> for a <see cref="ProcessMemoryRegion"/>.
    /// </summary>
    /// <param name="regionList">The <see cref="ProcessMemoryRegion"/> list.</param>
    /// <param name="address">The region base address.</param>
    /// <param name="goToAllocationBase">True to go to the allocation base.</param>
    /// <param name="type">The <see cref="MemoryRegionType"/>.</param>
    /// <returns>The <see cref="ProcessMemoryRegion"/>.</returns>
    private static ProcessMemoryRegion? SetMemoryRegionType(List<ProcessMemoryRegion> regionList, nint address, bool goToAllocationBase, MemoryRegionType type)
    {
        ProcessMemoryRegion? region = regionList.FirstOrDefault(r => r.BaseAddress == address);
        if (region is null)
            return null;

        if (goToAllocationBase && region.AllocationBaseItem is not null)
            region = region.AllocationBaseItem;

        if (region.Type != MemoryRegionType.Unknown)
            return null;

        region.Type = type;
        return region;
    }

    /// <summary>
    /// Gets the memory mapped file name.
    /// </summary>
    /// <param name="hProcess">The handle to the process.</param>
    /// <param name="baseAddress">The mapped file base address.</param>
    /// <returns>The memory mapped file name.</returns>
    private static string GetMappedFileName(SafeProcessHandle hProcess, nint baseAddress)
    {
        long bufferSize = 0x220;
        using ScopedBuffer buffer = new(bufferSize);
        int status = NtQueryVirtualMemory(hProcess, baseAddress, MEMORY_INFORMATION_CLASS.MappedFilenameInformation, buffer, bufferSize, out bufferSize);
        if (status != 0)
            return string.Empty;

        UNICODE_STRING nativeString = Marshal.PtrToStructure<UNICODE_STRING>(buffer);

        return NativeIO.GetFileDosPathFromDevicePath(nativeString.ToString());
    }

    /// <summary>
    /// Processes the PEB for a process.
    /// </summary>
    /// <param name="hProcess">The handle to the process.</param>
    /// <param name="regionList">The <see cref="ProcessMemoryRegion"/> list.</param>
    /// <param name="peb64">The process PEB.</param>
    /// <param name="pebBase">The PEB base address.</param>
    private static unsafe void ProcessPebInformation(SafeProcessHandle hProcess, List<ProcessMemoryRegion> regionList, ref PEB peb64, nint pebBase)
    {
        SetMemoryRegionType(regionList, pebBase, WinVer.CurrentVersion < WinVer.WINDOWS_10_RS2, MemoryRegionType.Peb);

        int heapsBufferSize = (int)peb64.NumberOfHeaps * nint.Size;
        byte[] heapsBuffer = new byte[heapsBufferSize];
        if (TryReadProcessMemory(hProcess, peb64.ProcessHeaps, new Span<byte>(heapsBuffer), heapsBufferSize)) {
            fixed (byte* heapsPtr = heapsBuffer) {
                nint offset = new(heapsPtr);
                for (int i = 0; i < peb64.NumberOfHeaps; i++) {
                    ProcessMemoryRegion? currentHeapSegment = SetMemoryRegionType(regionList, offset, true, MemoryRegionType.NtHeap);
                    if (currentHeapSegment is not null) {
                        int headerSize = Marshal.SizeOf<ANY_HEAP>();
                        byte[] headerBuffer = new byte[headerSize];

                        if (TryReadProcessMemory(hProcess, offset, new Span<byte>(headerBuffer), headerSize)) {
                            fixed (byte* headerPtr = headerBuffer) {
                                ANY_HEAP* header = (ANY_HEAP*)headerPtr;
                                currentHeapSegment.HeapInformation ??= new((uint)(i + 1), offset, currentHeapSegment.Size);

                                if (WinVer.CurrentVersion >= WinVer.WINDOWS_8_1 && header->SegmentHeap.Signature == Constants.SEGMENT_HEAP_SIGNATURE)
                                    currentHeapSegment.Type = MemoryRegionType.SegmentHeap;
                            }
                        }
                    }

                    offset = nint.Add(offset, nint.Size);
                }

                if (peb64.ApiSetMap != nint.Zero) SetMemoryRegionType(regionList, peb64.ApiSetMap, true, MemoryRegionType.ApiSetMap);
                if (peb64.ReadOnlySharedMemoryBase != nint.Zero) SetMemoryRegionType(regionList, peb64.ReadOnlySharedMemoryBase, true, MemoryRegionType.ReadOnlySharedMemory);
                if (peb64.AnsiCodePageData != nint.Zero) SetMemoryRegionType(regionList, peb64.AnsiCodePageData, true, MemoryRegionType.CodePageData);
                if (peb64.GdiSharedHandleTable != nint.Zero) SetMemoryRegionType(regionList, peb64.GdiSharedHandleTable, true, MemoryRegionType.GdiSharedHandleTable);
                if (peb64.pShimData != nint.Zero) SetMemoryRegionType(regionList, peb64.pShimData, true, MemoryRegionType.ShimData);
                if (peb64.ActivationContextData != nint.Zero) SetMemoryRegionType(regionList, peb64.ActivationContextData, true, MemoryRegionType.ProcessActivationContext);
                if (peb64.SystemDefaultActivationContextData != nint.Zero) SetMemoryRegionType(regionList, peb64.SystemDefaultActivationContextData, true, MemoryRegionType.SystemActivationContext);
                if (peb64.WerRegistrationData != nint.Zero) SetMemoryRegionType(regionList, peb64.WerRegistrationData, true, MemoryRegionType.WerRegistrationData);
                if (peb64.SharedData != nint.Zero) SetMemoryRegionType(regionList, peb64.SharedData, true, MemoryRegionType.SiloSharedData);
                if (peb64.TelemetryCoverageHeader != nint.Zero) SetMemoryRegionType(regionList, peb64.TelemetryCoverageHeader, true, MemoryRegionType.TelemetryCoverage);
            }
        }
    }

    /// <summary>
    /// Processes the 32-bit PEB for a process.
    /// </summary>
    /// <param name="hProcess">The handle to the process.</param>
    /// <param name="regionList">The <see cref="ProcessMemoryRegion"/> list.</param>
    /// <param name="peb32">The process PEB.</param>
    /// <param name="pebBase">The PEB base address.</param>
    private static unsafe void ProcessPebInformation32(SafeProcessHandle hProcess, List<ProcessMemoryRegion> regionList, ref PEB32 peb32, nint pebBase)
    {
        SetMemoryRegionType(regionList, pebBase, WinVer.CurrentVersion < WinVer.WINDOWS_10_RS2, MemoryRegionType.Peb);

        int heapsBufferSize = (int)peb32.NumberOfHeaps * nint.Size;
        byte[] heapsBuffer = new byte[heapsBufferSize];
        if (TryReadProcessMemory(hProcess, (nint)peb32.ProcessHeaps, new Span<byte>(heapsBuffer), heapsBufferSize)) {
            fixed (byte* heapsPtr = heapsBuffer) {
                nint offset = new(heapsPtr);
                for (int i = 0; i < peb32.NumberOfHeaps; i++) {
                    ProcessMemoryRegion? currentHeapSegment = SetMemoryRegionType(regionList, offset, true, MemoryRegionType.NtHeap);
                    if (currentHeapSegment is not null) {
                        int headerSize = Marshal.SizeOf<ANY_HEAP>();
                        byte[] headerBuffer = new byte[headerSize];

                        if (TryReadProcessMemory(hProcess, offset, new Span<byte>(headerBuffer), headerSize)) {
                            fixed (byte* headerPtr = headerBuffer) {
                                ANY_HEAP* header = (ANY_HEAP*)headerPtr;
                                currentHeapSegment.HeapInformation ??= new((uint)(i + 1), offset, currentHeapSegment.Size);

                                if (WinVer.CurrentVersion >= WinVer.WINDOWS_8_1 && header->SegmentHeap.Signature == Constants.SEGMENT_HEAP_SIGNATURE)
                                    currentHeapSegment.Type = MemoryRegionType.SegmentHeap;
                            }
                        }
                    }

                    offset = nint.Add(offset, nint.Size);
                }

                if (peb32.ApiSetMap != 0) SetMemoryRegionType(regionList, (nint)peb32.ApiSetMap, true, MemoryRegionType.ApiSetMap);
                if (peb32.ReadOnlySharedMemoryBase != 0) SetMemoryRegionType(regionList, (nint)peb32.ReadOnlySharedMemoryBase, true, MemoryRegionType.ReadOnlySharedMemory);
                if (peb32.AnsiCodePageData != 0) SetMemoryRegionType(regionList, (nint)peb32.AnsiCodePageData, true, MemoryRegionType.CodePageData);
                if (peb32.GdiSharedHandleTable != 0) SetMemoryRegionType(regionList, (nint)peb32.GdiSharedHandleTable, true, MemoryRegionType.GdiSharedHandleTable);
                if (peb32.pShimData != 0) SetMemoryRegionType(regionList, (nint)peb32.pShimData, true, MemoryRegionType.ShimData);
                if (peb32.ActivationContextData != 0) SetMemoryRegionType(regionList, (nint)peb32.ActivationContextData, true, MemoryRegionType.ProcessActivationContext);
                if (peb32.SystemDefaultActivationContextData != 0) SetMemoryRegionType(regionList, (nint)peb32.SystemDefaultActivationContextData, true, MemoryRegionType.SystemActivationContext);
                if (peb32.WerRegistrationData != 0) SetMemoryRegionType(regionList, (nint)peb32.WerRegistrationData, true, MemoryRegionType.WerRegistrationData);
                if (peb32.SharedData != 0) SetMemoryRegionType(regionList, (nint)peb32.SharedData, true, MemoryRegionType.SiloSharedData);
                if (peb32.TelemetryCoverageHeader != 0) SetMemoryRegionType(regionList, (nint)peb32.TelemetryCoverageHeader, true, MemoryRegionType.TelemetryCoverage);
            }
        }
    }
}