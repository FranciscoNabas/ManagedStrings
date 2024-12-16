// This file is part of the ManagedStrings project and repository.
// Project files are licensed under the MIT license.
// https://github.com/FranciscoNabas/ManagedStrings

using System;
using System.Text;
using System.Runtime.InteropServices;

namespace ManagedStrings.Interop.Windows;

#region Enumerations

/// <summary>
/// System information class to be used by <see cref="Common.NtQuerySystemInformation(ManagedStrings.Interop.Windows.SYSTEM_INFORMATION_CLASS, nint, int, out int)"/>.
/// </summary>
/// <remarks>
/// The original enum is much much bigger than this.
/// </remarks>
internal enum SYSTEM_INFORMATION_CLASS
{
    SystemExtendedProcessInformation       = 57,
    SystemHypervisorSharedPageInformation  = 197,
}

/// <summary>
/// Process architecture to be used by <see cref="SYSTEM_INFO"/>.
/// </summary>
internal enum ProcessorArchitecture : ushort
{
    INTEL           = 0x0,
    MIPS            = 0x1,
    ALPHA           = 0x2,
    PPC             = 0x3,
    SHX             = 0x4,
    ARM             = 0x5,
    IA64            = 0x6,
    ALPHA64         = 0x7,
    MSIL            = 0x8,
    AMD64           = 0x9,
    IA32_ON_WIN64   = 0xA,
    NEUTRAL         = 0xB,
    ARM64           = 0xC,
    ARM32_ON_WIN64  = 0xD,
    IA32_ON_ARM64   = 0xE,
}

/// <summary>
/// Processor type to be used by <see cref="SYSTEM_INFO"/>.
/// </summary>
/// <remarks>
/// Obsolete. Keeping it for completion sake.
/// </remarks>
internal enum ProcessorType : uint
{
    INTEL_386      = 386,
    INTEL_486      = 486,
    INTEL_PENTIUM  = 586,
    INTEL_IA64     = 2200,
    AMD_X8664      = 8664,
    MIPS_R4000     = 4000,    // incl R4101 & R3910 for Windows CE
    ALPHA_21064    = 21064,
    PPC_601        = 601,
    PPC_603        = 603,
    PPC_604        = 604,
    PPC_620        = 620,
    HITACHI_SH3    = 10003,   // Windows CE
    HITACHI_SH3E   = 10004,   // Windows CE
    HITACHI_SH4    = 10005,   // Windows CE
    MOTOROLA_821   = 821,     // Windows CE
    SHx_SH3        = 103,     // Windows CE
    SHx_SH4        = 104,     // Windows CE
    STRONGARM      = 2577,    // Windows CE - 0xA11
    ARM720         = 1824,    // Windows CE - 0x720
    ARM820         = 2080,    // Windows CE - 0x820
    ARM920         = 2336,    // Windows CE - 0x920
    ARM_7TDMI      = 70001,   // Windows CE
    OPTIL          = 0x494F,  // MSIL
}

/// <summary>
/// General system access type.
/// </summary>
/// <remarks>
/// winnt.h
/// </remarks>
internal enum AccessType : uint
{
    DELETE                    = 0x00010000,
    READ_CONTROL              = 0x00020000,
    WRITE_DAC                 = 0x00040000,
    WRITE_OWNER               = 0x00080000,
    SYNCHRONIZE               = 0x00100000,
    STANDARD_RIGHTS_REQUIRED  = 0x000F0000,
    STANDARD_RIGHTS_READ      = READ_CONTROL,
    STANDARD_RIGHTS_WRITE     = READ_CONTROL,
    STANDARD_RIGHTS_EXECUTE   = READ_CONTROL,
    STANDARD_RIGHTS_ALL       = 0x001F0000,
    SPECIFIC_RIGHTS_ALL       = 0x0000FFFF,
    ACCESS_SYSTEM_SECURITY    = 0x01000000,
    MAXIMUM_ALLOWED           = 0x02000000,
    GENERIC_READ              = 0x80000000,
    GENERIC_WRITE             = 0x40000000,
    GENERIC_EXECUTE           = 0x20000000,
    GENERIC_ALL               = 0x10000000,
}

