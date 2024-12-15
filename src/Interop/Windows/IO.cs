// This file is part of the ManagedStrings project and repository.
// Project files are licensed under the MIT license.
// https://github.com/FranciscoNabas/ManagedStrings

using System;
using System.IO;
using System.Threading;
using System.IO.Enumeration;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using ManagedStrings.Engine;

namespace ManagedStrings.Interop.Windows;

#region Enumerations

/// <summary>
/// File access.
/// </summary>
/// <remarks>
/// winnt.h
/// </remarks>
internal enum NativeFileAccess : uint
{
    READ_DATA             = 0x0001, // file & pipe
    LIST_DIRECTORY        = 0x0001, // directory
    WRITE_DATA            = 0x0002, // file & pipe
    ADD_FILE              = 0x0002, // directory
    APPEND_DATA           = 0x0004, // file
    ADD_SUBDIRECTORY      = 0x0004, // directory
    CREATE_PIPE_INSTANCE  = 0x0004, // named pipe
    READ_EA               = 0x0008, // file & directory
    WRITE_EA              = 0x0010, // file & directory
    EXECUTE               = 0x0020, // file
    TRAVERSE              = 0x0020, // directory
    DELETE_CHILD          = 0x0040, // directory
    READ_ATTRIBUTES       = 0x0080, // all
    WRITE_ATTRIBUTES      = 0x0100, // all
    ALL_ACCESS            = AccessType.STANDARD_RIGHTS_REQUIRED | AccessType.SYNCHRONIZE | 0x1FF,
    GENERIC_READ          = AccessType.STANDARD_RIGHTS_READ | READ_DATA | READ_ATTRIBUTES | READ_EA | AccessType.SYNCHRONIZE,
    GENERIC_WRITE         = AccessType.STANDARD_RIGHTS_WRITE | WRITE_DATA | WRITE_ATTRIBUTES | WRITE_EA | APPEND_DATA | AccessType.SYNCHRONIZE,
    GENERIC_EXECUTE       = AccessType.STANDARD_RIGHTS_EXECUTE | READ_ATTRIBUTES | EXECUTE | AccessType.SYNCHRONIZE,
}

/// <summary>
/// File share options.
/// </summary>
/// <remarks>
/// winnt.h
/// </remarks>
internal enum NativeFileShare : uint
{
    READ       = 0x00000001,
    WRITE      = 0x00000002,
    READWRITE  = 0x00000003,
    DELETE     = 0x00000004,
    ALL        = 0x00000007,
}

/// <summary>
/// File options.
/// </summary>
/// <remarks>
/// winternl.h
/// </remarks>
internal enum NativeFileOptions : uint
{
    NONE                         = 0x00000000,
    DIRECTORY_FILE               = 0x00000001,
    WRITE_THROUGH                = 0x00000002,
    SEQUENTIAL_ONLY              = 0x00000004,
    NO_INTERMEDIATE_BUFFERING    = 0x00000008,
    SYNCHRONOUS_IO_ALERT         = 0x00000010,
    SYNCHRONOUS_IO_NONALERT      = 0x00000020,
    NON_DIRECTORY_FILE           = 0x00000040,
    CREATE_TREE_CONNECTION       = 0x00000080,
    COMPLETE_IF_OPLOCKED         = 0x00000100,
    NO_EA_KNOWLEDGE              = 0x00000200,
    OPEN_REMOTE_INSTANCE         = 0x00000400,
    RANDOM_ACCESS                = 0x00000800,
    DELETE_ON_CLOSE              = 0x00001000,
    OPEN_BY_FILE_ID              = 0x00002000,
    OPEN_FOR_BACKUP_INTENT       = 0x00004000,
    NO_COMPRESSION               = 0x00008000,
    OPEN_REQUIRING_OPLOCK        = 0x00010000,
    RESERVE_OPFILTER             = 0x00100000,
    OPEN_REPARSE_POINT           = 0x00200000,
    OPEN_NO_RECALL               = 0x00400000,
    OPEN_FOR_FREE_SPACE_QUERY    = 0x00800000,
}

/// <summary>
/// File type.
/// </summary>
internal enum FileType : uint
{
    UNKNOWN  = 0x0000,
    DISK     = 0x0001,
    CHAR     = 0x0002,
    PIPE     = 0x0003,
    REMOTE   = 0x8000,
}

