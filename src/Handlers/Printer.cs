// This file is part of the ManagedStrings project and repository.
// Project files are licensed under the MIT license.
// https://github.com/FranciscoNabas/ManagedStrings

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using ManagedStrings.Engine;
using ManagedStrings.Serialization;
using ManagedStrings.Engine.Console;

namespace ManagedStrings.Handlers;

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
    /// Checks whether an <see cref="OutputFileType"/> flag is <see cref="OutputFileType.PlainText"/> and/or <see cref="OutputFileType.Csv"/>.
    /// </summary>
    /// <param name="flags">The input <see cref="OutputFileType"/>.</param>
    /// <returns>True if the flag is <see cref="OutputFileType.PlainText"/> and/or <see cref="OutputFileType.Csv"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsSimpleTextOutFile(this OutputFileType flags)
        => ((int)flags & 0x3) > 0;
}

/// <summary>
/// The glorious ManagedStrings printer.
/// </summary>
internal sealed class Printer : IDisposable
{
    /// <summary>
    /// The printing data type.
    /// </summary>
    /// <remarks>
    /// This is used internally to manage the printer buffer.
    /// </remarks>
    private enum PrintDataType
    {
        OffsetStart,
        OffsetEnd,
        Encoding,
        File,
        ProcessId,
        ProcessMemoryType,
        ProcessMemoryDetails,
        ProcessName,
        ResultString,
    }

    // The printer buffer contains objects that will be used
    // to create the final formatted string.
    private readonly object[] m_buffer;
    private readonly bool m_isPrintToFile;
    private readonly bool m_isOutFileConsole;
    private readonly string m_headerTemplate;
    private readonly string m_resultTemplate;
    private readonly CommandLineOptions m_options;
    private readonly Encoding m_originalConsoleEncoding;

    // This dictionary maps each data type to their format string index.
    private readonly Dictionary<PrintDataType, byte> m_templateMap;

    private TextWriter? m_writer;
    private Stream? m_outputStream;
    private bool m_disposeOfStream;
    private ResultCollection? m_resultCollection;

    // IsUnicode is everywhere.
    internal bool IsUnicode { get; }
    internal bool IsPrintToFile => m_isPrintToFile && !m_isOutFileConsole;

