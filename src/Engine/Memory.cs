// This file is part of the ManagedStrings project and repository.
// Project files are licensed under the MIT license.
// https://github.com/FranciscoNabas/ManagedStrings

using System;
using System.Runtime.CompilerServices;
using ManagedStrings.Interop.Windows;

namespace ManagedStrings.Engine;

/// <summary>
/// The process virtual memory region types we categorize.
/// </summary>
[Flags]
public enum MemoryRegionType
{
    Unknown                   = 0x0,
    Teb                       = 0x1,
    Peb                       = 0x2,
    UserSharedData            = 0x4,
    HypervisorSharedData      = 0x8,
    CfgBitmap                 = 0x10,
    ApiSetMap                 = 0x20,
    ReadOnlySharedMemory      = 0x40,
    CodePageData              = 0x80,
    GdiSharedHandleTable      = 0x100,
    ShimData                  = 0x200,
    ActivationContextData     = 0x400,
    ProcessActivationContext  = 0x800,
    SystemActivationContext   = 0x1000,
    WerRegistrationData       = 0x2000,
    SiloSharedData            = 0x4000,
    TelemetryCoverage         = 0x8000,
    Stack                     = 0x10000,
    NtHeap                    = 0x20000,
    NtLfhHeap                 = 0x40000,
    SegmentHeap               = 0x80000,
    NtHeapSegment             = 0x100000,
    NtLfhSegment              = 0x200000,
    SegmentHeapSegment        = 0x400000,
    PrivateData               = 0x800000,
    MappedFile                = 0x1000000,
    Shareable                 = 0x2000000,
    Image                     = 0x4000000,
}

/// <summary>
/// The user options to which process virtual
/// memory region it wants to search.
/// </summary>
/// <remarks>
/// These bit flag values allign with the <see cref="MemoryRegionType"/>
/// so the user can include multiple regions without a lot of options.
/// </remarks>
[Flags]
public enum ReadMemoryFlags
{
    Stack       = 0x10000,    // Only thread stacks.
    Heap        = 0x7E0000,   // Only heaps (all types).
    Private     = 0xFFFFFF,   // Stacks, heaps, plus the remaining private data.
    MappedFile  = 0x1000000,  // Only mapped files.
    Shareable   = 0x2000000,  // Only mapped shareable regions.
    Mapped      = 0x3000000,  // Mapped files, shareable regions, plus the remaining mapped data (if any).
    Image       = 0x4000000,  // Only image files.
    All         = 0x7FFFFFF,  // All the processe's commited memory (it's a lot).
}

/// <summary>
/// Enumeration extensions.
/// </summary>
/// <remarks>
/// The motivation behind this is to have specialized methods to check
/// enumeration flags without using the slower <see cref="Enum.HasFlag(Enum)"/>.
/// </remarks>
internal static partial class EnumExtensions
{
    /// <summary>
    /// Checks if the input <see cref="ReadMemoryFlags"/> flags contains a flag.
    /// </summary>
    /// <param name="flags">The input <see cref="ReadMemoryFlags"/>.</param>
    /// <param name="flag">The <see cref="ReadMemoryFlags"/> flag(s) to compare to.</param>
    /// <returns>True if it contains the flag(s).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool HasMemoryFlag(this ReadMemoryFlags flags, ReadMemoryFlags flag)
        => (flags & flag) != 0;

    /// <summary>
    /// Checks if the input <see cref="MemoryRegionType"/> flags contains a <see cref="ReadMemoryFlags"/> flag.
    /// </summary>
    /// <param name="flags">The input <see cref="MemoryRegionType"/>.</param>
    /// <param name="flag">The <see cref="ReadMemoryFlags"/> flag(s) to compare to.</param>
    /// <returns>True if it contains the flag(s).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool HasMemoryFlag(this MemoryRegionType flags, ReadMemoryFlags flag)
        => (flags & (MemoryRegionType)flag) != 0;
}

/// <summary>
/// Information about a process virtual memory region.
/// </summary>
/// <param name="processId">The origin process ID.</param>
/// <param name="imagePath">The origin process image path.</param>
/// <param name="mbi">The memory basic information from <see cref="NativeProcess.NtQueryVirtualMemory(Microsoft.Win32.SafeHandles.SafeProcessHandle, nint, MEMORY_INFORMATION_CLASS, nint, long, out long)"/>.</param>
internal sealed class ProcessMemoryRegion(uint processId, string imagePath, ref MEMORY_BASIC_INFORMATION mbi)
{
    internal ProcessMemoryRegion? AllocationBaseItem { get; set; }

    internal uint ProcessId { get; } = processId;
    internal string ImagePath { get; } = imagePath;
    internal MemoryRegionType Type { get; set; }
    internal nint BaseAddress { get; } = mbi.BaseAddress;
    internal long Size { get; } = mbi.RegionSize;
    internal MemoryState State { get; } = mbi.State;
    internal PageProtection AllocationProtection { get; } = mbi.AllocationProtect;
    internal MemoryType BasicType { get; } = mbi.Type;
    internal bool IsValid { get; set; }

    // /sight... because you can't compare IntPtrs.
    internal long BaseLong { get; } = mbi.BaseAddress;

    internal uint ThreadId { get; set; }
    internal string? MappedFilePath { get; set; }
    internal ProcessHeap? HeapInformation { get; set; }
}