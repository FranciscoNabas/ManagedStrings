// This file is part of the ManagedStrings project and repository.
// Project files are licensed under the MIT license.
// https://github.com/FranciscoNabas/ManagedStrings

using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Globalization;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using ManagedStrings.Decoders;
using ManagedStrings.Filtering;
using ManagedStrings.Interop.Windows;

namespace ManagedStrings.Engine;

/// <summary>
/// Valid encoding to use on string decoding.
/// </summary>
[Flags]
public enum ValidEncoding
{
    ASCII    = 0x1,
    Unicode  = 0x2,
    UTF8     = 0x4,
    All      = 0x7,
}

/// <summary>
/// The operation mode.
/// </summary>
/// <remarks>
/// SingleFile: The input is a single file path.
/// Directory: The input is a directory.
/// FileSystemWildcard: The input path contains wildcard characters.
/// Process: The input is one or more process IDs.
/// </remarks>
internal enum Mode
{
    SingleFile,
    Directory,
    FileSystemWildcard,
    Process,
}

/// <summary>
/// The filter type.
/// </summary>
internal enum FilterType
{
    Regex,
    PowerShell,
}

/// <summary>
/// The type of file path to print.
/// Name: Only the file name.
/// Relative: The relative path to the root.
/// FullPath: The full file path.
/// </summary>
/// <remarks>
/// For relative the path is printed as follows:
///     - Root folder: C:\Windows\System32
///     - File: C:\Windows\System32\drivers\etc\hosts
///     - Relative path: drivers\etc\hosts
/// </remarks>
internal enum PrintFileType
{
    None,
    Name,
    Relative,
    FullPath,
}

/// <summary>
/// The process information to print.
/// ProcessId: The process ID.
/// ProcessName: The process name.
/// MemoryType: The virtual memory region type where the string was found.
/// Details: Details related to the memory region. E.g., thread ID, heap ID, file name, etc.
/// </summary>
[Flags]
internal enum PrintProcessInfoType
{
    None        = 0x0,
    ProcessId   = 0x1,
    ProcessName = 0x2,
    MemoryType  = 0x4,
    Details     = 0x8,
    All         = 0xF,
}

/// <summary>
/// The output file type.
/// </summary>
internal enum OutputFileType
{
    PlainText   = 0x1,
    Csv         = 0x2,
    Xml         = 0x4,
    Json        = 0x8,
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
    /// Checks if the input <see cref="ValidEncoding"/> flags contains a flag.
    /// </summary>
    /// <param name="flags">The input <see cref="ValidEncoding"/>.</param>
    /// <param name="flag">The <see cref="ValidEncoding"/> flag(s) to compare to.</param>
    /// <returns>True if it contains the flag(s).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool HasEncodingFlag(this ValidEncoding flags, ValidEncoding flag)
       => (flags & flag) != 0;


    /// <summary>
    /// Checks if the input <see cref="PrintProcessInfoType"/> flags contains a flag.
    /// </summary>
    /// <param name="flags">The input <see cref="PrintProcessInfoType"/>.</param>
    /// <param name="flag">The <see cref="PrintProcessInfoType"/> flag(s) to compare to.</param>
    /// <returns>True if it contains the flag(s).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool HasPrintProcessInfoFlag(this PrintProcessInfoType flags, PrintProcessInfoType flag)
        => (flags & flag) != 0;
}

/// <summary>
/// List[T] extension methods.
/// </summary>
/// <remarks>
/// Not entirely necessary, we use it only once in debug and the method
/// is something that can easily be implemented inline.
/// </remarks>
internal static partial class ListExtensions
{
    /// <summary>
    /// Converts a list of <see cref="IBinaryInteger{TSelf}"/> to a string.
    /// </summary>
    /// <typeparam name="T">The list type.</typeparam>
    /// <param name="list">The list.</param>
    /// <returns>A string containing the list items separated by a comma.</returns>
    internal static string IntegerListToString<T>(this List<T> list) where T : IBinaryInteger<T>
    {
        string? output = null;
        for (int i = 0; i < list.Count; i++)
            output = output is null ? list[i].ToString() : string.Join(", ", output, list[i]);

        return output is null ? string.Empty : output;
    }
}

