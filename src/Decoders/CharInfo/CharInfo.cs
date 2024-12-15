// This file is part of the ManagedStrings project and repository.
// Project files are licensed under the MIT license.
// https://github.com/FranciscoNabas/ManagedStrings

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace ManagedStrings.Decoders;

/// <summary>
/// Contains methods to categorize character code points.
/// </summary>
internal static partial class CharInfo
{
    /// <summary>
    /// Determines if an ASCII character byte is printable.
    /// </summary>
    /// <param name="b">The character byte.</param>
    /// <param name="excludeControlCodePoints">Exclude the control code points 'HT', 'LF', and 'CR'.</param>
    /// <returns>True if the character is printable</returns>
    /// <seealso cref="PrintableASCIIMap"/>
    /// <seealso cref="PrintableASCIIMapNoControl"/>
    internal static bool IsPrintableASCII(this byte b, bool excludeControlCodePoints)
        => excludeControlCodePoints ? PrintableASCIIMapNoControl[b] : PrintableASCIIMap[b];

    /// <summary>
    /// Gets the <see cref="UnicodeBlock"/> for a character.
    /// </summary>
    /// <param name="c">The character to get the block.</param>
    /// <param name="block">The output <see cref="UnicodeBlock"/> or null if not found.</param>
    /// <returns>True if the character falls within the range of a <see cref="UnicodeBlock"/>.</returns>
    /// <seealso cref="UnicodeBlockCodePointMap"/>
    /// <seealso cref="UnicodeBlocks"/>
    internal static bool TryGetUnicodeBlock(this char c, [NotNullWhen(true)] out UnicodeBlock? block)
    {
        block = null;

        // Getting the bit index for this char from the map.
        byte bitIndex = Unsafe.AddByteOffset(ref MemoryMarshal.GetReference(UnicodeBlockCodePointMap), c);
        
        // 0xFF represents characters that don fall within a block range, or is categorized as 'OtherNotAssigned'.
        if (bitIndex == 0xFF)
            return false;

        block = UnicodeBlocks.GetBlock(bitIndex);

        return true;
    }
}