// This file is part of the ManagedStrings project and repository.
// Project files are licensed under the MIT license.
// https://github.com/FranciscoNabas/ManagedStrings

using System;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ManagedStrings.Interop.Windows;

// https://github.com/microsoft/terminal/blob/main/dep/Console
// https://www.codeproject.com/Articles/5364085/Tracing-and-Logging-Technologies-on-Windows-Part-3

#region Enumerations

/// <summary>
/// Console API number.
/// </summary>
/// <remarks>
/// Used when calling the console driver.
/// </remarks>
internal enum CONSOLE_API_NUMBER
{
    ConsolepGetCP = 1 << 24,
    ConsolepGetMode,
    ConsolepSetMode,
    ConsolepGetNumberOfInputEvents,
    ConsolepGetConsoleInput,
    ConsolepReadConsole,
    ConsolepWriteConsole,
    ConsolepNotifyLastClose,
    ConsolepGetLangId,
    ConsolepMapBitmap,
}

/// <summary>
/// Console color attribute.
/// </summary>
internal enum Color : short
{
    Black                = 0x00,
    ForegroundBlue       = 0x01,
    ForegroundGreen      = 0x02,
    ForegroundRed        = 0x04,
    ForegroundYellow     = 0x06,
    ForegroundIntensity  = 0x08,
    BackgroundBlue       = 0x10,
    BackgroundGreen      = 0x20,
    BackgroundRed        = 0x40,
    BackgroundYellow     = 0x60,
    BackgroundIntensity  = 0x80,
    ForegroundMask       = 0x0F,
    BackgroundMask       = 0xF0,
    ColorMask            = 0xFF
}

/// <summary>
/// Console driver IO code.
/// </summary>
internal enum ConsoleIoCode
{
    READ_IO                 = 0x500006,  // #define IOCTL_CONDRV_READ_IO CTL_CODE(FILE_DEVICE_CONSOLE, 1, METHOD_OUT_DIRECT, FILE_ANY_ACCESS)
    COMPLETE_IO             = 0x50000B,  // #define IOCTL_CONDRV_COMPLETE_IO  CTL_CODE(FILE_DEVICE_CONSOLE, 2, METHOD_NEITHER, FILE_ANY_ACCESS)
    READ_INPUT              = 0x50000F,  // #define IOCTL_CONDRV_READ_INPUT CTL_CODE(FILE_DEVICE_CONSOLE, 3, METHOD_NEITHER, FILE_ANY_ACCESS)
    WRITE_OUTPUT            = 0x500013,  // #define IOCTL_CONDRV_WRITE_OUTPUT CTL_CODE(FILE_DEVICE_CONSOLE, 4, METHOD_NEITHER, FILE_ANY_ACCESS)
    ISSUE_USER_IO           = 0x500016,  // #define IOCTL_CONDRV_ISSUE_USER_IO CTL_CODE(FILE_DEVICE_CONSOLE, 5, METHOD_OUT_DIRECT, FILE_ANY_ACCESS)
    DISCONNECT_PIPE         = 0x50001B,  // #define IOCTL_CONDRV_DISCONNECT_PIPE CTL_CODE(FILE_DEVICE_CONSOLE, 6, METHOD_NEITHER, FILE_ANY_ACCESS)
    SET_SERVER_INFORMATION  = 0x50001F,  // #define IOCTL_CONDRV_SET_SERVER_INFORMATION CTL_CODE(FILE_DEVICE_CONSOLE, 7, METHOD_NEITHER, FILE_ANY_ACCESS)
    GET_SERVER_PID          = 0x500023,  // #define IOCTL_CONDRV_GET_SERVER_PID CTL_CODE(FILE_DEVICE_CONSOLE, 8, METHOD_NEITHER, FILE_ANY_ACCESS)
    GET_DISPLAY_SIZE        = 0x500027,  // #define IOCTL_CONDRV_GET_DISPLAY_SIZE CTL_CODE(FILE_DEVICE_CONSOLE, 9, METHOD_NEITHER, FILE_ANY_ACCESS)
    UPDATE_DISPLAY          = 0x50002B,  // #define IOCTL_CONDRV_UPDATE_DISPLAY CTL_CODE(FILE_DEVICE_CONSOLE, 10, METHOD_NEITHER, FILE_ANY_ACCESS)
    SET_CURSOR              = 0x50002F,  // #define IOCTL_CONDRV_SET_CURSOR CTL_CODE(FILE_DEVICE_CONSOLE, 11, METHOD_NEITHER, FILE_ANY_ACCESS)
    ALLOW_VIA_UIACCESS      = 0x500033,  // #define IOCTL_CONDRV_ALLOW_VIA_UIACCESS CTL_CODE(FILE_DEVICE_CONSOLE, 12, METHOD_NEITHER, FILE_ANY_ACCESS)
    LAUNCH_SERVER           = 0x500037,  // #define IOCTL_CONDRV_LAUNCH_SERVER CTL_CODE(FILE_DEVICE_CONSOLE, 13, METHOD_NEITHER, FILE_ANY_ACCESS)
    GET_FONT_SIZE           = 0x50003B,  // #define IOCTL_CONDRV_GET_FONT_SIZE CTL_CODE(FILE_DEVICE_CONSOLE, 14, METHOD_NEITHER, FILE_ANY_ACCESS)
}