/// <summary>
/// Operating system object attributes used by <see cref="OBJECT_ATTRIBUTES"/>
/// </summary>
/// <remarks>
/// winternl.h
/// </remarks>
internal enum ObjectAttributes : uint
{
    OBJ_INHERIT                        = 0x00000002,
    OBJ_PERMANENT                      = 0x00000010,
    OBJ_EXCLUSIVE                      = 0x00000020,
    OBJ_CASE_INSENSITIVE               = 0x00000040,
    OBJ_OPENIF                         = 0x00000080,
    OBJ_OPENLINK                       = 0x00000100,
    OBJ_KERNEL_HANDLE                  = 0x00000200,
    OBJ_FORCE_ACCESS_CHECK             = 0x00000400,
    OBJ_IGNORE_IMPERSONATED_DEVICEMAP  = 0x00000800,
    OBJ_DONT_REPARSE                   = 0x00001000,
    OBJ_VALID_ATTRIBUTES               = 0x00001FF2,
}

/// <summary>
/// The control type send to the <see cref="HandlerRoutine"/>.
/// </summary>
internal enum CtrlType
{
    CtrlC,
    CtrlBreak,
    CtrlClose,
    CtrlLogoff = 5,
    CtrlShutdown,
}

#endregion

#region Structures

/// <summary>
/// The native representation of a <see cref="ulong"/>.
/// </summary>
[StructLayout(LayoutKind.Explicit)]
internal struct ULARGE_INTEGER
{
    [FieldOffset(0)] internal uint LowPart;
    [FieldOffset(4)] internal uint HighPart;
    [FieldOffset(0)] internal ulong QuadPart;
}

/// <summary>
/// The native representation of a <see cref="long"/>.
/// </summary>
[StructLayout(LayoutKind.Explicit)]
internal struct LARGE_INTEGER
{
    [FieldOffset(0)] internal uint LowPart;
    [FieldOffset(4)] internal int HighPart;
    [FieldOffset(0)] internal long QuadPart;
}

/// <summary>
/// The native representation of a <see cref="Guid"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct GUID
{
    internal uint Data1;
    internal ushort Data2;
    internal ushort Data3;

    [MarshalAs(UnmanagedType.LPArray, SizeConst = 8)]
    internal byte[] Data4;
}

/// <summary>
/// The native representation of a Ansi <see cref="string"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct STRING
{
    internal ushort Length;
    internal ushort MaximumLength;
    internal nint Buffer;

    public override readonly string ToString()
    {
        if (Buffer != nint.Zero) {
            string? s = Marshal.PtrToStringAnsi(Buffer);
            return s is null ? string.Empty : s;
        }

        return string.Empty;
    }
}

/// <summary>
/// The native representation of a unicode <see cref="string"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct UNICODE_STRING
{
    internal ushort Length;
    internal ushort MaximumLength;
    internal nint Buffer;

    public override readonly string ToString()
    {
        if (Buffer != nint.Zero) {
            string? s = Marshal.PtrToStringUni(Buffer);
            return s is null ? string.Empty : s;
        }
        
        return string.Empty;
    }
}

/// <summary>
/// The 32-bit native representation of a unicode <see cref="string"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct UNICODE_STRING32
{
    internal ushort Length;
    internal ushort MaximumLength;
    internal ulong Buffer;

    public override readonly string ToString()
    {
        if (Buffer != 0) {
            string? s = Marshal.PtrToStringUni((nint)Buffer);
            return s is null ? string.Empty : s;

        }

        return string.Empty;
    }
}

