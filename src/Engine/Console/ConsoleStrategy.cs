// This file is part of the ManagedStrings project and repository.
// Project files are licensed under the MIT license.
// https://github.com/FranciscoNabas/ManagedStrings

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Win32.SafeHandles;
using ManagedStrings.Interop.Windows;

namespace ManagedStrings.Engine.Console;

/// <summary>
/// The IO type used to read and write to the console.
/// </summary>
/// <remarks>
/// Driver: ReadDriver, WriteDriver;
/// Console: ReadConsoleW, WriteConsoleW;
/// File: ReadFile, WriteFile;
/// </remarks>
public enum ConsoleIoType
{
    Driver,
    Console,
    File,
}

// Not in use anymore.

/// <summary>
/// A string buffer header.
/// </summary>
/// <remarks>
/// This was used when playng around with writing aligned strings while flushing to the console.
/// It contains the string length in bytes, if it's unicode and a magic number.
/// 4 bytes are needed because we can use a bit for unicode and the other 31 which amount to the maximum array size.
/// The magic number was chosen so it doesn't colide with any character:
///     - For ASCII the max. character is 0x7F.
///     - UTF8 doesn't have chars that starts with 0xF5-0xFF.
///     - For unicode these bytes fall within the Basic Plane Private Use Area (0xE000-0xF8FF).
/// </remarks>
internal static class StringBufferHeaderUtils
{
    internal const byte HeaderSize = 6;
    internal const ushort Magic = 0xF5F8;

    /// <summary>
    /// Writes a header to the buffer.
    /// </summary>
    /// <param name="buffer">The input buffer.</param>
    /// <param name="stringByteLength">The string size in bytes.</param>
    /// <param name="unicode">True if the string is Unicode.</param>
    /// <exception cref="ArgumentException">The buffer is not big enough to hold the header.</exception>
    internal static unsafe void WriteHeader(Span<byte> buffer, int stringByteLength, bool unicode)
    {
        if (buffer.Length < HeaderSize)
            throw new ArgumentException("Buffer is not big enough to hold the header.");

        fixed (byte* bufferPtr = &MemoryMarshal.GetReference(buffer)) {
            *(ushort*)bufferPtr = Magic;
            *(uint*)(bufferPtr + 2) = (uint)(unicode ? 1 : 0) | ((uint)stringByteLength << 1);
        }
    }

    /// <summary>
    /// Attempts to get the header from a buffer.
    /// </summary>
    /// <param name="buffer">The input buffer.</param>
    /// <param name="length">The output string length in bytes.</param>
    /// <param name="unicode">True if it's Unicode.</param>
    /// <returns>True if successfully extracted the header.</returns>
    /// <exception cref="ArgumentException">The buffer is not big enough to contain the header.</exception>
    internal static unsafe bool TryGetBufferHeader(ReadOnlySpan<byte> buffer, out int length, out bool unicode)
    {
        if (buffer.Length < HeaderSize)
            throw new ArgumentException("Buffer is not big enough to contain the header.");
        
        length = 0;
        unicode = false;
        fixed (byte* bufferPtr = &MemoryMarshal.GetReference(buffer)) {
            if (*(ushort*)bufferPtr != Magic)
                return false;

            byte* headerOffset = bufferPtr + 2;
            unicode = (*headerOffset & 0x1) > 0;
            length = (int)(*(uint*)headerOffset >> 1);
        }

        return true;
    }
}

/// <summary>
/// The base class for all console stream strategies.
/// </summary>
/// <param name="encoding">The encoding used by the stream.</param>
/// <remarks>
/// This was inspired on the 'System.IO.FileStreamStrategy' class, including functionalities
/// from the <see cref="StreamWriter"/> to write text to the underlying stream.
/// </remarks>
/// <runtimefile>src/libraries/System.Private.CoreLib/src/System/IOStrategies/FileStreamStrategy.cs</runtimefile>
internal abstract class ConsoleStreamStrategy(Encoding encoding) : Stream
{
    // The UTF16 / Unicode character byte size.
    internal const int WCharTByteSize = 2;

    // A null strategy.
    internal static readonly ConsoleStreamStrategy NullStrategy = new NullConsoleStreamStrategy();

    protected Encoding StrategyEncoding = encoding;

    // Console stream doesn't support seeking.
    public override long Length => throw new InvalidOperationException("Console stream doesn't support seekking.");
    public override long Position {
        get => throw new InvalidOperationException("Console stream doesn't support seekking.");
        set => throw new InvalidOperationException("Console stream doesn't support seekking.");
    }

    /// <summary>
    /// When overridden by base classes it gets if the stream is closed.
    /// </summary>
    internal abstract bool IsClosed { get; }

    /// <summary>
    /// When overridden by base classes it gets if the stream is Unicode.
    /// </summary>
    internal abstract bool IsUnicode { get; set; }

    /// <summary>
    /// When overridden by a base class it gets the <see cref="ConsoleIoType"/> used to interact with the console.
    /// </summary>
    internal abstract ConsoleIoType IoType { get; set;  }