/// <summary>
/// Console key state. Used by <see cref="CONSOLE_READCONSOLE_CONTROL"/>
/// </summary>
[Flags]
internal enum ControlKeyState : uint
{
    RIGHT_ALT_PRESSED   = 0x0001,  // The right ALT key is pressed.
    LEFT_ALT_PRESSED    = 0x0002,  // The left ALT key is pressed.
    RIGHT_CTRL_PRESSED  = 0x0004,  // The right CTRL key is pressed.
    LEFT_CTRL_PRESSED   = 0x0008,  // The left CTRL key is pressed.
    SHIFT_PRESSED       = 0x0010,  // The SHIFT key is pressed.
    NUMLOCK_ON          = 0x0020,  // The NUM LOCK light is on.
    SCROLLLOCK_ON       = 0x0040,  // The SCROLL LOCK light is on.
    CAPSLOCK_ON         = 0x0080,  // The CAPS LOCK light is on.
    ENHANCED_KEY        = 0x0100,  // The key is enhanced. See remarks.
}

#endregion

#region Structures

[StructLayout(LayoutKind.Sequential)]
internal struct CD_IO_BUFFER : IDisposable
{
    internal int Size;
    internal nint Buffer;

    internal static CD_IO_BUFFER CreateReadConsoleMsg(BOOLEAN unicode, BOOLEAN processCtrlZ, ushort exeNameLen, uint initNumBytes, uint ctrlWakeMask, uint ctrlKeyState, uint numBytes)
    {
        READCONSOLE_MSG message = new(unicode, processCtrlZ, exeNameLen, initNumBytes, ctrlWakeMask, ctrlKeyState, numBytes);
        CD_IO_BUFFER output = new() {
            Size = ConDrv.ReadConsoleMsgSize,
            Buffer = Heap.Alloc(ConDrv.ReadConsoleMsgSize)
        };
        
        Marshal.StructureToPtr(message, output.Buffer, false);
        return output;
    }

    internal static CD_IO_BUFFER CreateWriteConsoleMsg(int numBytes, BOOLEAN unicode)
    {
        WRITECONSOLE_MSG message = new(numBytes, unicode);
        CD_IO_BUFFER output = new() {
            Size = ConDrv.WriteConsoleMsgSize,
            Buffer = Heap.Alloc(ConDrv.WriteConsoleMsgSize)
        };

        Marshal.StructureToPtr(message, output.Buffer, false);
        return output;
    }

    internal static CD_IO_BUFFER CreateUserIo(ReadOnlySpan<byte> userBuffer)
    {
        CD_IO_BUFFER output = new();

        if (userBuffer.Length < 1)
            return output;

        output.Buffer = Heap.Alloc(userBuffer.Length);
        Common.CopyMemory(userBuffer, output.Buffer);

        output.Size = userBuffer.Length;

        return output;
    }

    public readonly void Dispose()
    {
        if (Buffer != nint.Zero)
            Heap.Free(Buffer);
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 0x4)]
internal struct CONSOLE_READCONSOLE_MSG(BOOLEAN unicode, BOOLEAN processCtrlZ, ushort exeNameLen, uint initNumBytes, uint ctrlWakeMask, uint ctrlKeyState, uint numBytes)
{
    internal BOOLEAN Unicode = unicode;
    internal BOOLEAN ProcessControlZ = processCtrlZ;
    internal ushort ExeNameLength = exeNameLen;
    internal uint InitialNumBytes = initNumBytes;
    internal uint CtrlWakeupMask = ctrlWakeMask;
    internal uint ControlKeyState = ctrlKeyState;
    internal uint NumBytes = numBytes;
}

