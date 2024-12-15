<#
.SYNOPSIS

    Generates objects and types for categorizing characters.

.DESCRIPTION

    This script generates the types and objects neccessary to categorize characters in the ManagedStrings application.
    The itens include:
        - UnicodeBlocks: A enumeration containing the unicode blocks. This will be used to categorize a character.
        - PrintableASCIIMap: A 'ReadOnlySpan<bool>' containing the printable ASCII characters.
        - PrintableASCIIMapNoControl: A 'ReadOnlySpan<bool>' containing the printable ASCII characters, excluding HT (horizontal tabulation), LF (line feed), and CR (carriage return).
        - UnicodeBlockCodePointMap: A 'ReadOnlySpan<uint>' containing the Unicode block map per code point.

    This script uses the 'System.Text.Unicode.UnicodeRanges' type to generate the blocks enum, and the block code point map.

.PARAMETER Destination

    A destination directory. If the files already exists will be overwritten.

.EXAMPLE

    .\GetCharInfo.ps1 -Destination "$env:SystemDrive\MyProject\src"

.NOTES

    This file is part of the ManagedStrings project and repository.
    Project files are licensed under the MIT license.

.LINK

    https://github.com/FranciscoNabas/ManagedStrings
#>

# I don't like using 'using namespace' that often, but it makes the range map cleaner and easier to write.
using namespace System.Text.Unicode

[CmdletBinding()]
param (
    # The destination file. When used it needs to point to a file in a existing directory.
    # If the file exists it gets overwritten. If no input is provided we print the result to the console.
    [Parameter(
        Mandatory,
        Position = 0
    )]
    [ValidateNotNullOrEmpty()]
    [string]$Destination
)

# This script was not tested in previous PowerShell versions or editions.
#Requires -Version 7.4