    /// <summary>
    /// When overridden by a base class it gets the underlying console handle.
    /// </summary>
    internal abstract SafeFileHandle ConsoleHandle { get; }

    // System.IO.Stream overrides.
    public override int Read(byte[] buffer, int offset, int count)
    {
        ValidateBufferArguments(buffer, offset, count);
        return ReadSpan(new Span<byte>(buffer, offset, count));
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        ValidateBufferArguments(buffer, offset, count);
        WriteSpan(new ReadOnlySpan<byte>(buffer, offset, count));
    }

    public override long Seek(long offset, SeekOrigin origin)
        => throw new InvalidOperationException("Console stream doesn't support seekking.");

    public override void SetLength(long value)
        => throw new InvalidOperationException("Console stream doesn't support seekking.");

    internal virtual void SetBufferSize(int newSize) { }

    /// <summary>
    /// Writes a character to the underlying stream.
    /// </summary>
    /// <param name="value">The character to write.</param>
    internal void Write(char value)
        => WriteSpan(new ReadOnlySpan<byte>(StrategyEncoding.GetBytes([value])));

    /// <summary>
    /// Writes a string to the underlying stream.
    /// </summary>
    /// <param name="value">The value to write.</param>
    internal void Write(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return;

        WriteSpan(new ReadOnlySpan<byte>(StrategyEncoding.GetBytes(value)));
    }

    /// <summary>
    /// Writes a formatted string to the underlying stream.
    /// </summary>
    /// <param name="value">The value to write.</param>
    /// <param name="arguments">The arguments to format the string.</param>
    internal void Write(string? value, params object[] arguments)
    {
        if (string.IsNullOrEmpty(value))
            return;

        WriteSpan(new ReadOnlySpan<byte>(StrategyEncoding.GetBytes(string.Format(value, arguments))));
    }

    /// <summary>
    /// Writes a string to the underlying stream.
    /// </summary>
    /// <param name="value">The value to write.</param>
    /// <returns>A <see cref="Task"/> representing the asyncronous operation.</returns>
    internal Task WriteAsync(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return Task.CompletedTask;

        return WriteSpanAsync(new ReadOnlyMemory<byte>(StrategyEncoding.GetBytes(value)), CancellationToken.None);
    }

    /// <summary>
    /// Writes the current line terminator to the underlying stream.
    /// </summary>
    internal void WriteLine()
        => WriteSpan(new ReadOnlySpan<byte>(StrategyEncoding.GetBytes("\n\r")));

    /// <summary>
    /// Writes the specified string value, followed by the current line terminator, to the underlying stream.
    /// </summary>
    /// <param name="value">The value to write.</param>
    internal unsafe void WriteLine(string? value)
    {
        byte[] writeBuffer;
        ReadOnlySpan<byte> newLineBuffer;
        byte[] bytes = string.IsNullOrEmpty(value) ? [] : StrategyEncoding.GetBytes(value);
        if (StrategyEncoding.CodePage == WindowsConsole.UnicodeCodePage) {
            writeBuffer = new byte[bytes.Length + 4];
            if (BitConverter.IsLittleEndian)
                newLineBuffer = new([0x0A, 0x00, 0x0D, 0x00]);
            else
                newLineBuffer = new([0x00, 0x0A, 0x00, 0x0D]);
        }
        else {
            writeBuffer = new byte[bytes.Length + 2];
            newLineBuffer = new([0x0A, 0x0D]);
        }

        Span<byte> writeSpan = new(writeBuffer);
        if (bytes.Length > 0)
            bytes.AsSpan().CopyTo(writeSpan);
        
        newLineBuffer.CopyTo(writeSpan[bytes.Length..]);

        WriteSpan(writeSpan);
    }

    /// <summary>
    /// Writes the current line terminator to the underlying stream asynchronously.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asyncronous operation.</returns>
    internal Task WriteLineAsync()
        => WriteLineAsync("\n\r");

    /// <summary>
    /// Writes the specified string value, followed by the current line terminator, to the underlying stream asynchronously.
    /// </summary>
    /// <param name="value"></param>
    /// <returns>A <see cref="Task"/> representing the asyncronous operation.</returns>
    internal Task WriteLineAsync(string? value)
    {
        byte[] writeBuffer;
        ReadOnlySpan<byte> newLineBuffer;
        byte[] bytes = string.IsNullOrEmpty(value) ? [] : StrategyEncoding.GetBytes(value);
        if (StrategyEncoding.CodePage == WindowsConsole.UnicodeCodePage) {

            writeBuffer = new byte[bytes.Length + 4];
            if (BitConverter.IsLittleEndian)
                newLineBuffer = new([0x0A, 0x00, 0x0D, 0x00]);
            else
                newLineBuffer = new([0x00, 0x0A, 0x00, 0x0D]);
        }
        else {
            writeBuffer = new byte[bytes.Length + 2];
            newLineBuffer = new([0x0A, 0x0D]);
        }

        Span<byte> writeSpan = new(writeBuffer);
        if (bytes.Length > 0)
            bytes.AsSpan().CopyTo(writeSpan);

        newLineBuffer.CopyTo(writeSpan[bytes.Length..]);

        return WriteSpanAsync(new ReadOnlyMemory<byte>(writeBuffer), CancellationToken.None);
    }