[StructLayout(LayoutKind.Sequential, Pack = 0x4, Size = 0x8)]
internal struct CONSOLE_WRITECONSOLE_MSG(int numBytes, BOOLEAN unicode)
{
    internal int NumBytes = numBytes;
    internal BOOLEAN Unicode = unicode;
}

[StructLayout(LayoutKind.Sequential, Pack = 0x4, Size = 0x8)]
internal struct CONSOLE_MSG_HEADERS(CONSOLE_API_NUMBER apiNumber, int descSize)
{
    internal CONSOLE_API_NUMBER ApiNumber = apiNumber;
    internal int ApiDescriptorSize = descSize;
}

// This is a union with all the console message types.
// We just add the ones we're using.
[StructLayout(LayoutKind.Explicit, Pack = 0x4)]
internal struct CONSOLE_MSG_BODY
{
    [FieldOffset(0)] internal CONSOLE_WRITECONSOLE_MSG WriteConsole;
    [FieldOffset(0)] internal CONSOLE_READCONSOLE_MSG ReadConsole;
}

[StructLayout(LayoutKind.Sequential)]
internal struct CONSOLE_MSG
{
    internal CONSOLE_MSG_HEADERS Header;
    internal CONSOLE_MSG_BODY Message;
}

// These are not the original structures (the ones from conmsgl1.h and condrv.h).
// The original ones uses the  'CONSOLE_MSG' - 'CONSOLE_MSG_BODY' scheme, but when writing
// to the console the driver expects a message of 16 bytes in size. Due to the union the size
// of 'CONSOLE_MSG' is 28 bytes. This causes the console to write the 12 remaining bytes
// before our data.
[StructLayout(LayoutKind.Sequential, Pack = 0x4, Size = 0x10)]
internal struct WRITECONSOLE_MSG(int numBytes, BOOLEAN unicode)
{
    internal CONSOLE_MSG_HEADERS Header = new(CONSOLE_API_NUMBER.ConsolepWriteConsole, ConDrv.WriteConsoleMsgBodySize);
    internal CONSOLE_WRITECONSOLE_MSG Message = new(numBytes, unicode);
}

[StructLayout(LayoutKind.Sequential)]
internal struct READCONSOLE_MSG(BOOLEAN unicode, BOOLEAN processCtrlZ, ushort exeNameLen, uint initNumBytes, uint ctrlWakeMask, uint ctrlKeyState, uint numBytes)
{
    internal CONSOLE_MSG_HEADERS Header = new(CONSOLE_API_NUMBER.ConsolepReadConsole, ConDrv.ReadConsoleMsgBodySize);
    internal CONSOLE_READCONSOLE_MSG Message = new(unicode, processCtrlZ, exeNameLen, initNumBytes, ctrlWakeMask, ctrlKeyState, numBytes);
}

[StructLayout(LayoutKind.Sequential)]
internal struct COORD
{
    internal short X;
    internal short Y;
}

[StructLayout(LayoutKind.Sequential)]
internal struct SMAL_RECT
{
    internal short Left;
    internal short Top;
    internal short Right;
    internal short Bottom;
}

[StructLayout(LayoutKind.Sequential)]
internal struct CONSOLE_SCREEN_BUFFER_INFO
{
    internal COORD dwSize;
    internal COORD dwCursorPosition;
    internal short wAttributes;
    internal SMAL_RECT srWindow;
    internal COORD dwMaximumWindowSize;
}

[StructLayout(LayoutKind.Sequential)]
internal struct CONSOLE_READCONSOLE_CONTROL
{
    internal uint nLength;
    internal uint nInitialChars;
    internal uint dwCtrlWakeupMask;
    internal ControlKeyState dwControlKeyState;
}

#endregion