#
# This function generates the 'ReadOnlySpan<bool>' body for the 'PrintableASCIIMap's.
# The logic only considers printable a non-control character, with the exception of HT, LF, and CR.
# If 'ExcludeControlCodePoints' is used these previous characters are excluded.
# Since the ASCII range goes from 0x00 to 0x7F we create a span big enough to index any byte value (256 values).
# The function terminates the span with '];'.
#
# Span body (with HT, LF, and CR):
#
# /*           0      1      2      3          4      5      6      7          8      9      A      B          C      D      E      F   */
# /* 0x00 */ false, false, false, false,     false, false, false, false,     false, true , true , false,     false, true , false, false,
# /* 0x10 */ false, false, false, false,     false, false, false, false,     false, false, false, false,     false, false, false, false,
# /* 0x20 */ true , true , true , true ,     true , true , true , true ,     true , true , true , true ,     true , true , true , true ,
# /* 0x30 */ true , true , true , true ,     true , true , true , true ,     true , true , true , true ,     true , true , true , true ,
# /* 0x40 */ true , true , true , true ,     true , true , true , true ,     true , true , true , true ,     true , true , true , true ,
# /* 0x50 */ true , true , true , true ,     true , true , true , true ,     true , true , true , true ,     true , true , true , true ,
# /* 0x60 */ true , true , true , true ,     true , true , true , true ,     true , true , true , true ,     true , true , true , true ,
# /* 0x70 */ true , true , true , true ,     true , true , true , true ,     true , true , true , true ,     true , true , true , false,
# /* 0x80 */ false, false, false, false,     false, false, false, false,     false, false, false, false,     false, false, false, false,
# /* 0x90 */ false, false, false, false,     false, false, false, false,     false, false, false, false,     false, false, false, false,
# /* 0xA0 */ false, false, false, false,     false, false, false, false,     false, false, false, false,     false, false, false, false,
# /* 0xB0 */ false, false, false, false,     false, false, false, false,     false, false, false, false,     false, false, false, false,
# /* 0xC0 */ false, false, false, false,     false, false, false, false,     false, false, false, false,     false, false, false, false,
# /* 0xD0 */ false, false, false, false,     false, false, false, false,     false, false, false, false,     false, false, false, false,
# /* 0xE0 */ false, false, false, false,     false, false, false, false,     false, false, false, false,     false, false, false, false,
# /* 0xF0 */ false, false, false, false,     false, false, false, false,     false, false, false, false,     false, false, false, false
#
function Invoke-PrintableASCIIMapWrite {

    param(
        [System.IO.StreamWriter]$Writer, # The TextWriter.
        [switch]$ExcludeControlCodePoints   # Switch to exclude HT, LF, and CR.
    )

    # The range of printable ASCII characters excluding HT, LF, and CR.
    $printableRangeNoCCPs = [Tuple[byte, byte]]::new(0x20, 0x7E)

    # We use a 'System.Text.StringBuilder' so we can trim the spaces at the end of
    # each line, and trim the last comma before writing to the stream.
    $commentIndex = 0
    $sb = [System.Text.StringBuilder]::new()
    [void]$sb.AppendLine("$Script:innerIndent/*           0      1      2      3          4      5      6      7          8      9      A      B          C      D      E      F   */")
    for ($byte = 0; $byte -lt [byte]::MaxValue; ) {
        
        # Writing a comment with the current line offset.
        [void]$sb.Append(("$Script:innerIndent/* 0x{0:X2} */ " -f $commentIndex))
        
        # Writing lines with 16 values in each one.
        :Column for ($columnIndex = 0; $columnIndex -lt 16; ) {
        
            # Since this span is not big we divide the 16 values of each line in 4 columns
            # with 4 spaces between them.
            for ($blockIndex = 0; $blockIndex -lt 4; $blockIndex++) {
                if ($byte -gt [byte]::MaxValue) {
                    break Column
                }
                
                # Checking if the current byte is HT, LF, or CR, and writing its value according to the 'ExcludeControlCodePoints' switch.
                if ($byte -eq 0x09 -or $byte -eq 0x0A -or $byte -eq 0x0D) {
                    if ($ExcludeControlCodePoints) {
                        [void]$sb.Append("false, ")
                    }
                    else {
                        [void]$sb.Append("true , ")
                    }

                    $columnIndex++
                    $byte++

                    continue
                }

                # Printing the current byte boolean value.
                if ($byte -ge $printableRangeNoCCPs.Item1 -and $byte -le $printableRangeNoCCPs.Item2) {
                    [void]$sb.Append("true , ")
                }
                else {
                    [void]$sb.Append("false, ")
                }

                $columnIndex++
                $byte++
            }

            [void]$sb.Append($Script:indent)
        }

        # Cleaning up the end of the line.
        [void]$sb.Remove($sb.Length - 5, 5)
        [void]$sb.AppendLine()
        $commentIndex += 0x10
    }

    # Removing the last comma and terminating the span.
    [void]$sb.Remove($sb.Length - 3, 1)
    [void]$sb.AppendLine("$($Script:indent)];")
    $Writer.Write($sb.ToString())
}

#region StreamInit

# It creates a file stream to 'CharInfo.Data.cs' and one to 'UnicodeBlocks.Data.cs'
$charInfoWriter = [System.IO.StreamWriter]::new([System.IO.Path]::Combine($Destination, 'CharInfo.Data.cs'))
$unicodeBlocksWriter = [System.IO.StreamWriter]::new([System.IO.Path]::Combine($Destination, 'UnicodeBlocks.Data.cs'))

#endregion