    internal void DisposeInternal(bool disposing)
        => Dispose(disposing);

    // Abstract methods.
    internal abstract int ReadSpan(Span<byte> buffer);
    internal abstract void WriteSpan(ReadOnlySpan<byte> source);
    internal abstract Task WriteSpanAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken);
    internal abstract void EnsureCanRead();
    internal abstract void EnsureCanWrite();

    /// <summary>
    /// A null console stream strategy.
    /// </summary>
    /// <remarks>
    /// Methods on this class do nothing and the properties returns default values.
    /// </remarks>
    /// <runtimefile>src/libraries/System.Private.CoreLib/src/System/IO/Stream.cs</runtimefile>
    private sealed class NullConsoleStreamStrategy : ConsoleStreamStrategy
    {
        private static Task<int>? s_nullReadTask;
        private static SafeFileHandle? s_nullConsoleHandle;

        public override bool CanRead => true;
        public override bool CanWrite => true;
        public override bool CanSeek => true;
        public override long Length => 0L;
        public override long Position { get => 0L; set { } }
        internal override bool IsClosed => false;
        internal override bool IsUnicode { get => false; set { } }
        internal override ConsoleIoType IoType { get => ConsoleIoType.File; set { } }
        internal override SafeFileHandle ConsoleHandle {
            get {
                s_nullConsoleHandle ??= new(Constants.INVALID_HANDLE_VALUE, false);
                return s_nullConsoleHandle;
            }
        }

        internal NullConsoleStreamStrategy()
            : base(Encoding.Default) { }

        public override int Read(byte[] buffer, int offset, int count) => 0;
        public override int ReadByte() => -1;
        public override void Write(byte[] buffer, int offset, int count) { }
        public override void WriteByte(byte value) { }
        public override long Seek(long offset, SeekOrigin origin) => 0L;
        public override void SetLength(long length) { }
        public override void Flush() { }
        internal override int ReadSpan(Span<byte> buffer) => 0;
        internal override void WriteSpan(ReadOnlySpan<byte> source) { }
        internal override void EnsureCanRead() { }
        internal override void EnsureCanWrite() { }
        protected override void Dispose(bool disposing) { }

        [ComVisible(false)]
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            s_nullReadTask ??= Task.FromResult(0);
            return s_nullReadTask;
        }

        [ComVisible(false)]
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
                return Task.CompletedTask;

            return Task.FromCanceled(cancellationToken);
        }

        [ComVisible(false)]
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
                return Task.CompletedTask;

            return Task.FromCanceled(cancellationToken);
        }

        internal override Task WriteSpanAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
                return Task.CompletedTask;

            return Task.FromCanceled(cancellationToken);
        }
    }
}

/// <summary>
/// A buffered console stream.
/// </summary>
/// <remarks>
/// This class is the backbone of why we wrote our own console class.
/// To be able to buffer the text and minimize the calls to the underlying console API.
/// This class was inspired on the 'System.IO.BufferedStreamStrategy'.
/// I found adequate the way they handle file streams and applied this to our console solution.
/// This class is merely a wapper on the <see cref="WindowsConsoleStreamStrategy"/>.
/// </remarks>
/// <runtimefile>src/libraries/System.Private.CoreLib/src/System/IO/Strategies/BufferedFileStreamStrategy.cs</runtimefile>
internal sealed class BufferedConsoleStreamStrategy : ConsoleStreamStrategy
{
    // Since we're calling this instance with Tasks we might have multiple tasks
    // calling from the same thread, which means the lock needs to be non-reentrant.
    // 'SemaphoreSlim' attends that requisite.
    private readonly SemaphoreSlim m_semaphore;
    private readonly ConsoleStreamStrategy m_strategy;

    // It doesn't make sense buffering the console read.
    private byte[]? m_buffer;

    private int m_bufferSize;
    private int m_writePosition;

    /// <summary>
    /// The underlying console handle.
    /// </summary>
    internal override SafeFileHandle ConsoleHandle {
        get {
            m_semaphore.Wait();
            try {
                Flush(true);
                return m_strategy.ConsoleHandle;
            }
            finally {
                m_semaphore.Release();
            }
        }
    }

    internal override bool IsClosed => m_strategy.IsClosed;
    public override bool CanRead => m_strategy.CanRead;
    public override bool CanWrite => m_strategy.CanWrite;
    public override bool CanSeek => false;

    /// <summary>
    /// Gets or sets if the underlying stream is Unicode.
    /// </summary>
    internal override bool IsUnicode {
        get => m_strategy.IsUnicode;
        set => m_strategy.IsUnicode = value;
    }

    /// <summary>
    /// Gets or sets the underlying stream <see cref="ConsoleIoType"/>.
    /// </summary>
    internal override ConsoleIoType IoType {
        get => m_strategy.IoType;
        set => m_strategy.IoType = value;
    }