    /// <summary>
    /// Constructs a <see cref="Printer"/> from the <see cref="CommandLineOptions"/>.
    /// </summary>
    /// <param name="options">The options.</param>
    internal Printer(CommandLineOptions options)
    {
        // The gist here is that we build a template string that contains the formatting
        // indexes for each thing we want to print.
        // E.g., let's suppose the user wants to print the file name, offset, and encoding.
        // Assuming the separator char is ',', the template string will look like this:
        //     "{0},{1},0x{2:X},0x{3:X},{4}"
        //
        // And the header template:
        //     "{0},{1},{2},{3},{4}"
        //
        // The last index is for the result string itself.
        // Then we call 'string.Format()' with the template string and the printer buffer.
        //
        // Every time we print we get the result, go through each index from the template map
        // and fill the printer buffer with the data.

        m_templateMap = [];
        m_options = options;

        byte formatIndex = 0;

        if (options.PrintFileName != PrintFileType.None) {
            m_resultTemplate = formatIndex == 0 ? m_resultTemplate = string.Concat(null, $"{{{formatIndex}}}")
                    : string.Join(options.DelimiterChar.ToString(), m_resultTemplate, $"{{{formatIndex}}}");

            m_templateMap.Add(PrintDataType.File, formatIndex++);
        }
        else if (options.ProcessInfoType != PrintProcessInfoType.None) {
            if ((options.ProcessInfoType & PrintProcessInfoType.ProcessId) > 0) {
                m_resultTemplate = formatIndex == 0 ? m_resultTemplate = string.Concat(null, $"{{{formatIndex}}}")
                    : string.Join(options.DelimiterChar.ToString(), m_resultTemplate, $"{{{formatIndex}}}");

                m_templateMap.Add(PrintDataType.ProcessId, formatIndex++);
            }

            if (options.ProcessInfoType != PrintProcessInfoType.ProcessName) {
                m_resultTemplate = formatIndex == 0 ? m_resultTemplate = string.Concat(null, $"{{{formatIndex}}}")
                    : string.Join(options.DelimiterChar.ToString(), m_resultTemplate, $"{{{formatIndex}}}");
                
                m_templateMap.Add(PrintDataType.ProcessName, formatIndex++);
            }

            if ((options.ProcessInfoType & PrintProcessInfoType.MemoryType) > 0) {
                m_resultTemplate = formatIndex == 0 ? m_resultTemplate = string.Concat(null, $"{{{formatIndex}}}")
                    : string.Join(options.DelimiterChar.ToString(), m_resultTemplate, $"{{{formatIndex}}}");

                m_templateMap.Add(PrintDataType.ProcessMemoryType, formatIndex++);
            }

            if ((options.ProcessInfoType & PrintProcessInfoType.Details) > 0) {
                m_resultTemplate = formatIndex == 0 ? m_resultTemplate = string.Concat(null, $"{{{formatIndex}}}")
                    : string.Join(options.DelimiterChar.ToString(), m_resultTemplate, $"{{{formatIndex}}}");

                m_templateMap.Add(PrintDataType.ProcessMemoryDetails, formatIndex++);
            }
        }

        if (options.PrintEncoding) {
            m_resultTemplate = formatIndex == 0 ? string.Concat(m_resultTemplate, $"{{{formatIndex}}}")
                : string.Join(options.DelimiterChar.ToString(), m_resultTemplate, $"{{{formatIndex}}}");

            m_templateMap.Add(PrintDataType.Encoding, formatIndex++);
        }

        if (options.PrintOffset) {
            m_resultTemplate = formatIndex == 0 ? string.Concat(m_resultTemplate, $"0x{{{formatIndex}:X}}{options.DelimiterChar}0x{{{formatIndex + 1}:X}}")
                : string.Join(options.DelimiterChar.ToString(), m_resultTemplate, $"0x{{{formatIndex}:X}}{options.DelimiterChar}0x{{{formatIndex + 1}:X}}");

            m_templateMap.Add(PrintDataType.OffsetStart, formatIndex++);
            m_templateMap.Add(PrintDataType.OffsetEnd, formatIndex++);
        }

        m_resultTemplate = formatIndex == 0 ? string.Concat(m_resultTemplate, $"{{{formatIndex}}}")
            : string.Join(options.DelimiterChar.ToString(), m_resultTemplate, $"{{{formatIndex}}}");

        m_templateMap.Add(PrintDataType.ResultString, formatIndex);

        m_headerTemplate = m_resultTemplate.Replace("0x", "").Replace(":X", "");
        m_buffer = new object[m_templateMap.Count];

        // Checking if we're printing to an output file.
        if (options.OutFile is not null) {

            // Checking if the file is the 'console'.
            if (options.OutFile.Equals("console", StringComparison.OrdinalIgnoreCase)) {
                if (options.OutputFileType.IsSimpleTextOutFile()) {

                    // If the output file is the console, but the output file type is 'PlainText' or 'Csv' the output
                    // will be the same as if we're not using an output file, but slower because we have an extra 'TextWriter' in between.
                    // So we default to printing to the console directly.
                    m_isPrintToFile = false;
                    m_isOutFileConsole = false;
                }
                else {
                    // We have a complex output format and the user wants to print the serialized data to the console.
                    m_isPrintToFile = true;
                    m_isOutFileConsole = true;
                }
            }
            else {
                // Straight to a file.
                m_isPrintToFile = true;
                m_isOutFileConsole = false;
            }
        }
            
        // Deciding which encode to use on this run.
        // Since UTF8 is compatible with ASCII, if Unicode is not chosen we default to UTF8.
        // If we have Unicode in the mix we default to Unicode.
        // This prevents us from having to change the console encoding multiple times while printing,
        // which is very expensive.
        Encoding finalEncoding;
        Encoding defaultEncoding = Encoding.Default;
        m_originalConsoleEncoding = WindowsConsole.OutputEncoding;
        byte[] headerBytes = defaultEncoding.GetBytes(m_headerTemplate);
        byte[] resultBytes = defaultEncoding.GetBytes(m_resultTemplate);
        if ((options.Encoding & ValidEncoding.Unicode) != 0) {
            finalEncoding = Encoding.Unicode;
            WindowsConsole.OutputEncoding = finalEncoding;
            this.IsUnicode = true;
        }
        else {
            finalEncoding = Encoding.UTF8;
            WindowsConsole.OutputEncoding = finalEncoding;
            this.IsUnicode = false;
        }

        // Encoding the header and template strings to the run encoding.
        m_headerTemplate = finalEncoding.GetString(Encoding.Convert(defaultEncoding, finalEncoding, headerBytes));
        m_resultTemplate = finalEncoding.GetString(Encoding.Convert(defaultEncoding, finalEncoding, resultBytes));

        // Setting the console buffer if any.
        if (options.ConsoleBufferSize > 0 && !IsPrintToFile)
            WindowsConsole.BufferSize = (int)options.ConsoleBufferSize;

        // Setting the console to use the 'ConDrv' API if requested.
        if (options.ConsoleUseDriver)
            WindowsConsole.UseDriver = true;
    }