/// <summary>
/// File information class.
/// </summary>
/// <remarks>
/// Not the entire enumeration.
/// Used by <see cref="NativeIO.NtQueryDirectoryFile(SafeFileHandle, nint, nint, nint, ref IO_STATUS_BLOCK, nint, int, FILE_INFORMATION_CLASS, bool, nint, bool)"/>
/// </remarks>
internal enum FILE_INFORMATION_CLASS
{
    DirectoryInformation = 1,
    FullDirectoryInformation
}

#endregion

#region Structures

/// <summary>
/// Used for asynchronous file operations.
/// </summary>
[StructLayout(LayoutKind.Explicit)]
internal struct OVERLAPPED
{
    [FieldOffset(0)] internal ulong Internal;
    [FieldOffset(0x8)] internal ulong InternalHigh;
    [FieldOffset(0x10)] internal uint Offset;
    [FieldOffset(0x14)] internal uint OffsetHigh;
    [FieldOffset(0x10)] internal nint Pointer;
    [FieldOffset(0x18)] internal nint hEvent;
}

/// <summary>
/// File directory information.
/// </summary>
/// <remarks>
/// To be able to get the file name we have
/// to use pointers, in the context of the buffer
/// returned by the NT function.
/// </remarks>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct FILE_FULL_DIR_INFORMATION
{
    internal int NextEntryOffset;
    internal uint FileIndex;
    internal long CreationTime;
    internal long LastAccessTime;
    internal long LastWriteTime;
    internal long ChangeTime;
    internal long EndOfFile;
    internal long AllocationSize;
    internal FileAttributes FileAttributes;
    internal int FileNameLength;
    internal int EaSize;
    private readonly char m_fileName;

    internal readonly string FileName => GetName();

    private readonly unsafe string GetName()
    {
        fixed (char* namePtr = &m_fileName) {
            return new string(namePtr, 0, FileNameLength / 2);
        }
    }
}

#endregion

/// <summary>
/// File system object information.
/// </summary>
internal sealed class FSObjectInfo
{
    internal string Name { get;}
    internal string FullName { get; }
    internal string Directory { get;}
    internal bool ContainsWildcard { get; }

    internal FSObjectInfo(string path)
    {
        path = Path.TrimEndingDirectorySeparator(path);
        string name = path[(path.LastIndexOf('\\') + 1)..];

        this.Name = name;
        this.FullName = path;
        this.Directory = path[..path.LastIndexOf('\\')];
        this.ContainsWildcard = name.AsSpan().IndexOfAny('*', '?') > -1;
    }
}

/// <summary>
/// Contains minimal information about a file.
/// </summary>
/// <param name="fullName">The file full path.</param>
/// <param name="eof">The file last byte offset.</param>
/// <remarks>
/// This object should be kept as small as possible.
/// </remarks>
internal sealed class FileMinimalInformation(string fullName, long eof)
{
    internal string FullName { get; } = fullName;
    internal long EndOfFile { get; } = eof;
}

/// <summary>
/// Native IO functions.
/// </summary>
internal static partial class NativeIO
{
    /// <seealso href="https://learn.microsoft.com/windows/win32/api/winternl/nf-winternl-ntopenfile">NtOpenFile function (winternl.h)</seealso>
    [LibraryImport("ntdll.dll", SetLastError = true)]
    private static partial int NtOpenFile(
        ref nint FileHandle,
        int DesiredAccess,
        OBJECT_ATTRIBUTES ObjectAttributes,
        ref IO_STATUS_BLOCK IoStatusBlock,
        NativeFileShare ShareAccess,
        NativeFileOptions OpenOptions
    );

    /// <seealso href="https://learn.microsoft.com/windows-hardware/drivers/ddi/ntifs/nf-ntifs-ntquerydirectoryfile">NtQueryDirectoryFile function (ntifs.h)</seealso>
    [LibraryImport("ntdll.dll", SetLastError = true)]
    private static partial int NtQueryDirectoryFile(
        SafeFileHandle FileHandle,
        nint Event,
        nint ApcRoutine,
        nint ApcContext,
        ref IO_STATUS_BLOCK IoStatusBlock,
        nint FileInformation,
        int Length,
        FILE_INFORMATION_CLASS FileInformationClass,
        [MarshalAs(UnmanagedType.Bool)] bool ReturnSingleEntry,
        nint FileName,
        [MarshalAs(UnmanagedType.Bool)] bool RestartScan
    );