    /// <summary>
    /// Constructs a buffered stream strategy with the specified buffer size.
    /// </summary>
    /// <param name="strategy">The <see cref="ConsoleStreamStrategy"/> to wrap.</param>
    /// <param name="bufferSize">The initial buffer size.</param>
    internal BufferedConsoleStreamStrategy(ConsoleStreamStrategy strategy, int bufferSize) : base(strategy.IsUnicode ? Encoding.Unicode : Encoding.UTF8)
    {
        Debug.Assert(bufferSize > 1, "Buffering must not be enabled for smaller buffer sizes");

        m_semaphore = new(1, 1);
        m_strategy = strategy;
        m_bufferSize = bufferSize;
    }

    /// <summary>
    /// Reads a byte from the underlying stream.
    /// </summary>
    /// <returns>The value read, or -1 if there's nothing to read.</returns>
    public override int ReadByte()
        => m_strategy.ReadByte();

    /// <summary>
    /// Reads a number of bytes from the underlying stream asynchronously.
    /// </summary>
    /// <param name="buffer">The buffer to read into.</param>
    /// <param name="offset">The byte offset in buffer at which to begin writing data from the stream.</param>
    /// <param name="count">The maximum number of bytes to read.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the read operation.</returns>
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => m_strategy.ReadAsync(buffer, offset, count, cancellationToken);

    /// <summary>
    /// Writes a byte to the underlying stream.
    /// </summary>
    /// <param name="value">The byte to write.</param>
    public override void WriteByte(byte value)
    {
        m_semaphore.Wait();
        try {
            // If we are withing our buffer boundaries we write to it.
            if (m_writePosition > 0 && m_writePosition < m_bufferSize - 1)
                m_buffer![m_writePosition++] = value;
            else
                WriteByteSlow(value);
        }
        finally {
            m_semaphore.Release();
        }
    }

    /// <summary>
    /// Flushes the buffer into the underlying stream
    /// </summary>
    public override void Flush()
    {
        m_semaphore.Wait();
        try {
            Flush(true);
        }
        finally {
            m_semaphore.Release();
        }
    }

    /// <summary>
    /// Disposes of unmanaged resources.
    /// </summary>
    /// <param name="disposing">True to dispose of unmanaged resources.</param>
    protected override void Dispose(bool disposing)
    {
        if (m_strategy.IsClosed)
            return;

        m_semaphore.Wait();
        try {
            Flush(false);
            m_writePosition = 0;
            m_strategy.Dispose();
        }
        finally {
            m_semaphore.Release();
            m_semaphore.Dispose();
        }
    }

    /// <summary>
    /// Changes the stream buffer size.
    /// </summary>
    /// <param name="newSize">The new buffer size.</param>
    internal override void SetBufferSize(int newSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(newSize, 2, nameof(newSize));

        m_semaphore.Wait();
        try {
            // We only need to modify the buffer if it has already been allocated.
            if (m_buffer is not null) {
                Debug.Assert(m_writePosition >= m_buffer.Length, "Write position can't be greater or equal to the buffer length!");
                
                // Flush before creating the new buffer.
                if (m_writePosition > 0)
                    Flush(true);

                m_writePosition = 0;
                m_buffer = new byte[newSize];
                m_bufferSize = newSize;
            }
        }
        finally {
            m_semaphore.Release();
        }
    }

    /// <summary>
    /// Reads the input span length in bytes from the underlying stream into the destination.
    /// </summary>
    /// <param name="destination">The destination span.</param>
    /// <returns>The number of bytes read.</returns>
    internal override int ReadSpan(Span<byte> destination)
        => m_strategy.ReadSpan(destination);

    /// <summary>
    /// Writes a span of bytes to the underlying stream.
    /// </summary>
    /// <param name="source">The source span.</param>
    internal override void WriteSpan(ReadOnlySpan<byte> source)
    {
        m_semaphore.Wait();
        try {
            WriteSpanInternal(source);
        }
        finally {
            m_semaphore.Release();
        }
    }

    /// <summary>
    /// Ensures the underlying stream can read.
    /// </summary>
    internal override void EnsureCanRead()
        => m_strategy.EnsureCanRead();

    /// <summary>
    /// Ensures the underlying stream can write.
    /// </summary>
    internal override void EnsureCanWrite()
        => m_strategy.EnsureCanWrite();