// When interfacing with the console driver we need to use the correct buffer format for the operation we want to,
// well... operate. The buffer must be a 'PCD_USER_DEFINED_IO', which has this layout:
//
// typedef struct _CD_USER_DEFINED_IO
// {
//     HANDLE Client;
//     ULONG InputCount;
//     ULONG OutputCount;
//     CD_IO_BUFFER Buffers[ANYSIZE_ARRAY];
// } CD_USER_DEFINED_IO, * PCD_USER_DEFINED_IO;
//
// The 'Buffers' contains an array of 'CD_IO_BUFFER', and the number of items depends on the operation.
// For writing we need 3 buffers, for reading 4. The structure has this layout:
//
// typedef struct _CD_IO_BUFFER
// {
//     ULONG Size;
//     PVOID Buffer;
// } CD_IO_BUFFER, * PCD_IO_BUFFER;
//
// For writing the first buffer contains a 'CONSOLE_MSG', the second contains the text we want to write,
// and the third is the output buffer, which contains a 'CONSOLE_WRITECONSOLE_MSG', which is also a member of the 'CONSOLE_MSG'.
// The 'CONSOLE_MSG', 'CONSOLE_WRITECONSOLE_MSG' has these layouts:
//
// typedef struct _CONSOLE_MSG_HEADERS
// {
//     ULONG ApiNumber;
//     ULONG ApiDescriptorSize;
// } CONSOLE_MSG_HEADER, * PCONSOLE_MSG_HEADER;
//
// typedef struct _CONSOLE_MSG : public CONSOLE_MSG_HEADER
// {
// 	   CONSOLE_MSG_BODY u;
// } CONSOLE_MSG, * PCONSOLE_MSG;
//
// #pragma pack(push, 4)
// typedef union _CONSOLE_MSG_BODY
// {
//     CONSOLE_GETCP_MSG GetConsoleCP;
//     CONSOLE_MODE_MSG GetConsoleMode;
//     CONSOLE_MODE_MSG SetConsoleMode;
//     CONSOLE_GETNUMBEROFINPUTEVENTS_MSG GetNumberOfConsoleInputEvents;
//     CONSOLE_GETCONSOLEINPUT_MSG GetConsoleInput;
//     CONSOLE_READCONSOLE_MSG ReadConsoleMsg;
//     CONSOLE_WRITECONSOLE_MSG WriteConsoleMsg;
//     CONSOLE_LANGID_MSG GetConsoleLangId;
// 
// #if defined(BUILD_WOW6432) && !defined(BUILD_WOW3232)
//     CONSOLE_MAPBITMAP_MSG64 MapBitmap;
// #else
//     CONSOLE_MAPBITMAP_MSG MapBitmap;
// #endif
// 
// } CONSOLE_MSG_BODY, * PCONSOLE_MSG_BODY;
// #pragma pack(pop)
//
// typedef struct _CONSOLE_WRITECONSOLE_MSG
// {
//     OUT ULONG NumBytes;
//     IN BOOLEAN Unicode;
// } CONSOLE_WRITECONSOLE_MSG, * PCONSOLE_WRITECONSOLE_MSG;
//
// On a sane programming language you would prepare the buffers as follows:
//
// constexpr size_t bufferSize = sizeof(CD_USER_DEFINED_IO) + 2 * sizeof(CD_IO_BUFFER);
// PCD_USER_DEFINED_IO msgBuffer = (PCD_USER_DEFINED_IO)HeapAlloc(GetProcessHeap(), HEAP_ZERO_MEMORY, bufferSize);
// HANDLE out = GetStdHandle(STD_OUTPUT_HANDLE);
// 
// CONSOLE_MSG message = {
//     ConsolepWriteConsole,
//     sizeof(CONSOLE_WRITECONSOLE_MSG),
//     0,
//     0
// };
// 
// msgBuffer->Client = NULL;
// msgBuffer->InputCount = 2;
// msgBuffer->OutputCount = 1;
// 
// msgBuffer->Buffers[0].Size = sizeof(message);
// msgBuffer->Buffers[0].Buffer = &message;
// 
// msgBuffer->Buffers[1].Size = text.Length()* 2;
// msgBuffer->Buffers[1].Buffer = (PVOID) text.GetBuffer();
// 
// msgBuffer->Buffers[2].Size = sizeof(message.u.WriteConsoleMsg);
// msgBuffer->Buffers[2].Buffer = &message.u.WriteConsoleMsg;
//
// NTSTATUS status = NtDeviceIoControlFile(out, nullptr, nullptr, nullptr, &statusBlock, IOCTL_CONDRV_ISSUE_USER_IO, msgBuffer, (ULONG)bufferSize, nullptr, 0);
//
// For reading from the console is the same principle, what changes is the number of buffers. The first buffer
// it's still the message, the second contains exe name, the third contains the 'CONSOLE_READCONSOLE_MSG', and the
// forth contains the user buffer to read into.
// Everything is a pointer to a pointer to something, so we need to emulate this in dotnet.

