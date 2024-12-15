// This file is part of the ManagedStrings project and repository.
// Project files are licensed under the MIT license.
// https://github.com/FranciscoNabas/ManagedStrings

using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Win32.SafeHandles;
using ManagedStrings.Interop.Windows;

namespace ManagedStrings.Engine;

/// <summary>
/// Contains information about the process stream at a given offset.
/// It's used to convert the stream offset to the RVA.
/// </summary>
internal sealed class ProcessStreamOffsetInfo
{
    private readonly ProcessMemoryRegion? m_region;

    internal long Offset { get; }
    internal long Base => m_region is null ? 0 : m_region.BaseLong;
    internal long Size => m_region is null ? 0 : m_region.Size;
    internal uint ProcessId { get; set; }
    internal MemoryRegionType RegionType => m_region is null ? MemoryRegionType.Unknown : m_region.Type;
    internal string Image { get; set; }
    internal Lazy<string> Details { get; }
    internal long RelativeVa { get; set; }

    /// <summary>
    /// Constructs from a memory region and an offset.
    /// </summary>
    /// <param name="region">The process memory region information.</param>
    /// <param name="offset">The offset.</param>
    internal ProcessStreamOffsetInfo(ProcessMemoryRegion? region, long offset)
    {
        this.Offset = offset;
        this.Details = new(InitDetails);
        this.ProcessId = region is null ? 0 : region.ProcessId;
        this.Image = region is null ? string.Empty : region.ImagePath;
        m_region = region;        
    }

    /// <summary>
    /// Lazy initialized for the <see cref="Details"/>.
    /// </summary>
    /// <returns>The details string.</returns>
    private string InitDetails()
        => m_region is null ? string.Empty : m_region.Type switch {
            MemoryRegionType.Teb or MemoryRegionType.Stack => m_region.ThreadId.ToString(),
            MemoryRegionType.NtHeap or MemoryRegionType.NtLfhHeap or
            MemoryRegionType.NtHeapSegment or MemoryRegionType.SegmentHeap or MemoryRegionType.NtLfhSegment or
            MemoryRegionType.SegmentHeapSegment => m_region.HeapInformation is not null ? m_region.HeapInformation.Id.ToString() : string.Empty,
            MemoryRegionType.MappedFile or MemoryRegionType.Image => m_region.MappedFilePath is not null ? m_region.MappedFilePath : string.Empty,
            _ => string.Empty,
        };
}

/// <summary>
/// A stream wrapping a process virtual address space.
/// </summary>
/// <remarks>
/// This stream, in oposition to the console stream, only reads.
/// Although it's possible to write to a process virtual memory space
/// we don't need to.
/// Tha main chanllenge here is to have a contiguous buffer while process
/// virtual memory is not. We have to make sure our buffer read position aligns
/// with the process virtual memory offsets.
/// </remarks>
/// <runtimefile>src/libraries/System.Private.CoreLib/src/System/IO/Strategies/BufferedFileStreamStrategy.cs</runtimefile>
/// <runtimefile>src/libraries/System.Private.CoreLib/src/System/IO/MemoryStream.cs</runtimefile>
/// <runtimefile>referencesource/mscorlib/system/io/filestream.cs</runtimefile>
internal sealed class ProcessStream : Stream
{
    private readonly long m_length;
    private readonly int m_bufferSize;
    private readonly uint m_processId;
    private readonly string m_imagePath;
    
    // The process handle. Needs PROCESS_VM_READ and PROCESS_QUERY_INFORMATION (although PROCESS_QUERY_LIMITED_INFORMATION might work).
    private readonly SafeProcessHandle m_process;
    private readonly MemoryRegionOffsetComparer m_regionComparer;
    
    
    // The list of categorized memory regions and offsets.
    private readonly List<ProcessStreamOffsetInfo> m_regionMap;

    private byte[]? m_buffer;
    private bool m_isOpen;
    private int m_readLength;
    private int m_readPosition;
    private int m_previousRegionIndex;
    private long m_unbufferedPosition;
    private long m_currentRegionPosition;