    /// <summary>
    /// Writes a span of bytes to the underlying stream asynchronously.
    /// </summary>
    /// <param name="source">The source span.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the operation.</returns>
    internal override Task WriteSpanAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        return new(() => WriteSpan(source.Span));
    }

    /// <summary>
    /// Writes a span of bytes using the buffer if applicable.
    /// </summary>
    /// <param name="source">The source span.</param>
    private void WriteSpanInternal(ReadOnlySpan<byte> source)
    {
        // Making sure we can write and the buffer is allocated.
        if (m_writePosition == 0) {
            m_strategy.EnsureCanWrite();
            EnsureBufferAllocated();
        }

        // We have data in the buffer.
        if (m_writePosition > 0) {
            int remainingBytes = m_bufferSize - m_writePosition; // Remaining bytes in the buffer.
            if (remainingBytes > 0) {
                if (remainingBytes >= source.Length) {

                    // If our buffer has enough space to accomodate the data we copy into it.
                    source.CopyTo(m_buffer.AsSpan(m_writePosition));
                    m_writePosition += source.Length;

                    return;
                }

                // Since we only write strings to the console we don't want to break a string bytes into separate
                // buffers. So if we don't have enough space for the whole source we flush and continue.
                //
                // WriteHeaderToBuffer(remainingBytes);
                // source.Slice(0, remainingBytes).CopyTo(m_buffer.AsSpan(m_writePosition));
                // m_writePosition += remainingBytes;
                // source = source.Slice(remainingBytes);
            }

            Flush(true);
        }

        // If we got to this point the write position needs to be zero.
        // It either was zero before, or was flushed because we didn't had enough space to accomodate the buffer.
        if (source.Length >= m_bufferSize) {
            
            // If the source is bigger than our buffer we write straight to the underlying stream.
            m_strategy.WriteSpan(source);
            return;
        }
        else if (source.Length == 0)
            return;


        // Copying the data into our buffer.
        source.CopyTo(m_buffer.AsSpan(m_writePosition));
        m_writePosition = source.Length;
    }

    /// <summary>
    /// Writes a byte to the buffer.
    /// </summary>
    /// <param name="value">The byte to write.</param>
    /// <remarks>
    /// This method is called by <see cref="WriteByte(byte)"/> when we don't have enough space in the buffer.
    /// So we flush it before writing to it.
    /// </remarks>
    private void WriteByteSlow(byte value)
    {
        if (m_writePosition == 0) {
            EnsureCanWrite();
            EnsureBufferAllocated();
        }
        else
            Flush(true);

        m_buffer![m_writePosition++] = value;
    }

    /// <summary>
    /// Flushes the buffer to the underlying stream.
    /// </summary>
    /// <param name="throwOnError">True to throw if the write operation threw an exception.</param>
    private void Flush(bool throwOnError)
    {
        try {
            m_strategy.WriteSpan(m_buffer.AsSpan(0, m_writePosition));
        }
        catch {
            if (throwOnError)
                throw;
        }
        finally {
            m_writePosition = 0;
        }
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
}

/// <summary>
/// A Windows console stream.
/// </summary>
/// <remarks>
/// This is the stream that will actually read and write to the console.
/// This stream is unbuffered.
/// </remarks>
internal sealed class WindowsConsoleStreamStrategy : ConsoleStreamStrategy
{
    // The read and write delegates. These delegates are defined by the console IO type.
    private delegate int WriteDelegate(ReadOnlySpan<byte> buffer);
    private delegate int ReadDelegate(Span<byte> buffer, out int bytesRead);

    private SemaphoreSlim? m_asyncSemaphore;
    private readonly bool m_isPipe, m_canRead, m_canWrite;

    private ConsoleIoType m_ioType;
    private UserDefinedIo? m_usrWriteIo;
    private ReadDelegate? m_readDelegate;
    private WriteDelegate? m_writeDelegate;
    private bool m_isDisposed, m_isUnicode;

    internal override SafeFileHandle ConsoleHandle { get; }

    internal override bool IsClosed => m_isDisposed;
    public override bool CanRead => m_canRead;
    public override bool CanWrite => m_canWrite;
    public override bool CanSeek => false;
    
    /// <summary>
    /// Gets or sets if the stream is Unicode.
    /// </summary>
    /// <remarks>
    /// The APIs to use will be defined as follows:
    ///     - Not Unicode, no driver: ReadFile / WriteFile.
    ///     - Unicode, no driver: ReadConsoleW / WriteConsoleW.
    ///     - Not Unicode, with driver: ReadFile / WriteDriver.
    ///     - Unicode, with driver: ReadConsoleW / WriteDriver.
    /// </remarks>
    internal override bool IsUnicode {
        get => m_isUnicode;
        set {
            if (!m_isUnicode && value) {
                if (m_ioType == ConsoleIoType.File) {
                    if (m_canRead)
                        m_readDelegate = new(ReadConsole);
                    else if (m_canWrite)
                        m_writeDelegate = new(WriteConsole);

                    m_ioType = ConsoleIoType.Console;
                }

                // Need to reset the user IO buffer if we're using the ConDrv API.
                // Next call to 'Write' will create with Unicode.
                m_usrWriteIo?.Dispose();
                m_usrWriteIo = null;
                StrategyEncoding = Encoding.Unicode;
                m_isUnicode = true;
            }
            else if (m_isUnicode && !value) {
                if (m_ioType == ConsoleIoType.Console) {
                    if (m_canRead)
                        m_readDelegate = new(ReadFile);
                    else if (m_canWrite)
                        m_writeDelegate = new(WriteFile);

                    m_ioType = ConsoleIoType.File;
                }

                // Need to reset the user IO buffer if we're using the ConDrv API.
                // Next call to 'Write' will create without Unicode.
                m_usrWriteIo?.Dispose();
                m_usrWriteIo = null;
                StrategyEncoding = Encoding.Unicode;
                m_isUnicode = false;
            }
        }
    }

