// This file is part of the ManagedStrings project and repository.
// Project files are licensed under the MIT license.
// https://github.com/FranciscoNabas/ManagedStrings

using System;
using System.Text;
using System.Threading;
using System.Diagnostics.CodeAnalysis;

namespace ManagedStrings.Decoders;

/// <summary>
/// A decoder to decode Unicode characters from sequences of bytes.
/// </summary>
/// <param name="blocks">The <see cref="UnicodeBlocks"/> to be used by the decoder.</param>
internal sealed class UnicodeDecoder(UnicodeBlocks blocks) : Decoder
{
    private readonly UnicodeBlocks m_blocks = blocks;
    private readonly Encoding m_unicode = Encoding.Unicode;

    /// <summary>
    /// Attempts to get a string from the buffer in the determined offset.
    /// </summary>
    /// <param name="buffer">The buffer containing the bytes to decode.</param>
    /// <param name="bufferLength">The total length of the buffer.</param>
    /// <param name="decodeInformation">Information containing offsets, bytes read, and options.</param>
    /// <param name="value">The output string.</param>
    /// <param name="currentBytesRead">The number of bytes read.</param>
    /// <param name="currentStringBytesRead">The number of valid bytes read.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if we parsed a string bigger than the minimum string length.</returns>
    internal override unsafe bool TryGetString(byte* buffer, int bufferLength, DecodeInformation decodeInformation,
        [NotNullWhen(true)] out string? value, out int currentBytesRead, out int currentStringBytesRead, CancellationToken cancellationToken)
    {
        value = null;
        UnicodeBlock? runBlock = null;
        int initialPosition = decodeInformation.Offset;

        // Caching the options to avoid having to get them from the object in the loop.
        // Not sure if strictly necessary, but when this was .NET Framework 4.7.2 we had a performance increase.
        int spanLen = bufferLength;
        int offset = initialPosition;
        bool escapeCCPs = decodeInformation.EscapeControlCodePoints;
        for (;;) {
            cancellationToken.ThrowIfCancellationRequested();

            if (offset + 2 > spanLen) {
                
                // We don't have enough bytes left in the buffer.
                // Setting the offset to the buffer length so we don keep stuck in an infinite loop.
                offset = bufferLength;
                break;
            }

            // Since we only parse characters from the Unicode Basic Plane, all characters are two bytes long.
            byte firstByte = buffer[offset++];
            byte secondByte = buffer[offset++];
            
            // Checking endianness.
            char currentChar = '\0';
            if (BitConverter.IsLittleEndian) {
                
                // Checking if we have a possible ASCII character.
                if (secondByte == 0x00 && firstByte <= 0x80) {
                    if (firstByte.IsPrintableASCII(escapeCCPs)) {
                        continue;
                    }

                    break;
                }

                // Building the character from the bytes.
                currentChar |= (char)(secondByte << 8);
                currentChar |= (char)firstByte;
            }
            else {
                // Checking if we have a possible ASCII character.
                if (secondByte <= 0x80 && firstByte == 0x00) {
                    if (firstByte.IsPrintableASCII(escapeCCPs)) {
                        continue;
                    }

                    break;
                }

                // Building the character from the bytes.
                currentChar |= (char)(firstByte << 8);
                currentChar |= (char)secondByte;
            }

            // If the character category is 'OtherNotAssigned', or it's not in any of our ranges this returns false.
            if (!currentChar.TryGetUnicodeBlock(out UnicodeBlock? block))
                break;
            
            // Break if it's not in the chosen groups.
            if (!m_blocks.HasBlock(block))
                break;

            if (runBlock is null) {
                runBlock = block;
            }
            else {
                // Checking if the current group is compatible with the previous one.
                // I.e., they are in the same group or if they are 'BasicLatin' + 'LatinExtensions'.
                if (block == UnicodeBlocks.BasicLatin || block == UnicodeBlocks.LatinExtensions) {
                    if (runBlock != UnicodeBlocks.BasicLatin && runBlock != UnicodeBlocks.LatinExtensions)
                        break;
                }
                else {
                    // Breaking if the groups are not compatible.
                    if (block != runBlock)
                        break;
                }
            }
        }

        currentBytesRead = offset - initialPosition;
        currentStringBytesRead = currentBytesRead - 2;

        // Checking if we parsed a string bigger than the minimum string length.
        decodeInformation.Offset = offset;
        if (currentBytesRead / UnicodeEncoding.CharSize >= decodeInformation.MinStringLength) {
            ReadOnlySpan<byte> slice = new(buffer + initialPosition, currentStringBytesRead);
            value = m_unicode.GetString([.. slice]);

            return true;
        }

        return false;
    }
}