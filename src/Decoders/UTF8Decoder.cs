// This file is part of the ManagedStrings project and repository.
// Project files are licensed under the MIT license.
// https://github.com/FranciscoNabas/ManagedStrings

using System;
using System.Text;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace ManagedStrings.Decoders;

/// <summary>
/// A decoder to decode UTF8 characters from sequences of bytes.
/// </summary>
/// <param name="blocks">The <see cref="UnicodeBlocks"/> to be used by the decoder.</param>
internal sealed class UTF8Decoder(UnicodeBlocks blocks) : Decoder
{
    private readonly UnicodeBlocks m_blocks = blocks;
    private readonly Encoding m_utf8 = Encoding.UTF8;
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
        int currentCharsRead = 0;
        UnicodeBlock? runBlock = null;
        int initialPosition = decodeInformation.Offset;

        // This is used in case we have a valid string so we can remove the last
        // invalid bytes, since UTF8 has variable byte length characters.
        int lastCharByteCount;
        
        // Caching the options to avoid having to get them from the object in the loop.
        // Not sure if strictly necessary, but when this was .NET Framework 4.7.2 we had a performance increase.
        int offset = initialPosition;
        bool escapeCCPs = decodeInformation.EscapeControlCodePoints;
        for (;;) {
            cancellationToken.ThrowIfCancellationRequested();

            lastCharByteCount = 0;
            if (offset >= bufferLength)
                break;

            // First byte analysis.
            byte firstByte = buffer[offset++];
            lastCharByteCount = 1;
            if (firstByte < 0x80) {
                
                // We are within the ASCII range. Checking if it's printable.
                if (firstByte.IsPrintableASCII(escapeCCPs)) {
                    currentCharsRead++;
                    continue;
                }

                break;
            }

            // Continuation bytes cannot appear at the start of a character (0x80..0xBF).
            // Here we also check for bytes that never appear on UTF8 (0xC0, 0xC1, 0xF5..0xFF).
            // https://wikipedia.org/wiki/UTF-8#Byte_map
            // https://wikipedia.org/wiki/UTF-8#Error_handling
            if (firstByte < 0xC2 || firstByte > 0xF4) {
                break;
            }

            // Two-byte characters.
            char currentChar ;
            if (firstByte < 0xE0) {
                if (offset >= bufferLength)
                    break;

                lastCharByteCount++;

                // Attempting to transcode the bytes into a UTF16 character.
                if (!TryTranscodeTwoBytesToUTF16(firstByte, buffer[offset++], out currentChar))
                    break;

                // If the character category is 'OtherNotAssigned', or it's not in any of our ranges this returns false.
                if (!IsIncludableUnicode(currentChar, ref runBlock))
                    break;

                currentCharsRead++;

                continue;
            }

            // Three-byte characters.
            if (firstByte < 0xF0) {
                if (offset + 1 >= bufferLength)
                    break;

                lastCharByteCount += 2;

                // Attempting to transcode the bytes into a UTF16 character.
                if (!TryTranscodeThreeBytesToUTF16(firstByte, buffer[offset++], buffer[offset++], out currentChar))
                    break;

                // If the character category is 'OtherNotAssigned', or it's not in any of our ranges this returns false.
                if (!IsIncludableUnicode(currentChar, ref runBlock))
                    break;

                currentCharsRead++;

                continue;
            }

            // Four-byte characters are not processed because they are out of the Basic Multilingual Plane (BMP).
            // 0x010000..0x10FFFF.
            else {
                break;
            }
        }

        currentBytesRead = offset - initialPosition;

        // Checking if we parsed a string bigger than the minimum string length.
        decodeInformation.Offset = offset;
        if (currentCharsRead >= decodeInformation.MinStringLength) {
            ReadOnlySpan<byte> slice = new(buffer + initialPosition, currentBytesRead - lastCharByteCount);
            if (decodeInformation.IsUnicode)
                value = m_unicode.GetString(Encoding.Convert(m_utf8, m_unicode, [.. slice]));
            else
                value = m_utf8.GetString([.. slice]);

            return true;
        }
        