/// <summary>
/// Class with helper methods to parse command line arguments.
/// </summary>
/// <remarks>
/// This was implemented using a very crude finite state machine.
/// Was done at the very beginning and little has changed.
/// There's certainly a more efficient way of doing this, and we might change in the future.
/// </remarks>
internal sealed class CommandLineOptions
{
    /// <summary>
    /// The FSM token type.
    /// </summary>
    private enum TokenType
    {
        None,
        Encoding,
        Offset,
        BytesToScan,
        MinLength,
        BufferSize,
        Recurse,
        PrintOffset,
        ProcessId,
        MemoryRegionType,
        UnicodeBlocks,
        Filter,
        FilterType,
        FilterOptions,
        ExcludeControlCp,
        PrintEncoding,
        PrintFileName,
        PrintProcess,
        PrintHeader,
        OutFile,
        OutFileType,
        DelimiterChar,
        Force,
        Sync,
        Literal,

        // Test tokens.
        TestConsoleBufferSize,
        TestConsoleUseDriver,
        TestBenchmark,
        TestRunMultipleItemsAsync,
    }

    /// <summary>
    /// A token parsed.
    /// </summary>
    /// <param name="type">The token type.</param>
    /// <param name="value">The token value.</param>
    /// <param name="isComplete">A bool to indicate if a token is complete, I.e., the required arguments were also parsed.</param>
    private sealed class Token(TokenType type, string value, bool isComplete)
    {
        internal TokenType Type { get; } = type;
        internal string Value { get; } = value;
        internal bool IsComplete { get; set; } = isComplete;
    }

    /// <summary>
    /// Represents the FSM state.
    /// </summary>
    private sealed class State
    {
        internal string? Argument { get; set; } = null;
        internal TokenType Type { get; set; } = TokenType.None;
        internal Token? PreviousToken { get; set; } = null;
        internal bool WasPathParsed { get; set; } = false;
        internal bool WasRecurseParsed { get; set; } = false;
        internal bool WasProcessIdParsed { get; set; } = false;
        internal bool WasPrintFileNameParsed { get; set; } = false;
        internal bool WasPrintProcessInfoTypeParsed { get; set; } = false;
        internal bool WasPrintMemoryRegionTypeParsed { get; set; } = false;
        internal string? FilterOptionsArgument { get; set; } = null;
        internal List<Token> ProcessedTokens { get; set; } = [];
    }

    // The command line options.
    internal string? Path { get; private set; } = null;
    internal Mode Mode { get; private set; } = Mode.SingleFile;
    internal ValidEncoding Encoding { get; private set; } = ValidEncoding.Unicode | ValidEncoding.UTF8;
    internal UnicodeBlocks? UnicodeBlocks { get; private set; }
    internal string? Filter { get; private set; } = null;
    internal FilterType FilterType { get; private set; } = FilterType.Regex;
    internal RegexOptions RegexFilterOptions { get; private set; } = RegexOptions.None;
    internal WildcardOptions WildcardFilterOptions { get; private set; } = WildcardOptions.None;
    internal bool WasWildcardOptionsNoneSet { get; set; } = false;
    internal long StartOffset { get; private set; } = 0;
    internal long BytesToScan { get; private set; } = 0;
    internal uint MinStringLength { get; private set; } = 3;
    internal int BufferSize { get; private set; } = 1048576;
    internal List<uint> ProcessId { get; private set; } = [];
    internal ReadMemoryFlags MemoryRegionType { get; private set; } = ReadMemoryFlags.Private;
    internal bool ExcludeControlCp { get; private set; } = false;
    internal bool PrintOffset { get; private set; } = false;
    internal bool PrintEncoding { get; private set; } = false;
    internal PrintFileType PrintFileName { get; private set; } = PrintFileType.None;
    internal PrintProcessInfoType ProcessInfoType { get; private set; } = PrintProcessInfoType.None;
    internal bool PrintHeader { get; private set; } = false;
    internal bool Recurse { get; private set; } = false;
    internal string? OutFile { get; private set; } = null;
    internal OutputFileType OutputFileType { get; private set; } = OutputFileType.PlainText;
    internal char DelimiterChar { get; private set; } = ',';
    internal bool Force { get; private set; } = false;
    internal bool Synchronous { get; private set; } = false;

    // Testing options.
    internal uint ConsoleBufferSize { get; private set; } = 81920;
    internal bool ConsoleUseDriver { get; private set; } = false;
    internal bool Benchmark { get; private set; } = false;
    internal bool RunMultipleItemsAsync { get; private set; } = false;

    // Helps us to query directories accounting for wildcards.
    internal FSObjectInfo? FSInfo { get; private set; }

    private CommandLineOptions() { }