/// <summary>
/// Represents a disposable <see cref="CD_USER_DEFINED_IO"/>
/// </summary>
internal sealed class UserDefinedIo : IDisposable
{
    internal enum Operation
    {
        Read,
        Write,
    }

    private static readonly int UsrDefinedIoSize = Marshal.SizeOf<CD_USER_DEFINED_IO>();

    private static readonly int s_readConsoleBufferSize;
    private static readonly int s_writeConsoleBufferSize;

    private byte[]? m_buffer;
    private bool m_isDisposed;
    private Operation m_operation;
    private ConsoleIoBuffer? m_userBuffer;
    private ConsoleIoBuffer? m_exeNameBuffer;
    private ConsoleIoBuffer? m_messageBuffer;

    static UserDefinedIo()
    {
        s_readConsoleBufferSize = UsrDefinedIoSize + 4 * ConDrv.IoBufferSize;
        s_writeConsoleBufferSize = UsrDefinedIoSize + 3 * ConDrv.IoBufferSize;
    }

    private UserDefinedIo() { }

    /// <summary>
    /// Creates a <see cref="UserDefinedIo"/> for a <see cref="Operation"/>.
    /// </summary>
    /// <param name="operation">The operation to create the buffer.</param>
    /// <param name="unicode">True if it's Unicode.</param>
    /// <returns>The <see cref="UserDefinedIo"/>.</returns>
    /// <exception cref="NotSupportedException">Invalid operation option.</exception>
    internal static UserDefinedIo Create(Operation operation, bool unicode)
        => operation switch {
            Operation.Write => CreateForWriteConsole(unicode),
            Operation.Read => CreateForReadConsole(unicode),
            _ => throw new NotSupportedException()
        };

    /// <summary>
    /// Retrieves the <see cref="UserDefinedIo"/> buffer for a write operation.
    /// </summary>
    /// <param name="buffer">The input buffer.</param>
    /// <returns>The buffer containing the <see cref="UserDefinedIo"/> buffer merged with the user buffer.</returns>
    /// <remarks>
    /// We implemented this to minimize the amount of times we create the whole buffer.
    /// With this we utilize our buffer with properties that doesn't change and just write the
    /// user data in the middle of it.
    /// </remarks>
    internal unsafe ReadOnlySpan<byte> GetBufferForWriteBuffer(ReadOnlySpan<byte> buffer)
    {
        ValidateOperation(Operation.Write);

        m_userBuffer?.Dispose();
        m_userBuffer = ConsoleIoBuffer.CreateUserIo(buffer);
        Common.CopyMemory(m_userBuffer.UserBuffer, new Span<byte>(m_buffer, UsrDefinedIoSize + ConDrv.IoBufferSize, ConDrv.IoBufferSize));

        return new(m_buffer);
    }

    /// <summary>
    /// Retrieves the <see cref="UserDefinedIo"/> buffer for a read operation.
    /// </summary>
    /// <param name="buffer">The input buffer.</param>
    /// <returns>The buffer containing the <see cref="UserDefinedIo"/> buffer merged with the user buffer.</returns>
    /// <remarks>
    /// We implemented this to minimize the amount of times we create the whole buffer.
    /// With this we utilize our buffer with properties that doesn't change and just write the
    /// user data in the middle of it.
    /// </remarks>
    internal ReadOnlySpan<byte> GetBufferForReadBuffer(ReadOnlySpan<byte> buffer)
    {
        ValidateOperation(Operation.Read);

        m_userBuffer?.Dispose();
        m_userBuffer = ConsoleIoBuffer.CreateUserIo(buffer);
        Common.CopyMemory(m_userBuffer.UserBuffer, new Span<byte>(m_buffer, UsrDefinedIoSize + (ConDrv.IoBufferSize * 3), ConDrv.IoBufferSize));

        return new(m_buffer);
    }

