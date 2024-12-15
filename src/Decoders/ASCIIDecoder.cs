// This file is part of the ManagedStrings project and repository.
// Project files are licensed under the MIT license.
// https://github.com/FranciscoNabas/ManagedStrings

using System;
using System.Text;
using System.Threading;
using System.Diagnostics.CodeAnalysis;

namespace ManagedStrings.Decoders;

/// <summary>
/// A decoder to decode ASCII characters from sequences of bytes.
/// </summary>
internal sealed class ASCIIDecoder : Decoder
{
    // Caching the encodings.
    // If the user choose Unicode we default to it in all decoders.
    private readonly Encoding m_ascii = Encoding.ASCII;
    private readonly Encoding m_unicode = Encoding.Unicode;

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
    internal override unsafe bool TryGetString(byte* buffer, int bufferLength, DecodeInformation decodeInformation,
        [NotNullWhen(true)] out string? value, out int currentBytesRead, CancellationToken cancellationToken)
    {
        value = null;

        // Caching the starting offset.
        int initialPosition = decodeInformation.Offset;
        
        // Caching the options to avoid having to get them from the object in the loop.
        // Not sure if strictly necessary, but when this was .NET Framework 4.7.2 we had a performance increase.
        int offset = initialPosition;
        bool escapeCCPs = decodeInformation.EscapeControlCodePoints;

        // The main loop.
        // Since ASCII characters are one byte we just go byte by byte
        // checking if they are printable.
        for (;;) {
            cancellationToken.ThrowIfCancellationRequested();

            if (offset >= bufferLength)
                break;

            byte currentByte = buffer[offset++];
            if (!currentByte.IsPrintableASCII(escapeCCPs))
                break;
        }

        currentBytesRead = offset - initialPosition;

        // Checking if we parsed a string bigger than the minimum string length.
        decodeInformation.Offset = offset;
        if (currentBytesRead >= decodeInformation.MinStringLength) {
            ReadOnlySpan<byte> slice = new(buffer + initialPosition, currentBytesRead - 1);
            if (decodeInformation.IsUnicode)
                value = m_unicode.GetString(Encoding.Convert(m_ascii, m_unicode, [.. slice]));
            else
                value = m_ascii.GetString([.. slice]);

            return true;
        }

        return false;
    }
}