    /// <summary>
    /// Disposes of unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Prints the header to the defined output.
    /// </summary>
    /// <exception cref="ArgumentException">Invalid <see cref="PrintDataType"/>.</exception>
    internal void PrintHeader()
    {
        // We don't print headers for the other file types.
        if (m_options.OutputFileType != OutputFileType.PlainText && m_options.OutputFileType != OutputFileType.Csv)
            return;

        // Filling the printer buffer.
        foreach (KeyValuePair<PrintDataType, byte> templateId in m_templateMap) {
            m_buffer[templateId.Value] = templateId.Key switch {
                PrintDataType.OffsetStart => "OffsetStart",
                PrintDataType.OffsetEnd => "OffsetEnd",
                PrintDataType.Encoding => "Encoding",
                PrintDataType.File => "File",
                PrintDataType.ProcessId => "Id",
                PrintDataType.ProcessMemoryType => "MemoryType",
                PrintDataType.ProcessMemoryDetails => "MemoryDetails",
                PrintDataType.ProcessName => "Name",
                PrintDataType.ResultString => "String",
                _ => throw new ArgumentException($"Invalid print data type '{templateId.Key}'.")
            };
        }

        // 'InitStream()' will take care of creating the writer to the correct stream.
        if (m_isPrintToFile) {
            if (m_writer is null)
                InitStream();

            m_writer.WriteLine(m_headerTemplate, m_buffer);
        }
        else {
            // Writing to the console.
            string res = string.Format(m_headerTemplate, m_buffer);
            WindowsConsole.WriteLine(res);
        }
    }

    /// <summary>
    /// Flushes the <see cref="ResultCollection"/> to the underlying stream.
    /// </summary>
    /// <exception cref="ArgumentException">Invalid options.</exception>
    internal void FlushToFile()
    {
        // There's nothing to flush.
        if (m_resultCollection is null)
            return;

        // 'InitStream()' will take care of creating the writer to the correct stream.
        if (m_writer is null || m_outputStream is null)
            InitStream();

        if (IsPrintToFile) {
            WindowsConsole.WriteLine("Serializing data to the output file...");
            WindowsConsole.Flush();
        }

        switch (m_options.OutputFileType) {
            
            // 'PlainText' and 'Csv' are exactly the same. Don't even know why we have both.
            // In this case we follow the same steps as if printing, fill the buffer with the data
            // and write to the stream.
            case OutputFileType.PlainText:
            case OutputFileType.Csv:
                foreach (Result result in m_resultCollection) {
                    if (result is FileResult fileResult) {
                        foreach (KeyValuePair<PrintDataType, byte> templateId in m_templateMap) {
                            m_buffer[templateId.Value] = templateId.Key switch {
                                PrintDataType.OffsetStart => fileResult.OffsetStart,
                                PrintDataType.OffsetEnd => fileResult.OffsetEnd,
                                PrintDataType.Encoding => fileResult.Encoding,
                                PrintDataType.ResultString => fileResult.ResultString,
                                PrintDataType.File => fileResult.File,
                                _ => throw new ArgumentException($"Invalid print data type '{templateId.Key}'.")
                            };
                        }
                    }
                    else if (result is ProcessResult processResult) {
                        foreach (KeyValuePair<PrintDataType, byte> templateId in m_templateMap) {
                            m_buffer[templateId.Value] = templateId.Key switch {
                                PrintDataType.OffsetStart => processResult.OffsetStart,
                                PrintDataType.OffsetEnd => processResult.OffsetEnd,
                                PrintDataType.Encoding => processResult.Encoding,
                                PrintDataType.ResultString => processResult.ResultString,
                                PrintDataType.ProcessId => processResult.ProcessId,
                                PrintDataType.ProcessName => processResult.Name,
                                PrintDataType.ProcessMemoryType => processResult.RegionType,
                                PrintDataType.ProcessMemoryDetails => processResult.Details,
                                _ => throw new ArgumentException($"Invalid print data type '{templateId.Key}'.")
                            };
                        }
                    }

                    m_writer.WriteLine(m_resultTemplate, m_buffer);
                }

                break;

            // 'Xml' and 'Json' are handled with their respective serializer.
            case OutputFileType.Xml:
                ResultCollectionXmlSerializer.Serialize(m_resultCollection, m_writer);
                break;

            case OutputFileType.Json:
                JsonExtensions.SerializeResultCollection(m_resultCollection, m_outputStream);
                break;

            default:
                throw new ArgumentException($"Invalid output file type '{m_options.OutputFileType}'.");
        }
    }