    /// <summary>
    /// Parses the command line options.
    /// </summary>
    /// <param name="arguments">The command line arguments from <see cref="Program.Main(string[])"/>.</param>
    /// <returns>The command line options.</returns>
    /// <exception cref="CommandLineParserException">An invalid argument was found.</exception>
    /// <exception cref="FileNotFoundException">The input file, directory, or output file directory doesn't exit.</exception>
    /// <exception cref="ArgumentException">Invalid token type, or no path or process ID were parsed.</exception>
    /// <exception cref="InvalidOperationException">Invalid options were parsed.</exception>
    [MemberNotNull(nameof(UnicodeBlocks))]
    internal static CommandLineOptions Parse(string[] arguments)
    {
        Token currentToken;
        State fsmState = new();
        bool wasEncodingSet = false;
        CommandLineOptions output = new();
        
        // The main loop. Goes through each argument and tries to parse it.
        // Although not documenting, we support case insensitive arguments beginning with '-', '--', or '/'.
        foreach (string argument in arguments) {
            fsmState.Argument = argument;
            switch (argument.ToUpper()) {
                
                // Encoding.
                // Requires extra argument: True.
                case "-E":
                case "--E":
                case "/E":
                    fsmState.Type = TokenType.Encoding;
                    currentToken = new(TokenType.Encoding, argument, false);
                    break;

                // Starting offset.
                // Requires extra argument: True.
                case "-S":
                case "--S":
                case "/S":
                    fsmState.Type = TokenType.Offset;
                    currentToken = new(TokenType.Offset, argument, false);
                    break;

                // Number of bytes to scan.
                // Requires extra argument: True.
                case "-L":
                case "--L":
                case "/L":
                    fsmState.Type = TokenType.BytesToScan;
                    currentToken = new(TokenType.BytesToScan, argument, false);
                    break;

                // String minimum length.
                // Requires extra argument: True.
                case "-N":
                case "--N":
                case "/N":
                    fsmState.Type = TokenType.MinLength;
                    currentToken = new(TokenType.MinLength, argument, false);
                    break;

                // Reading buffer size.
                // Requires extra argument: True.
                case "-B":
                case "--B":
                case "/B":
                    fsmState.Type = TokenType.BufferSize;
                    currentToken = new(TokenType.BufferSize, argument, false);
                    break;

                // Recursive directory search.
                // Requires extra argument: False.
                case "-R":
                case "--R":
                case "/R":
                    fsmState.Type = TokenType.Recurse;
                    ThrowIfInvalidArgument(fsmState);
                    currentToken = new(TokenType.Recurse, argument, true);
                    output.Recurse = true;
                    fsmState.WasRecurseParsed = true;
                    break;

                // Process IDs.
                // Requires extra argument: True.
                case "-PID":
                case "--PID":
                case "/PID":
                    fsmState.Type = TokenType.ProcessId;
                    ThrowIfInvalidArgument(fsmState);
                    currentToken = new(TokenType.ProcessId, argument, false);
                    fsmState.WasProcessIdParsed = true;
                    break;

                // Process virtual memory region type.
                // Requires extra argument: True.
                case "-MT":
                case "--MT":
                case "/MT":
                    fsmState.Type = TokenType.MemoryRegionType;
                    currentToken = new(TokenType.MemoryRegionType, argument, false);
                    break;

                // Print offset.
                // Requires extra argument: False.
                case "-PO":
                case "--PO":
                case "/PO":
                    fsmState.Type = TokenType.PrintOffset;
                    output.PrintOffset = true;
                    currentToken = new(TokenType.PrintOffset, argument, true);
                    break;

                // Unicode blocks.
                // Requires extra argument: True.
                case "-UB":
                case "--UB":
                case "/UB":
                    fsmState.Type = TokenType.UnicodeBlocks;
                    currentToken = new(TokenType.UnicodeBlocks, argument, false);
                    break;

                // Filter expression.
                // Requires extra argument: True.
                case "-F":
                case "--F":
                case "/F":
                    fsmState.Type = TokenType.Filter;
                    currentToken = new(TokenType.Filter, argument, false);
                    break;

                // Filter type.
                // Requires extra argument: True.
                case "-FT":
                case "--FT":
                case "/FT":
                    fsmState.Type = TokenType.FilterType;
                    currentToken = new(TokenType.FilterType, argument, false);
                    break;

                // Filter options.
                // Requires extra argument: True.
                case "-FO":
                case "--FO":
                case "/FO":
                    fsmState.Type = TokenType.FilterOptions;
                    currentToken = new(TokenType.FilterOptions, argument, false);
                    break;

                // Print encoding.
                // Requires extra argument: False.
                case "-PE":
                case "--PE":
                case "/PE":
                    fsmState.Type = TokenType.PrintEncoding;
                    output.PrintEncoding = true;
                    currentToken = new(TokenType.PrintEncoding, argument, true);
                    break;

                // Print file name.
                // Requires extra argument: False.
                case "-PF":
                case "--PF":
                case "/PF":
                    fsmState.Type = TokenType.PrintFileName;
                    currentToken = new(TokenType.PrintFileName, argument, false);
                    break;

                // Print process information.
                // Requires extra argument: True.
                case "-PP":
                case "--PP":
                case "/PP":
                    fsmState.Type = TokenType.PrintProcess;
                    currentToken = new(TokenType.PrintProcess, argument, false);
                    break;

                // Print header.
                // Requires extra argument: False.
                case "-PH":
                case "--PH":
                case "/PH":
                    fsmState.Type = TokenType.PrintHeader;
                    output.PrintHeader = true;
                    currentToken = new(TokenType.PrintHeader, argument, true);
                    break;

                // Output file.
                // Requires extra argument: True.
                case "-O":
                case "--O":
                case "/O":
                    fsmState.Type = TokenType.OutFile;
                    currentToken = new(TokenType.OutFile, argument, false);
                    break;

                // Output file type.
                // Requires extra argument: True.
                case "-OT":
                case "--OT":
                case "/OT":
                    fsmState.Type = TokenType.OutFileType;
                    currentToken = new(TokenType.OutFileType, argument, false);
                    break;

                // Exclude ASCII control code points.
                // Requires extra argument: False.
                case "-EC":
                case "--EC":
                case "/EC":
                    fsmState.Type = TokenType.ExcludeControlCp;
                    output.ExcludeControlCp = true;
                    currentToken = new(TokenType.ExcludeControlCp, argument, true);
                    break;

                // Delimiter char used to separate information from the result string.
                // Requires extra argument: True.
                case "-D":
                case "--D":
                case "/D":
                    fsmState.Type = TokenType.DelimiterChar;
                    currentToken = new(TokenType.DelimiterChar, argument, false);
                    break;

                // Overwrites the output file if it already exists.
                // Requires extra argument: False.
                case "--FORCE":
                case "/FORCE":
                    fsmState.Type = TokenType.Force;
                    output.Force = true;
                    currentToken = new(TokenType.Force, argument, true);
                    break;

                // Runs the decoding tasks synchronously.
                // Requires extra argument: False.
                case "--SYNC":
                case "/SYNC":
                    fsmState.Type = TokenType.Sync;
                    output.Synchronous = true;
                    currentToken = new(TokenType.Sync, argument, true);
                    break;


                // Tests.

                // Console stream buffer size.
                // Requires extra argument: True.
                case "--TESTCONSOLEBUFFERSIZE":
                    fsmState.Type = TokenType.TestConsoleBufferSize;
                    currentToken = new(TokenType.TestConsoleBufferSize, argument, false);
                    break;

                // Uses the 'ConDrv' API to write to the console.
                // Requires extra argument: False.
                case "--TESTCONSOLEUSEDRIVER":
                    fsmState.Type = TokenType.TestConsoleUseDriver;
                    output.ConsoleUseDriver = true;
                    currentToken = new(TokenType.TestConsoleUseDriver, argument, true);
                    break;

                // Measures execution time for each item and prints it at the end.
                // Requires extra argument: False.
                case "--TESTDOBENCHMARK":
                    fsmState.Type = TokenType.TestBenchmark;
                    output.Benchmark = true;
                    currentToken = new(TokenType.TestBenchmark, argument, true);
                    break;

                // Runs multiple files or processes in parallel.
                // For more info see 'ProcessHandler.cs' and 'FileHandler.cs'.
                // Requires extra argument: False.
                case "--TESTRUNITEMSASYNC":
                    fsmState.Type = TokenType.TestRunMultipleItemsAsync;
                    output.RunMultipleItemsAsync = true;
                    currentToken = new(TokenType.TestRunMultipleItemsAsync, argument, true);
                    break;

                // Catch all.
                default:
                    fsmState.Type = TokenType.Literal;
                    ThrowIfInvalidArgument(fsmState);
                    if (fsmState.PreviousToken is null) {
                        
                        // It's a path.
                        output.Path = argument;
                        fsmState.WasPathParsed = true;
                    }
                    else {
                        switch (fsmState.PreviousToken.Type) {
                            case TokenType.Encoding:
                                
                                // Parsing the encoding.
                                foreach (string singleEncoding in argument.Split('|')) {
                                    if (string.IsNullOrEmpty(singleEncoding))
                                        throw new CommandLineParserException(argument);

                                    if (!Enum.TryParse(singleEncoding, true, out ValidEncoding encoding))
                                        throw new CommandLineParserException(argument);

                                    if (!wasEncodingSet) {
                                        output.Encoding = encoding;
                                        wasEncodingSet = true;
                                    }
                                    else
                                        output.Encoding |= encoding;
                                }

                                fsmState.PreviousToken.IsComplete = true;
                                break;

                            case TokenType.Offset:

                                // Parsing the starting offset.
                                long offset;
                                if (argument.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) {
                                    if (!long.TryParse(argument.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out offset) || offset < 0)
                                        throw new CommandLineParserException(argument);
                                }
                                else {
                                    if (!long.TryParse(argument, out offset) || offset < 0)
                                        throw new CommandLineParserException(argument);
                                }

                                output.StartOffset = offset;
                                fsmState.PreviousToken.IsComplete = true;
                                break;

                            case TokenType.BytesToScan:

                                // Parsing the number of bytes to scan.
                                long bytesToScan;
                                if (argument.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) {
                                    if (!long.TryParse(argument.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out bytesToScan) || bytesToScan < 0)
                                        throw new CommandLineParserException(argument);
                                }
                                else {
                                    if (!long.TryParse(argument, out bytesToScan) || bytesToScan < 0)
                                        throw new CommandLineParserException(argument);
                                }

                                output.BytesToScan = bytesToScan;
                                fsmState.PreviousToken.IsComplete = true;
                                break;

                            case TokenType.MinLength:

                                // Parsing the minimum string length.
                                uint minLength;
                                if (argument.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) {
                                    if (!uint.TryParse(argument.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out minLength) || minLength < 1)
                                        throw new CommandLineParserException(argument);
                                }
                                else {
                                    if (!uint.TryParse(argument, out minLength) || minLength < 1)
                                        throw new CommandLineParserException(argument);
                                }

                                output.MinStringLength = minLength;
                                fsmState.PreviousToken.IsComplete = true;
                                break;

                            case TokenType.BufferSize:

                                // Parsing the read buffer size.
                                int bufferSize;
                                if (argument.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) {
                                    if (!int.TryParse(argument.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out bufferSize) || bufferSize < 1)
                                        throw new CommandLineParserException(argument);
                                }
                                else {
                                    if (!int.TryParse(argument, out bufferSize) || bufferSize < 1)
                                        throw new CommandLineParserException(argument);
                                }
                                
                                output.BufferSize = bufferSize;
                                fsmState.PreviousToken.IsComplete = true;
                                break;

                            // Either a literal or an option that doesn't require extra arguments.
                            case TokenType.Sync:
                            case TokenType.Force:
                            case TokenType.Recurse:
                            case TokenType.PrintOffset:
                            case TokenType.PrintHeader:
                            case TokenType.PrintEncoding:
                            case TokenType.TestBenchmark:
                            case TokenType.ExcludeControlCp:
                            case TokenType.TestConsoleUseDriver:
                            case TokenType.TestRunMultipleItemsAsync:
                            case TokenType.Literal:
                                if (fsmState.WasPathParsed)
                                    throw new CommandLineParserException(argument);

                                if (!string.IsNullOrWhiteSpace(argument)) {
                                    output.Path = argument;
                                    fsmState.WasPathParsed = true;
                                }
                                break;

                            case TokenType.ProcessId:

                                // Parsing process IDs.
                                uint processId;
                                foreach (string singlePidArgument in argument.Split(',')) {
                                    if (singlePidArgument.StartsWith("0x")) {
                                        if (!uint.TryParse(singlePidArgument.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out processId) || processId < 0)
                                            throw new CommandLineParserException(argument);
                                    }
                                    else {
                                        if (!uint.TryParse(singlePidArgument, out processId) || processId < 0)
                                            throw new CommandLineParserException(argument);
                                    }

                                    output.ProcessId.Add(processId);
                                }

                                if (output.ProcessId.Count > 0)
                                    fsmState.PreviousToken.IsComplete = true;

                                break;

                            case TokenType.MemoryRegionType:

                                // Parsing process virtual memory region type.
                                foreach (string regionType in argument.Split('|')) {
                                    if (string.IsNullOrEmpty(regionType))
                                        throw new CommandLineParserException(argument);

                                    if (!Enum.TryParse(regionType, true, out ReadMemoryFlags type))
                                        throw new CommandLineParserException(argument);

                                    if (!fsmState.WasPrintMemoryRegionTypeParsed) {
                                        output.MemoryRegionType = type;
                                        fsmState.WasPrintMemoryRegionTypeParsed = true;
                                    }
                                    else
                                        output.MemoryRegionType |= type;
                                }

                                fsmState.PreviousToken.IsComplete = true;
                                break;

                            case TokenType.PrintProcess:

                                // Parsing the print process information options.
                                foreach (string procInfoType in argument.Split('|')) {
                                    if (string.IsNullOrEmpty(procInfoType))
                                        throw new CommandLineParserException(argument);

                                    if (!Enum.TryParse(procInfoType, true, out PrintProcessInfoType type) || type == PrintProcessInfoType.None)
                                        throw new CommandLineParserException(argument);

                                    if (!fsmState.WasPrintProcessInfoTypeParsed) {
                                        output.ProcessInfoType = type;
                                        fsmState.WasPrintProcessInfoTypeParsed = true;
                                    }
                                    else
                                        output.ProcessInfoType |= type;
                                }

                                fsmState.PreviousToken.IsComplete = true;
                                break;

                            case TokenType.UnicodeBlocks:

                                // Parsing unicode blocks.
                                foreach (string singleBlock in argument.Split('|')) {
                                    if (string.IsNullOrEmpty(singleBlock))
                                        throw new CommandLineParserException(argument);

                                    if (!UnicodeBlocks.TryParse(singleBlock, out UnicodeBlock? block))
                                        throw new CommandLineParserException(argument);

                                    output.UnicodeBlocks ??= new();
                                    output.UnicodeBlocks |= block;
                                }

                                fsmState.PreviousToken.IsComplete = true;
                                break;

                            case TokenType.Filter:

                                // Parsing the filter expression.
                                if (!string.IsNullOrWhiteSpace(argument)) {
                                    output.Filter = argument;
                                    fsmState.PreviousToken.IsComplete = true;
                                }
                                break;

                            case TokenType.FilterType:

                                // Parsing the filter type.
                                if (!Enum.TryParse(argument, true, out FilterType filterType))
                                    throw new CommandLineParserException(argument);

                                output.FilterType = filterType;
                                fsmState.PreviousToken.IsComplete = true;
                                break;

                            case TokenType.FilterOptions:

                                // Parsing the filter options.
                                if (!string.IsNullOrWhiteSpace(argument)) {
                                    fsmState.FilterOptionsArgument = argument;
                                    fsmState.PreviousToken.IsComplete = true;
                                }
                                break;

                            case TokenType.PrintFileName:

                                // Parsing the print file name options.
                                if (!Enum.TryParse(argument, true, out PrintFileType printFileType) || printFileType == PrintFileType.None)
                                    throw new CommandLineParserException(argument);

                                output.PrintFileName = printFileType;
                                fsmState.PreviousToken.IsComplete = true;
                                break;

                            case TokenType.OutFile:

                                // Parsing the output file.
                                // We use 'console' as a keyword to print formatted output to the console.
                                if (!argument.Equals("console", StringComparison.OrdinalIgnoreCase)) {
                                    string? outDir = System.IO.Path.GetDirectoryName(argument);
                                    if (!Directory.Exists(outDir))
                                        throw new FileNotFoundException($"Cannot find directory '{outDir}'.");
                                }

                                output.OutFile = argument;
                                fsmState.PreviousToken.IsComplete = true;
                                break;

                            case TokenType.OutFileType:

                                // Parsing the output file type.
                                if (!Enum.TryParse(argument, true, out OutputFileType outFileType))
                                    throw new CommandLineParserException(argument);

                                output.OutputFileType = outFileType;
                                fsmState.PreviousToken.IsComplete = true;
                                break;

                            case TokenType.DelimiterChar:

                                // Parsing the delimiter character.
                                if (!char.TryParse(argument, out char delimiterChar))
                                    throw new CommandLineParserException(argument);

                                output.DelimiterChar = delimiterChar;
                                fsmState.PreviousToken.IsComplete = true;
                                break;


                            case TokenType.TestConsoleBufferSize:

                                // Parsing the console buffer size.
                                if (!uint.TryParse(argument, out uint consoleBufferSize) || consoleBufferSize > int.MaxValue)
                                    throw new CommandLineParserException(argument);

                                output.ConsoleBufferSize = consoleBufferSize;
                                fsmState.PreviousToken.IsComplete = true;
                                break;

                            default:
                                throw new ArgumentException($"Invalid token type '{fsmState.PreviousToken.Type}'.");
                        }
                    }

                    // Caching the current token.
                    currentToken = new(TokenType.Literal, argument, true);
                    break;
            }

            // Caching the current token.
            fsmState.ProcessedTokens.Add(currentToken);
            fsmState.PreviousToken = currentToken;
        }

        // Checking if we have incomplete parameters.
        IEnumerable<Token> incompleteTokens = fsmState.ProcessedTokens.Where(t => !t.IsComplete);
        if (incompleteTokens.Any()) {
            foreach (Token token in incompleteTokens) {
                
                // '-pf' is allowed to be empty. We default to Name.
                if (token.Type == TokenType.PrintFileName) {
                    output.PrintFileName = PrintFileType.Name;
                    continue;
                }

                // Same with '-pp'.
                if (token.Type == TokenType.PrintProcess) {
                    output.ProcessInfoType = PrintProcessInfoType.ProcessId;
                    continue;
                }

                throw new CommandLineParserException(token.Value);
            }
        }

        // Parsing the filter options, if any.
        // We parse these after the main loop because we need to know which filter type is going to be used.
        if (output.Filter is not null && fsmState.FilterOptionsArgument is not null) {
            
            // Regex.
            if (output.FilterType == FilterType.Regex) {
                foreach (string singleOption in fsmState.FilterOptionsArgument.Split('|')) {
                    if (string.IsNullOrEmpty(singleOption))
                        throw new CommandLineParserException(singleOption);

                    if (!Enum.TryParse(singleOption, true, out RegexOptions option))
                        throw new InvalidOperationException($"'{singleOption}' is not a valid Regex option.");

                    output.RegexFilterOptions |= option;
                }
            }

            // 'PowerShell' WildcardPattern.
            else {
                foreach (string singleOption in fsmState.FilterOptionsArgument.Split('|')) {
                    if (string.IsNullOrEmpty(singleOption))
                        throw new CommandLineParserException(singleOption);

                    if (!Enum.TryParse(singleOption, true, out WildcardOptions option)) {
                        if (Enum.TryParse(singleOption, out RegexOptions regexOption))
                            throw new InvalidOperationException($"'{singleOption}' is not valid with this filter type.");

                        throw new InvalidOperationException($"'{singleOption}' is not a valid Wildcard option.");
                    }
                        
                    if (option == WildcardOptions.None)
                        output.WasWildcardOptionsNoneSet = true;

                    output.WildcardFilterOptions |= option;
                }
            }
        }

        // Setting the default for the 'WildCardPattern' options if no option was given.
        if (output.WildcardFilterOptions == WildcardOptions.None && !output.WasWildcardOptionsNoneSet)
            output.WildcardFilterOptions = WildcardOptions.CultureInvariant | WildcardOptions.IgnoreCase;

        // Checking if the output file already exists.
        if (output.OutFile is not null) {
            if (File.Exists(output.OutFile) && !output.Force)
                throw new InvalidOperationException($"The file '{output.OutFile}' already exists. To overwrite it use '--force'.");
        }

        // Checking parameters for files and processes.
        if (fsmState.WasPathParsed) {
            if (fsmState.WasProcessIdParsed)
                throw new InvalidOperationException("Path can't be used with Process ID.");

            if (fsmState.WasPrintProcessInfoTypeParsed)
                throw new InvalidOperationException("'-pp' can't be used with files.");

            if (fsmState.WasPrintMemoryRegionTypeParsed)
                throw new InvalidOperationException("'-mt' can't be used with files.");

            // Recurse. Directory must exist.
            if (fsmState.WasRecurseParsed) {
                if (System.IO.Path.EndsInDirectorySeparator(output.Path)) {
                    if (!Directory.Exists(output.Path))
                        throw new FileNotFoundException($"Could not find directory '{output.Path}'.");

                    output.FSInfo = new(output.Path!);
                    output.Mode = Mode.Directory;
                }
                else {
                    output.FSInfo = new(output.Path!);
                    if (!output.FSInfo.ContainsWildcard) {
                        if (!Directory.Exists(output.Path))
                            throw new FileNotFoundException($"Could not find directory '{output.Path}'.");

                        output.Mode = Mode.Directory;
                    }
                    else {
                        // We have wildcards, parent directory must exist.
                        if (!Directory.Exists(output.FSInfo.Directory))
                            throw new FileNotFoundException($"Could not find directory '{output.FSInfo.Directory}'.");

                        output.Mode = Mode.FileSystemWildcard;
                    }
                }
            }
            else {
                if (System.IO.Path.EndsInDirectorySeparator(output.Path)) {
                    if (!Directory.Exists(output.Path))
                        throw new FileNotFoundException($"Could not find directory '{output.Path}'.");

                    output.FSInfo = new(output.Path!);
                    output.Mode = Mode.Directory;
                }
                else {
                    output.FSInfo = new(output.Path!);
                    if (!output.FSInfo.ContainsWildcard) {
                        if (!File.Exists(output.Path)) {
                            if (!Directory.Exists(output.Path))
                                throw new FileNotFoundException($"Could not find directory '{output.Path}'.");

                            output.Mode = Mode.Directory;
                        }
                        else {
                            output.Mode = Mode.SingleFile;
                        }
                    }
                    else {
                        if (!Directory.Exists(output.FSInfo.Directory))
                            throw new FileNotFoundException($"Could not find directory '{output.FSInfo.Directory}'.");

                        output.Mode = Mode.FileSystemWildcard;
                    }
                }
            }
        }
        else {
            // Checking if no process ID was parsed.
            if (!fsmState.WasProcessIdParsed)
                throw new ArgumentException("Target path missing.");

            if (output.PrintFileName != PrintFileType.None)
                throw new InvalidOperationException("'-pf' can't be used with process.");

            output.Mode = Mode.Process;
        }

        // UnicodeBlocks always contains BasicLatin.
        if (output.UnicodeBlocks is null)
            output.UnicodeBlocks = new(UnicodeBlocks.BasicLatin);
        else
            output.UnicodeBlocks |= UnicodeBlocks.BasicLatin;

        return output;
    }