/// <summary>
/// Structure used by many native APIs to report operation status.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct IO_STATUS_BLOCK
{
    internal IO_STATUS_BLOCK_Union Union;
    internal nint Information;

    [StructLayout(LayoutKind.Explicit)]
    internal struct IO_STATUS_BLOCK_Union
    {
        [FieldOffset(0)] internal int Status;
        [FieldOffset(0)] internal nint Pointer;
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct OBJECT_ATTRIBUTES
{
    internal int Length;
    internal nint RootDirectory;
    internal nint ObjectName;
    internal ObjectAttributes Attributes;
    internal nint SecurityDescriptor;
    internal nint SecurityQualityOfService;
}

[StructLayout(LayoutKind.Sequential)]
internal struct LIST_ENTRY
{
    internal nint Flink; // LIST_ENTRY*.
    internal nint Blink; // LIST_ENTRY*.
}

[StructLayout(LayoutKind.Sequential)]
internal struct LIST_ENTRY32
{
    internal uint Flink; // LIST_ENTRY*.
    internal uint Blink; // LIST_ENTRY*.
}

[StructLayout(LayoutKind.Explicit)]
internal struct SYSTEM_INFO
{
    [FieldOffset(0x0)] internal uint dwOemId;
    [FieldOffset(0x0)] internal ProcessorArchitecture wProcessorArchitecture;
    [FieldOffset(0x2)] internal ushort wReserved;
    [FieldOffset(0x4)] internal ushort dwPageSize;
    [FieldOffset(0x8)] internal nint lpMinimumApplicationAddress;
    [FieldOffset(0x10)] internal nint lpMaximumApplicationAddress;
    [FieldOffset(0x18)] internal ulong dwActiveProcessorMask;
    [FieldOffset(0x20)] internal uint dwNumberOfProcessors;
    [FieldOffset(0x24)] internal ProcessorType dwProcessorType;
    [FieldOffset(0x28)] internal uint dwAllocationGranularity;
    [FieldOffset(0x2C)] internal ushort wProcessorLevel;
    [FieldOffset(0x2E)] internal ushort wProcessorRevision;
}

/// <summary>
/// Represents a disposable <see cref="UNICODE_STRING"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 8, Size = 16)]
internal readonly struct SafeUnicodeString : IDisposable
{
    internal readonly ushort Length;
    internal readonly ushort MaximumLength;
    private readonly nint Buffer;

    internal SafeUnicodeString(string s)
    {
        Buffer = Heap.StringToHeapAllocUni(s);
        Length = (ushort)(s.Length * UnicodeEncoding.CharSize);
        MaximumLength = (ushort)(Length + UnicodeEncoding.CharSize);
    }

    public override readonly string? ToString()
        => Buffer != nint.Zero ? Marshal.PtrToStringUni(Buffer) : null;

    public readonly void Dispose()
    {
        if (Buffer != nint.Zero)
            Heap.Free(Buffer);
    }
}

#endregion

internal enum BOOL : int
{
    FALSE  = 0,
    TRUE   = 1,
}

internal enum BOOLEAN : byte
{
    FALSE  = 0,
    TRUE   = 1,
}

/// <summary>
/// Partial class containing unmanaged macros.
/// </summary>
internal static partial class Constants
{
    internal const int MAX_PATH = 260;

    // Console handle types.
    internal const int STD_INPUT_HANDLE   = -10;
    internal const int STD_OUTPUT_HANDLE  = -11;
    internal const int STD_ERROR_HANDLE   = -12;

    internal static nint INVALID_HANDLE_VALUE = (nint)(-1);

    // FormatMessage flags.
    internal const int FORMAT_MESSAGE_FROM_SYSTEM      = 0x00001000;
    internal const int FORMAT_MESSAGE_FROM_HMODULE     = 0x00000800;
    internal const int FORMAT_MESSAGE_IGNORE_INSERTS   = 0x00000200;
    internal const int FORMAT_MESSAGE_ALLOCATE_BUFFER  = 0x00000100;
}

/// <summary>
/// Partial class containing native error codes and status.
/// </summary>
internal static partial class ErrorCodes
{
    // NTSTATUS constants.
    internal const int STATUS_SUCCESS               = 0;
    internal const int STATUS_BUFFER_TOO_SMALL      = -1073741789;
    internal const int STATUS_NO_MEMORY             = -1073741801;
    internal const int STATUS_INFO_LENGTH_MISMATCH  = -1073741820;
    internal const int STATUS_UNSUCCESSFUL          = -1073741823;
    internal const int STATUS_NO_MORE_FILES         = -2147483642;

    // Win32 error constants.
    internal const int ERROR_SUCCESS                = 0;
    internal const int ERROR_INVALID_HANDLE         = 6;
    internal const int ERROR_INVALID_ACCESS         = 12;
    internal const int ERROR_BROKEN_PIPE            = 109;
    internal const int ERROR_NO_DATA                = 232;
    internal const int ERROR_PIPE_NOT_CONNECTED     = 233;
    internal const int ERROR_PARTIAL_COPY           = 299;
    internal const int ERROR_NOACCESS               = 998;
}

/// <summary>
/// The control handler delegate.
/// </summary>
/// <param name="dwCtrlType">The control type.</param>
/// <returns></returns>
[UnmanagedFunctionPointer(CallingConvention.StdCall)]
[return: MarshalAs(UnmanagedType.Bool)]
internal delegate bool HandlerRoutine(CtrlType dwCtrlType);

/// <summary>
/// Class containing common Win32 APIs.
/// </summary>
internal static partial class Common
{
    internal static SYSTEM_INFO SystemInfo;

    /// <seealso href="https://learn.microsoft.com/windows/console/setconsolectrlhandler">SetConsoleCtrlHandler function</seealso>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetConsoleCtrlHandler(
        HandlerRoutine HandlerRoutine,
        [MarshalAs(UnmanagedType.Bool)] bool Add
    );

    /// <seealso href="https://learn.microsoft.com/windows/win32/api/winternl/nf-winternl-ntquerysysteminformation">NtQuerySystemInformation function (winternl.h)</seealso>
    [LibraryImport("ntdll.dll")]
    internal static partial int NtQuerySystemInformation(
        SYSTEM_INFORMATION_CLASS SystemInformationClass,
        nint SystemInformation,
        int SystemInformationLength,
        out int ReturnLength
    );

    /// <seealso href="https://learn.microsoft.com/windows-hardware/drivers/ddi/wdm/nf-wdm-rtlcopymemory">RtlCopyMemory macro (wdm.h)</seealso>
    [LibraryImport("ntdll.dll", EntryPoint = "RtlCopyMemory")]
    private static unsafe partial void CopyMemory(
        void* dest,
        void* src,
        long count
    );

    /// <seealso href="https://learn.microsoft.com/windows-hardware/drivers/ddi/wdm/nf-wdm-rtlzeromemory">RtlZeroMemory macro (wdm.h)</seealso>
    [LibraryImport("ntdll.dll")]
    private static unsafe partial void RtlZeroMemory(
        byte* Destination,
        long Length
    );

    /// <seealso href="https://learn.microsoft.com/windows/win32/api/winternl/nf-winternl-rtlntstatustodoserror">RtlNtStatusToDosError function (winternl.h)</seealso>
    [LibraryImport("ntdll.dll")]
    internal static partial int RtlNtStatusToDosError(int Status);

    /// <seealso href="https://learn.microsoft.com/windows/win32/api/sysinfoapi/nf-sysinfoapi-getsysteminfo">GetSystemInfo function (sysinfoapi.h)</seealso>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial void GetSystemInfo(ref SYSTEM_INFO lpSystemInfo);

    /// <seealso href="https://learn.microsoft.com/windows/console/getstdhandle">GetStdHandle function</seealso>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    internal static partial nint GetStdHandle(int nStdHandle);

    /// <seealso href="https://learn.microsoft.com/windows/win32/api/handleapi/nf-handleapi-closehandle">CloseHandle function (handleapi.h)</seealso>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool CloseHandle(nint hObject);

    static Common()
    {
        SystemInfo = new();
        GetSystemInfo(ref SystemInfo);
    }

    /// <summary>
    /// Copies memory from a source address to a destination span.
    /// </summary>
    /// <param name="source">The source pointer.</param>
    /// <param name="destination">The destination span.</param>
    internal static unsafe void CopyMemory(nint source, Span<byte> destination)
    {
        fixed (byte* destPtr = &MemoryMarshal.GetReference(destination))
            CopyMemory(destPtr, source.ToPointer(), destination.Length);
    }

    /// <summary>
    /// Copies memory from a source span to a destination address.
    /// </summary>
    /// <param name="source">The source span.</param>
    /// <param name="destination">The destination buffer.</param>
    internal static unsafe void CopyMemory(ReadOnlySpan<byte> source, nint destination)
    {
        fixed (byte* sourcePtr = &MemoryMarshal.GetReference(source))
            CopyMemory(destination.ToPointer(), sourcePtr, source.Length);
    }

    /// <summary>
    /// Zeroes out a byte array.
    /// </summary>
    /// <param name="buffer">The buffer.</param>
    internal static unsafe void ClearBuffer(byte[] buffer)
    {
        fixed (byte* bufferPtr = buffer)
            RtlZeroMemory(bufferPtr, buffer.Length);
    }

    /// <summary>
    /// Sets the console control handler.
    /// </summary>
    /// <param name="handlerRoutine">The <see cref="HandlerRoutine"/>.</param>
    /// <param name="add">True to add.</param>
    /// <exception cref="NativeException"></exception>
    internal static void SetCtrlHandler(HandlerRoutine handlerRoutine, bool add)
    {
        if (!SetConsoleCtrlHandler(handlerRoutine, add))
            throw new NativeException(Marshal.GetLastWin32Error());
    }
}

/// <summary>
/// Represents a disposable <see cref="OBJECT_ATTRIBUTES"/>.
/// </summary>
internal sealed class ManagedObjectAttributes : IDisposable
{
    private bool m_isDisposed;
    private nint m_objectName;
    private nint m_objectNameStruct;
    private OBJECT_ATTRIBUTES m_objectAttributes;

    internal OBJECT_ATTRIBUTES ObjectAttributes => m_objectAttributes;

    internal ManagedObjectAttributes(string objectName, ObjectAttributes attributes)
    {
        m_objectName = Marshal.StringToHGlobalUni(objectName);
        try {
            ushort nameByteSize = (ushort)(objectName.Length * UnicodeEncoding.CharSize);
            UNICODE_STRING nativeObjectName = new() {
                Length = nameByteSize,
                MaximumLength = (ushort)(nameByteSize + UnicodeEncoding.CharSize),
                Buffer = m_objectName,
            };

            m_objectNameStruct = Marshal.AllocHGlobal(Marshal.SizeOf(nativeObjectName));
            Marshal.StructureToPtr(nativeObjectName, m_objectNameStruct, false);

            m_objectAttributes = new() {
                Length = Marshal.SizeOf<OBJECT_ATTRIBUTES>(),
                RootDirectory = nint.Zero,
                ObjectName = m_objectNameStruct,
                Attributes = attributes,
                SecurityDescriptor = nint.Zero,
                SecurityQualityOfService = nint.Zero,
            };

            m_isDisposed = false;
        }
        catch {
            Marshal.FreeHGlobal(m_objectName);

            if (m_objectNameStruct != nint.Zero)
                Marshal.FreeHGlobal(m_objectNameStruct);

            throw;
        }
    }

    ~ManagedObjectAttributes()
        => Dispose(disposing: false);

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposing && !m_isDisposed) {
            Marshal.FreeHGlobal(m_objectNameStruct);
            Marshal.FreeHGlobal(m_objectName);

            m_objectName = nint.Zero;
            m_objectNameStruct = nint.Zero;

            m_isDisposed = true;
        }
    }
}