    /// <summary>
    /// Gets or sets the <see cref="ConsoleIoType"/>.
    /// </summary>
    /// <remarks>The APIs to use are chosen using the same method as <see cref="IsUnicode"/>.</remarks>
    internal override ConsoleIoType IoType {
        get => m_ioType;
        set {
            switch (value) {
                case ConsoleIoType.Driver:
                    if (m_ioType != ConsoleIoType.Driver) {
                        if (m_canRead) {
                            if (WindowsConsole.InputEncoding.CodePage == WindowsConsole.UnicodeCodePage && !m_isUnicode)
                                m_isUnicode = true;

                            m_readDelegate = new(ReadDriver);
                        }
                        else if (m_canWrite) {
                            if (WindowsConsole.OutputEncoding.CodePage == WindowsConsole.UnicodeCodePage && !m_isUnicode)
                                m_isUnicode = true;

                            m_writeDelegate = new(WriteDriver);
                        }

                        m_ioType = value;
                    }
                    break;
                case ConsoleIoType.Console:
                    if (m_ioType != ConsoleIoType.Console) {
                        if (m_canRead) {
                            Debug.Assert(WindowsConsole.InputEncoding.CodePage == WindowsConsole.UnicodeCodePage, "ConsoleIoType.Console is only supported by a unicode console input!");
                            m_readDelegate = new(ReadConsole);
                        }
                        else if (m_canWrite) {
                            Debug.Assert(WindowsConsole.OutputEncoding.CodePage == WindowsConsole.UnicodeCodePage, "ConsoleIoType.Console is only supported by a unicode console output!");
                            m_writeDelegate = new(WriteConsole);
                        }

                        m_ioType = value;
                    }
                    break;
                case ConsoleIoType.File:
                    if (m_ioType != ConsoleIoType.File) {
                        if (m_canRead) {
                            Debug.Assert(WindowsConsole.InputEncoding.CodePage != WindowsConsole.UnicodeCodePage, "ConsoleIoType.File is only supported by a non-unicode console input!");
                            m_readDelegate = new(ReadFile);
                        }
                        else if (m_canWrite) {
                            Debug.Assert(WindowsConsole.OutputEncoding.CodePage != WindowsConsole.UnicodeCodePage, "ConsoleIoType.File is only supported by a non-unicode console output!");
                            m_writeDelegate = new(WriteFile);
                        }
                        
                        m_ioType = value;
                    }
                    break;
                default:
                    break;
            }
        }
    }

    /// <summary>
    /// Constructs a console stream strategy.
    /// </summary>
    /// <param name="handle">The handle to the console.</param>
    /// <param name="access">The access. Read for input and write for output.</param>
    /// <param name="ioType">The <see cref="ConsoleIoType"/> to use.</param>
    /// <param name="unicode">True if it's Unicode.</param>
    /// <exception cref="NativeException">The input handle is invalid.</exception>
    /// <exception cref="ArgumentException">Invalid <see cref="ConsoleIoType"/>.</exception>
    internal WindowsConsoleStreamStrategy(IntPtr handle, FileAccess access, ConsoleIoType ioType, bool unicode) : base(unicode ? Encoding.Unicode : Encoding.UTF8)
    {
        if (handle == IntPtr.Zero || handle == Constants.INVALID_HANDLE_VALUE)
            throw new NativeException(ErrorCodes.ERROR_INVALID_HANDLE);

        ConsoleHandle = new(handle, false);
        m_isPipe = NativeIO.GetFileType(handle) == FileType.PIPE;
        if ((access & FileAccess.Read) == FileAccess.Read) {
            m_canRead = true;
            m_readDelegate = ioType switch {
                ConsoleIoType.File => new(ReadFile),
                ConsoleIoType.Console => new(ReadConsole),

                // Read console calling the ConDrv is broken and we don't need it. One day might fix it tho.
                ConsoleIoType.Driver => unicode ? new(ReadConsole) : new(ReadFile),
                _ => throw new ArgumentException($"Invalid console IO type '{ioType}'.")
            };
        }
        else if ((access & FileAccess.Write) == FileAccess.Write) {
            m_canWrite = true;
            m_writeDelegate = ioType switch {
                ConsoleIoType.File => new(WriteFile),
                ConsoleIoType.Console => new(WriteConsole),
                ConsoleIoType.Driver => new(WriteDriver),
                _ => throw new ArgumentException($"Invalid console IO type '{ioType}'.")
            };
        }

        m_ioType = ioType;
        m_isUnicode = unicode;
        m_isDisposed = false;
    }

    /// <summary>
    /// Reads a byte from the stream.
    /// </summary>
    /// <returns>The byte value or -1 if there's nothing to read.</returns>
    public override int ReadByte()
    {
        EnsureCanRead();
        byte b = 0;
        int result = ReadSpan(new Span<byte>([b]));

        return result != 0 ? b : -1;
    }