    /// <summary>
    /// Checks if we have an invalid argument.
    /// </summary>
    /// <param name="currentState">The FSM state.</param>
    /// <exception cref="CommandLineParserException">An invalid argument was found.</exception>
    private static void ThrowIfInvalidArgument(State currentState)
    {
        if (currentState.PreviousToken is not null) {
            if (currentState.Type != TokenType.Literal) {
                if (currentState.PreviousToken.Type != TokenType.Literal &&
                    currentState.PreviousToken.Type != TokenType.Sync &&
                    currentState.PreviousToken.Type != TokenType.Force &&
                    currentState.PreviousToken.Type != TokenType.Recurse &&
                    currentState.PreviousToken.Type != TokenType.PrintOffset &&
                    currentState.PreviousToken.Type != TokenType.PrintHeader &&
                    currentState.PreviousToken.Type != TokenType.PrintEncoding &&
                    currentState.PreviousToken.Type != TokenType.TestBenchmark &&
                    currentState.PreviousToken.Type != TokenType.ExcludeControlCp &&
                    currentState.PreviousToken.Type != TokenType.TestConsoleUseDriver &&
                    currentState.PreviousToken.Type != TokenType.TestRunMultipleItemsAsync
                )
                    throw new CommandLineParserException(currentState.Argument!);

                switch (currentState.Type) {
                    case TokenType.Recurse:
                        if (currentState.WasProcessIdParsed)
                            throw new CommandLineParserException(currentState.Argument!);

                        break;

                    case TokenType.ProcessId:
                        if (currentState.WasPathParsed || currentState.WasRecurseParsed)
                            throw new CommandLineParserException(currentState.Argument!);

                        break;

                    default:
                        break;
                }

                if (currentState.ProcessedTokens.Any(t => t.Type == currentState.Type))
                    throw new CommandLineParserException(currentState.Argument!);
            }
            else {
                if ((currentState.PreviousToken.Type == TokenType.Literal || currentState.PreviousToken.Type == TokenType.Recurse) && currentState.WasPathParsed)
                    throw new CommandLineParserException(currentState.Argument!);
            }
        }
    }
}

/// <summary>
/// Simple command line parser exception.
/// </summary>
internal sealed class CommandLineParserException : Exception
{
    internal string Argument { get; }

    internal CommandLineParserException(string argument)
        : base() => Argument = argument;
}