        return false;
    }

    /// <summary>
    /// Checks if a character is valid within the decoder's context.
    /// </summary>
    /// <param name="c">The character.</param>
    /// <param name="runBlock">The <see cref="UnicodeBlock"/> for the current run.</param>
    /// <returns>True if the character can be included in the output string.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsIncludableUnicode(char c, ref UnicodeBlock? runBlock)
    {
        // If the character category is 'OtherNotAssigned', or it's not in any of our ranges this returns false.
        if (!c.TryGetUnicodeBlock(out UnicodeBlock? block))
            return false;

        // Break if it's not in the chosen groups.
        if (!m_blocks.HasBlock(block))
            return false;

        if (runBlock is null) {
            runBlock = block;
            return true;
        }

        // Checking if the current group is compatible with the previous one.
        // I.e., they are in the same group or if they are 'BasicLatin' + 'LatinExtensions'.
        if (block == UnicodeBlocks.BasicLatin || block == UnicodeBlocks.LatinExtensions) {
            if (runBlock != UnicodeBlocks.BasicLatin && runBlock != UnicodeBlocks.LatinExtensions)
                return false;
        }
        else {
            // Breaking if the groups are not compatible.
            if (block != runBlock)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Attempts to transcode two bytes into a UTF16 character.
    /// </summary>
    /// <param name="firstByte">The first byte.</param>
    /// <param name="secondByte">The second byte.</param>
    /// <param name="unicodeChar">The output character.</param>
    /// <returns>True if we successfully transcoded the bytes.</returns>
    /// <runtimefile>src/libraries/System.Private.CoreLib/src/System/Text/Unicode/Utf8Utility.Transcoding.cs</runtimefile>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryTranscodeTwoBytesToUTF16(byte firstByte, byte secondByte, out char unicodeChar)
    {
        unicodeChar = '\0';
        uint firstByteScalar = firstByte - 0xC2u;
        if ((byte)firstByteScalar > (0xDFu - 0xC2u))
            return false;

        uint secondByteScalar = secondByte;
        
        // 2-byte marker not followed by continuation byte.
        if (!IsLowByteUTF8ContinuationByte(secondByteScalar))
            return false;

        // Remove UTF-8 markers from scalar.
        uint asChar = (firstByteScalar << 6) + secondByteScalar + ((0xC2u - 0xC0u) << 6) - 0x80u;
        unicodeChar = (char)asChar;

        return true;
    }

    /// <summary>
    /// Attempts to transcode three bytes into a UTF16 character.
    /// </summary>
    /// <param name="firstByte">The first byte.</param>
    /// <param name="secondByte">The second byte.</param>
    /// <param name="thirdByte">The third byte.</param>
    /// <param name="unicodeChar">THe output character.</param>
    /// <returns>True if we successfully transcoded the bytes.</returns>
    /// <runtimefile>src/libraries/System.Private.CoreLib/src/System/Text/Unicode/Utf8Utility.Transcoding.cs</runtimefile>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryTranscodeThreeBytesToUTF16(byte firstByte, byte secondByte, byte thirdByte, out char unicodeChar)
    {
        unicodeChar = '\0';
        uint firstByteScalar = firstByte - 0xC2u;
        if ((byte)firstByteScalar > (0xEFu - 0xC2u))
            return false;

        uint secondByteScalar = secondByte;
        uint thirdByteScalar = thirdByte;

        // 3-byte marker not followed by 2 continuation bytes.
        if (!IsLowByteUTF8ContinuationByte(secondByteScalar) || !IsLowByteUTF8ContinuationByte(thirdByteScalar))
            return false;

        uint partialChar = (firstByteScalar << 12) + (secondByteScalar << 6);

        // This is an overlong encoding; fail.
        if (partialChar < ((0xE0u - 0xC2u) << 12) + (0xA0u << 6))
            return false;

        // If partialChar = 0, we're at beginning of UTF-16 surrogate code point range.
        partialChar -= ((0xEDu - 0xC2u) << 12) + (0xA0u << 6);

        // Attempted to encode a UTF-16 surrogate code point; fail.
        if (partialChar < 0x0800u)
            return false;

        // Restore the full scalar value.
        partialChar += thirdByte;
        partialChar += 0xD800; // Undo "move to beginning of UTF-16 surrogate code point range" from earlier, fold it with later adds.
        partialChar -= 0x80u; // Remove third byte continuation marker.

        unicodeChar = (char)partialChar;

        return true;
    }

    /// <summary>
    /// Checks if a byte is a UTF8 continuation byte.
    /// </summary>
    /// <param name="value">The scalar byte.</param>
    /// <returns>True if it's a continuation byte.</returns>
    /// <runtimefile>src/libraries/System.Private.CoreLib/src/System/Text/Unicode/Utf8Utility.Helpers.cs</runtimefile>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsLowByteUTF8ContinuationByte(uint value)
        => (byte)(value - 0x80u) <= 0x3Fu;
}