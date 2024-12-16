// This file is part of the ManagedStrings project and repository.
// Project files are licensed under the MIT license.
// https://github.com/FranciscoNabas/ManagedStrings

using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using ManagedStrings.Engine;

namespace ManagedStrings.Interop.Windows;

#region Enumerations

internal enum RTLP_HP_LOCK_TYPE
{
    HeapLockPaged,
    HeapLockNonPaged,
    HeapLockTypeMax,
}

[Flags]
internal enum HeapFlags : ushort
{
    BUSY               = 0x0001,
    SEGMENT            = 0x0002,
    SETTABLE_VALUE     = 0x0010,
    SETTABLE_FLAG1     = 0x0020,
    SETTABLE_FLAG2     = 0x0040,
    SETTABLE_FLAG3     = 0x0080,
    SETTABLE_FLAGS     = 0x00e0,
    UNCOMMITTED_RANGE  = 0x1000,
    PROTECTED_ENTRY    = 0x2000,
    LARGE_ALLOC        = 0x4000,
    LFH_ALLOC          = 0x8000,
}

internal enum HeapAllocFlags : uint
{
    NONE                 = 0x00000000,
    NO_SERIALIZE         = 0x00000001,
    GENERATE_EXCEPTIONS  = 0x00000004,
    ZERO_MEMORY          = 0x00000008,
}

#endregion

#region Structures

[StructLayout(LayoutKind.Explicit)]
internal struct ANY_HEAP
{
    [FieldOffset(0)] internal HEAP_OLD HeapOld;
    [FieldOffset(0)] internal HEAP_OLD32 HeapOld32;
    [FieldOffset(0)] internal HEAP Heap;
    [FieldOffset(0)] internal HEAP32 Heap32;
    [FieldOffset(0)] internal SEGMENT_HEAP SegmentHeap;
    [FieldOffset(0)] internal SEGMENT_HEAP32 SegmentHeap32;
}

#region x64

#region NtHeap

[StructLayout(LayoutKind.Explicit)]
internal struct HEAP
{
    [FieldOffset(0x0)] internal HEAP_SEGMENT Segment; // Offset: 0x0. Original type: _HEAP_SEGMENT.
    [FieldOffset(0x0)] internal HEAP_ENTRY Entry; // Offset: 0x0. Original type: _HEAP_ENTRY.
    [FieldOffset(0x10)] internal uint SegmentSignature;
    [FieldOffset(0x14)] internal uint SegmentFlags;
    [FieldOffset(0x18)] internal LIST_ENTRY SegmentListEntry; // Offset: 0x18. Original type: _LIST_ENTRY.
    [FieldOffset(0x28)] internal nint Heap; // Offset: 0x28. Pointer type: _HEAP *.
    [FieldOffset(0x30)] internal nint BaseAddress; // Offset: 0x30. Pointer type: void *.
    [FieldOffset(0x38)] internal uint NumberOfPages;
    [FieldOffset(0x40)] internal nint FirstEntry; // Offset: 0x40. Pointer type: _HEAP_ENTRY *.
    [FieldOffset(0x48)] internal nint LastValidEntry; // Offset: 0x48. Pointer type: _HEAP_ENTRY *.
    [FieldOffset(0x50)] internal uint NumberOfUnCommittedPages;
    [FieldOffset(0x54)] internal uint NumberOfUnCommittedRanges;
    [FieldOffset(0x58)] internal ushort SegmentAllocatorBackTraceIndex;
    [FieldOffset(0x5A)] internal ushort Reserved;
    [FieldOffset(0x60)] internal LIST_ENTRY UCRSegmentList; // Offset: 0x60. Original type: _LIST_ENTRY.
    [FieldOffset(0x70)] internal uint Flags;
    [FieldOffset(0x74)] internal uint ForceFlags;
    [FieldOffset(0x78)] internal uint CompatibilityFlags;
    [FieldOffset(0x7C)] internal uint EncodeFlagMask;
    [FieldOffset(0x80)] internal HEAP_ENTRY Encoding; // Offset: 0x80. Original type: _HEAP_ENTRY.
    [FieldOffset(0x90)] internal uint Interceptor;
    [FieldOffset(0x94)] internal uint VirtualMemoryThreshold;
    [FieldOffset(0x98)] internal uint Signature;
    [FieldOffset(0xA0)] internal ulong SegmentReserve;
    [FieldOffset(0xA8)] internal ulong SegmentCommit;
    [FieldOffset(0xB0)] internal ulong DeCommitFreeBlockThreshold;
    [FieldOffset(0xB8)] internal ulong DeCommitTotalFreeThreshold;
    [FieldOffset(0xC0)] internal ulong TotalFreeSize;
    [FieldOffset(0xC8)] internal ulong MaximumAllocationSize;
    [FieldOffset(0xD0)] internal ushort ProcessHeapsListIndex;
    [FieldOffset(0xD2)] internal ushort HeaderValidateLength;
    [FieldOffset(0xD8)] internal nint HeaderValidateCopy; // Offset: 0xD8. Pointer type: void *.
    [FieldOffset(0xE0)] internal ushort NextAvailableTagIndex;
    [FieldOffset(0xE2)] internal ushort MaximumTagIndex;
    [FieldOffset(0xE8)] internal nint TagEntries; // Offset: 0xE8. Pointer type: _HEAP_TAG_ENTRY *.
    [FieldOffset(0xF0)] internal LIST_ENTRY UCRList; // Offset: 0xF0. Original type: _LIST_ENTRY.
    [FieldOffset(0x100)] internal ulong AlignRound;
    [FieldOffset(0x108)] internal ulong AlignMask;
    [FieldOffset(0x110)] internal LIST_ENTRY VirtualAllocdBlocks; // Offset: 0x110. Original type: _LIST_ENTRY.
    [FieldOffset(0x120)] internal LIST_ENTRY SegmentList; // Offset: 0x120. Original type: _LIST_ENTRY.
    [FieldOffset(0x130)] internal ushort AllocatorBackTraceIndex;
    [FieldOffset(0x134)] internal uint NonDedicatedListLength;
    [FieldOffset(0x138)] internal nint BlocksIndex; // Offset: 0x138. Pointer type: void *.
    [FieldOffset(0x140)] internal nint UCRIndex; // Offset: 0x140. Pointer type: void *.
    [FieldOffset(0x148)] internal nint PseudoTagEntries; // Offset: 0x148. Pointer type: _HEAP_PSEUDO_TAG_ENTRY *.
    [FieldOffset(0x150)] internal LIST_ENTRY FreeLists; // Offset: 0x150. Original type: _LIST_ENTRY.
    [FieldOffset(0x160)] internal nint LockVariable; // Offset: 0x160. Pointer type: _HEAP_LOCK *.
    [FieldOffset(0x168)] internal nint CommitRoutine; // Offset: 0x168. Function pointer: long (__cdecl*)(void *,void * *,unsigned __int64 *).
    [FieldOffset(0x170)] internal RTL_RUN_ONCE StackTraceInitVar; // Offset: 0x170. Original type: _RTL_RUN_ONCE.
    [FieldOffset(0x178)] internal RTL_HEAP_MEMORY_LIMIT_DATA CommitLimitData; // Offset: 0x178. Original type: _RTL_HEAP_MEMORY_LIMIT_DATA.
    [FieldOffset(0x198)] internal nint FrontEndHeap; // Offset: 0x198. Pointer type: void *.
    [FieldOffset(0x1A0)] internal ushort FrontHeapLockCount;
    [FieldOffset(0x1A2)] internal byte FrontEndHeapType;
    [FieldOffset(0x1A3)] internal byte RequestedFrontEndHeapType;
    [FieldOffset(0x1A8)] internal nint FrontEndHeapUsageData; // Offset: 0x1A8. Pointer type: unsigned short *.
    [FieldOffset(0x1B0)] internal ushort FrontEndHeapMaximumIndex;

    [FieldOffset(0x1B2)] internal byte FrontEndHeapStatusBitmap1;
    [FieldOffset(0x232)] internal byte FrontEndHeapStatusBitmap129;

    [FieldOffset(0x233)] private readonly byte InternalFlags;
    internal readonly bool ReadOnly => (InternalFlags & 0x1) > 0; // :1.