    /// <summary>
    /// Reads a number of bytes from the stream asynchronously.
    /// </summary>
    /// <param name="buffer">The buffer to read into.</param>
    /// <param name="offset">The byte offset in buffer at which to begin writing data from the stream.</param>
    /// <param name="count">The maximum number of bytes to read.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the read operation.</returns>
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return (Task<int>)Task.FromCanceled(cancellationToken);

        ValidateRead(buffer, offset, count);
        
        SemaphoreSlim semaphore = EnsureAsyncSemaphoreInitialized();
        semaphore.Wait(cancellationToken);
        try {
            Task<int> task = new(() => Read(buffer, offset, count));
            task.Start();
            return task;
        }
        finally {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Writes a byte to the stream.
    /// </summary>
    /// <param name="value">The byte value.</param>
    public override void WriteByte(byte value)
    {
        EnsureCanWrite();
        WriteSpan(new ReadOnlySpan<byte>([value]));
    }

    /// <summary>
    /// Writes a sequence of bytes to the stream asynchronously.
    /// </summary>
    /// <param name="buffer">The buffer to write data from.</param>
    /// <param name="offset">The zero-based byte offset in buffer from which to begin copying bytes to the stream.</param>
    /// <param name="count">The maximum number of bytes to write.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous write operation.</returns>
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ValidateWrite(buffer, offset, count);
        return WriteSpanAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken);
    }

    public override void Flush() { }

    /// <summary>
    /// Reads the input span length in bytes from the stream into the destination.
    /// </summary>
    /// <param name="destination">The destination span.</param>
    /// <returns>The number of bytes read.</returns>
    internal override int ReadSpan(Span<byte> destination)
    {
        int errorCode = m_readDelegate!(destination, out int bytesRead);
        if (errorCode != ErrorCodes.ERROR_SUCCESS && errorCode != ErrorCodes.ERROR_NO_DATA && errorCode != ErrorCodes.ERROR_BROKEN_PIPE)
            throw new NativeException(errorCode);

        return bytesRead;
    }

    /// <summary>
    /// Writes a span of bytes to the stream.
    /// </summary>
    /// <param name="source">The source span.</param>
    internal override void WriteSpan(ReadOnlySpan<byte> source)
    {
        int errorCode = m_writeDelegate!(source);
        if (errorCode != ErrorCodes.ERROR_SUCCESS && errorCode != ErrorCodes.ERROR_NO_DATA && errorCode != ErrorCodes.ERROR_BROKEN_PIPE && errorCode != ErrorCodes.ERROR_PIPE_NOT_CONNECTED)
            throw new NativeException(errorCode);
    }

    /// <summary>
    /// Writes a span of bytes to the underlying stream asynchronously.
    /// </summary>
    /// <param name="source">The source span.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the operation.</returns>
    internal override Task WriteSpanAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        SemaphoreSlim semaphore = EnsureAsyncSemaphoreInitialized();
        semaphore.Wait(cancellationToken);
        try {
            Task task = new(() => WriteSpan(buffer.Span));
            task.Start();
            return task;
        }
        finally {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Ensures we can read from the stream.
    /// </summary>
    /// <exception cref="InvalidOperationException">Console stream state prevents reading.</exception>
    internal override void EnsureCanRead()
    {
        ObjectDisposedException.ThrowIf(m_isDisposed, this);

        if (!m_canRead)
            throw new InvalidOperationException("Console stream state prevents reading.");
    }

    /// <summary>
    /// Ensures we can write to the stream.
    /// </summary>
    /// <exception cref="InvalidOperationException">Console stream state prevents writing.</exception>
    internal override void EnsureCanWrite()
    {
        ObjectDisposedException.ThrowIf(m_isDisposed, this);

        if (!m_canWrite)
            throw new InvalidOperationException("Console stream state prevents writing.");
    }

    /// <summary>
    /// Disposes of unmanaged resources.
    /// </summary>
    /// <param name="disposing">True to dispose of unmanaged resources.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && !m_isDisposed) {
            m_asyncSemaphore?.Dispose();
            m_usrWriteIo?.Dispose();
            m_isDisposed = true;
        }
    }

    /// <summary>
    /// Reads from the stream using 'ReadFile'.
    /// </summary>
    /// <param name="buffer">The buffer to read into.</param>
    /// <param name="bytesRead">The number of bytes read from the stream.</param>
    /// <returns>Zero if succeded or the native error code.</returns>
    /// <seealso cref="ErrorCodes"/>
    private int ReadFile(Span<byte> buffer, out int bytesRead)
        => NativeIO.ReadFile(ConsoleHandle, buffer, out bytesRead);

    /// <summary>
    /// Writes file to the stream using 'WriteFile.
    /// </summary>
    /// <param name="buffer">The buffer to write from.</param>
    /// <returns>Zero if succeded or the native error code.</returns>
    /// <seealso cref="ErrorCodes"/>
    private int WriteFile(ReadOnlySpan<byte> buffer)
        => NativeIO.WriteFile(ConsoleHandle, buffer);

    /// <summary>
    /// Reads from the stream using 'ReadConsoleW'.
    /// </summary>
    /// <param name="buffer">The buffer to read into.</param>
    /// <param name="bytesRead">The number of bytes read from the stream.</param>
    /// <returns>Zero if succeded or the native error code.</returns>
    /// <seealso cref="ErrorCodes"/>
    private unsafe int ReadConsole(Span<byte> buffer, out int bytesRead)
    {
        int result = 0;
        fixed (byte* bufferPtr = &MemoryMarshal.GetReference(buffer)) {
            if (!ConsoleAPI.ReadConsole(ConsoleHandle.DangerousGetHandle(), bufferPtr, buffer.Length / WCharTByteSize, out int charsRead, IntPtr.Zero))
                result = Marshal.GetLastWin32Error();

            bytesRead = charsRead * WCharTByteSize;
        }

        return result;
    }

    /// <summary>
    /// Writes file to the stream using 'WriteConsoleW.
    /// </summary>
    /// <param name="buffer">The buffer to write from.</param>
    /// <returns>Zero if succeded or the native error code.</returns>
    /// <seealso cref="ErrorCodes"/>
    private unsafe int WriteConsole(ReadOnlySpan<byte> buffer)
    {
        int result = 0;
        fixed (byte* bufferPtr = &MemoryMarshal.GetReference(buffer)) {
            if (!ConsoleAPI.WriteConsole(ConsoleHandle.DangerousGetHandle(), bufferPtr, buffer.Length / WCharTByteSize, out _, IntPtr.Zero))
                result = Marshal.GetLastWin32Error();
        }

        return result;
    }

    /// <summary>
    /// Reads from the stream using 'NtDeviceIoControlFile'.
    /// </summary>
    /// <param name="buffer">The buffer to read into.</param>
    /// <param name="bytesRead">The number of bytes read from the stream.</param>
    /// <returns>Zero if succeded or the native error code.</returns>
    /// <seealso cref="ErrorCodes"/>
    private unsafe int ReadDriver(Span<byte> buffer, out int bytesRead)
    {
        int result = 0;
        m_usrWriteIo ??= UserDefinedIo.Create(UserDefinedIo.Operation.Read, m_isUnicode);
        ReadOnlySpan<byte> userIoBuffer = m_usrWriteIo.GetBufferForReadBuffer(buffer);
        fixed (byte* usrIoBuffPtr = &MemoryMarshal.GetReference(userIoBuffer)) {
            int status = ConDrv.IssueUserIo(ConsoleHandle.DangerousGetHandle(), usrIoBuffPtr, userIoBuffer.Length, null, 0);
            if (status != ErrorCodes.STATUS_SUCCESS)
                result = Common.RtlNtStatusToDosError(status);

            bytesRead = m_usrWriteIo.GetLastReadByteCount();
        }

        return result;
    }

    /// <summary>
    /// Writes file to the stream using 'NtDeviceIoControlFile.
    /// </summary>
    /// <param name="buffer">The buffer to write from.</param>
    /// <returns>Zero if succeded or the native error code.</returns>
    /// <seealso cref="ErrorCodes"/>
    private unsafe int WriteDriver(ReadOnlySpan<byte> buffer)
    {
        int result = 0;
        m_usrWriteIo ??= UserDefinedIo.Create(UserDefinedIo.Operation.Write, m_isUnicode);
        ReadOnlySpan<byte> userIoBuffer = m_usrWriteIo.GetBufferForWriteBuffer(buffer);
        fixed (byte* usrIoBuffPtr = &MemoryMarshal.GetReference(userIoBuffer)) {
            int status = ConDrv.IssueUserIo(ConsoleHandle.DangerousGetHandle(), usrIoBuffPtr, userIoBuffer.Length, null, 0);
            if (status != ErrorCodes.STATUS_SUCCESS)
                result = Common.RtlNtStatusToDosError(status);
        }

        return result;
    }

    /// <summary>
    /// Ensures the <see cref="SemaphoreSlim"/> is initialized.
    /// </summary>
    /// <returns>The <see cref="SemaphoreSlim"/>.</returns>
    private SemaphoreSlim EnsureAsyncSemaphoreInitialized()
        => Volatile.Read(ref m_asyncSemaphore)
            ?? Interlocked.CompareExchange(ref m_asyncSemaphore, new SemaphoreSlim(1, 1), null) ?? m_asyncSemaphore;

    /// <summary>
    /// Validates the stream can read and the buffer arguments are valid.
    /// </summary>
    /// <param name="buffer">The buffer to read into.</param>
    /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
    /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
    private void ValidateRead(byte[] buffer, int offset, int count)
    {
        EnsureCanRead();
        ValidateBufferArguments(buffer, offset, count);
    }

    /// <summary>
    /// Validates the stream can write and the buffer arguments are valid.
    /// </summary>
    /// <param name="buffer">The buffer to write into.</param>
    /// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
    /// <param name="count">The number of bytes to be written to the current stream.</param>
    private void ValidateWrite(byte[] buffer, int offset, int count)
    {
        EnsureCanWrite();
        ValidateBufferArguments(buffer, offset, count);
    }
}