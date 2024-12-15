// This file is part of the ManagedStrings project and repository.
// Project files are licensed under the MIT license.
// https://github.com/FranciscoNabas/ManagedStrings

using System.IO;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System;
using System.Linq;

namespace ManagedStrings.Engine;

/// <summary>
/// A result collection. Used to store results and serialize them.
/// </summary>
public sealed class ResultCollection
{
    private readonly List<Result> m_list = [];
    
    internal int Count => m_list.Count;

    internal Result this[int index] {
        get => m_list[index];
        set => m_list[index] = value;
    }

    internal void Add(Result item)
        => m_list.Add(item);
    
    public List<Result>.Enumerator GetEnumerator()
        => m_list.GetEnumerator();
}

/// <summary>
/// The base class for all the results.
/// </summary>
/// <param name="encoding">The result encoding.</param>
/// <param name="offsetStart">The result offset start.</param>
/// <param name="offsetEnd">The result offset end.</param>
/// <param name="result">The result string.</param>
public abstract partial class Result(ValidEncoding encoding, long offsetStart, long offsetEnd, string result)
{
    public ValidEncoding Encoding { get; set; } = encoding;
    public long OffsetStart { get; set; } = offsetStart;
    public long OffsetEnd { get; set; } = offsetEnd;
    public string ResultString { get; set; } = result;
}

/// <summary>
/// A file result.
/// </summary>
public sealed partial class FileResult : Result
{
    public string File { get; set; }

    public FileResult()
        : base(ValidEncoding.ASCII, 0L, 0L, string.Empty)
            => File = string.Empty;

    public FileResult(string file, ValidEncoding encoding, long offsetStart, long offsetEnd, string result)
        : base(encoding, offsetStart, offsetEnd, result)
            => File = file;
}

/// <summary>
/// A process result.
/// </summary>
public sealed partial class ProcessResult : Result
{
    private readonly string m_imagePath;

    public uint ProcessId { get; set; }
    public string Name { get; set; }
    public MemoryRegionType RegionType { get; set; }
    public string Details { get; set; }

    public ProcessResult()
        : base(ValidEncoding.ASCII, 0L, 0L, string.Empty)
            => (ProcessId, Name, RegionType, Details, m_imagePath) = (0, string.Empty, MemoryRegionType.Unknown, string.Empty, string.Empty);

    internal ProcessResult(ProcessStreamOffsetInfo offsetInfo, ValidEncoding encoding, int bytesRead, string result)
        : base(encoding, offsetInfo.RelativeVa, offsetInfo.RelativeVa + bytesRead, result)
            => (ProcessId, Name, RegionType, Details, m_imagePath) = (offsetInfo.ProcessId, Path.GetFileName(offsetInfo.Image), offsetInfo.RegionType, offsetInfo.Details.Value, offsetInfo.Image);

    // In case we need in the future. Don't want to serialize this.
    internal string GetImagePath() => m_imagePath;
}