    // Caching the current region so we don't need to perform binary searches all the time.
    private ProcessStreamOffsetInfo m_regionInfoCache;
    private ProcessStreamOffsetInfo m_currentRegionInfo;

    public override bool CanRead => m_isOpen;
    public override bool CanSeek => m_isOpen;
    public override bool CanWrite => false;
    public override long Length => m_length;
    public override long Position {
        get => m_unbufferedPosition + m_readPosition - m_readLength;
        set {
            m_readLength = 0;
            m_readPosition = 0;
            SeekBytes(value - m_unbufferedPosition);
        }
    }

    internal uint ProcessId => m_processId;
    internal string ImagePath => m_imagePath;

    /// <summary>
    /// Constructs a process stream with default buffer size.
    /// </summary>
    /// <param name="processId">The process ID.</param>
    /// <param name="regionsToMap">The regions to map.</param>
    internal ProcessStream(uint processId, ReadMemoryFlags regionsToMap)
        : this(processId, 4096, regionsToMap) { }

    /// <summary>
    /// Constructs a process stream.
    /// </summary>
    /// <param name="processId">The process ID.</param>
    /// <param name="bufferSize">The buffer size.</param>
    /// <param name="regionsToMap">The regions to map.</param>
    /// <exception cref="ArgumentException">The process is not a running process.</exception>
    internal ProcessStream(uint processId, int bufferSize, ReadMemoryFlags regionsToMap)
    {
        if (!NativeProcess.IsProcess(processId))
            throw new ArgumentException($"Process id '{processId}' is not a running process.");

        // Opening a handle to the process and mapping the virtual memory regions.
        m_process = NativeProcess.OpenProcess(processId, ProcessAccess.QUERY_INFORMATION | ProcessAccess.VM_READ, false);
        List<ProcessMemoryRegion> regionList = NativeProcess.GetProcessMemoryInformation(processId, m_process, regionsToMap);

        m_length = 0;
        m_regionMap = [];
        m_imagePath = regionList[0].ImagePath;

        // Caching the region information.
        for (int i = 0; i < regionList.Count; i++) {
            m_regionMap.Add(new(regionList[i], m_length));
            m_length += regionList[i].Size;
        }

        m_processId = processId;
        m_bufferSize = bufferSize;
        m_regionComparer = new();
        m_currentRegionInfo = m_regionInfoCache = m_regionMap[0];
        
        m_isOpen = true;
    }

    public override void Flush() { }

    /// <summary>
    /// Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
    /// </summary>
    /// <param name="buffer">The buffer to read into.</param>
    /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
    /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
    /// <returns>The total number of bytes read into the buffer.</returns>
    public override int Read(byte[] buffer, int offset, int count)
        => ReadSpan(new Span<byte>(buffer, offset, count));

    /// <summary>
    /// Reads a byte from the stream and advances the position within the stream by one byte.
    /// </summary>
    /// <returns>The unsigned byte cast to an Int32, or -1 if at the end of the stream.</returns>
    public override int ReadByte()
         => m_readPosition != m_readLength ? m_buffer![m_readPosition++] : ReadByteSlow();

    /// <summary>
    /// Sets the position within the current stream.
    /// </summary>
    /// <param name="offset">A byte offset relative to the origin parameter.</param>
    /// <param name="origin">A value of <see cref="SeekOrigin"/> indicating the reference point used to obtain the new position.</param>
    /// <returns>The new position within the current stream.</returns>
    public override long Seek(long offset, SeekOrigin origin)
    {
        EnsureOpen();
        if (m_readLength - m_readPosition > 0 && origin == SeekOrigin.Current)
            offset -= m_readLength - m_readPosition;

        long oldPosition = Position;
        long newPosition = SeekCore(offset, origin);
        long readPosition = newPosition - (oldPosition - m_readPosition);
        if (0 <= readPosition && readPosition < m_readLength) {
            m_readPosition = (int)readPosition;
            SeekCore(m_readLength - m_readPosition, SeekOrigin.Current);
        }
        else {
            m_readPosition = m_readLength = 0;
        }

        return newPosition;
    }

    public override void SetLength(long value)
        => throw new NotSupportedException("The stream doesn't support setting the length.");