    /// <summary>
    /// Prints the <see cref="Result"/> to the console or caches to print to a stream.
    /// </summary>
    /// <param name="result">The <see cref="Result"/>.</param>
    /// <exception cref="ArgumentException">Invalid <see cref="PrintDataType"/>.</exception>
    internal void Print(Result result)
    {
        // If we're printing to a file or formatted data to the console we just cache the result.
        if (m_isPrintToFile) {
            m_resultCollection ??= new();
            m_resultCollection.Add(result);

            return;
        }

        // Filling the printer buffer.
        if (result is FileResult fileResult) {
            foreach (KeyValuePair<PrintDataType, byte> templateId in m_templateMap) {
                m_buffer[templateId.Value] = templateId.Key switch {
                    PrintDataType.OffsetStart => fileResult.OffsetStart,
                    PrintDataType.OffsetEnd => fileResult.OffsetEnd,
                    PrintDataType.Encoding => fileResult.Encoding,
                    PrintDataType.ResultString => fileResult.ResultString,
                    PrintDataType.File => fileResult.File,
                    _ => throw new ArgumentException($"Invalid print data type '{templateId.Key}'.")
                };
            }
        }
        else if (result is ProcessResult processResult) {
            foreach (KeyValuePair<PrintDataType, byte> templateId in m_templateMap) {
                m_buffer[templateId.Value] = templateId.Key switch {
                    PrintDataType.OffsetStart => processResult.OffsetStart,
                    PrintDataType.OffsetEnd => processResult.OffsetEnd,
                    PrintDataType.Encoding => processResult.Encoding,
                    PrintDataType.ResultString => processResult.ResultString,
                    PrintDataType.ProcessId => processResult.ProcessId,
                    PrintDataType.ProcessName => processResult.Name,
                    PrintDataType.ProcessMemoryType => processResult.RegionType,
                    PrintDataType.ProcessMemoryDetails => processResult.Details,
                    _ => throw new ArgumentException($"Invalid print data type '{templateId.Key}'.")
                };
            }
        }

        // Here's where the magic happens.
        // The two costlier operations are decoding and formatting/printing to the console.
        string res = string.Format(m_resultTemplate, m_buffer);
        WindowsConsole.WriteLine(res);
    }

    /// <summary>
    /// Instantiate the output stream and writer according to the options.
    /// </summary>
    /// <exception cref="ArgumentException">Output file can't be null</exception>
    [MemberNotNull(nameof(m_writer))]
    [MemberNotNull(nameof(m_outputStream))]
    private void InitStream()
    {
        if (m_options.OutFile is null)
            throw new ArgumentException("Output file cannot be null.");

        // If we're printing formatted data to the console we wrap the console stream with a sync. text writer.
        if (m_isOutFileConsole) {
            m_outputStream = WindowsConsole.Out;
            m_writer = TextWriter.Synchronized(new StreamWriter(WindowsConsole.Out, WindowsConsole.OutputEncoding, WindowsConsole.BufferSize, leaveOpen: true));
            m_disposeOfStream = false;
        }

        // If we're printing to a file we open the file and create a sync. text writer.
        else {
            m_outputStream = new FileStream(m_options.OutFile, FileMode.Create, FileAccess.ReadWrite, FileShare.Read, 1048576, FileOptions.SequentialScan);
            m_writer = TextWriter.Synchronized(new StreamWriter(m_outputStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true), 1048576, leaveOpen: true));
            m_disposeOfStream = true;
        }
    }

    /// <summary>
    /// Flushes the console, restores the original encoding, flushes
    /// to the file, if applicable, and disposes of the streams.
    /// </summary>
    /// <param name="disposing">True to dispose of unmanaged resources.</param>
    private void Dispose(bool disposing)
    {
        if (disposing) {
            
            // Flushing the console and restoring the original encoding.
            WindowsConsole.Flush();
            WindowsConsole.OutputEncoding = m_originalConsoleEncoding;

            // Flush to file if needed.
            if (m_isPrintToFile)
                FlushToFile();

            // Disposing of the writer.
            if (m_writer is not null) {
                m_writer.Dispose();
                m_writer = null;
            }

            // Disposing of the stream if needed.
            if (m_outputStream is not null && m_disposeOfStream) {
                m_outputStream.Dispose();
                m_outputStream = null;
            }
        }
    }
}