    /// <seealso href="https://learn.microsoft.com/windows/win32/api/fileapi/nf-fileapi-readfile">ReadFile function (fileapi.h)</seealso>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static unsafe partial bool ReadFile(
        nint hFile,
        byte* lpBuffer,
        int nNumberOfBytesToRead,
        out int lpNumberOfBytesRead,
        nint lpOverlapped
    );

    /// <seealso href="https://learn.microsoft.com/windows/win32/api/fileapi/nf-fileapi-writefile">WriteFile function (fileapi.h)</seealso>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static unsafe partial bool WriteFile(
        nint hFile,
        byte* lpBuffer,
        int nNumberOfBytesToWrite,
        out int lpNumberOfBytesWritten,
        nint lpOverlapped
    );

    /// <seealso href="https://learn.microsoft.com/windows/win32/api/winbase/nf-winbase-querydosdevicew">QueryDosDeviceW function (winbase.h)</seealso>
    [LibraryImport("kernel32.dll", EntryPoint = "QueryDosDeviceW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial int QueryDosDevice(
        string lpDeviceName,
        nint lpTargetPath,
        int ucchMax
    );

    /// <seealso href="https://learn.microsoft.com/windows/win32/api/fileapi/nf-fileapi-getlogicaldrivestringsw">GetLogicalDriveStringsW function (fileapi.h)</seealso>
    [LibraryImport("kernel32.dll", EntryPoint = "GetLogicalDriveStringsW", SetLastError = true)]
    private static partial int GetLogicalDriveStrings(
        int nBufferLength,
        nint lpBuffer
    );

    /// <seealso href="https://learn.microsoft.com/windows/win32/api/fileapi/nf-fileapi-getfilesizeex">GetFileSizeEx function (fileapi.h)</seealso>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetFileSizeEx(
        nint hFile,
        out LARGE_INTEGER lpFileSize
    );

    /// <seealso href="https://learn.microsoft.com/windows/win32/api/fileapi/nf-fileapi-getfiletype">GetFileType function (fileapi.h)</seealso>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    internal static partial FileType GetFileType(nint hFile);

    /// <summary>
    /// Opens a handle to a file.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <param name="desiredAccess">The desired access.</param>
    /// <param name="shareAccess">The share options.</param>
    /// <param name="options">The file options.</param>
    /// <returns>A <see cref="SafeFileHandle"/> to the file.</returns>
    /// <exception cref="NativeException">The native API returned a non-success status.</exception>
    internal static SafeFileHandle OpenFile(string path, int desiredAccess, NativeFileShare shareAccess, NativeFileOptions options)
    {
        nint hFile = nint.Zero;
        string openPath = $"\\??\\{path}";
        IO_STATUS_BLOCK statusBlock = new();
        using ManagedObjectAttributes objectAttributes = new(openPath, ObjectAttributes.OBJ_CASE_INSENSITIVE);
        int status = NtOpenFile(ref hFile, desiredAccess, objectAttributes.ObjectAttributes, ref statusBlock, shareAccess, options);
        if (status != ErrorCodes.STATUS_SUCCESS)
            throw new NativeException(status, true);

        return new(hFile, true);
    }

    /// <summary>
    /// Read from a file.
    /// </summary>
    /// <param name="hFile">The file handle.</param>
    /// <param name="buffer">The buffer to read into.</param>
    /// <param name="bytesRead">The number of bytes read.</param>
    /// <returns>Zero if the call succeeded otherwise the last error set by the native function.</returns>
    internal static unsafe int ReadFile(SafeFileHandle hFile, ReadOnlySpan<byte> buffer, out int bytesRead)
    {
        int result = 0;
        fixed (byte* bufferPtr = &MemoryMarshal.GetReference(buffer)) {
            if (!ReadFile(hFile.DangerousGetHandle(), bufferPtr, buffer.Length, out bytesRead, nint.Zero))
                result = Marshal.GetLastWin32Error();
        }

        return result;
    }

    /// <summary>
    /// Write to a file.
    /// </summary>
    /// <param name="hFile">The file handle.</param>
    /// <param name="buffer">The buffer to write from.</param>
    /// <returns>Zero if the call succeeded otherwise the last error set by the native function.</returns>
    internal static unsafe int WriteFile(SafeFileHandle hFile, ReadOnlySpan<byte> buffer)
    {
        int result = 0;
        fixed (byte* bufferPtr = &MemoryMarshal.GetReference(buffer)) {
            if (!WriteFile(hFile.DangerousGetHandle(), bufferPtr, buffer.Length, out _, nint.Zero))
                result = Marshal.GetLastWin32Error();
        }

        return result;
    }

    /// <summary>
    /// Gets a file size.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <returns></returns>
    /// <exception cref="NativeException">The call to 'GetFileSizeEx' failed.</exception>
    internal static long GetFileSize(string filePath)
    {
        using SafeFileHandle hFile = OpenFile(filePath, (int)NativeFileAccess.READ_ATTRIBUTES, NativeFileShare.READ, NativeFileOptions.NONE);
        if (!GetFileSizeEx(hFile.DangerousGetHandle(), out LARGE_INTEGER fileSize))
            throw new NativeException(Marshal.GetLastWin32Error());

        return fileSize.QuadPart;
    }

    /// <summary>
    /// List files within a directory.
    /// </summary>
    /// <param name="info">The <see cref="FSObjectInfo"/> with directory information.</param>
    /// <param name="recurse">True to list files recursively.</param>
    /// <param name="token">The <see cref="CancellationToken"/> to be monitored.</param>
    /// <returns>A <see cref="List{string}"/> with the file paths.</returns>
    internal static List<FileMinimalInformation> GetDirectoryFiles(FSObjectInfo info, bool recurse, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        List<FileMinimalInformation> output = [];
        GetDirectoryFilesInternal(info, ref output, recurse, token);

        return output;
    }

    /// <summary>
    /// Converts a device path to a DOS path.
    /// </summary>
    /// <param name="devicePath">The file device path.</param>
    /// <returns>The DOS file path.</returns>
    /// <exception cref="NativeException">A call to a native API returned a non-success code.</exception>
    public static string GetFileDosPathFromDevicePath(string devicePath)
    {
        using ScopedBuffer logicalDrivesBuffer = new(1024);
        string deviceName = $"\\Device\\{devicePath.Split('\\')[2]}";
        
        // Listing the logical drives.
        int charCount = GetLogicalDriveStrings(512, logicalDrivesBuffer);
        if (charCount == 0)
            throw new NativeException(Marshal.GetLastWin32Error());

        foreach (string drive in SplitDriveStrings(logicalDrivesBuffer, charCount)) {
            using ScopedBuffer currentDeviceBuffer = new(512);
            string trimmedDrive = drive[..2];
            
            // Querying the device name for the current logical drive.
            if (0 == QueryDosDevice(trimmedDrive, currentDeviceBuffer, 256))
                throw new NativeException(Marshal.GetLastWin32Error());

            // Found our device name.
            if (deviceName.Equals(Marshal.PtrToStringUni(currentDeviceBuffer)))
                return $"{trimmedDrive}\\{devicePath.Remove(0, deviceName.Length + 1)}";
        }

        return devicePath;
    }

    /// <summary>
    /// List files within a directory.
    /// </summary>
    /// <param name="info">The <see cref="FSObjectInfo"/> with directory information.</param>
    /// <param name="fileList">The file information list.</param>
    /// <param name="recurse">True to list files recursively.</param>
    /// <param name="token">The <see cref="CancellationToken"/> to be monitored.</param>
    /// <exception cref="FileNotFoundException">The root directory doesn't exist.</exception>
    /// <exception cref="NativeException">A call to a native function returned a non-success status.</exception>
    private static unsafe void GetDirectoryFilesInternal(FSObjectInfo info, ref List<FileMinimalInformation> fileList, bool recurse, CancellationToken token)
    {
        // This method is either called for a directory path, or if the path contains
        // wildcard characters. If it's a directory we set the path to the full name,
        // otherwise to the parent directory.
        string directory;
        if (info.ContainsWildcard)
            directory = info.Directory;
        else
            directory = info.FullName;

        if (!Directory.Exists(directory))
            throw new FileNotFoundException($"Can't find directory '{directory}'.");

        // Opening a handle to the directory.
        NativeFileOptions openOptions = NativeFileOptions.OPEN_FOR_BACKUP_INTENT | NativeFileOptions.SYNCHRONOUS_IO_NONALERT;
        int desiredAccess = (int)AccessType.SYNCHRONIZE | (int)NativeFileAccess.READ_ATTRIBUTES | (int)NativeFileAccess.LIST_DIRECTORY;
        using SafeFileHandle hFile = OpenFile(directory, desiredAccess, NativeFileShare.READ, openOptions);

        // Initial call. The function returns a number of structures that fit in our buffer.
        int bufferSize = 1 << 13;
        IO_STATUS_BLOCK statusBlock = new();
        using ScopedBuffer buffer = new(bufferSize);
        int status = NtQueryDirectoryFile(hFile, nint.Zero, nint.Zero, nint.Zero, ref statusBlock, buffer, bufferSize, FILE_INFORMATION_CLASS.FullDirectoryInformation, false, nint.Zero, false);
        if (status != ErrorCodes.STATUS_SUCCESS)
            throw new NativeException(status, true);

        nint offset = buffer;
        string rootTerminatedPath = $"{directory}\\";
        FILE_FULL_DIR_INFORMATION* currentInformation;
        ReadOnlySpan<char> rootNameChars = new(info.Name.ToCharArray());
        
        // Calling the function untill we have no more files.
        do {
            token.ThrowIfCancellationRequested();

            // Going through each file in the buffer.
            do {
                token.ThrowIfCancellationRequested();

                // Getting the name. This has to be done in the buffer context as a pointer.
                currentInformation = (FILE_FULL_DIR_INFORMATION*)offset;
                string name = currentInformation->FileName;

                // Skip '.' and '..'.
                if (name.Equals(".", StringComparison.OrdinalIgnoreCase) || name.Equals("..", StringComparison.OrdinalIgnoreCase)) {
                    offset = nint.Add(offset, currentInformation->NextEntryOffset);
                    continue;
                }

                // Skipping if it doesn't match our expression.
                if (info.ContainsWildcard && !FileSystemName.MatchesWin32Expression(rootNameChars, name)) {
                    offset = nint.Add(offset, currentInformation->NextEntryOffset);
                    continue;
                }

                // Calling recursively if it's a directory.
                if ((currentInformation->FileAttributes & FileAttributes.Directory) == FileAttributes.Directory) {
                    if (recurse)
                        GetDirectoryFilesInternal(new(rootTerminatedPath + name), ref fileList, recurse, token);
                }
                else
                    // It's a file.
                    fileList.Add(new(rootTerminatedPath + name, currentInformation->EndOfFile));

                // Advancing the buffer offset.
                offset = nint.Add(offset, currentInformation->NextEntryOffset);

            } while (currentInformation->NextEntryOffset != 0);

            // Refilling the buffer.
            status = NtQueryDirectoryFile(hFile, nint.Zero, nint.Zero, nint.Zero, ref statusBlock, buffer, bufferSize, FILE_INFORMATION_CLASS.FullDirectoryInformation, false, nint.Zero, false);
            if (status != ErrorCodes.STATUS_SUCCESS && status != ErrorCodes.STATUS_NO_MORE_FILES)
                throw new NativeException(status, true);

            offset = buffer;

        } while (status == ErrorCodes.STATUS_SUCCESS && offset != nint.Zero);
    }

    /// <summary>
    /// Splits a string containing the logical drive names into individual logical drive strings.
    /// </summary>
    /// <param name="drivesString">The logical drives string.</param>
    /// <param name="charCount">The char count.</param>
    /// <returns>A <see cref="string[]"/> containing the split logical drives.</returns>
    public static unsafe string[] SplitDriveStrings(nint drivesString, int charCount)
    {
        List<string> output = [];
        if (charCount <= 4) {
            output.Add(new string((char*)drivesString, 0, 4));
            return [.. output];
        }

        for (int i = 0; i < charCount; i += 4) {
            char* currentOffset = (char*)nint.Add(drivesString, i);
            output.Add(new string(currentOffset, 0, 4));
        }

        return [.. output];
    }
}