    /// <summary>
    /// Gets the number of bytes read from the console for the last read operation.
    /// </summary>
    /// <returns>The number of bytes read from the console.</returns>
    internal unsafe int GetLastReadByteCount()
    {
        ValidateOperation(Operation.Read);

        READCONSOLE_MSG* msg = (READCONSOLE_MSG*)((CD_IO_BUFFER*)m_messageBuffer!.Message)->Buffer;
        return (int)msg->Message.NumBytes;
    }

    // The beautiful and extremely enjoyable dance of trying to have
    // variable size unmanaged arrays with pointers to structures with
    // more pointers in .NET.
    private static UserDefinedIo CreateForWriteConsole(bool unicode)
    {
        UserDefinedIo output = new();

        CD_USER_DEFINED_IO udio = new() {
            Client = nint.Zero,
            InputCount = 2,
            OutputCount = 1
        };

        nint udioBuffer = Heap.Alloc(UsrDefinedIoSize);
        try {
            Marshal.StructureToPtr(udio, udioBuffer, false);
            output.m_messageBuffer = ConsoleIoBuffer.CreateWriteConsoleMsg(0, unicode ? BOOLEAN.TRUE : BOOLEAN.FALSE);

            output.m_buffer = new byte[s_writeConsoleBufferSize];
            Common.CopyMemory(udioBuffer, new Span<byte>(output.m_buffer, 0, UsrDefinedIoSize));
            Common.CopyMemory(output.m_messageBuffer.Message, new Span<byte>(output.m_buffer, UsrDefinedIoSize, ConDrv.IoBufferSize));
            Common.CopyMemory(output.m_messageBuffer.Body, new Span<byte>(output.m_buffer, UsrDefinedIoSize + (ConDrv.IoBufferSize * 2), ConDrv.IoBufferSize));

            output.m_operation = Operation.Write;
            output.m_isDisposed = false;

            return output;
        }
        finally {
            Heap.Free(udioBuffer);
        }
    }

    private static UserDefinedIo CreateForReadConsole(bool unicode)
    {
        UserDefinedIo output = new();
        ReadOnlySpan<byte> exeNameBuffer;
        if (unicode)
            exeNameBuffer = new(Encoding.Unicode.GetBytes(Process.GetCurrentProcess().MainModule!.ModuleName));
        else
            exeNameBuffer = new(Console.InputEncoding.GetBytes(Process.GetCurrentProcess().MainModule!.ModuleName));

        CD_USER_DEFINED_IO udio = new() {
            Client = nint.Zero,
            InputCount = 2,
            OutputCount = 2
        };

        nint udioBuffer = Heap.Alloc(UsrDefinedIoSize);
        try {
            Marshal.StructureToPtr(udio, udioBuffer, false);

            output.m_exeNameBuffer = ConsoleIoBuffer.CreateUserIo(exeNameBuffer);
            output.m_messageBuffer = ConsoleIoBuffer.CreateReadConsoleMsg(unicode ? BOOLEAN.TRUE : BOOLEAN.FALSE, BOOLEAN.FALSE, (ushort)exeNameBuffer.Length, 0, 0, 0, 0);

            output.m_buffer = new byte[s_readConsoleBufferSize];
            Common.CopyMemory(udioBuffer, new Span<byte>(output.m_buffer, 0, UsrDefinedIoSize));
            Common.CopyMemory(output.m_messageBuffer.Message, new Span<byte>(output.m_buffer, UsrDefinedIoSize, ConDrv.IoBufferSize));
            Common.CopyMemory(output.m_exeNameBuffer.UserBuffer, new Span<byte>(output.m_buffer, UsrDefinedIoSize + ConDrv.IoBufferSize, ConDrv.IoBufferSize));
            Common.CopyMemory(output.m_messageBuffer.Body, new Span<byte>(output.m_buffer, UsrDefinedIoSize + (ConDrv.IoBufferSize * 2), ConDrv.IoBufferSize));

            output.m_operation = Operation.Read;
            output.m_isDisposed = false;

            return output;
        }
        finally {
            Heap.Free(udioBuffer);
        }
    }

