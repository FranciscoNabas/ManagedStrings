// This file is part of the ManagedStrings project and repository.
// Project files are licensed under the MIT license.
// https://github.com/FranciscoNabas/ManagedStrings

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using ManagedStrings.Engine;
using System.Linq;

namespace ManagedStrings.Interop.Windows;

// Words cannot express how much I owe to the SystemInformer project.
// Every bug I encounter, every dead end I get, everything I need to implement
// they already did it brilliantly, and documented.

#region Enumerations

/// <summary>
/// Query process debug information flags.
/// </summary>
[Flags]
internal enum QueryProcessFlags : uint
{
    MODULES               = 0x00000001,
    BACKTRACES            = 0x00000002,
    HEAP_SUMMARY          = 0x00000004,
    HEAP_TAGS             = 0x00000008,
    HEAP_ENTRIES          = 0x00000010,
    LOCKS                 = 0x00000020,
    MODULES32             = 0x00000040,
    VERIFIER_OPTIONS      = 0x00000080, // rev
    MODULESEX             = 0x00000100, // rev
    HEAP_SEGMENTS         = 0x00000200,
    CS_OWNER              = 0x00000400, // rev
    NONINVASIVE           = 0x80000000,
    NONINVASIVE_CS_OWNER  = 0x80000800, // WIN11
}

#endregion

#region Structures

[StructLayout(LayoutKind.Sequential)]
internal struct RTL_DEBUG_INFORMATION
{
    internal nint SectionHandleClient;
    internal nint ViewBaseClient;
    internal nint ViewBaseTarget;
    internal ulong ViewBaseDelta;
    internal nint EventPairClient;
    internal nint EventPairTarget;
    internal nint TargetProcessId;
    internal nint TargetThreadHandle;
    internal uint Flags;
    internal ulong OffsetFree;
    internal ulong CommitSize;
    internal ulong ViewSize;
    internal nint Modules; // PRTL_PROCESS_MODULES / PRTL_PROCESS_MODULE_INFORMATION_EX.
    internal nint BackTraces; // PRTL_PROCESS_BACKTRACES.
    internal nint Heaps;
    internal nint Locks; // PRTL_PROCESS_LOCKS.
    internal nint SpecificHeap;
    internal nint TargetProcessHandle;
    internal nint VerifierOptions; // PRTL_PROCESS_VERIFIER_OPTIONS.
    internal nint ProcessHeap;
    internal nint CriticalSectionHandle;
    internal nint CriticalSectionOwnerThread;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    internal nint[] Reserved;
}

// Windows 7/8/10.
[StructLayout(LayoutKind.Sequential)]
internal struct RTL_HEAP_INFORMATION_V1
{
    internal nint BaseAddress;
    internal uint Flags;
    internal ushort EntryOverhead;
    internal ushort CreatorBackTraceIndex;
    internal long BytesAllocated;
    internal long BytesCommitted;
    internal uint NumberOfTags;
    internal uint NumberOfEntries;
    internal uint NumberOfPseudoTags;
    internal uint PseudoTagGranularity;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
    internal uint[] Reserved;

    internal nint Tags;  // PRTL_HEAP_TAG.
    internal nint Entries;  // PRTL_HEAP_ENTRY.
}

// Windows 11 > 22000.
[StructLayout(LayoutKind.Sequential)]
internal struct RTL_HEAP_INFORMATION_V2
{
    internal nint BaseAddress;
    internal uint Flags;
    internal ushort EntryOverhead;
    internal ushort CreatorBackTraceIndex;
    internal long BytesAllocated;
    internal long BytesCommitted;
    internal uint NumberOfTags;
    internal uint NumberOfEntries;
    internal uint NumberOfPseudoTags;
    internal uint PseudoTagGranularity;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
    internal uint[] Reserved;

    internal nint Tags;  // PRTL_HEAP_TAG.
    internal nint Entries;  // PRTL_HEAP_ENTRY.
    internal ulong HeapTag;
}


// ## TODO: Fix this offset fiasco.
[StructLayout(LayoutKind.Sequential)]
internal struct RTL_PROCESS_HEAPS_V1
{
    internal RTL_HEAP_INFORMATION_V1[] Heaps;

    internal RTL_PROCESS_HEAPS_V1(nint ptr)
    {
        nint offset = nint.Add(ptr, 8);
        int heapCount = Marshal.ReadInt32(ptr, 0);
        Heaps = new RTL_HEAP_INFORMATION_V1[heapCount];

        int heapSize = Marshal.SizeOf<RTL_HEAP_INFORMATION_V1>();
        for (int i = 0; i < heapCount; i++) {
            Heaps[i] = Marshal.PtrToStructure<RTL_HEAP_INFORMATION_V1>(offset);
            offset = nint.Add(offset, heapSize);
        }
    }
}

// ## TODO: Fix this offset fiasco.
[StructLayout(LayoutKind.Sequential)]
internal struct RTL_PROCESS_HEAPS_V2
{
    internal RTL_HEAP_INFORMATION_V2[] Heaps;

