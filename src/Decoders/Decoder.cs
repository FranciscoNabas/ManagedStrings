// This file is part of the ManagedStrings project and repository.
// Project files are licensed under the MIT license.
// https://github.com/FranciscoNabas/ManagedStrings

using System.Threading;
using ManagedStrings.Engine;
using System.Diagnostics.CodeAnalysis;

namespace ManagedStrings.Decoders;

/// <summary>
/// The base class for all decoders.
/// </summary>
internal abstract class Decoder
{
    /// <summary>
    /// Attempts to get a string from the buffer in the determined offset.
    /// </summary>
    /// <param name="buffer">The buffer containing the bytes to decode.</param>
    /// <param name="bufferLength">The total length of the buffer.</param>
    /// <param name="decodeInformation">Information containing offsets, bytes read, and options.</param>
    /// <param name="value">The output string.</param>
    /// <param name="currentBytesRead">The number of bytes read.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if we parsed a string bigger than the minimum string length.</returns>
    internal abstract unsafe bool TryGetString(byte* buffer, int bufferLength, DecodeInformation decodeInformation,
        [NotNullWhen(true)] out string? value, out int currentBytesRead, CancellationToken cancellationToken);
}

/// <summary>
/// Contains information about the decoding operation.
/// </summary>
/// <param name="minLength">The minimum string length.</param>
/// <param name="offset">The starting offset for the decoding.</param>
/// <param name="escapeCCPs">Indicates whether we escape control code points. For more info see <see cref="CharInfo.IsPrintableASCII(byte, bool)"/>.</param>
/// <param name="isUnicode">Indicates if we should use Unicode encoding.</param>
/// <param name="relOffset">The relative offset for a decoding buffer.</param>
/// <param name="encoding">The operation's <see cref="ValidEncoding"/>.</param>
/// <param name="decoder">The <see cref="Decoder"/>.</param>
internal sealed class DecodeInformation(uint minLength, int offset, bool escapeCCPs, bool isUnicode,
    long relOffset, ValidEncoding encoding, Decoder decoder)
{
    private long m_relativeOffset = relOffset;

    internal ref long RelativeOffset => ref m_relativeOffset;

    internal uint MinStringLength { get; } = minLength;
    internal int Offset { get; set; } = offset;
    internal int BytesRead { get; set; } = 0;
    internal bool EscapeControlCodePoints { get; } = escapeCCPs;
    internal bool IsUnicode { get; } = isUnicode;

    internal ValidEncoding Encoding { get; } = encoding;
    internal Decoder Decoder { get; } = decoder;
    internal bool IsRunning { get; set; } = true;
}