try {
    #region Variables
    
    # This is the map that holds the Unicode blocks and their respective range.
    # Due to the large number of groups some of them were combined, like the Latin Extensions.
    # A combined block contains multiple 'System.Text.Unicode.UnicodeRange's.
    $rangesMap = [System.Collections.Generic.Dictionary[string, UnicodeRange[]]]::new()
    $rangesMap.Add('BasicLatin', @([UnicodeRanges]::BasicLatin))
    $rangesMap.Add('LatinExtensions', @(
        [UnicodeRanges]::Latin1Supplement
        [UnicodeRanges]::LatinExtendedA
        [UnicodeRanges]::LatinExtendedAdditional
        [UnicodeRanges]::LatinExtendedB
        [UnicodeRanges]::LatinExtendedC
        [UnicodeRanges]::LatinExtendedD
        [UnicodeRanges]::LatinExtendedE
    ))
    $rangesMap.Add('AlphabeticPresentationForms', @([UnicodeRanges]::AlphabeticPresentationForms))
    $rangesMap.Add('Arabic', @(
        [UnicodeRanges]::Arabic
        [UnicodeRanges]::ArabicExtendedA
        [UnicodeRanges]::ArabicExtendedB
        [UnicodeRanges]::ArabicPresentationFormsA
        [UnicodeRanges]::ArabicPresentationFormsB
        [UnicodeRanges]::ArabicSupplement
    ))
    $rangesMap.Add('Armenian', @([UnicodeRanges]::Armenian))
    $rangesMap.Add('Arrows', @([UnicodeRanges]::Arrows))
    $rangesMap.Add('Balinese', @([UnicodeRanges]::Balinese))
    $rangesMap.Add('Bamum', @([UnicodeRanges]::Bamum))
    $rangesMap.Add('Batak', @([UnicodeRanges]::Batak))
    $rangesMap.Add('Bengali', @([UnicodeRanges]::Bengali))
    $rangesMap.Add('BlockElements', @([UnicodeRanges]::BlockElements))
    $rangesMap.Add('Bopomofo', @(
        [UnicodeRanges]::Bopomofo
        [UnicodeRanges]::BopomofoExtended
    ))
    $rangesMap.Add('BoxDrawing', @([UnicodeRanges]::BoxDrawing))
    $rangesMap.Add('BraillePatterns', @([UnicodeRanges]::BraillePatterns))
    $rangesMap.Add('Buginese', @([UnicodeRanges]::Buginese))
    $rangesMap.Add('Buhid', @([UnicodeRanges]::Buhid))
    $rangesMap.Add('Cham', @([UnicodeRanges]::Cham))
    $rangesMap.Add('Cherokee', @(
        [UnicodeRanges]::Cherokee
        [UnicodeRanges]::CherokeeSupplement
    ))
    $rangesMap.Add('Cjk', @(
        [UnicodeRanges]::CjkCompatibility
        [UnicodeRanges]::CjkCompatibilityForms
        [UnicodeRanges]::CjkCompatibilityIdeographs
        [UnicodeRanges]::CjkRadicalsSupplement
        [UnicodeRanges]::CjkStrokes
        [UnicodeRanges]::CjkSymbolsandPunctuation
        [UnicodeRanges]::CjkUnifiedIdeographs
        [UnicodeRanges]::CjkUnifiedIdeographsExtensionA
    ))
    $rangesMap.Add('CombiningDiacriticalMarks', @(
        [UnicodeRanges]::CombiningDiacriticalMarks
        [UnicodeRanges]::CombiningDiacriticalMarksExtended
        [UnicodeRanges]::CombiningDiacriticalMarksforSymbols
        [UnicodeRanges]::CombiningDiacriticalMarksSupplement
    ))
    $rangesMap.Add('CombiningHalfMarks', @([UnicodeRanges]::CombiningHalfMarks))
    $rangesMap.Add('CommonIndicNumberForms', @([UnicodeRanges]::CommonIndicNumberForms))
    $rangesMap.Add('ControlPictures', @([UnicodeRanges]::ControlPictures))
    $rangesMap.Add('Coptic', @([UnicodeRanges]::Coptic))
    $rangesMap.Add('CurrencySymbols', @([UnicodeRanges]::CurrencySymbols))
    $rangesMap.Add('Cyrillic', @(
        [UnicodeRanges]::Cyrillic
        [UnicodeRanges]::CyrillicExtendedA
        [UnicodeRanges]::CyrillicExtendedB
        [UnicodeRanges]::CyrillicExtendedC
        [UnicodeRanges]::CyrillicSupplement
    ))
    $rangesMap.Add('Devanagari', @(
        [UnicodeRanges]::Devanagari
        [UnicodeRanges]::DevanagariExtended
    ))
    $rangesMap.Add('Dingbats', @([UnicodeRanges]::Dingbats))
    $rangesMap.Add('EnclosedAlphanumerics', @([UnicodeRanges]::EnclosedAlphanumerics))
    $rangesMap.Add('EnclosedCjkLettersandMonths', @([UnicodeRanges]::EnclosedCjkLettersandMonths))
    $rangesMap.Add('Ethiopic', @(
        [UnicodeRanges]::Ethiopic
        [UnicodeRanges]::EthiopicExtended
        [UnicodeRanges]::EthiopicExtendedA
        [UnicodeRanges]::EthiopicSupplement
    ))
    $rangesMap.Add('GeneralPunctuation', @([UnicodeRanges]::GeneralPunctuation))
    $rangesMap.Add('GeometricShapes', @([UnicodeRanges]::GeometricShapes))
    $rangesMap.Add('Georgian', @(
        [UnicodeRanges]::Georgian
        [UnicodeRanges]::GeorgianExtended
        [UnicodeRanges]::GeorgianSupplement
    ))
    $rangesMap.Add('Glagolitic', @([UnicodeRanges]::Glagolitic))
    $rangesMap.Add('GreekandCoptic', @(
        [UnicodeRanges]::GreekandCoptic
        [UnicodeRanges]::GreekExtended
    ))
    $rangesMap.Add('Gujarati', @([UnicodeRanges]::Gujarati))
    $rangesMap.Add('Gurmukhi', @([UnicodeRanges]::Gurmukhi))
    $rangesMap.Add('HalfwidthandFullwidthForms', @([UnicodeRanges]::HalfwidthandFullwidthForms))
    $rangesMap.Add('Hangul', @(
        [UnicodeRanges]::HangulCompatibilityJamo
        [UnicodeRanges]::HangulJamo
        [UnicodeRanges]::HangulJamoExtendedA
        [UnicodeRanges]::HangulJamoExtendedB
        [UnicodeRanges]::HangulSyllables
    ))
    $rangesMap.Add('Hanunoo', @([UnicodeRanges]::Hanunoo))
    $rangesMap.Add('Hebrew', @([UnicodeRanges]::Hebrew))
    $rangesMap.Add('Hiragana', @([UnicodeRanges]::Hiragana))
    $rangesMap.Add('IdeographicDescriptionCharacters', @([UnicodeRanges]::IdeographicDescriptionCharacters))
    $rangesMap.Add('IpaExtensions', @([UnicodeRanges]::IpaExtensions))
    $rangesMap.Add('Javanese', @([UnicodeRanges]::Javanese))
    $rangesMap.Add('Kanbun', @([UnicodeRanges]::Kanbun))
    $rangesMap.Add('KangxiRadicals', @([UnicodeRanges]::KangxiRadicals))
    $rangesMap.Add('Kannada', @([UnicodeRanges]::Kannada))
    $rangesMap.Add('Katakana', @(
        [UnicodeRanges]::Katakana
        [UnicodeRanges]::KatakanaPhoneticExtensions
    ))
    $rangesMap.Add('KayahLi', @([UnicodeRanges]::KayahLi))
    $rangesMap.Add('Khmer', @(
        [UnicodeRanges]::Khmer
        [UnicodeRanges]::KhmerSymbols
    ))
    $rangesMap.Add('Lao', @([UnicodeRanges]::Lao))
    $rangesMap.Add('Lepcha', @([UnicodeRanges]::Lepcha))
    $rangesMap.Add('LetterlikeSymbols', @([UnicodeRanges]::LetterlikeSymbols))
    $rangesMap.Add('Limbu', @([UnicodeRanges]::Limbu))
    $rangesMap.Add('Lisu', @([UnicodeRanges]::Lisu))
    $rangesMap.Add('Malayalam', @([UnicodeRanges]::Malayalam))
    $rangesMap.Add('Mandaic', @([UnicodeRanges]::Mandaic))
    $rangesMap.Add('MathematicalOperators', @([UnicodeRanges]::MathematicalOperators))
    $rangesMap.Add('MeeteiMayek', @([UnicodeRanges]::MeeteiMayek))
    $rangesMap.Add('MeeteiMayekExtensions', @([UnicodeRanges]::MeeteiMayekExtensions))
    $rangesMap.Add('Miscellaneous', @(
        [UnicodeRanges]::MiscellaneousMathematicalSymbolsA
        [UnicodeRanges]::MiscellaneousMathematicalSymbolsB
        [UnicodeRanges]::MiscellaneousSymbols
        [UnicodeRanges]::MiscellaneousSymbolsandArrows
        [UnicodeRanges]::MiscellaneousTechnical
    ))
    $rangesMap.Add('ModifierToneLetters', @([UnicodeRanges]::ModifierToneLetters))
    $rangesMap.Add('Mongolian', @([UnicodeRanges]::Mongolian))
    $rangesMap.Add('Myanmar', @(
        [UnicodeRanges]::Myanmar
        [UnicodeRanges]::MyanmarExtendedA
        [UnicodeRanges]::MyanmarExtendedB
    ))
    $rangesMap.Add('NewTaiLue', @([UnicodeRanges]::NewTaiLue))
    $rangesMap.Add('NKo', @([UnicodeRanges]::NKo))
    $rangesMap.Add('NumberForms', @([UnicodeRanges]::NumberForms))
    $rangesMap.Add('Ogham', @([UnicodeRanges]::Ogham))
    $rangesMap.Add('OlChiki', @([UnicodeRanges]::OlChiki))
    $rangesMap.Add('OpticalCharacterRecognition', @([UnicodeRanges]::OpticalCharacterRecognition))
    $rangesMap.Add('Oriya', @([UnicodeRanges]::Oriya))
    $rangesMap.Add('Phagspa', @([UnicodeRanges]::Phagspa))
    $rangesMap.Add('PhoneticExtensions', @(
       [UnicodeRanges]::PhoneticExtensions
       [UnicodeRanges]::PhoneticExtensionsSupplement
    ))
    $rangesMap.Add('Rejang', @([UnicodeRanges]::Rejang))
    $rangesMap.Add('Runic', @([UnicodeRanges]::Runic))
    $rangesMap.Add('Samaritan', @([UnicodeRanges]::Samaritan))
    $rangesMap.Add('Saurashtra', @([UnicodeRanges]::Saurashtra))
    $rangesMap.Add('Sinhala', @([UnicodeRanges]::Sinhala))
    $rangesMap.Add('SmallFormVariants', @([UnicodeRanges]::SmallFormVariants))
    $rangesMap.Add('SpacingModifierLetters', @([UnicodeRanges]::SpacingModifierLetters))
    $rangesMap.Add('Specials', @([UnicodeRanges]::Specials))
    $rangesMap.Add('Sundanese', @(
        [UnicodeRanges]::Sundanese
        [UnicodeRanges]::SundaneseSupplement
    ))
    $rangesMap.Add('SuperscriptsandSubscripts', @([UnicodeRanges]::SuperscriptsandSubscripts))
    $rangesMap.Add('Supplemental', @(
        [UnicodeRanges]::SupplementalArrowsA
        [UnicodeRanges]::SupplementalArrowsB
        [UnicodeRanges]::SupplementalMathematicalOperators
        [UnicodeRanges]::SupplementalPunctuation
    ))
    $rangesMap.Add('SylotiNagri', @([UnicodeRanges]::SylotiNagri))
    $rangesMap.Add('Syriac', @(
        [UnicodeRanges]::Syriac
        [UnicodeRanges]::SyriacSupplement
    ))
    $rangesMap.Add('Tagalog', @([UnicodeRanges]::Tagalog))
    $rangesMap.Add('Tagbanwa', @([UnicodeRanges]::Tagbanwa))
    $rangesMap.Add('TaiLe', @([UnicodeRanges]::TaiLe))
    $rangesMap.Add('TaiTham', @([UnicodeRanges]::TaiTham))
    $rangesMap.Add('TaiViet', @([UnicodeRanges]::TaiViet))
    $rangesMap.Add('Tamil', @([UnicodeRanges]::Tamil))
    $rangesMap.Add('Telugu', @([UnicodeRanges]::Telugu))
    $rangesMap.Add('Thaana', @([UnicodeRanges]::Thaana))
    $rangesMap.Add('Thai', @([UnicodeRanges]::Thai))
    $rangesMap.Add('Tibetan', @([UnicodeRanges]::Tibetan))
    $rangesMap.Add('Tifinagh', @([UnicodeRanges]::Tifinagh))
    $rangesMap.Add('UnifiedCanadianAboriginalSyllabics', @(
        [UnicodeRanges]::UnifiedCanadianAboriginalSyllabics
        [UnicodeRanges]::UnifiedCanadianAboriginalSyllabicsExtended
    ))
    $rangesMap.Add('Vai', @([UnicodeRanges]::Vai))
    $rangesMap.Add('VariationSelectors', @([UnicodeRanges]::VariationSelectors))
    $rangesMap.Add('VedicExtensions', @([UnicodeRanges]::VedicExtensions))
    $rangesMap.Add('VerticalForms', @([UnicodeRanges]::VerticalForms))
    $rangesMap.Add('YijingHexagramSymbols', @([UnicodeRanges]::YijingHexagramSymbols))
    $rangesMap.Add('Yi', @(
        [UnicodeRanges]::YiRadicals
        [UnicodeRanges]::YiSyllables
    ))

    # Default header text.
    $headerText = @'
// This file is part of the ManagedStrings project and repository.
// Project files are licensed under the MIT license.
// https://github.com/FranciscoNabas/ManagedStrings

'@

    # Identation to apply to the text.
    # We use file-scoped namespaces, so the identation affects the class memers, types, and their identation.
    $Script:indent = [string]::new(' ', 4)
    $Script:innerIndent = [string]::new(' ', 8)

    #endregion

    #region Headers
    
    # Headers include the licensing and manual modification warning, the 'usings', and namespace.
    $charInfoWriter.WriteLine($headerText)
    $charInfoWriter.WriteLine(@'
using System;

namespace ManagedStrings.Decoders;

// This file was auto-generated by the 'GetCharInfoData.ps1' script under the 'Tools' folder.
// PLEASE DO NOT MODIFY BY HAND!

'@)

    $unicodeBlocksWriter.WriteLine($headerText)
    $unicodeBlocksWriter.WriteLine(@'
namespace ManagedStrings.Decoders;

// This file was auto-generated by the 'GetCharInfoData.ps1' script under the 'Tools' folder.
// PLEASE DO NOT MODIFY BY HAND!

'@)

    #endregion

    #region UnicodeBlocks

    # Creating the range list and getting the biggest block name length to align the values.
    $blockRangeMap = [System.Collections.Generic.List[Tuple[ushort, ushort, byte]]]::new()

    # Writing the summary and enum declaration.
    $unicodeBlocksWriter.WriteLine(@'
/// <summary>
/// Static members representing the Unicode blocks.
/// This is used to determine with characters are printed.
/// It's generated from all the current <see cref="UnicodeRange"/> in <see cref="UnicodeRanges">.
/// </summary>
/// <seealso href="https://en.wikipedia.org/wiki/List_of_Unicode_characters">List of Unicode characters</seealso>
/// <seealso href="https://learn.microsoft.com/dotnet/api/system.text.unicode.unicoderanges">UnicodeRanges Class</seealso>
/// <seealso href="https://learn.microsoft.com/dotnet/api/system.text.unicode.unicoderange">UnicodeRange Class</seealso>
/// <runtimefile>src/libraries/System.Text.Encodings.Web/src/System/Text/Unicode/UnicodeRanges.generated.cs</runtimefile>
'@)
    
    # Printing the 'UnicodeBlocks' static properties.
    $unicodeBlocksWriter.WriteLine('internal sealed partial class UnicodeBlocks')
    $unicodeBlocksWriter.WriteLine('{')

    $buffer = [System.Text.StringBuilder]::new()
    $biggestBlockNameLength = ($rangesMap.Keys | Sort-Object -Property Length -Descending | Select-Object -First 1).Length

    $bitIndex = 0
    foreach ($blockName in $rangesMap.Keys) {
        [void]$buffer.AppendLine("$($Script:indent)internal static UnicodeBlock $blockName$([string]::new(' ', $biggestBlockNameLength - $blockName.Length))  { get; } = new FriendUnicodeBlock($('0x{0:X2}' -f $bitIndex));")
        foreach ($block in $rangesMap[$blockName]) {
            $blockRangeMap.Add([Tuple[ushort, ushort, byte]]::new($block.FirstCodePoint, $block.FirstCodePoint + $block.Length - 1, $bitIndex))
        }

        $bitIndex++
    }

    [void]$buffer.AppendLine("$($Script:indent)internal static UnicodeBlock All$([string]::new(' ', $biggestBlockNameLength - 3))  { get; } = new FriendUnicodeBlock($('0x{0:X2}' -f 0xFE));")
    $unicodeBlocksWriter.Write($buffer.ToString())
    [void]$buffer.Clear()

    $unicodeBlocksWriter.Write('}')
    
    #endregion
    
    #region PrintableAscii

    # Writing the class declaration
    $charInfoWriter.WriteLine("internal static partial class CharInfo")
    $charInfoWriter.WriteLine('{')

    # This section writes both 'PrintableASCIIMap' spans.
    # For the logic chack the 'Invoke-PrintableASCIIMapWrite' function.
    $charInfoWriter.WriteLine(@'
    /// <summary>
    /// A collection of booleans indicating if the byte representation of an ASCII character is printable.
    /// A printable character are all characters excluding control code points.
    /// This collection includes the control code points 'HT', 'LF', and 'CR', representing Horizontal Tabulation (0x09),
    /// Line Feed (0x0A), and Carriage Return (0x0D), respectively.
    /// </summary>
    /// <seealso href="https://en.wikipedia.org/wiki/ASCII">ASCII - Wikipedia</seealso>
'@)
    $charInfoWriter.WriteLine("$($Script:indent)private static ReadOnlySpan<bool> PrintableASCIIMap =>")
    $charInfoWriter.WriteLine("$($Script:indent)[")
    Invoke-PrintableASCIIMapWrite -Writer $charInfoWriter

    $charInfoWriter.WriteLine()
        
    $charInfoWriter.WriteLine(@'
    /// <summary>
    /// A collection of booleans indicating if the byte representation of an ASCII character is printable.
    /// It's similar to <see cref="PrintableASCIIMap"/>, but without the 'HT', 'LF', and 'CR' code points.
    /// </summary>
    /// <seealso href="https://en.wikipedia.org/wiki/ASCII">ASCII - Wikipedia</seealso>
'@)
    $charInfoWriter.WriteLine("$($Script:indent)private static ReadOnlySpan<bool> PrintableASCIIMapNoControl =>")
    $charInfoWriter.WriteLine("$($Script:indent)[")
    Invoke-PrintableASCIIMapWrite -Writer $charInfoWriter -ExcludeControlCodePoints

    $charInfoWriter.WriteLine()
    
    #endregion

    #region UnicodeBlockMap
    
    # Defining the max. code point value to facilitate testing.
    # $lastCodePage = 0x2000
    $lastCodePage = [ushort]::MaxValue

    # Writing the span declaration and a header.
    $charInfoWriter.WriteLine(@'
    /// <summary>
    /// A collection of bytes mapping each Unicode Basic Plane code point
    /// to a block from <see cref="UnicodeBlocks"/>.
    /// Used to quickly retrieve a Unicode character block, or determine if the code point is not assigned.
    /// </summary>
    /// <seealso href="https://en.wikipedia.org/wiki/List_of_Unicode_characters">List of Unicode characters</seealso>
    /// <seealso href="https://learn.microsoft.com/dotnet/api/system.text.unicode.unicoderanges">UnicodeRanges Class</seealso>
    /// <seealso href="https://learn.microsoft.com/dotnet/api/system.text.unicode.unicoderange">UnicodeRange Class</seealso>
'@)
    $charInfoWriter.WriteLine("$($Script:indent)private static ReadOnlySpan<byte> UnicodeBlockCodePointMap =>")
    $charInfoWriter.WriteLine("$($Script:indent)[")
    $charInfoWriter.WriteLine("$($Script:innerIndent)/*            00    01    02    03    04    05    06    07    08    09    0A    0B    0C    0D    0E    0F    10    11    12    13    14    15    16    17    18    19    1A    1B    1C    1D    1E    1F  */")

    $commentIndex = 0
    $currentCodePoint = 0

    # Caching the first range. We do this every time we advance a range max. code page value, so we don't check it in
    # every iteration. This operation is not cheap.
    $currentRange = $blockRangeMap | Where-Object -FilterScript { $_.Item1 -eq 0 }
    $currentRangeEndOffset = $currentRange.Item2

    # The main loop. We go through each code point in the Unicode Basic Plane (0x0000..0xFFFF).
    for ($currentCodePoint; $currentCodePoint -lt $lastCodePage; ) {
        Write-Progress -Activity "Creating 'UnicodeBlockCodePointMap' span" -Status "Current code page: $('0x{0:X4}' -f $currentCodePoint)" -PercentComplete (($currentCodePoint / $lastCodePage) * 100)
            
        # Checking if we advanced to the next region.
        if ($currentCodePoint -ge $currentRangeEndOffset) {
            $currentRange = $blockRangeMap | Where-Object -FilterScript { $_.Item1 -le $currentCodePoint -and $_.Item2 -ge $currentCodePoint }
            $currentRangeEndOffset = $currentRange.Item2
        }

        # Writing a comment with the current line offset.
        [void]$buffer.Append(("$($Script:innerIndent)/* 0x{0:X4} */ " -f $commentIndex))
        for ($i = 0; $i -lt 32; $i++) {
            if ($currentCodePoint -gt $lastCodePage) {
                break
            }

            # Check if we advanced to the next range in the inner loop also.
            if ($currentCodePoint -ge $currentRangeEndOffset) {
                $currentRange = $blockRangeMap | Where-Object -FilterScript { $_.Item1 -le $currentCodePoint -and $_.Item2 -ge $currentCodePoint }
                $currentRangeEndOffset = $currentRange.Item2
            }

            # If we don't have a range for the current code point, or it's categorized as 'OtherNotAssigned' we set its value to 0xFF.
            if ($null -eq $currentRange -or [System.Globalization.CharUnicodeInfo]::GetUnicodeCategory([char]$currentCodePoint) -eq 'OtherNotAssigned') {
                [void]$buffer.Append('0xFF, ')
            }
            else {
                # Writing the value obtained in the enum creation.
                [void]$buffer.Append(('0x{0:X2}, ' -f $currentRange.Item3))
            }

            $currentCodePoint++
        }

        # Trimming the last space.
        [void]$buffer.Remove($buffer.Length - 1, 1)
        [void]$buffer.AppendLine()
        $commentIndex += 0x20
    }

    # We done.
    Write-Progress -Activity 'Creating groups span' -Status "Current code page: $('0x{0:X4}' -f $currentCodePoint)" -Completed

    # Trimming the last comma, completing the span, and writting it to the stream.
    [void]$buffer.Remove($buffer.Length - 3, 1)
    $charInfoWriter.Write($buffer.ToString())
    $charInfoWriter.WriteLine("$Script:indent];")

    #endregion

    # Terminating the 'CharInfo' class and flushing the writer.
    $charInfoWriter.Write('}')
    $charInfoWriter.Flush()
}
catch { throw $_ }
finally {
    # Cleaning up.
    $charInfoWriter.Dispose()
    $unicodeBlocksWriter.Dispose()
}