    internal RTL_PROCESS_HEAPS_V2(nint ptr)
    {
        nint offset = nint.Add(ptr, 8);
        int heapCount = Marshal.ReadInt32(ptr, 0);
        Heaps = new RTL_HEAP_INFORMATION_V2[heapCount];

        int heapSize = Marshal.SizeOf<RTL_HEAP_INFORMATION_V2>();
        for (int i = 0; i < heapCount; i++) {
            Heaps[i] = Marshal.PtrToStructure<RTL_HEAP_INFORMATION_V2>(offset);
            offset = nint.Add(offset, heapSize);
        }
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct RTL_HEAP_ENTRY
{
    internal ulong Size;
    internal HeapFlags Flags;
    internal ushort AllocationBackTraceIndex;
    internal ulong CommitedSize;
    internal nint FirstBlock;
}

#endregion

/// <summary>
/// Class with RTL debug functions.
/// </summary>
internal static partial class RtlDebugAPI
{
    // These functions are not documented.
    [LibraryImport("ntdll.dll")]
    internal static partial nint RtlCreateQueryDebugBuffer(
        uint MaximumCommit,
        [MarshalAs(UnmanagedType.Bool)] bool UseEventPair
    );

    [LibraryImport("ntdll.dll")]
    internal static partial int RtlQueryProcessDebugInformation(
        uint UniqueProcessId,
        QueryProcessFlags Flags,
        nint Buffer
    );

    [LibraryImport("ntdll.dll")]
    internal static partial int RtlDestroyQueryDebugBuffer(nint Buffer);
}


/// <summary>
/// Represents a process debug information.
/// </summary>
/// <remarks>
/// This class can be extended to support all the information options.
/// For now we just support heap information.
/// W</remarks>
internal class ProcessDebugInformation
{
    internal ProcessHeap[]? Heaps { get; }
    internal long HeapSizes { get; }

    /// <summary>
    /// Constructs the debug information for a given process ID.
    /// </summary>
    /// <param name="processId">The process ID.</param>
    /// <param name="flags">The flags to create the debug info.</param>
    /// <exception cref="NativeException">A call to a native function failed.</exception>
    /// <exception cref="OutOfMemoryException">The system couldn't allocate memory for the debug buffer.</exception>
    internal ProcessDebugInformation(uint processId, QueryProcessFlags flags, bool sizeOnly = false)
    {
        // Opening a handle to the process.
        SafeProcessHandle? hProcess = null;
        if (WinVer.CurrentVersion >= WinVer.WINDOWS_8 && WinVer.CurrentVersion <= WinVer.WINDOWS_8_1) {

            // Windows 8 requires ALL_ACCESS for PLM execution requests.
            hProcess = NativeProcess.OpenProcess(processId, ProcessAccess.ALL_ACCESS, false);
            if (hProcess.IsInvalid)
                throw new NativeException(Marshal.GetLastWin32Error());
        }
        else if (WinVer.CurrentVersion >= WinVer.WINDOWS_10) {

            // Windows 10 and above require SET_LIMITED for PLM execution requests.
            hProcess = NativeProcess.OpenProcess(processId, ProcessAccess.QUERY_LIMITED_INFORMATION | ProcessAccess.SET_LIMITED_INFORMATION | ProcessAccess.VM_READ, false);
            if (hProcess.IsInvalid)
                throw new NativeException(Marshal.GetLastWin32Error());
        }

        if (hProcess is null)
            throw new NativeException(ErrorCodes.ERROR_INVALID_HANDLE);

        // From SystemInformer:
        // The RtlQueryProcessDebugInformation function has two bugs on some versions
        // when querying the ProcessId for a frozen (suspended) immersive process.
        //
        // 1) It'll deadlock the current thread for 30 seconds.
        // 2) It'll return STATUS_SUCCESS but with a NULL Heaps buffer.
        //
        // A workaround was implemented using PowerRequest.CreateExecutionRequiredRequest()
        using SafePowerRequestHandle hPowerRequest = PowerRequest.CreateExecutionRequiredRequest(hProcess);

        int status;
        nint debugBuffer = 0;
        for (uint i = 0x400000; ; i *= 2) {
            
            // Creating the debug buffer.
            debugBuffer = RtlDebugAPI.RtlCreateQueryDebugBuffer(i, false);
            if (debugBuffer == 0)
                throw new OutOfMemoryException();

            // Querying the debug information.
            status = RtlDebugAPI.RtlQueryProcessDebugInformation(processId, flags, debugBuffer);
            if (status != ErrorCodes.STATUS_SUCCESS) {
                _ = RtlDebugAPI.RtlDestroyQueryDebugBuffer(debugBuffer);
                debugBuffer = 0;
            }

            if (status == ErrorCodes.STATUS_SUCCESS)
                break;

            if (status != ErrorCodes.STATUS_NO_MEMORY) {
                _ = RtlDebugAPI.RtlDestroyQueryDebugBuffer(debugBuffer);
                throw new NativeException(status, true);
            }

            if (2 * i <= i) {
                _ = RtlDebugAPI.RtlDestroyQueryDebugBuffer(debugBuffer);
                throw new OutOfMemoryException();
            }
        }

        // Marshaling the buffer.
        RTL_DEBUG_INFORMATION debugInfo = Marshal.PtrToStructure<RTL_DEBUG_INFORMATION>(debugBuffer);
        if (debugInfo.Heaps == 0) {
            _ = RtlDebugAPI.RtlDestroyQueryDebugBuffer(debugBuffer);
            throw new NativeException(ErrorCodes.STATUS_UNSUCCESSFUL, true);
        }

        // Getting heap information.
        try {
            if (sizeOnly) {
                if (WinVer.CurrentVersion >= WinVer.WINDOWS_11)
                    HeapSizes = GetHeapSizesV2(hProcess, ref debugInfo);
                else
                    HeapSizes = GetHeapSizesV1(hProcess, ref debugInfo);
            }
            else {
                if (WinVer.CurrentVersion >= WinVer.WINDOWS_11)
                    Heaps = GetHeapsV2(hProcess, ref debugInfo);
                else
                    Heaps = GetHeapsV1(hProcess, ref debugInfo);
            }
        }
        finally {
            _ = RtlDebugAPI.RtlDestroyQueryDebugBuffer(debugBuffer);
        }
    }

    /// <summary>
    /// Gets heap information for versions prior to Windows 11.
    /// </summary>
    /// <param name="hProcess">The handle to the process.</param>
    /// <param name="debugInfo">The debug information buffer.</param>
    /// <returns>An array of <see cref="ProcessHeap"/> containing the heap information.</returns>
    private static ProcessHeap[] GetHeapsV1(SafeProcessHandle hProcess, ref RTL_DEBUG_INFORMATION debugInfo)
    {
        uint heapId = 1;
        List<ProcessHeap> heaps = [];
        RTL_PROCESS_HEAPS_V1 heapInfo = new(debugInfo.Heaps);

        foreach (RTL_HEAP_INFORMATION_V1 heap in heapInfo.Heaps) {
            ProcessHeap heapData = new(heapId, heap.BaseAddress, heap.BytesCommitted);
            heaps.AddRange(Heap.GetHeapSegmentInformation(hProcess, heapData));
            heaps.Add(heapData);

            heapId++;
        }

        return [.. heaps];
    }

    /// <summary>
    /// Gets heap information for versions >= Windows 11.
    /// </summary>
    /// <param name="hProcess">The handle to the process.</param>
    /// <param name="debugInfo">The debug information buffer.</param>
    /// <returns>An array of <see cref="ProcessHeap"/> containing the heap information.</returns>
    private static ProcessHeap[] GetHeapsV2(SafeProcessHandle hProcess, ref RTL_DEBUG_INFORMATION debugInfo)
    {
        uint heapId = 1;
        List<ProcessHeap> heaps = [];
        RTL_PROCESS_HEAPS_V2 heapInfo = new(debugInfo.Heaps);

        foreach (RTL_HEAP_INFORMATION_V2 heap in heapInfo.Heaps) {
            ProcessHeap heapData = new(heapId, heap.BaseAddress, heap.BytesCommitted);
            heaps.AddRange(Heap.GetHeapSegmentInformation(hProcess, heapData));
            heaps.Add(heapData);

            heapId++;
        }

        return [.. heaps];
    }

    /// <summary>
    /// Get heap sizes for versions prior to Windows 11.
    /// </summary>
    /// <param name="hProcess">The handle to the process.</param>
    /// <param name="debugInfo">The debug information buffer.</param>
    /// <returns>The heap segment sizes.</returns>
    private static long GetHeapSizesV1(SafeProcessHandle hProcess, ref RTL_DEBUG_INFORMATION debugInfo)
    {
        long output = 0;
        RTL_PROCESS_HEAPS_V1 heapInfo = new(debugInfo.Heaps);
        foreach (RTL_HEAP_INFORMATION_V1 heap in heapInfo.Heaps) {
            output += Heap.GetHeapSegmentSize(hProcess, heap.BaseAddress).Sum();
        }

        return output;
    }

    /// <summary>
    /// Get heap sizes for versions >= Windows 11.
    /// </summary>
    /// <param name="hProcess">The handle to the process.</param>
    /// <param name="debugInfo">The debug information buffer.</param>
    /// <returns>The heap segment sizes.</returns>
    private static long GetHeapSizesV2(SafeProcessHandle hProcess, ref RTL_DEBUG_INFORMATION debugInfo)
    {
        long output = 0;
        RTL_PROCESS_HEAPS_V2 heapInfo = new(debugInfo.Heaps);
        foreach (RTL_HEAP_INFORMATION_V2 heap in heapInfo.Heaps) {
            output += Heap.GetHeapSegmentSize(hProcess, heap.BaseAddress).Sum();
        }

        return output;
    }
}