    ~UserDefinedIo() => Dispose(disposing: false);

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposing && !m_isDisposed) {
            m_userBuffer?.Dispose();
            m_exeNameBuffer?.Dispose();
            m_messageBuffer?.Dispose();
            m_isDisposed = true;
        }
    }

    private void ValidateOperation(Operation operation)
    {
        ObjectDisposedException.ThrowIf(m_isDisposed, this);

        if (m_operation != operation)
            throw new InvalidOperationException("Invalid operation for the current buffer type.");
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    private struct CD_USER_DEFINED_IO
    {
        internal nint Client;
        internal uint InputCount;
        internal uint OutputCount;
        // CD_IO_BUFFER Buffers[ANY_SIZE_ARRAY];
    }

    /// <summary>
    /// Represents a disposable <see cref="CD_IO_BUFFER"/>.
    /// </summary>
    private sealed class ConsoleIoBuffer : IDisposable
    {
        private enum BufferType
        {
            Message,
            User
        }

        private bool m_isDisposed;
        private BufferType m_type;
        private CD_IO_BUFFER m_buffer;
        
        // This dude can't be disposed! We're building it from pointers.
        private CD_IO_BUFFER m_body;

        internal unsafe nint Message {
            get {
                if (m_type != BufferType.Message)
                    throw new InvalidOperationException("Invalid buffer type.");

                fixed (CD_IO_BUFFER* ptr = &m_buffer)
                    return new(ptr);
            }
        }

        internal unsafe nint Body {
            get {
                if (m_type != BufferType.Message)
                    throw new InvalidOperationException("Invalid buffer type.");

                fixed (CD_IO_BUFFER* ptr = &m_body)
                    return new(ptr);
            }
        }

        internal unsafe nint UserBuffer {
            get {
                if (m_type != BufferType.User)
                    throw new InvalidOperationException("Invalid buffer type.");

                fixed (CD_IO_BUFFER* ptr = &m_buffer)
                    return new(ptr);
            }
        }

        private ConsoleIoBuffer() { }

        internal static unsafe ConsoleIoBuffer CreateReadConsoleMsg(BOOLEAN unicode, BOOLEAN processCtrlZ, ushort exeNameLen, uint initNumBytes, uint ctrlWakeMask, uint ctrlKeyState, uint numBytes)
        {
            ConsoleIoBuffer output = new() {
                m_buffer = CD_IO_BUFFER.CreateReadConsoleMsg(unicode, processCtrlZ, exeNameLen, initNumBytes, ctrlWakeMask, ctrlKeyState, numBytes),
                m_type = BufferType.Message,
            };

            CONSOLE_MSG* message = (CONSOLE_MSG*)output.m_buffer.Buffer;
            output.m_body = new() {
                Size = ConDrv.ReadConsoleMsgBodySize,
                Buffer = new(&message->Message)
            };

            output.m_isDisposed = false;
            return output;
        }
        
        internal static unsafe ConsoleIoBuffer CreateWriteConsoleMsg(int numBytes, BOOLEAN unicode)
        {
            ConsoleIoBuffer output = new() {
                m_buffer = CD_IO_BUFFER.CreateWriteConsoleMsg(numBytes, unicode),
                m_type = BufferType.Message,
            };

            CONSOLE_MSG* message = (CONSOLE_MSG*)output.m_buffer.Buffer;
            output.m_body = new() {
                Size = ConDrv.WriteConsoleMsgBodySize,
                Buffer = new(&message->Message)
            };

            output.m_isDisposed = false;
            return output;
        }

        internal static ConsoleIoBuffer CreateUserIo(ReadOnlySpan<byte> buffer)
            => new() {
                m_buffer = CD_IO_BUFFER.CreateUserIo(buffer),
                m_type = BufferType.User,
                m_isDisposed = false
            };

        public void Dispose()
        {
            if (!m_isDisposed) {
                m_buffer.Dispose();
                GC.SuppressFinalize(this);
                m_isDisposed = true;
            }
        }
    }
}