    [FieldOffset(0x238)] internal HEAP_COUNTERS Counters; // Offset: 0x1F4. Original type: _HEAP_COUNTERS.
    [FieldOffset(0x2B0)] internal HEAP_TUNING_PARAMETERS TuningParameters; // Offset: 0x250. Original type: _HEAP_TUNING_PARAMETERS.
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct HEAP_SEGMENT
{
    internal readonly HEAP_ENTRY Entry; // Offset: 0x0. Original type: _HEAP_ENTRY.
    internal readonly uint SegmentSignature;
    internal readonly uint SegmentFlags;
    internal readonly LIST_ENTRY SegmentListEntry; // Offset: 0x18. Original type: _LIST_ENTRY.
    internal readonly nint Heap; // Offset: 0x28. Pointer type: _HEAP *.
    internal readonly nint BaseAddress; // Offset: 0x30. Pointer type: void *.
    internal readonly uint NumberOfPages;
    internal readonly nint FirstEntry; // Offset: 0x40. Pointer type: _HEAP_ENTRY *.
    internal readonly nint LastValidEntry; // Offset: 0x48. Pointer type: _HEAP_ENTRY *.
    internal readonly uint NumberOfUnCommittedPages;
    internal readonly uint NumberOfUnCommittedRanges;
    internal readonly ushort SegmentAllocatorBackTraceIndex;
    internal readonly ushort Reserved;
    internal readonly LIST_ENTRY UCRSegmentList; // Offset: 0x60. Original type: _LIST_ENTRY.
}

[StructLayout(LayoutKind.Explicit)]
internal readonly struct HEAP_ENTRY
{
    [FieldOffset(0x0)] internal readonly HEAP_UNPACKED_ENTRY UnpackedEntry; // Offset: 0x0. Original type: _HEAP_UNPACKED_ENTRY.
    [FieldOffset(0x0)] internal readonly nint PreviousBlockPrivateData; // Offset: 0x0. Pointer type: void *.
    [FieldOffset(0x0)] internal readonly HEAP_EXTENDED_ENTRY ExtendedEntry; // Offset: 0x0. Original type: _HEAP_EXTENDED_ENTRY.
    [FieldOffset(0x0)] internal readonly nint Reserved; // Offset: 0x0. Pointer type: void *.
    [FieldOffset(0x0)] internal readonly nint ReservedForAlignment; // Offset: 0x0. Pointer type: void *.
    [FieldOffset(0x8)] internal readonly ushort Size;
    [FieldOffset(0x8)] internal readonly uint SubSegmentCode;
    [FieldOffset(0x8)] internal readonly ulong CompactHeader;
    [FieldOffset(0x8)] internal readonly ushort FunctionIndex;
    [FieldOffset(0x8)] internal readonly uint InterceptorValue;
    [FieldOffset(0x8)] internal readonly uint Code1;
    [FieldOffset(0x8)] internal readonly ulong AgregateCode;
    [FieldOffset(0xA)] internal readonly byte Flags;
    [FieldOffset(0xA)] internal readonly ushort ContextValue;
    [FieldOffset(0xB)] internal readonly byte SmallTagIndex;
    [FieldOffset(0xC)] internal readonly ushort PreviousSize;
    [FieldOffset(0xC)] internal readonly ushort UnusedBytesLength;
    [FieldOffset(0xC)] internal readonly ushort Code2;
    [FieldOffset(0xC)] internal readonly uint Code234;
    [FieldOffset(0xE)] internal readonly byte SegmentOffset;
    [FieldOffset(0xE)] internal readonly byte LFHFlags;
    [FieldOffset(0xE)] internal readonly byte EntryOffset;
    [FieldOffset(0xE)] internal readonly byte Code3;
    [FieldOffset(0xF)] internal readonly byte UnusedBytes;
    [FieldOffset(0xF)] internal readonly byte ExtendedBlockSignature;
    [FieldOffset(0xF)] internal readonly byte Code4;
}

[StructLayout(LayoutKind.Explicit)]
internal readonly struct RTL_RUN_ONCE
{
    [FieldOffset(0x0)] internal readonly nint Ptr; // Offset: 0x0. Pointer type: void *.

    [FieldOffset(0x0)] private readonly ulong Value;
    internal byte State => (byte)(Value & 0x3); // :2.
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct RTL_HEAP_MEMORY_LIMIT_DATA
{
    internal readonly ulong CommitLimitBytes;
    internal readonly ulong CommitLimitFailureCode;
    internal readonly ulong MaxAllocationSizeBytes;
    internal readonly ulong AllocationLimitFailureCode;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct HEAP_COUNTERS
{
    internal readonly ulong TotalMemoryReserved;
    internal readonly ulong TotalMemoryCommitted;
    internal readonly ulong TotalMemoryLargeUCR;
    internal readonly ulong TotalSizeInVirtualBlocks;
    internal readonly uint TotalSegments;
    internal readonly uint TotalUCRs;
    internal readonly uint CommittOps;
    internal readonly uint DeCommitOps;
    internal readonly uint LockAcquires;
    internal readonly uint LockCollisions;
    internal readonly uint CommitRate;
    internal readonly uint DecommittRate;
    internal readonly uint CommitFailures;
    internal readonly uint InBlockCommitFailures;
    internal readonly uint PollIntervalCounter;
    internal readonly uint DecommitsSinceLastCheck;
    internal readonly uint HeapPollInterval;
    internal readonly uint AllocAndFreeOps;
    internal readonly uint AllocationIndicesActive;
    internal readonly uint InBlockDeccommits;
    internal readonly ulong InBlockDeccomitSize;
    internal readonly ulong HighWatermarkSize;
    internal readonly ulong LastPolledSize;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct HEAP_TUNING_PARAMETERS
{
    internal readonly uint CommittThresholdShift;
    internal readonly ulong MaxPreCommittThreshold;
}

[StructLayout(LayoutKind.Explicit)]
internal readonly struct HEAP_UNPACKED_ENTRY
{
    [FieldOffset(0x0)] internal readonly nint PreviousBlockPrivateData; // Offset: 0x0. Pointer type: void *.
    [FieldOffset(0x8)] internal readonly ushort Size;
    [FieldOffset(0x8)] internal readonly uint SubSegmentCode;
    [FieldOffset(0x8)] internal readonly ulong CompactHeader;
    [FieldOffset(0xA)] internal readonly byte Flags;
    [FieldOffset(0xB)] internal readonly byte SmallTagIndex;
    [FieldOffset(0xC)] internal readonly ushort PreviousSize;
    [FieldOffset(0xE)] internal readonly byte SegmentOffset;
    [FieldOffset(0xE)] internal readonly byte LFHFlags;
    [FieldOffset(0xF)] internal readonly byte UnusedBytes;
}

[StructLayout(LayoutKind.Explicit)]
internal readonly struct HEAP_EXTENDED_ENTRY
{
    [FieldOffset(0x0)] internal readonly nint Reserved; // Offset: 0x0. Pointer type: void *.
    [FieldOffset(0x8)] internal readonly ushort FunctionIndex;
    [FieldOffset(0x8)] internal readonly uint InterceptorValue;
    [FieldOffset(0xA)] internal readonly ushort ContextValue;
    [FieldOffset(0xC)] internal readonly ushort UnusedBytesLength;
    [FieldOffset(0xE)] internal readonly byte EntryOffset;
    [FieldOffset(0xF)] internal readonly byte ExtendedBlockSignature;
}

#endregion

#region SegmentHeap

[StructLayout(LayoutKind.Explicit)]
internal struct SEGMENT_HEAP
{
    [FieldOffset(0x0)] internal RTL_HP_ENV_HANDLE EnvHandle; // Offset: 0x0. Original type: RTL_HP_ENV_HANDLE.
    [FieldOffset(0x10)] internal uint Signature;
    [FieldOffset(0x14)] internal uint GlobalFlags;
    [FieldOffset(0x18)] internal uint Interceptor;
    [FieldOffset(0x1C)] internal ushort ProcessHeapListIndex;

    [FieldOffset(0x1E)] private readonly ushort Flags; // Root field for bitfields at offset '0x1E'.
    internal readonly bool AllocatedFromMetadata => (Flags & 0x1) > 0; // :1.

    [FieldOffset(0x20)] internal RTL_HEAP_MEMORY_LIMIT_DATA CommitLimitData; // Offset: 0x20. Original type: _RTL_HEAP_MEMORY_LIMIT_DATA.
    [FieldOffset(0x20)] internal ulong ReservedMustBeZero1;
    [FieldOffset(0x28)] internal nint UserContext; // Offset: 0x28. Pointer type: void *.
    [FieldOffset(0x30)] internal ulong ReservedMustBeZero2;
    [FieldOffset(0x38)] internal nint Spare; // Offset: 0x38. Pointer type: void *.
    [FieldOffset(0x40)] internal ulong LargeMetadataLock;
    [FieldOffset(0x48)] internal RTL_RB_TREE LargeAllocMetadata; // Offset: 0x48. Original type: _RTL_RB_TREE.
    [FieldOffset(0x58)] internal ulong LargeReservedPages;
    [FieldOffset(0x60)] internal ulong LargeCommittedPages;
    [FieldOffset(0x68)] internal ulong Tag;
    [FieldOffset(0x70)] internal RTL_RUN_ONCE StackTraceInitVar; // Offset: 0x70. Original type: _RTL_RUN_ONCE.
    [FieldOffset(0x80)] internal HEAP_RUNTIME_MEMORY_STATS MemStats; // Offset: 0x80. Original type: _HEAP_RUNTIME_MEMORY_STATS.
    [FieldOffset(0xD8)] internal ushort GlobalLockCount;
    [FieldOffset(0xDC)] internal uint GlobalLockOwner;
    [FieldOffset(0xE0)] internal ulong ContextExtendLock;
    [FieldOffset(0xE8)] internal nint AllocatedBase; // Offset: 0xE8. Pointer type: unsigned char *.
    [FieldOffset(0xF0)] internal nint UncommittedBase; // Offset: 0xF0. Pointer type: unsigned char *.
    [FieldOffset(0xF8)] internal nint ReservedLimit; // Offset: 0xF8. Pointer type: unsigned char *.
    [FieldOffset(0x100)] internal nint ReservedRegionEnd; // Offset: 0x100. Pointer type: unsigned char *.
    [FieldOffset(0x108)] internal RTL_HP_HEAP_VA_CALLBACKS_ENCODED CallbacksEncoded; // Offset: 0x108. Original type: _RTL_HP_HEAP_VA_CALLBACKS_ENCODED.

    // For some annoying reason the runtime doesn't get the allignment of this array.
    [FieldOffset(0x140)] internal HEAP_SEG_CONTEXT SegContexts1;
    [FieldOffset(0x200)] internal HEAP_SEG_CONTEXT SegContexts2;

    [FieldOffset(0x2C0)] internal HEAP_VS_CONTEXT VsContext; // Offset: 0x2C0. Original type: _HEAP_VS_CONTEXT.
    [FieldOffset(0x380)] internal HEAP_LFH_CONTEXT LfhContext; // Offset: 0x380. Original type: _HEAP_LFH_CONTEXT.
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct RTL_HP_ENV_HANDLE
{
    internal readonly nint h1;
    internal readonly nint h2;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct RTL_RB_TREE
{
    internal readonly nint Root; // Offset: 0x0. Pointer type: _RTL_BALANCED_NODE *.

    private readonly nint Min; // Offset: 0x8. Pointer type: _RTL_BALANCED_NODE *.
    internal bool Encoded => ((long)Min & 0x1) > 0; // :1.
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct HEAP_RUNTIME_MEMORY_STATS
{
    internal readonly ulong TotalReservedPages;
    internal readonly ulong TotalCommittedPages;
    internal readonly ulong FreeCommittedPages;
    internal readonly ulong LfhFreeCommittedPages;

    internal readonly HEAP_OPPORTUNISTIC_LARGE_PAGE_STATS LargePageStats1;
    internal readonly HEAP_OPPORTUNISTIC_LARGE_PAGE_STATS LargePageStats2;

    internal readonly RTL_HP_SEG_ALLOC_POLICY LargePageUtilizationPolicy; // Offset: 0x40. Original type: _RTL_HP_SEG_ALLOC_POLICY.
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct HEAP_OPPORTUNISTIC_LARGE_PAGE_STATS
{
    internal readonly ulong SmallPagesInUseWithinLarge;
    internal readonly ulong OpportunisticLargePageCount;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct RTL_HP_SEG_ALLOC_POLICY
{
    internal readonly ulong MinLargePages;
    internal readonly ulong MaxLargePages;
    internal readonly byte MinUtilization;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct RTL_HP_HEAP_VA_CALLBACKS_ENCODED
{
    internal readonly ulong CallbackContext;
    internal readonly ulong AllocateVirtualMemoryEncoded;
    internal readonly ulong FreeVirtualMemoryEncoded;
    internal readonly ulong QueryVirtualMemoryEncoded;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct HEAP_SEG_CONTEXT
{
    internal readonly ulong SegmentMask;
    internal readonly byte UnitShift;
    internal readonly byte PagesPerUnitShift;
    internal readonly byte FirstDescriptorIndex;
    internal readonly byte CachedCommitSoftShift;
    internal readonly byte CachedCommitHighShift;

    internal readonly byte Flags;
    internal byte LargePagePolicy => (byte)(Flags & 0x7); // :3.
    internal bool FullDecommit => (Flags & 0x8) > 0; // :1.
    internal bool ReleaseEmptySegments => (Flags & 0x10) > 0; // :1.

    internal readonly uint MaxAllocationSize;
    internal readonly short OlpStatsOffset;
    internal readonly short MemStatsOffset;
    internal readonly nint LfhContext; // Offset: 0x18. Pointer type: void *.
    internal readonly nint VsContext; // Offset: 0x20. Pointer type: void *.
    internal readonly RTL_HP_ENV_HANDLE EnvHandle; // Offset: 0x28. Original type: RTL_HP_ENV_HANDLE.
    internal readonly nint Heap; // Offset: 0x38. Pointer type: void *.
    internal readonly ulong SegmentLock;
    internal readonly LIST_ENTRY SegmentListHead; // Offset: 0x48. Original type: _LIST_ENTRY.
    internal readonly ulong SegmentCount;
    internal readonly RTL_RB_TREE FreePageRanges; // Offset: 0x60. Original type: _RTL_RB_TREE.
    internal readonly ulong FreeSegmentListLock;

    internal readonly SINGLE_LIST_ENTRY FreeSegmentList1;
    internal readonly SINGLE_LIST_ENTRY FreeSegmentList2;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct SINGLE_LIST_ENTRY
{
    internal readonly nint Next; // Offset: 0x0. Pointer type: _SINGLE_LIST_ENTRY *.
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct HEAP_VS_CONTEXT
{
    internal readonly ulong Lock;
    internal readonly RTLP_HP_LOCK_TYPE LockType; // Offset: 0x8. Original type: _RTLP_HP_LOCK_TYPE.
    internal readonly RTL_RB_TREE FreeChunkTree; // Offset: 0x10. Original type: _RTL_RB_TREE.
    internal readonly LIST_ENTRY SubsegmentList; // Offset: 0x20. Original type: _LIST_ENTRY.
    internal readonly ulong TotalCommittedUnits;
    internal readonly ulong FreeCommittedUnits;
    internal readonly HEAP_VS_DELAY_FREE_CONTEXT DelayFreeContext; // Offset: 0x40. Original type: _HEAP_VS_DELAY_FREE_CONTEXT.
    internal readonly nint BackendCtx; // Offset: 0x80. Pointer type: void *.
    internal readonly HEAP_SUBALLOCATOR_CALLBACKS Callbacks; // Offset: 0x88. Original type: _HEAP_SUBALLOCATOR_CALLBACKS.
    internal readonly RTL_HP_VS_CONFIG Config; // Offset: 0xB0. Original type: _RTL_HP_VS_CONFIG.
    internal readonly uint Flags;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct HEAP_VS_DELAY_FREE_CONTEXT
{
    internal readonly SLIST_HEADER ListHead; // Offset: 0x0. Original type: _SLIST_HEADER.
}

[StructLayout(LayoutKind.Explicit)]
internal readonly struct SLIST_HEADER
{
    [FieldOffset(0x0)] internal readonly ulong Alignment;
    [FieldOffset(0x8)] internal readonly ulong Region;

    [FieldOffset(0x0)] private readonly ulong Headerx641;
    internal ushort Depth => (ushort)(Headerx641 & 0xFFFF); // :16.
    internal ulong Sequence => (ulong)((Headerx641 >> 0x30) & 0xFFFFFFFFFFFF); // :48.

    [FieldOffset(0x8)] private readonly ulong Headerx642;
    internal byte Reserved => (byte)(Headerx642 & 0xF); // :4.
    internal ulong NextEntry => (ulong)((Headerx642 >> 0x3C) & 0xFFFFFFFFFFFFFFF); // :60.
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct HEAP_SUBALLOCATOR_CALLBACKS
{
    internal readonly ulong Allocate;
    internal readonly ulong Free;
    internal readonly ulong Commit;
    internal readonly ulong Decommit;
    internal readonly ulong ExtendContext;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct RTL_HP_VS_CONFIG
{
    internal readonly uint Flags;
    internal bool PageAlignLargeAllocs => (Flags & 0x1) > 0; // :1.
    internal bool FullDecommit => (Flags & 0x2) > 0; // :1.
    internal bool EnableDelayFree => (Flags & 0x4) > 0; // :1.
}

[StructLayout(LayoutKind.Explicit)]
internal readonly struct HEAP_LFH_CONTEXT
{
    [FieldOffset(0x0)] internal readonly nint BackendCtx; // Offset: 0x0. Pointer type: void *.
    [FieldOffset(0x8)] internal readonly HEAP_SUBALLOCATOR_CALLBACKS Callbacks; // Offset: 0x8. Original type: _HEAP_SUBALLOCATOR_CALLBACKS.
    [FieldOffset(0x30)] internal readonly nint AffinityModArray; // Offset: 0x30. Pointer type: unsigned char *.
    [FieldOffset(0x38)] internal readonly byte MaxAffinity;
    [FieldOffset(0x39)] internal readonly byte LockType;
    [FieldOffset(0x3A)] internal readonly short MemStatsOffset;
    [FieldOffset(0x3C)] internal readonly RTL_HP_LFH_CONFIG Config; // Offset: 0x3C. Original type: _RTL_HP_LFH_CONFIG.
    [FieldOffset(0x40)] internal readonly HEAP_LFH_SUBSEGMENT_STATS BucketStats; // Offset: 0x40. Original type: _HEAP_LFH_SUBSEGMENT_STATS.
    [FieldOffset(0x172)] internal readonly ulong SubsegmentCreationLock;

    [FieldOffset(0x17A)] internal readonly nint Buckets1;
    [FieldOffset(0x582)] internal readonly nint Buckets129;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct RTL_HP_LFH_CONFIG
{
    internal readonly ushort MaxBlockSize;

    private readonly ushort Flags;
    internal bool WitholdPageCrossingBlocks => (Flags & 0x1) > 0; // :1.
    internal bool DisableRandomization => (Flags & 0x2) > 0; // :1.
}

[StructLayout(LayoutKind.Explicit)]
internal readonly struct HEAP_LFH_SUBSEGMENT_STATS
{
    [FieldOffset(0x0)] internal readonly HEAP_LFH_SUBSEGMENT_STAT Bucket1;
    [FieldOffset(0x66)] internal readonly HEAP_LFH_SUBSEGMENT_STAT Bucket2;
    [FieldOffset(0xCC)] internal readonly HEAP_LFH_SUBSEGMENT_STAT Bucket3;
    [FieldOffset(0x132)] internal readonly HEAP_LFH_SUBSEGMENT_STAT Bucket4;

    [FieldOffset(0x0)] internal readonly nint AllStats; // Offset: 0x0. Pointer type: void *.
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct HEAP_LFH_SUBSEGMENT_STAT
{
    internal readonly byte Index;
    internal readonly byte Count;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct HEAP_LFH_BUCKET
{
    internal readonly HEAP_LFH_SUBSEGMENT_OWNER State; // Offset: 0x0. Original type: _HEAP_LFH_SUBSEGMENT_OWNER.
    internal readonly ulong TotalBlockCount;
    internal readonly ulong TotalSubsegmentCount;
    internal readonly uint ReciprocalBlockSize;
    internal readonly byte Shift;
    internal readonly byte ContentionCount;
    internal readonly ulong AffinityMappingLock;
    internal readonly nint ProcAffinityMapping; // Offset: 0x58. Pointer type: unsigned char *.
    internal readonly nint AffinitySlots; // Offset: 0x60. Pointer type: _HEAP_LFH_AFFINITY_SLOT * *.
}

[StructLayout(LayoutKind.Explicit)]
internal readonly struct HEAP_LFH_SUBSEGMENT_OWNER
{
    [FieldOffset(0x0)] private readonly byte Flags; // Root field for bitfields at offset '0x0'.
    internal bool IsBucket => (Flags & 0x1) > 0; // :1.
    internal byte Spare0 => (byte)((Flags >> 0x7) & 0x7F); // :7.

    [FieldOffset(0x1)] internal readonly byte BucketIndex;
    [FieldOffset(0x2)] internal readonly byte SlotCount;
    [FieldOffset(0x2)] internal readonly byte SlotIndex;
    [FieldOffset(0x3)] internal readonly byte Spare1;
    [FieldOffset(0x8)] internal readonly ulong AvailableSubsegmentCount;
    [FieldOffset(0x10)] internal readonly ulong Lock;
    [FieldOffset(0x18)] internal readonly LIST_ENTRY AvailableSubsegmentList; // Offset: 0x18. Original type: _LIST_ENTRY.
    [FieldOffset(0x28)] internal readonly LIST_ENTRY FullSubsegmentList; // Offset: 0x28. Original type: _LIST_ENTRY.
}

[StructLayout(LayoutKind.Explicit)]
internal readonly struct HEAP_PAGE_SEGMENT
{
    [FieldOffset(0x0)] internal readonly LIST_ENTRY ListEntry;

    [FieldOffset(0x0)] internal readonly HEAP_PAGE_RANGE_DESCRIPTOR DescArray1;
    [FieldOffset(0x100)] internal readonly HEAP_PAGE_RANGE_DESCRIPTOR DescArray256;

    [FieldOffset(0x10)] internal readonly ulong Signature;
    [FieldOffset(0x18)] internal readonly nint SegmentCommitState; // _HEAP_SEGMENT_MGR_COMMIT_STATE*.
    [FieldOffset(0x20)] internal readonly byte UnusedWatermark;
}

[StructLayout(LayoutKind.Explicit)]
internal readonly struct HEAP_PAGE_RANGE_DESCRIPTOR
{
    [FieldOffset(0x0)] internal readonly RTL_BALANCED_NODE TreeNode;
    [FieldOffset(0x0)] internal readonly uint TreeSignature;
    [FieldOffset(0x4)] internal readonly uint UnusedBytes;

    [FieldOffset(0x8)] private readonly ushort Flags1;
    internal bool ExtraPresent => (Flags1 & 0x1) > 0;
    internal ushort Spare0 => (ushort)(Flags1 >> 1);

    [FieldOffset(0x18)] internal readonly byte RangeFlags;
    [FieldOffset(0x19)] internal readonly byte CommitedPageCount;
    [FieldOffset(0x1A)] internal readonly byte UnitOffset;
    [FieldOffset(0x1B)] internal readonly byte Spare;
    [FieldOffset(0x1C)] internal readonly HEAP_DESCRIPTOR_KEY Key;

    [FieldOffset(0x1C)] internal readonly byte Align1;
    [FieldOffset(0x1D)] internal readonly byte Align2;
    [FieldOffset(0x1E)] internal readonly byte Align3;

    [FieldOffset(0x1F)] internal readonly byte UnitSize;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct HEAP_SEGMENT_MGR_COMMIT_STATE
{
    internal readonly ushort EntireUShort;
    internal ushort CommittedPageCount => (ushort)(EntireUShort & 0x7FF);
    internal byte Spare => (byte)((EntireUShort >> 11) & 0x7);
    internal bool LargePageOperationInProgress => (EntireUShort & 0x4000) > 0;
    internal bool LargePageCommit => (EntireUShort & 0x8000) > 0;
}

[StructLayout(LayoutKind.Explicit)]
internal readonly struct RTL_BALANCED_NODE
{
    [FieldOffset(0x0)] internal readonly nint Children1; // _RTL_BALANCED_NODE*[2].
    [FieldOffset(0x8)] internal readonly nint Children2;

    [FieldOffset(0x0)] internal readonly nint Left; // _RTL_BALANCED_NODE*.
    [FieldOffset(0x8)] internal readonly nint Right; // _RTL_BALANCED_NODE*.

    [FieldOffset(0x10)] internal readonly ulong ParentValue;
    internal bool Red => (ParentValue & 0x1) > 0;
    internal byte Balance => (byte)(ParentValue & 0x3);
}

// Value not exported by the kernel.
[StructLayout(LayoutKind.Sequential)]
internal readonly struct HEAP_DESCRIPTOR_KEY
{
    internal readonly uint Key;
    internal ushort EncodedCommitedPageCount => (ushort)(Key & 0xFFFF);
    internal byte LargePageCost => (byte)((Key >> 16) & 0xFF);
    internal byte UnitCount => (byte)((Key >> 24) & 0xFF);
}

#endregion

#region Old

[StructLayout(LayoutKind.Explicit)]
internal struct HEAP_OLD
{
    [FieldOffset(0x0)] internal HEAP_ENTRY Entry;
    [FieldOffset(0x10)] internal uint SegmentSignature;
    [FieldOffset(0x14)] internal uint SegmentFlags;
    [FieldOffset(0x18)] internal LIST_ENTRY SegmentListEntry;
    [FieldOffset(0x28)] internal nint Heap;
    [FieldOffset(0x30)] internal nint BaseAddress;
    [FieldOffset(0x38)] internal uint NumberOfPages;
    [FieldOffset(0x40)] internal nint FirstEntry; // HEAP_ENTRY*.
    [FieldOffset(0x48)] internal nint LastValidEntry; // HEAP_ENTRY*.
    [FieldOffset(0x50)] internal uint NumberOfUnCommittedPages;
    [FieldOffset(0x54)] internal uint NumberOfUnCommittedRanges;
    [FieldOffset(0x58)] internal ushort SegmentAllocatorBackTraceIndex;
    [FieldOffset(0x5A)] internal ushort Reserved;
    [FieldOffset(0x60)] internal LIST_ENTRY UCRSegmentList;
    [FieldOffset(0x70)] internal uint Flags;
    [FieldOffset(0x74)] internal uint ForceFlags;
    [FieldOffset(0x78)] internal uint CompatibilityFlags;
    [FieldOffset(0x7C)] internal uint EncodeFlagMask;
    [FieldOffset(0x80)] internal HEAP_ENTRY Encoding;
    [FieldOffset(0x90)] internal ulong PointerKey;
    [FieldOffset(0x98)] internal uint Interceptor;
    [FieldOffset(0x9C)] internal uint VirtualMemoryThreshold;
    [FieldOffset(0xA0)] internal uint Signature;
    [FieldOffset(0xA8)] internal ulong SegmentReserve;
    [FieldOffset(0xB0)] internal ulong SegmentCommit;
    [FieldOffset(0xB8)] internal ulong DeCommitFreeBlockThreshold;
    [FieldOffset(0xC0)] internal ulong DeCommitTotalFreeThreshold;
    [FieldOffset(0xC8)] internal ulong TotalFreeSize;
    [FieldOffset(0xD0)] internal ulong MaximumAllocationSize;
    [FieldOffset(0xD8)] internal ushort ProcessListHeapsIndex;
    [FieldOffset(0xDA)] internal ushort HeaderValidateLength;
    [FieldOffset(0xE0)] internal nint HeaderValidateCopy;
    [FieldOffset(0xE8)] internal ushort NextAvailableTagIndex;
    [FieldOffset(0xEA)] internal ushort MaximumTagIndex;
    [FieldOffset(0xF0)] internal nint TagEntries; // HEAP_TAG_ENTRY*.
    [FieldOffset(0xF8)] internal LIST_ENTRY UCRList;
    [FieldOffset(0x108)] internal ulong AlignRound;
    [FieldOffset(0x110)] internal ulong AlignMask;
    [FieldOffset(0x118)] internal LIST_ENTRY VirtualAllocdBlocks;
    [FieldOffset(0x128)] internal LIST_ENTRY SegmentList;
    [FieldOffset(0x138)] internal ushort AllocatorBackTraceIndex;
    [FieldOffset(0x13C)] internal uint NonDedicatedListLength;
    [FieldOffset(0x140)] internal nint BlocksIndex;
    [FieldOffset(0x140)] internal nint UCRIndex;
    [FieldOffset(0x150)] internal nint PseudoTagEntries; // HEAP_PSEUDO_TAG_ENTRY*.
    [FieldOffset(0x158)] internal LIST_ENTRY FreeLists;
    [FieldOffset(0x168)] internal nint LockVariable; // HEAP_LOCK*.
    [FieldOffset(0x170)] internal nint CommitRoutine;
    [FieldOffset(0x178)] internal nint FrontEndHeap;
    [FieldOffset(0x180)] internal ushort FrontHeapLockCount;
    [FieldOffset(0x182)] internal byte FrontEndHeapType;
    [FieldOffset(0x188)] internal HEAP_COUNTERS Counters;
    [FieldOffset(0x1F8)] internal HEAP_TUNING_PARAMETERS TuningParameters;
}

#endregion

#endregion

#region x86

#region NtHeap

[StructLayout(LayoutKind.Explicit)]
internal struct HEAP32
{
    [FieldOffset(0x0)] internal HEAP_SEGMENT32 Segment; // Offset: 0x0. Original type: _HEAP_SEGMENT.
    [FieldOffset(0x0)] internal HEAP_ENTRY32 Entry; // Offset: 0x0. Original type: _HEAP_ENTRY.
    [FieldOffset(0x8)] internal uint SegmentSignature;
    [FieldOffset(0xC)] internal uint SegmentFlags;
    [FieldOffset(0x10)] internal LIST_ENTRY32 SegmentListEntry; // Offset: 0x10. Original type: _LIST_ENTRY.
    [FieldOffset(0x18)] internal uint Heap; // Offset: 0x18. Pointer type: _HEAP *.
    [FieldOffset(0x1C)] internal uint BaseAddress; // Offset: 0x1C. Pointer type: void *.
    [FieldOffset(0x20)] internal uint NumberOfPages;
    [FieldOffset(0x24)] internal uint FirstEntry; // Offset: 0x24. Pointer type: _HEAP_ENTRY *.
    [FieldOffset(0x28)] internal uint LastValidEntry; // Offset: 0x28. Pointer type: _HEAP_ENTRY *.
    [FieldOffset(0x2C)] internal uint NumberOfUnCommittedPages;
    [FieldOffset(0x30)] internal uint NumberOfUnCommittedRanges;
    [FieldOffset(0x34)] internal ushort SegmentAllocatorBackTraceIndex;
    [FieldOffset(0x36)] internal ushort Reserved;
    [FieldOffset(0x38)] internal LIST_ENTRY32 UCRSegmentList; // Offset: 0x38. Original type: _LIST_ENTRY.
    [FieldOffset(0x40)] internal uint Flags;
    [FieldOffset(0x44)] internal uint ForceFlags;
    [FieldOffset(0x48)] internal uint CompatibilityFlags;
    [FieldOffset(0x4C)] internal uint EncodeFlagMask;
    [FieldOffset(0x50)] internal HEAP_ENTRY32 Encoding; // Offset: 0x50. Original type: _HEAP_ENTRY.
    [FieldOffset(0x58)] internal uint Interceptor;
    [FieldOffset(0x5C)] internal uint VirtualMemoryThreshold;
    [FieldOffset(0x60)] internal uint Signature;
    [FieldOffset(0x64)] internal uint SegmentReserve;
    [FieldOffset(0x68)] internal uint SegmentCommit;
    [FieldOffset(0x6C)] internal uint DeCommitFreeBlockThreshold;
    [FieldOffset(0x70)] internal uint DeCommitTotalFreeThreshold;
    [FieldOffset(0x74)] internal uint TotalFreeSize;
    [FieldOffset(0x78)] internal uint MaximumAllocationSize;
    [FieldOffset(0x7C)] internal ushort ProcessHeapsListIndex;
    [FieldOffset(0x7E)] internal ushort HeaderValidateLength;
    [FieldOffset(0x80)] internal uint HeaderValidateCopy; // Offset: 0x80. Pointer type: void *.
    [FieldOffset(0x84)] internal ushort NextAvailableTagIndex;
    [FieldOffset(0x86)] internal ushort MaximumTagIndex;
    [FieldOffset(0x88)] internal uint TagEntries; // Offset: 0x88. Pointer type: _HEAP_TAG_ENTRY *.
    [FieldOffset(0x8C)] internal LIST_ENTRY32 UCRList; // Offset: 0x8C. Original type: _LIST_ENTRY.
    [FieldOffset(0x94)] internal uint AlignRound;
    [FieldOffset(0x98)] internal uint AlignMask;
    [FieldOffset(0x9C)] internal LIST_ENTRY32 VirtualAllocdBlocks; // Offset: 0x9C. Original type: _LIST_ENTRY.
    [FieldOffset(0xA4)] internal LIST_ENTRY32 SegmentList; // Offset: 0xA4. Original type: _LIST_ENTRY.
    [FieldOffset(0xAC)] internal ushort AllocatorBackTraceIndex;
    [FieldOffset(0xB0)] internal uint NonDedicatedListLength;
    [FieldOffset(0xB4)] internal uint BlocksIndex; // Offset: 0xB4. Pointer type: void *.
    [FieldOffset(0xB8)] internal uint UCRIndex; // Offset: 0xB8. Pointer type: void *.
    [FieldOffset(0xBC)] internal uint PseudoTagEntries; // Offset: 0xBC. Pointer type: _HEAP_PSEUDO_TAG_ENTRY *.
    [FieldOffset(0xC0)] internal LIST_ENTRY32 FreeLists; // Offset: 0xC0. Original type: _LIST_ENTRY.
    [FieldOffset(0xC8)] internal uint LockVariable; // Offset: 0xC8. Pointer type: _HEAP_LOCK *.
    [FieldOffset(0xCC)] internal uint CommitRoutine; // Offset: 0xCC. Function pointer: long (*)(void *,void * *,unsigned long *).
    [FieldOffset(0xD0)] internal RTL_RUN_ONCE32 StackTraceInitVar; // Offset: 0xD0. Original type: _RTL_RUN_ONCE.
    [FieldOffset(0xD4)] internal RTLP_HEAP_COMMIT_LIMIT_DATA32 CommitLimitData; // Offset: 0xD4. Original type: _RTLP_HEAP_COMMIT_LIMIT_DATA.
    [FieldOffset(0xDC)] internal uint UserContext; // Offset: 0xDC. Pointer type: void *.
    [FieldOffset(0xE0)] internal uint Spare;
    [FieldOffset(0xE4)] internal uint FrontEndHeap; // Offset: 0xE4. Pointer type: void *.
    [FieldOffset(0xE8)] internal ushort FrontHeapLockCount;
    [FieldOffset(0xEA)] internal byte FrontEndHeapType;
    [FieldOffset(0xEB)] internal byte RequestedFrontEndHeapType;
    [FieldOffset(0xEC)] internal uint FrontEndHeapUsageData; // Offset: 0xEC. Pointer type: unsigned short *.
    [FieldOffset(0xF0)] internal ushort FrontEndHeapMaximumIndex;

    [FieldOffset(0xF2)] internal readonly byte FrontEndHeapStatusBitmap1;
    [FieldOffset(0x1F2)] internal readonly byte FrontEndHeapStatusBitmap257;

    [FieldOffset(0x1F3)] private readonly byte InternalFlags;
    internal readonly bool ReadOnly => (InternalFlags & 0x1) > 0; // :1.

    [FieldOffset(0x1F4)] internal HEAP_COUNTERS32 Counters; // Offset: 0x1F4. Original type: _HEAP_COUNTERS.
    [FieldOffset(0x250)] internal HEAP_TUNING_PARAMETERS32 TuningParameters; // Offset: 0x250. Original type: _HEAP_TUNING_PARAMETERS.
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct HEAP_SEGMENT32
{
    internal readonly HEAP_ENTRY32 Entry; // Offset: 0x0. Original type: _HEAP_ENTRY.
    internal readonly uint SegmentSignature;
    internal readonly uint SegmentFlags;
    internal readonly LIST_ENTRY32 SegmentListEntry; // Offset: 0x10. Original type: _LIST_ENTRY.
    internal readonly uint Heap; // Offset: 0x18. Pointer type: _HEAP *.
    internal readonly uint BaseAddress; // Offset: 0x1C. Pointer type: void *.
    internal readonly uint NumberOfPages;
    internal readonly uint FirstEntry; // Offset: 0x24. Pointer type: _HEAP_ENTRY *.
    internal readonly uint LastValidEntry; // Offset: 0x28. Pointer type: _HEAP_ENTRY *.
    internal readonly uint NumberOfUnCommittedPages;
    internal readonly uint NumberOfUnCommittedRanges;
    internal readonly ushort SegmentAllocatorBackTraceIndex;
    internal readonly ushort Reserved;
    internal readonly LIST_ENTRY32 UCRSegmentList; // Offset: 0x38. Original type: _LIST_ENTRY.
}

[StructLayout(LayoutKind.Explicit)]
internal readonly struct HEAP_ENTRY32
{
    [FieldOffset(0x0)] internal readonly HEAP_UNPACKED_ENTRY32 UnpackedEntry; // Offset: 0x0. Original type: _HEAP_UNPACKED_ENTRY.
    [FieldOffset(0x0)] internal readonly ushort Size;
    [FieldOffset(0x0)] internal readonly uint SubSegmentCode;
    [FieldOffset(0x0)] internal readonly HEAP_EXTENDED_ENTRY32 ExtendedEntry; // Offset: 0x0. Original type: _HEAP_EXTENDED_ENTRY.
    [FieldOffset(0x0)] internal readonly ushort FunctionIndex;
    [FieldOffset(0x0)] internal readonly uint InterceptorValue;
    [FieldOffset(0x0)] internal readonly uint Code1;
    [FieldOffset(0x0)] internal readonly ulong AgregateCode;
    [FieldOffset(0x2)] internal readonly byte Flags;
    [FieldOffset(0x2)] internal readonly ushort ContextValue;
    [FieldOffset(0x3)] internal readonly byte SmallTagIndex;
    [FieldOffset(0x4)] internal readonly ushort PreviousSize;
    [FieldOffset(0x4)] internal readonly ushort UnusedBytesLength;
    [FieldOffset(0x4)] internal readonly ushort Code2;
    [FieldOffset(0x4)] internal readonly uint Code234;
    [FieldOffset(0x6)] internal readonly byte SegmentOffset;
    [FieldOffset(0x6)] internal readonly byte LFHFlags;
    [FieldOffset(0x6)] internal readonly byte EntryOffset;
    [FieldOffset(0x6)] internal readonly byte Code3;
    [FieldOffset(0x7)] internal readonly byte UnusedBytes;
    [FieldOffset(0x7)] internal readonly byte ExtendedBlockSignature;
    [FieldOffset(0x7)] internal readonly byte Code4;
}

[StructLayout(LayoutKind.Explicit)]
internal readonly struct RTL_RUN_ONCE32
{
    [FieldOffset(0x0)] internal readonly uint Ptr; // Offset: 0x0. Pointer type: void *.

    [FieldOffset(0x0)] private readonly uint Value;
    internal byte State => (byte)(Value & 0x3); // :2.
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct RTLP_HEAP_COMMIT_LIMIT_DATA32
{
    internal readonly uint CommitLimitBytes;
    internal readonly uint CommitLimitFailureCode;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct HEAP_COUNTERS32
{
    internal readonly uint TotalMemoryReserved;
    internal readonly uint TotalMemoryCommitted;
    internal readonly uint TotalMemoryLargeUCR;
    internal readonly uint TotalSizeInVirtualBlocks;
    internal readonly uint TotalSegments;
    internal readonly uint TotalUCRs;
    internal readonly uint CommittOps;
    internal readonly uint DeCommitOps;
    internal readonly uint LockAcquires;
    internal readonly uint LockCollisions;
    internal readonly uint CommitRate;
    internal readonly uint DecommittRate;
    internal readonly uint CommitFailures;
    internal readonly uint InBlockCommitFailures;
    internal readonly uint PollIntervalCounter;
    internal readonly uint DecommitsSinceLastCheck;
    internal readonly uint HeapPollInterval;
    internal readonly uint AllocAndFreeOps;
    internal readonly uint AllocationIndicesActive;
    internal readonly uint InBlockDeccommits;
    internal readonly uint InBlockDeccomitSize;
    internal readonly uint HighWatermarkSize;
    internal readonly uint LastPolledSize;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct HEAP_TUNING_PARAMETERS32
{
    internal readonly uint CommittThresholdShift;
    internal readonly uint MaxPreCommittThreshold;
}

[StructLayout(LayoutKind.Explicit)]
internal readonly struct HEAP_UNPACKED_ENTRY32
{
    [FieldOffset(0x0)] internal readonly ushort Size;
    [FieldOffset(0x0)] internal readonly uint SubSegmentCode;
    [FieldOffset(0x2)] internal readonly byte Flags;
    [FieldOffset(0x3)] internal readonly byte SmallTagIndex;
    [FieldOffset(0x4)] internal readonly ushort PreviousSize;
    [FieldOffset(0x6)] internal readonly byte SegmentOffset;
    [FieldOffset(0x6)] internal readonly byte LFHFlags;
    [FieldOffset(0x7)] internal readonly byte UnusedBytes;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct HEAP_EXTENDED_ENTRY32
{
    internal readonly ushort FunctionIndex;
    internal readonly uint InterceptorValue;
    internal readonly ushort ContextValue;
    internal readonly ushort UnusedBytesLength;
    internal readonly byte EntryOffset;
    internal readonly byte ExtendedBlockSignature;
}

#endregion

#region SegmentHeap

[StructLayout(LayoutKind.Explicit)]
internal struct SEGMENT_HEAP32
{
    [FieldOffset(0x000)] internal RTL_HP_ENV_HANDLE32 EnvHandle;
    [FieldOffset(0x008)] internal uint Signature;
    [FieldOffset(0x00c)] internal uint GlobalFlags;
    [FieldOffset(0x010)] internal uint Interceptor;
    [FieldOffset(0x014)] internal ushort ProcessHeapListIndex;

    [FieldOffset(0x016)] internal ushort InternalFlags;
    internal readonly bool AllocatedFromMetadata => (InternalFlags & 0x1) > 0;
    internal readonly bool ReadOnly => (InternalFlags & 0x2) > 0;

    [FieldOffset(0x018)] internal RTLP_HEAP_COMMIT_LIMIT_DATA32 CommitLimitData;
    [FieldOffset(0x020)] internal uint ReservedMustBeZero;
    [FieldOffset(0x024)] internal uint UserContext;
    [FieldOffset(0x040)] internal uint LargeMetadataLock;
    [FieldOffset(0x044)] internal RTL_RB_TREE32 LargeAllocMetadata;
    [FieldOffset(0x04c)] internal uint LargeReservedPages;
    [FieldOffset(0x050)] internal uint LargeCommittedPages;
    [FieldOffset(0x058)] internal ulong Tag;
    [FieldOffset(0x060)] internal RTL_RUN_ONCE32 StackTraceInitVar;
    [FieldOffset(0x080)] internal HEAP_RUNTIME_MEMORY_STATS32 MemStats;
    [FieldOffset(0x0b0)] internal uint GlobalLockOwner;
    [FieldOffset(0x0b4)] internal uint ContextExtendLock;
    [FieldOffset(0x0b8)] internal uint AllocatedBase;
    [FieldOffset(0x0bc)] internal uint UncommittedBase;
    [FieldOffset(0x0c0)] internal uint ReservedLimit;
    [FieldOffset(0x0c4)] internal uint ReservedRegionEnd;
    [FieldOffset(0x0c8)] internal RTL_HP_HEAP_VA_CALLBACKS_ENCODED32 CallbacksEncoded;

    // For some annoying reason the runtime doesn't get the allignment of this array.
    [FieldOffset(0x100)] internal HEAP_SEG_CONTEXT32 SegContexts1;
    [FieldOffset(0x180)] internal HEAP_SEG_CONTEXT32 SegContexts2;

    [FieldOffset(0x200)] internal HEAP_VS_CONTEXT32 VsContext;
    [FieldOffset(0x2c0)] internal HEAP_LFH_CONTEXT32 LfhContext;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct RTL_HP_ENV_HANDLE32
{
    internal readonly uint h1;
    internal readonly uint h2;
}


[StructLayout(LayoutKind.Sequential)]
internal readonly struct RTL_RB_TREE32
{
    internal readonly uint Root;
    internal readonly uint Min;
    internal bool Encoded => (Min & 0x1) > 0;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct HEAP_RUNTIME_MEMORY_STATS32
{
    internal readonly uint TotalReservedPages;
    internal readonly uint TotalCommittedPages;
    internal readonly uint FreeCommittedPages;
    internal readonly uint LfhFreeCommittedPages;
    internal readonly uint VsFreeCommittedPages;

    internal readonly HEAP_OPPORTUNISTIC_LARGE_PAGE_STATS32 LargePageStats;
    internal readonly HEAP_OPPORTUNISTIC_LARGE_PAGE_STATS32 LargePageStats2;

    internal readonly RTL_HP_SEG_ALLOC_POLICY32 LargePageUtilizationPolicy;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct HEAP_OPPORTUNISTIC_LARGE_PAGE_STATS32
{
    internal readonly uint SmallPagesInUseWithinLarge;
    internal readonly uint OpportunisticLargePageCount;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct RTL_HP_SEG_ALLOC_POLICY32
{
    internal readonly uint MinLargePages;
    internal readonly uint MaxLargePages;
    internal readonly byte MinUtilization;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct RTL_HP_HEAP_VA_CALLBACKS_ENCODED32
{
    internal readonly uint CallbackContext;
    internal readonly uint AllocateVirtualMemoryEncoded;
    internal readonly uint FreeVirtualMemoryEncoded;
    internal readonly uint QueryVirtualMemoryEncoded;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct HEAP_SEG_CONTEXT32
{
    internal readonly uint SegmentMask;
    internal readonly byte UnitShift;
    internal readonly byte PagesPerUnitShift;
    internal readonly byte FirstDescriptorIndex;
    internal readonly byte CachedCommitSoftShift;
    internal readonly byte CachedCommitHighShift;

    internal readonly byte Flags;
    internal byte LargePagePolicy => (byte)(Flags & 0x7); // :3.
    internal bool FullDecommit => (Flags & 0x8) > 0; // :1.
    internal bool ReleaseEmptySegments => (Flags & 0x10) > 0; // :1.

    internal readonly uint MaxAllocationSize;
    internal readonly short OlpStatsOffset;
    internal readonly short MemStatsOffset;
    internal readonly uint LfhContext;
    internal readonly uint VsContext;
    internal readonly RTL_HP_ENV_HANDLE32 EnvHandle;
    internal readonly uint Heap;
    internal readonly uint SegmentLock;
    internal readonly LIST_ENTRY32 SegmentListHead;
    internal readonly uint SegmentCount;
    internal readonly RTL_RB_TREE32 FreePageRanges;
    internal readonly uint FreeSegmentListLock;

    internal readonly SINGLE_LIST_ENTRY32 FreeSegmentList1;
    internal readonly SINGLE_LIST_ENTRY32 FreeSegmentList2;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct SINGLE_LIST_ENTRY32
{
    internal readonly uint Next;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct HEAP_VS_CONTEXT32
{
    internal readonly uint Lock;
    internal readonly RTLP_HP_LOCK_TYPE LockType;
    internal readonly short MemStatsOffset;
    internal readonly RTL_RB_TREE32 FreeChunkTree;
    internal readonly LIST_ENTRY32 SubsegmentList;
    internal readonly uint TotalCommittedUnits;
    internal readonly uint FreeCommittedUnits;
    internal readonly HEAP_VS_DELAY_FREE_CONTEXT32 DelayFreeContext;
    internal readonly uint BackendCtx;
    internal readonly HEAP_SUBALLOCATOR_CALLBACKS32 Callbacks;
    internal readonly RTL_HP_VS_CONFIG Config;

    internal readonly uint Flags;
    internal bool EliminatePointers => (Flags & 0x1) > 0;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct HEAP_SUBALLOCATOR_CALLBACKS32
{
    internal readonly uint Allocate;
    internal readonly uint Free;
    internal readonly uint Commit;
    internal readonly uint Decommit;
    internal readonly uint ExtendContext;
    internal readonly uint TlsCleanup;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct HEAP_VS_DELAY_FREE_CONTEXT32
{
    internal readonly SLIST_HEADER32 ListHead;
}

[StructLayout(LayoutKind.Explicit)]
internal readonly struct SLIST_HEADER32
{
    [FieldOffset(0x0)] internal readonly ulong Alignment;
    [FieldOffset(0x0)] internal readonly SINGLE_LIST_ENTRY32 Next;
    [FieldOffset(0x4)] internal readonly ushort Depth;
    [FieldOffset(0x6)] internal readonly ushort CpuId;
}

[StructLayout(LayoutKind.Explicit)]
internal readonly struct HEAP_LFH_CONTEXT32
{
    [FieldOffset(0x0)] internal readonly uint BackendCtx;
    [FieldOffset(0x4)] internal readonly HEAP_SUBALLOCATOR_CALLBACKS32 Callbacks;
    [FieldOffset(0x1C)] internal readonly uint AffinityModArray;
    [FieldOffset(0x20)] internal readonly byte MaxAffinity;
    [FieldOffset(0x21)] internal readonly byte LockType;
    [FieldOffset(0x22)] internal readonly short MemStatsOffset;
    [FieldOffset(0x24)] internal readonly HEAP_LFH_CONFIG32 Config;
    [FieldOffset(0x28)] internal readonly uint TlsSlotIndex;
    [FieldOffset(0x2C)] internal readonly ulong EncodeKey;
    [FieldOffset(0x34)] internal readonly uint ExtensionLock;

    [FieldOffset(0x38)] internal readonly SINGLE_LIST_ENTRY32 MetadataList1;
    [FieldOffset(0x44)] internal readonly SINGLE_LIST_ENTRY32 MetadataList4;

    [FieldOffset(0x48)] internal readonly HEAP_LFH_HEAT_MAP32 HeatMap;

    [FieldOffset(0x148)] internal readonly uint Buckets1;
    [FieldOffset(0x544)] internal readonly uint Buckets128;

    [FieldOffset(0x548)] internal readonly HEAP_LFH_SLOT_MAP32 SlotMaps;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct HEAP_LFH_CONFIG32
{
    internal readonly RTL_HP_LFH_CONFIG32 Global;
}

[StructLayout(LayoutKind.Explicit)]
internal readonly struct RTL_HP_LFH_CONFIG32
{
    [FieldOffset(0x0)] internal readonly ushort MaxBlockSize;
    [FieldOffset(0x0)] internal readonly uint AllFields;
}

[StructLayout(LayoutKind.Explicit)]
internal readonly struct HEAP_LFH_HEAT_MAP32
{
    [FieldOffset(0x0)] internal readonly ushort Counts1;
    [FieldOffset(0xFA)] internal readonly ushort Counts126;

    [FieldOffset(0xFC)] internal readonly uint LastDecayPeriod;
}

[StructLayout(LayoutKind.Explicit)]
internal readonly struct HEAP_LFH_SLOT_MAP32
{
    [FieldOffset(0x0)] internal readonly ushort Map1;
    [FieldOffset(0xFE)] internal readonly ushort Map128;
}

#endregion

#region Old

[StructLayout(LayoutKind.Explicit)]
internal struct HEAP_OLD32
{
    [FieldOffset(0x0)] internal HEAP_ENTRY32 Entry;
    [FieldOffset(0x8)] internal uint SegmentSignature;
    [FieldOffset(0xC)] internal uint SegmentFlags;
    [FieldOffset(0x10)] internal LIST_ENTRY32 SegmentListEntry;
    [FieldOffset(0x18)] internal uint Heap;
    [FieldOffset(0x1C)] internal uint BaseAddress;
    [FieldOffset(0x20)] internal uint NumberOfPages;
    [FieldOffset(0x24)] internal uint FirstEntry; // HEAP_ENTRY*.
    [FieldOffset(0x28)] internal uint LastValidEntry; // HEAP_ENTRY*.
    [FieldOffset(0x2C)] internal uint NumberOfUnCommittedPages;
    [FieldOffset(0x30)] internal uint NumberOfUnCommittedRanges;
    [FieldOffset(0x34)] internal ushort SegmentAllocatorBackTraceIndex;
    [FieldOffset(0x36)] internal ushort Reserved;
    [FieldOffset(0x38)] internal LIST_ENTRY32 UCRSegmentList;
    [FieldOffset(0x40)] internal uint Flags;
    [FieldOffset(0x44)] internal uint ForceFlags;
    [FieldOffset(0x48)] internal uint CompatibilityFlags;
    [FieldOffset(0x4C)] internal uint EncodeFlagMask;
    [FieldOffset(0x50)] internal HEAP_ENTRY32 Encoding;
    [FieldOffset(0x58)] internal uint PointerKey;
    [FieldOffset(0x5C)] internal uint Interceptor;
    [FieldOffset(0x60)] internal uint VirtualMemoryThreshold;
    [FieldOffset(0x64)] internal uint Signature;
    [FieldOffset(0x68)] internal uint SegmentReserve;
    [FieldOffset(0x6C)] internal uint SegmentCommit;
    [FieldOffset(0x70)] internal uint DeCommitFreeBlockThreshold;
    [FieldOffset(0x74)] internal uint DeCommitTotalFreeThreshold;
    [FieldOffset(0x78)] internal uint TotalFreeSize;
    [FieldOffset(0x7C)] internal uint MaximumAllocationSize;
    [FieldOffset(0x7E)] internal ushort ProcessListHeapsIndex;
    [FieldOffset(0x80)] internal ushort HeaderValidateLength;
    [FieldOffset(0x84)] internal uint HeaderValidateCopy;
    [FieldOffset(0x86)] internal ushort NextAvailableTagIndex;
    [FieldOffset(0x88)] internal ushort MaximumTagIndex;
    [FieldOffset(0x8C)] internal uint TagEntries; // HEAP_TAG_ENTRY*.
    [FieldOffset(0x94)] internal LIST_ENTRY32 UCRList;
    [FieldOffset(0x98)] internal uint AlignRound;
    [FieldOffset(0x9C)] internal uint AlignMask;
    [FieldOffset(0xA4)] internal LIST_ENTRY32 VirtualAllocdBlocks;
    [FieldOffset(0xAC)] internal LIST_ENTRY32 SegmentList;
    [FieldOffset(0xB0)] internal ushort AllocatorBackTraceIndex;
    [FieldOffset(0xB4)] internal uint NonDedicatedListLength;
    [FieldOffset(0xB8)] internal uint BlocksIndex;
    [FieldOffset(0xBC)] internal uint UCRIndex;
    [FieldOffset(0xC0)] internal uint PseudoTagEntries; // HEAP_PSEUDO_TAG_ENTRY*.
    [FieldOffset(0xC8)] internal LIST_ENTRY32 FreeLists;
    [FieldOffset(0xCC)] internal uint LockVariable; // HEAP_LOCK*.
    [FieldOffset(0xD0)] internal uint CommitRoutine;
    [FieldOffset(0xD4)] internal uint FrontEndHeap;
    [FieldOffset(0xD8)] internal ushort FrontHeapLockCount;
    [FieldOffset(0xDA)] internal byte FrontEndHeapType;
    [FieldOffset(0xDB)] internal HEAP_COUNTERS32 Counters;
    [FieldOffset(0x137)] internal HEAP_TUNING_PARAMETERS32 TuningParameters;
}

#endregion

#endregion

#endregion

/// <summary>
/// Partial class containing unmanaged macros.
/// </summary>
internal static partial class Constants
{
    internal const uint HEAP_SIGNATURE          = 0xEEFFEEFF;
    internal const uint SEGMENT_HEAP_SIGNATURE  = 0xDDEEDDEE;
    internal const uint HEAP_SEGMENT_SIGNATURE  = 0xFFEEFFEE;
}

/// <summary>
/// Represents a process heap.
/// </summary>
internal sealed class ProcessHeap
{
    internal uint Id { get; set; }
    internal nint Base { get; }
    internal nint End { get; }
    internal long Size { get; }
    internal MemoryRegionType RegionType { get; set; }

    internal ProcessHeap(uint id, nint baseAddress, long size)
        => (Id, Base, End, Size) = (id, baseAddress, checked((nint)(baseAddress + size)), size);

    internal ProcessHeap(ProcessHeap baseHeap, nint baseAddress, long size)
        => (Id, Base, End, Size, RegionType)
            = (baseHeap.Id, baseAddress, checked((nint)(baseAddress + size)), size, baseHeap.RegionType);
}

/// <summary>
/// Partial class with functions to manage the heap.
/// </summary>
internal static partial class Heap
{
    private static nint m_processHeap;

    internal static nint ProcessHeap {
        get {
            if (m_processHeap == nint.Zero)
                m_processHeap = GetProcessHeap();

            if (m_processHeap == nint.Zero)
                throw new NativeException(Marshal.GetLastWin32Error());

            return m_processHeap;
        }
    }

    /// <remarks>These functions does not set the last error.</remarks>
    /// <seealso href="https://learn.microsoft.com/windows/win32/api/heapapi/nf-heapapi-heapalloc">HeapAlloc function (heapapi.h)</seealso>
    [LibraryImport("kernel32.dll")]
    private static partial nint HeapAlloc(
        nint hHeap,
        HeapAllocFlags dwFlags,
        ulong dwBytes
    );

    /// <seealso href="https://learn.microsoft.com/windows/win32/api/heapapi/nf-heapapi-heapfree">HeapFree function (heapapi.h)</seealso>
    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool HeapFree(
        nint hHeap,
        HeapAllocFlags dwFlags,
        nint lpMem
    );

    /// <remarks>This one does.</remarks>
    /// <seealso href="https://learn.microsoft.com/windows/win32/api/heapapi/nf-heapapi-getprocessheap">GetProcessHeap function (heapapi.h)</seealso>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial nint GetProcessHeap();


    internal static nint Alloc(int size)
        => Alloc((ulong)size);

    /// <summary>
    /// Allocates a block of memory in the current process heap.
    /// </summary>
    /// <param name="size">The block size.</param>
    /// <returns>The address of the allocated buffer.</returns>
    /// <exception cref="OutOfMemoryException">HeapAlloc returned NULL.</exception>
    internal static nint Alloc(ulong size)
    {
        nint mem = HeapAlloc(ProcessHeap, HeapAllocFlags.ZERO_MEMORY, size);
        if (mem == nint.Zero)
            throw new OutOfMemoryException();

        return mem;
    }

    /// <summary>
    /// Frees a block of memory previously allocated with <see cref="HeapAlloc(nint, HeapAllocFlags, ulong)"/>.
    /// </summary>
    /// <param name="mem">The memory address.</param>
    internal static void Free(nint mem)
        => HeapFree(ProcessHeap, 0, mem);

    /// <summary>
    /// Converts a Unicode string to a heap allocated buffer.
    /// </summary>
    /// <param name="str">The input string.</param>
    /// <returns>The address of the newly allocated buffer.</returns>
    /// <seealso cref="Marshal.StringToHGlobalUni(string?)"/>
    internal static unsafe nint StringToHeapAllocUni(string str)
    {
        if (str is null)
            return nint.Zero;

        long buffSize = (str.Length + 1) * 2;
        nint mem = HeapAlloc(ProcessHeap, HeapAllocFlags.NONE, (ulong)buffSize);
        fixed (char* strPtr = str) {
            Buffer.MemoryCopy(strPtr, mem.ToPointer(), buffSize, buffSize - 2);
        }

        return mem;
    }

    /// <summary>
    /// Converts a Ansi string to a heap allocated buffer.
    /// </summary>
    /// <param name="str">The input string.</param>
    /// <returns>The address of the newly allocated buffer.</returns>
    /// <seealso cref="Marshal.StringToHGlobalAnsi(string?)"/>
    internal static unsafe nint StringToHeapAllocAnsi(string str)
    {
        if (str is null)
            return nint.Zero;

        long buffSize = str.Length + 1;
        nint mem = HeapAlloc(ProcessHeap, HeapAllocFlags.ZERO_MEMORY, (ulong)buffSize);
        fixed (byte* ansiPtr = Encoding.ASCII.GetBytes(str))
            Buffer.MemoryCopy(ansiPtr, mem.ToPointer(), buffSize, buffSize - 1);

        return mem;
    }

    /// <summary>
    /// Collects process heap sizes.
    /// </summary>
    /// <param name="hProcess">A handle to the process.</param>
    /// <param name="managedHeap">The parent heap.</param>
    /// <returns>A list of heap segment sizes.</returns>
    internal static unsafe List<long> GetHeapSegmentSize(SafeProcessHandle hProcess, nint parentBase)
    {
        List<long> output = [];
        int initialSize = Marshal.SizeOf<ANY_HEAP>();
        using ScopedBuffer buffer = new(initialSize);
        if (NativeProcess.TryReadProcessMemory(hProcess, parentBase, buffer, initialSize)) {
            ANY_HEAP* rootHeap = (ANY_HEAP*)buffer;
            if (WinVer.CurrentVersion >= WinVer.WINDOWS_8) {
                if (rootHeap->Heap.Signature == Constants.HEAP_SIGNATURE) {
                    ProcessHeapSegments(hProcess, ref rootHeap->Heap, null, output, null, true);
                }
                else if (rootHeap->Heap32.Signature == Constants.HEAP_SIGNATURE) {
                    ProcessHeapSegments(hProcess, ref rootHeap->Heap32, null, output, null, true);
                }
            }
            else {
                if (rootHeap->HeapOld.Signature == Constants.HEAP_SIGNATURE) {
                    ProcessHeapSegments(hProcess, ref rootHeap->HeapOld, null, output, null, true);
                }
                else if (rootHeap->HeapOld32.Signature == Constants.HEAP_SIGNATURE) {
                    ProcessHeapSegments(hProcess, ref rootHeap->HeapOld32, null, output, null, true);
                }
            }
        }

        return output;
    }

    /// <summary>
    /// Collects process heap information.
    /// </summary>
    /// <param name="hProcess">A handle to the process.</param>
    /// <param name="managedHeap">The parent heap.</param>
    /// <returns>A list of <see cref="ProcessHeap"/> segments.</returns>
    internal static unsafe List<ProcessHeap> GetHeapSegmentInformation(SafeProcessHandle hProcess, ProcessHeap managedHeap)
    {
        List<ProcessHeap> output = [];
        int initialSize = Marshal.SizeOf<ANY_HEAP>();
        using ScopedBuffer buffer = new(initialSize);
        if (NativeProcess.TryReadProcessMemory(hProcess, managedHeap.Base, buffer, initialSize)) {
            ANY_HEAP rootHeap = Marshal.PtrToStructure<ANY_HEAP>(buffer);
            if (WinVer.CurrentVersion >= WinVer.WINDOWS_8) {
                if (rootHeap.Heap.Signature == Constants.HEAP_SIGNATURE) {
                    managedHeap.RegionType = rootHeap.Heap.FrontEndHeap != nint.Zero ? MemoryRegionType.NtLfhHeap : MemoryRegionType.NtHeap;
                    ProcessHeapSegments(hProcess, ref rootHeap.Heap, output, null, managedHeap);
                }
                else if (rootHeap.Heap32.Signature == Constants.HEAP_SIGNATURE) {
                    managedHeap.RegionType = rootHeap.Heap.FrontEndHeap != nint.Zero ? MemoryRegionType.NtLfhHeap : MemoryRegionType.NtHeap;
                    ProcessHeapSegments(hProcess, ref rootHeap.Heap32, output, null, managedHeap);
                }
                else if (WinVer.CurrentVersion >= WinVer.WINDOWS_8_1) {
                    if (rootHeap.SegmentHeap.Signature == Constants.SEGMENT_HEAP_SIGNATURE) {
                        // For now we just set the heap type and return without messing with the segments.
                        managedHeap.RegionType = MemoryRegionType.SegmentHeap;
                    }
                    else if (rootHeap.SegmentHeap32.Signature == Constants.SEGMENT_HEAP_SIGNATURE) {
                        // For now we just set the heap type and return without messing with the segments.
                        managedHeap.RegionType = MemoryRegionType.SegmentHeap;
                    }
                }
            }
            else {
                if (rootHeap.HeapOld.Signature == Constants.HEAP_SIGNATURE) {
                    managedHeap.RegionType = rootHeap.Heap.FrontEndHeap != nint.Zero ? MemoryRegionType.NtLfhHeap : MemoryRegionType.NtHeap;
                    ProcessHeapSegments(hProcess, ref rootHeap.HeapOld, output, null, managedHeap);
                }
                else if (rootHeap.HeapOld32.Signature == Constants.HEAP_SIGNATURE) {
                    managedHeap.RegionType = rootHeap.Heap.FrontEndHeap != nint.Zero ? MemoryRegionType.NtLfhHeap : MemoryRegionType.NtHeap;
                    ProcessHeapSegments(hProcess, ref rootHeap.HeapOld32, output, null, managedHeap);
                }
            }
        }

        return output;
    }

    /// <summary>
    /// Processes the heap segments for newer Windows versions.
    /// </summary>
    /// <param name="hProcess">A handle to the process.</param>
    /// <param name="heap">The heap header.</param>
    /// <param name="segmentList">The segment list.</param>
    /// <param name="parentHeap">The parent heap</param>
    private static unsafe void ProcessHeapSegments(SafeProcessHandle hProcess, ref HEAP heap, List<ProcessHeap>? segmentList, List<long>? sizeList, ProcessHeap? parentHeap, bool sizeOnly = false)
    {
        int heapSize = Marshal.SizeOf<HEAP>();
        int listEntrySize = Marshal.SizeOf<LIST_ENTRY>();
        nint segListOffset = heap.BaseAddress + Marshal.OffsetOf<HEAP>("SegmentList");
        using ScopedBuffer listBuffer = new(listEntrySize);
        nint entry = heap.SegmentList.Flink;
        do {
            nint currentSegmentOffset = CONTAINING_RECORD<HEAP>(entry, "SegmentListEntry");
            using ScopedBuffer heapBuffer = new(heapSize);
            if (currentSegmentOffset == heap.BaseAddress)
                goto ENDOFLOOP;

            if (NativeProcess.TryReadProcessMemory(hProcess, currentSegmentOffset, heapBuffer, heapSize)) {
                HEAP* currentHeap = (HEAP*)heapBuffer;
                if (sizeOnly && sizeList is not null) {
                    sizeList.Add((currentHeap->NumberOfPages - currentHeap->NumberOfUnCommittedPages) * Common.SystemInfo.dwPageSize);
                    goto ENDOFLOOP;
                }

                segmentList!.Add(new(parentHeap!, currentSegmentOffset, (currentHeap->NumberOfPages - currentHeap->NumberOfUnCommittedPages) * Common.SystemInfo.dwPageSize) {
                    RegionType = currentHeap->FrontEndHeap != nint.Zero ? MemoryRegionType.NtLfhSegment : MemoryRegionType.NtHeapSegment
                });
            }

        ENDOFLOOP:
            NativeProcess.ReadProcessMemory(hProcess, entry, listBuffer, listEntrySize);
            entry = ((LIST_ENTRY*)listBuffer)->Flink;

        } while (entry != segListOffset);
    }

    /// <summary>
    /// Processes the 32-bit heap segments for newer Windows versions.
    /// </summary>
    /// <param name="hProcess">A handle to the process.</param>
    /// <param name="heap">The heap header.</param>
    /// <param name="segmentList">The segment list.</param>
    /// <param name="parentHeap">The parent heap</param>
    private static unsafe void ProcessHeapSegments(SafeProcessHandle hProcess, ref HEAP32 heap, List<ProcessHeap>? segmentList, List<long>? sizeList, ProcessHeap? parentHeap, bool sizeOnly = false)
    {
        int heapSize = Marshal.SizeOf<HEAP32>();
        int listEntrySize = Marshal.SizeOf<LIST_ENTRY32>();
        nint segListOffset = (nint)heap.BaseAddress + Marshal.OffsetOf<HEAP32>("SegmentList");
        using ScopedBuffer listBuffer = new(listEntrySize);
        nint entry = (nint)heap.SegmentList.Flink;
        do {
            nint currentSegmentOffset = CONTAINING_RECORD<HEAP32>(entry, "SegmentListEntry");
            using ScopedBuffer heapBuffer = new(heapSize);
            if (currentSegmentOffset == (nint)heap.BaseAddress)
                goto ENDOFLOOP;

            if (NativeProcess.TryReadProcessMemory(hProcess, currentSegmentOffset, heapBuffer, heapSize)) {
                HEAP32* currentHeap = (HEAP32*)heapBuffer;
                if (sizeOnly && sizeList is not null) {
                    sizeList.Add((currentHeap->NumberOfPages - currentHeap->NumberOfUnCommittedPages) * Common.SystemInfo.dwPageSize);
                    goto ENDOFLOOP;
                }

                segmentList!.Add(new(parentHeap!, currentSegmentOffset, (currentHeap->NumberOfPages - currentHeap->NumberOfUnCommittedPages) * Common.SystemInfo.dwPageSize) {
                    RegionType = currentHeap->FrontEndHeap != 0 ? MemoryRegionType.NtLfhSegment : MemoryRegionType.NtHeapSegment
                });
            }

        ENDOFLOOP:
            NativeProcess.ReadProcessMemory(hProcess, entry, listBuffer, listEntrySize);
            entry = (nint)((LIST_ENTRY32*)listBuffer)->Flink;

        } while (entry != segListOffset);
    }

    /// <summary>
    /// Processes the heap segments for pre Windows 8 versions.
    /// </summary>
    /// <param name="hProcess">A handle to the process.</param>
    /// <param name="heap">The heap header.</param>
    /// <param name="segmentList">The segment list.</param>
    /// <param name="parentHeap">The parent heap</param>
    private static unsafe void ProcessHeapSegments(SafeProcessHandle hProcess, ref HEAP_OLD heap, List<ProcessHeap>? segmentList, List<long>? sizeList, ProcessHeap? parentHeap, bool sizeOnly = false)
    {
        int heapSize = Marshal.SizeOf<HEAP_OLD>();
        int listEntrySize = Marshal.SizeOf<LIST_ENTRY>();
        nint segListOffset = heap.BaseAddress + Marshal.OffsetOf<HEAP_OLD>("SegmentList");
        using ScopedBuffer listBuffer = new(listEntrySize);
        nint entry = heap.SegmentList.Flink;
        do {
            nint currentSegmentOffset = CONTAINING_RECORD<HEAP_OLD>(entry, "SegmentListEntry");
            using ScopedBuffer heapBuffer = new(heapSize);
            if (currentSegmentOffset == heap.BaseAddress)
                goto ENDOFLOOP;

            if (NativeProcess.TryReadProcessMemory(hProcess, currentSegmentOffset, heapBuffer, heapSize)) {
                HEAP_OLD* currentHeap = (HEAP_OLD*)heapBuffer;
                if (sizeOnly && sizeList is not null) {
                    sizeList.Add((currentHeap->NumberOfPages - currentHeap->NumberOfUnCommittedPages) * Common.SystemInfo.dwPageSize);
                    goto ENDOFLOOP;
                }

                segmentList!.Add(new(parentHeap!, currentSegmentOffset, (currentHeap->NumberOfPages - currentHeap->NumberOfUnCommittedPages) * Common.SystemInfo.dwPageSize) {
                    RegionType = currentHeap->FrontEndHeap != nint.Zero ? MemoryRegionType.NtLfhSegment : MemoryRegionType.NtHeapSegment
                });
            }

        ENDOFLOOP:
            NativeProcess.ReadProcessMemory(hProcess, entry, listBuffer, listEntrySize);
            entry = ((LIST_ENTRY*)listBuffer)->Flink;

        } while (entry != segListOffset);
    }

    /// <summary>
    /// Processes the 32-bit heap segments for pre Windows 8 versions.
    /// </summary>
    /// <param name="hProcess">A handle to the process.</param>
    /// <param name="heap">The heap header.</param>
    /// <param name="segmentList">The segment list.</param>
    /// <param name="parentHeap">The parent heap</param>
    private static unsafe void ProcessHeapSegments(SafeProcessHandle hProcess, ref HEAP_OLD32 heap, List<ProcessHeap>? segmentList, List<long>? sizeList, ProcessHeap? parentHeap, bool sizeOnly = false)
    {
        int heapSize = Marshal.SizeOf<HEAP_OLD32>();
        int listEntrySize = Marshal.SizeOf<LIST_ENTRY32>();
        nint segListOffset = (nint)heap.BaseAddress + Marshal.OffsetOf<HEAP_OLD32>("SegmentList");
        using ScopedBuffer listBuffer = new(listEntrySize);
        nint entry = (nint)heap.SegmentList.Flink;
        do {
            nint currentSegmentOffset = CONTAINING_RECORD<HEAP_OLD32>(entry, "SegmentListEntry");
            using ScopedBuffer heapBuffer = new(heapSize);
            if (currentSegmentOffset == (nint)heap.BaseAddress)
                goto ENDOFLOOP;

            if (NativeProcess.TryReadProcessMemory(hProcess, currentSegmentOffset, heapBuffer, heapSize)) {
                HEAP_OLD32* currentHeap = (HEAP_OLD32*)heapBuffer;
                if (sizeOnly && sizeList is not null) {
                    sizeList.Add((currentHeap->NumberOfPages - currentHeap->NumberOfUnCommittedPages) * Common.SystemInfo.dwPageSize);
                    goto ENDOFLOOP;
                }

                segmentList!.Add(new(parentHeap!, currentSegmentOffset, (currentHeap->NumberOfPages - currentHeap->NumberOfUnCommittedPages) * Common.SystemInfo.dwPageSize) {
                    RegionType = currentHeap->FrontEndHeap != 0 ? MemoryRegionType.NtLfhSegment : MemoryRegionType.NtHeapSegment
                });
            }

        ENDOFLOOP:
            NativeProcess.ReadProcessMemory(hProcess, entry, listBuffer, listEntrySize);
            entry = (nint)((LIST_ENTRY32*)listBuffer)->Flink;

        } while (entry != segListOffset);
    }

    // The SEGMENT_HEAP is both complex and designed to be safer, I.e., the block sizes and offsets are encoded by keys that are not exported by the kernel.
    // This concept is also very new, and the undocumented structures changes constantly.
    // For our purposes of classifying memory this task becomes a burden.
    // For more information:
    //
    // https://i.blackhat.com/USA21/Wednesday-Handouts/us-21-Windows-Heap-Backed-Pool-The-Good-The-Bad-And-The-Encoded.pdf
    //
    //private static unsafe void ProcessHeapSegments(SafeProcessHandle hProcess, nint baseAddress, ref SEGMENT_HEAP heap, List<ProcessHeap> segmentList, ProcessHeap parentHeap)
    //{
    //    int pageSegSize = Marshal.SizeOf<HEAP_PAGE_SEGMENT>();
    //    nint listOffset = Marshal.OffsetOf<HEAP_SEG_CONTEXT>("SegmentListHead");
    //    HEAP_SEG_CONTEXT[] segContexts = [
    //        heap.SegContexts1,
    //        heap.SegContexts2,
    //    ];

    //    for (int i = 1; i <= segContexts.Length; i++) {
    //        int listEntrySize = Marshal.SizeOf(typeof(LIST_ENTRY));
    //        nint segListOffset = (nint)((long)baseAddress + (long)Marshal.OffsetOf<SEGMENT_HEAP>($"SegContexts{i}") + (long)listOffset);
    //        using ScopedBuffer listBuffer = new(listEntrySize);
    //        NativeProcess.ReadProcessMemory(hProcess, segListOffset, listBuffer, listEntrySize);

    //        nint entry = ((LIST_ENTRY*)listBuffer)->Flink;
    //        do {
    //            nint currentPageSegOffset = CONTAINING_RECORD<HEAP_PAGE_SEGMENT>(entry, "ListEntry");
    //            using ScopedBuffer currentPageSegBuffer = new(pageSegSize);
    //            NativeProcess.ReadProcessMemory(hProcess, currentPageSegOffset, currentPageSegBuffer, pageSegSize);

    //            HEAP_PAGE_SEGMENT* currentSegment = (HEAP_PAGE_SEGMENT*)currentPageSegBuffer;
    //            /*
    //                From here there are many structures that can be queried, but sizes and offsets are encoded.
    //                HEAP_PAGE_SEGMENT
    //                    - Has an array of 256 descriptors.
    //                      Every descriptor describes a unit, which is part of a range.
    //                      It describes the type of subsegment and unit offset.
    //                      This structure changed a lot on the past versions.

    //                Subsegments are either LFH or VS.
    //                HEAP_LFH_SUBSEGMENT
    //                    - Used to describe all blocks of the Low Fragmentation Heap.

    //                HEAP_VS_SUBSEGMENT
    //                    - Used to describe all blocks that don't fit in the LFH buckets.
    //                      Every block has a header to describe it (HEAP_VS_CHUNK_HEADER).
    //             */

    //        } while (entry != segListOffset);
    //    }
    //}

    //private static unsafe void ProcessHeapSegments(SafeProcessHandle hProcess, nint baseAddress, ref SEGMENT_HEAP32 heap, List<ProcessHeap> segmentList, ProcessHeap parentHeap)
    //{

    //}

    /// <summary>
    /// Returns the base address of an instance of a structure given the type of the structure and the address of a field within the containing structure.
    /// </summary>
    /// <typeparam name="T">The structure type.</typeparam>
    /// <param name="address">The address.</param>
    /// <param name="field">The structure field name.</param>
    /// <returns>A pointer to the record.</returns>
    /// <seealso cref="LIST_ENTRY"/>
    /// <seealso href="https://learn.microsoft.com/windows/win32/api/ntdef/nf-ntdef-containing_record">CONTAINING_RECORD macro (ntdef.h)</seealso>
    private static unsafe nint CONTAINING_RECORD<T>(nint address, string field) where T : struct
            => (nint)((ulong)address - (ulong)Marshal.OffsetOf<T>(field));
}