    public override void Write(byte[] buffer, int offset, int count)
        => throw new InvalidOperationException("The stream doesn't support writing.");

    /// <summary>
    /// Disposes of the process handle.
    /// </summary>
    /// <param name="disposing">True to dispose of unmanaged resources.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && !m_isOpen) {
            m_process.Dispose();
            m_buffer = null;
            m_isOpen = false;
        }
    }

    /// <summary>
    /// Gets the process virtual memory offset information for a given buffer offset.
    /// </summary>
    /// <param name="offset">The stream buffer offset.</param>
    /// <param name="info">The output process virtual memory offset information.</param>
    /// <exception cref="ArgumentOutOfRangeException">Offset is out of the stream bounds.</exception>
    internal void GetRelativeOffsetInfo(long offset, out ProcessStreamOffsetInfo info)
    {
        if (offset < 0 || offset > m_length - 1)
            throw new ArgumentOutOfRangeException(nameof(offset));

        // Cache hit.
        if (offset > m_regionInfoCache.Offset && offset < m_regionInfoCache.Offset + m_regionInfoCache.Size) {
            info = m_regionInfoCache;
            info.RelativeVa = m_regionInfoCache.Base + (offset - m_regionInfoCache.Offset);
            return;
        }

        // The offset is not within the current cached region bounds.
        // We look in our region map for the closest lower offset region.
        int offsetIndex = m_regionMap.BinarySearch(new(default, offset), m_regionComparer);
        if (offsetIndex < 0) {
            offsetIndex = ~offsetIndex;
            if (offsetIndex == m_regionMap.Count)
                m_regionInfoCache = m_regionMap[^1];
            else
                m_regionInfoCache = m_regionMap[offsetIndex - 1];
        }

        info = m_regionInfoCache;
        info.RelativeVa = m_regionInfoCache.Base + (offset - m_regionInfoCache.Offset);
    }

    /// <summary>
    /// Reads a span from the stream.
    /// </summary>
    /// <param name="destination">The destination span.</param>
    /// <returns>The number of bytes read, or zero if there's nothing to read.</returns>
    /// <remarks>
    /// When reading from process virtual memory there is no such thing as a partial read.
    /// When calling the native API it scans the memory at the startig offset, and if it hits
    /// an invalid region it returns nothing.
    /// This can happen because process memory is dinamic, and we might try to read a region
    /// that was freed from the time we scanned.
    /// For this reason we can't quite maintain the Stream contract, and we can't throw if
    /// we fail to read, because other regions might still be valid.
    /// </remarks>
    private int ReadSpan(Span<byte> destination)
    {
        bool isBlocked = false;
        int bytesRead = m_readLength - m_readPosition; // Remaining bytes in the buffer.
        if (bytesRead == 0) {
            EnsureOpen();
            if (destination.Length >= m_bufferSize) {

                // Buffer is too small, we read straight from the stream.
                bytesRead = ReadSpanCore(destination);
                m_readLength = 0;
                m_readPosition = 0;

                return bytesRead;
            }

            // Reading the buffer size.
            EnsureBufferAllocated();
            bytesRead = ReadCore(m_buffer!, 0, m_bufferSize);
            if (bytesRead == 0)
                return 0;

            isBlocked = bytesRead < m_bufferSize;
            m_readPosition = 0;
            m_readLength = bytesRead;
        }

        if (bytesRead > destination.Length)
            bytesRead = destination.Length;

        // Copying from the buffer to the destination.
        new ReadOnlySpan<byte>(m_buffer, m_readPosition, bytesRead).CopyTo(destination);
        m_readPosition += bytesRead;
        
        // We might have more bytes to read, so we do it now.
        if (bytesRead < destination.Length && !isBlocked) {
            int moreBytesRead = ReadSpanCore(destination[bytesRead..]);
            bytesRead += moreBytesRead;
            m_readLength = 0;
            m_readPosition = 0;
        }

        return bytesRead;
    }

    /// <summary>
    /// Reads a byte from the stream.
    /// </summary>
    /// <returns>The unsigned byte cast to an Int32, or -1 if at the end of the stream.</returns>
    /// <remarks>
    /// This method gets called when <see cref="ReadByte()"/> can't read from the buffer.
    /// </remarks>
    private int ReadByteSlow()
    {
        EnsureOpen();
        EnsureBufferAllocated();

        m_readLength = ReadCore(m_buffer!, 0, m_bufferSize);
        m_readPosition = 0;
        
        if (m_readLength == 0)
            return -1;

        return m_buffer![m_readPosition++];
    }

    /// <summary>
    /// Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
    /// </summary>
    /// <param name="buffer">The buffer to read into.</param>
    /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
    /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
    /// <returns>The total number of bytes read into the buffer.</returns>
    private int ReadCore(byte[] buffer, int offset, int count)
    {
        ValidateBufferArguments(buffer, offset, count);

        long bytesRead = m_length - m_unbufferedPosition;
        if (bytesRead <= 0)
            return 0;

        if (count > bytesRead)
            count = (int)bytesRead;

        return ReadSpanCore(new Span<byte>(buffer, offset, count));
    }

    /// <summary>
    /// Reads bytes from the process virtual memory into the buffer.
    /// </summary>
    /// <param name="buffer">The buffer to read the bytes to.</param>
    /// <returns>The number of bytes read.</returns>
    private int ReadSpanCore(Span<byte> buffer)
    {
        EnsureOpen();
        long remainingBytes = m_length - m_unbufferedPosition;
        
        // There's nothing more to read.
        if (m_length - m_unbufferedPosition <= 0)
            return 0;

        // If we have less bytes than requested we read only what we got.
        int bytesToRead = buffer.Length;
        if (bytesToRead > remainingBytes)
            bytesToRead = (int)remainingBytes;

        // Reading from process memory can be complicated because if the user
        // requests a number of bytes that span across regions we need to make
        // sure we read everything from each region and set the offsets accordingly.
        int totalRead = 0;
        int bufferPosish = 0;
        do {
            if (m_unbufferedPosition >= m_length)
                break;

            // Remaining bytes to read.
            int currentRead = bytesToRead - totalRead;
            
            // Remaining bytes in the current region.
            // If we need to read more we read all from the current region first.
            long regionRemaining = m_currentRegionInfo.Size - m_currentRegionPosition;
            if (currentRead > regionRemaining)
                currentRead = (int)regionRemaining;

            // Getting the current offset address and reading from memory.
            nint currentAddress = checked((nint)(m_currentRegionInfo.Base + m_currentRegionPosition));
            if (!NativeProcess.TryReadProcessMemory(m_process, currentAddress, buffer.Slice(bufferPosish, currentRead), currentRead))
                break;

            // Adding the current read and advancing the position.
            totalRead += currentRead;
            bufferPosish += currentRead;
            SeekBytes(currentRead);
        }
        while (totalRead < bytesToRead);

        return totalRead;
    }

    /// <summary>
    /// Sets the position within the current stream.
    /// </summary>
    /// <param name="offset">A byte offset relative to the origin parameter.</param>
    /// <param name="origin">A value of <see cref="SeekOrigin"/> indicating the reference point used to obtain the new position.</param>
    /// <returns>The new position within the current stream.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Offset is out of the stream bounds.</exception>
    /// <exception cref="ArgumentException">Invalid <see cref="SeekOrigin"/>.</exception>
    private long SeekCore(long offset, SeekOrigin origin)
    {
        switch (origin) {
            case SeekOrigin.Begin:
                if (offset < 0 || offset > m_length)
                    throw new ArgumentOutOfRangeException(nameof(offset));

                SeekBytes(offset - m_unbufferedPosition);
                break;
            case SeekOrigin.Current:
                long endOffset = m_unbufferedPosition + offset;
                if (endOffset < 0 || endOffset > m_length)
                    throw new ArgumentOutOfRangeException(nameof(offset));

                SeekBytes(endOffset - m_unbufferedPosition);
                break;
            case SeekOrigin.End:
                endOffset = m_length + offset;
                if (endOffset < 0 || endOffset > m_length)
                    throw new ArgumentOutOfRangeException(nameof(offset));

                SeekBytes(endOffset - m_unbufferedPosition);
                break;
            default:
                throw new ArgumentException($"Invalid seek origin '{origin}',");
        }

        return m_unbufferedPosition;
    }

    /// <summary>
    /// Sets the position within the current stream accounting for the regions offsets.
    /// </summary>
    /// <param name="byteCount">The number of bytes to seek</param>
    /// <exception cref="ArgumentOutOfRangeException">Byte count is out of the stream bounds.</exception>
    /// <remarks>
    /// Here we need to make sure we not only advance the position on our stream, but also the memory regions.
    /// We need to set the current region and current region offset properly so we can read from valid addresses.
    /// </remarks>
    private void SeekBytes(long byteCount)
    {
        long nextRegionPosition = m_currentRegionPosition + byteCount;
        long nextUnbufferedPosition = m_unbufferedPosition + byteCount;
        if (nextUnbufferedPosition < 0 || nextUnbufferedPosition > m_length)
            throw new ArgumentOutOfRangeException(nameof(byteCount));

        // Hot path. The position we want is in another memory region.
        if (nextRegionPosition >= m_currentRegionInfo.Size || nextRegionPosition < 0) {
            
            // Performing the binary search to get the next lowest region offset.
            int currentIndex = m_regionMap.BinarySearch(new(default, nextUnbufferedPosition), m_regionComparer);
            if (currentIndex < 0) {
                
                // We are in the middle of a region.
                currentIndex = ~currentIndex;
                if (currentIndex != m_regionMap.Count) {
                    currentIndex --;
                    m_previousRegionIndex = currentIndex;
                    m_currentRegionPosition = nextUnbufferedPosition - m_regionMap[currentIndex].Offset;
                    m_currentRegionInfo = m_regionMap[currentIndex]!;
                }
                else {
                    // The next offset is in the last region or passed that.
                    // If the previous region is the one before the last we advance to the last.
                    if (m_previousRegionIndex < m_regionMap.Count - 1) {
                        m_previousRegionIndex++;
                        m_currentRegionPosition = 0;
                        m_currentRegionInfo = m_regionMap[m_previousRegionIndex]!;
                    }
                    else {
                        // We are in the last region. If the next position passes the last region size
                        // we cap it so we can read the remaining bytes.
                        if (nextRegionPosition > m_currentRegionInfo.Size)
                            nextRegionPosition = m_currentRegionInfo.Size;

                        m_currentRegionPosition = nextRegionPosition;
                    }
                }
            }
            else {
                // We nailed a region.
                m_currentRegionPosition = 0;
                m_previousRegionIndex = currentIndex;
                m_currentRegionInfo = m_regionMap[currentIndex]!;
            }
        }

        // Next position is within the current region.
        else
            m_currentRegionPosition = nextRegionPosition;

        m_unbufferedPosition = nextUnbufferedPosition;
    }

    /// <summary>
    /// Ensures the stream is open.
    /// </summary>
    /// <exception cref="InvalidOperationException">The stream is not open.</exception>
    private void EnsureOpen()
    {
        if (!m_isOpen)
            throw new InvalidOperationException("Stream is not open.");
    }

    /// <summary>
    /// Ensures the buffer is allocated.
    /// </summary>
    private void EnsureBufferAllocated()
    {
        if (m_buffer is null)
            AllocateBuffer();

        [MemberNotNull(nameof(m_buffer))]
        void AllocateBuffer()
            => Interlocked.CompareExchange(ref m_buffer, GC.AllocateUninitializedArray<byte>(m_bufferSize), null);
    }

    /// <summary>
    /// A comparer to compare memory region offsets in the binary search.
    /// </summary>
    private sealed class MemoryRegionOffsetComparer : Comparer<ProcessStreamOffsetInfo>
    {
        public override int Compare(ProcessStreamOffsetInfo? left, ProcessStreamOffsetInfo? right)
        {
            if (left is null && right is null)
                return 0;

            if (left is null && right is not null)
                return -1;

            if (left is not null && right is null)
                return 1;

            return left!.Offset.CompareTo(right!.Offset);
        }
    }
}