/// <summary>
/// Native console APIs.
/// </summary>
internal static partial class ConsoleAPI
{
    /// <seealso href="https://learn.microsoft.com/windows/console/getconsolescreenbufferinfo">GetConsoleScreenBufferInfo function</seealso>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetConsoleScreenBufferInfo(
        nint hConsoleOutput,
        out CONSOLE_SCREEN_BUFFER_INFO lpConsoleScreenBufferInfo
    );

    /// <seealso href="https://learn.microsoft.com/windows/console/setconsoletextattribute">SetConsoleTextAttribute function</seealso>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetConsoleTextAttribute(
        nint hConsoleOutput,
        short wAttributes
    );

    /// <seealso href="https://learn.microsoft.com/windows/console/readconsole">ReadConsole function</seealso>
    [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16, EntryPoint = "ReadConsoleW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static unsafe partial bool ReadConsole(
        nint hConsoleInput,
        byte* lpBuffer,
        int nNumberOfCharsToRead,
        out int lpNumberOfCharsRead,
        nint pInputControl // CONSOLE_READCONSOLE_CONTROL*. Requires command extensions, a console stdout and stdin needs to be unicode.
    );

    /// <seealso href="https://learn.microsoft.com/windows/console/writeconsole">WriteConsole function</seealso>
    [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16, EntryPoint = "WriteConsoleW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static unsafe partial bool WriteConsole(
        nint hConsoleOutput,
        byte* lpBuffer,
        int nNumberOfCharsToWrite,
        out int lpNumberOfCharsWritten,
        nint lpReserved
    );

    /// <seealso href="https://learn.microsoft.com/windows/console/getconsolemode">GetConsoleMode function</seealso>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetConsoleMode(nint handle, out int mode);

    /// <seealso href="https://learn.microsoft.com/windows/console/getconsolecp">GetConsoleCP function</seealso>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    internal static partial int GetConsoleCP();

    /// <seealso href="https://learn.microsoft.com/windows/console/getconsoleoutputcp">GetConsoleOutputCP function</seealso>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    internal static partial int GetConsoleOutputCP();

    /// <seealso href="https://learn.microsoft.com/windows/console/setconsolecp">SetConsoleCP function</seealso>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetConsoleCP(int wCodePageID);

    /// <seealso href="https://learn.microsoft.com/windows/console/setconsoleoutputcp">SetConsoleOutputCP function</seealso>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetConsoleOutputCP(int wCodePageID);
}

/// <summary>
/// Native console driver APIs.
/// </summary>
internal static partial class ConDrv
{
    internal static readonly int IoBufferSize = Marshal.SizeOf<CD_IO_BUFFER>();
    internal static readonly int MsgHeadersSize = Marshal.SizeOf<CONSOLE_MSG_HEADERS>();
    internal static readonly int ReadConsoleMsgSize = Marshal.SizeOf<READCONSOLE_MSG>();
    internal static readonly int WriteConsoleMsgSize = Marshal.SizeOf<WRITECONSOLE_MSG>();
    internal static readonly int ReadConsoleMsgBodySize = Marshal.SizeOf<CONSOLE_READCONSOLE_MSG>();
    internal static readonly int WriteConsoleMsgBodySize = Marshal.SizeOf<CONSOLE_WRITECONSOLE_MSG>();

    /// <seealso href="https://learn.microsoft.com/windows/win32/api/winternl/nf-winternl-ntdeviceiocontrolfile">NtDeviceIoControlFile function (winternl.h)</seealso>
    [LibraryImport("ntdll.dll")]
    private static unsafe partial int NtDeviceIoControlFile(
        nint FileHandle,
        nint Event,
        nint ApcRoutine,
        nint ApcContext,
        ref IO_STATUS_BLOCK IoStatusBlock,
        ConsoleIoCode IoControlCode,
        byte* InputBuffer,
        int InputBufferLength,
        byte* OutputBuffer,
        int OutputBufferLength
    );

    /// <summary>
    /// Issues a user IO to the console driver.
    /// </summary>
    /// <param name="hFile">The handle to the console.</param>
    /// <param name="inputBuffer">The input buffer.</param>
    /// <param name="inputBufferLength">The input buffer length.</param>
    /// <param name="outputBuffer">The output buffer.</param>
    /// <param name="outputBufferLength">The output buffer length.</param>
    /// <returns>A NTSTATUS result of the operation.</returns>
    internal static unsafe int IssueUserIo(nint hFile, byte* inputBuffer, int inputBufferLength, byte* outputBuffer, int outputBufferLength)
    {
        IO_STATUS_BLOCK statusBlock = default;
        return NtDeviceIoControlFile(hFile, nint.Zero, nint.Zero, nint.Zero, ref statusBlock, ConsoleIoCode.ISSUE_USER_IO, inputBuffer, inputBufferLength, outputBuffer, outputBufferLength);
    }
}