# Helper to print the blocks to the console so we can build the '-? UnicodeBlocks' help thext.

using namespace System.Text.Unicode

$rangesMap = [System.Collections.Generic.Dictionary[string, hashtable[]]]::new()
$rangesMap2 = [System.Collections.Generic.Dictionary[string, UnicodeRange[]]]::new()
$rangesMap2.Add('BasicLatin', @([UnicodeRanges]::BasicLatin))
$rangesMap.Add('LatinExtensions', @(
        @{ Name = 'Latin1Supplement'; Range = [UnicodeRanges]::Latin1Supplement }
        @{ Name = 'LatinExtendedA'; Range = [UnicodeRanges]::LatinExtendedA }
        @{ Name = 'LatinExtendedAdditional'; Range = [UnicodeRanges]::LatinExtendedAdditional }
        @{ Name = 'LatinExtendedB'; Range = [UnicodeRanges]::LatinExtendedB }
        @{ Name = 'LatinExtendedC'; Range = [UnicodeRanges]::LatinExtendedC }
        @{ Name = 'LatinExtendedD'; Range = [UnicodeRanges]::LatinExtendedD }
        @{ Name = 'LatinExtendedE'; Range = [UnicodeRanges]::LatinExtendedE }
    ))
$rangesMap2.Add('AlphabeticPresentationForms', @([UnicodeRanges]::AlphabeticPresentationForms))
$rangesMap.Add('Arabic', @(
        @{ Name = 'Arabic'; Range = [UnicodeRanges]::Arabic }
        @{ Name = 'ArabicExtendedA'; Range = [UnicodeRanges]::ArabicExtendedA }
        @{ Name = 'ArabicExtendedB'; Range = [UnicodeRanges]::ArabicExtendedB }
        @{ Name = 'ArabicPresentationFormsA'; Range = [UnicodeRanges]::ArabicPresentationFormsA }
        @{ Name = 'ArabicPresentationFormsB'; Range = [UnicodeRanges]::ArabicPresentationFormsB }
        @{ Name = 'ArabicSupplement'; Range = [UnicodeRanges]::ArabicSupplement }
    ))
$rangesMap2.Add('Armenian', @([UnicodeRanges]::Armenian))
$rangesMap2.Add('Arrows', @([UnicodeRanges]::Arrows))
$rangesMap2.Add('Balinese', @([UnicodeRanges]::Balinese))
$rangesMap2.Add('Bamum', @([UnicodeRanges]::Bamum))
$rangesMap2.Add('Batak', @([UnicodeRanges]::Batak))
$rangesMap2.Add('Bengali', @([UnicodeRanges]::Bengali))
$rangesMap2.Add('BlockElements', @([UnicodeRanges]::BlockElements))
$rangesMap.Add('Bopomofo', @(
        @{ Name = 'Bopomofo'; Range = [UnicodeRanges]::Bopomofo }
        @{ Name = 'BopomofoExtended'; Range = [UnicodeRanges]::BopomofoExtended }
    ))
$rangesMap2.Add('BoxDrawing', @([UnicodeRanges]::BoxDrawing))
$rangesMap2.Add('BraillePatterns', @([UnicodeRanges]::BraillePatterns))
$rangesMap2.Add('Buginese', @([UnicodeRanges]::Buginese))
$rangesMap2.Add('Buhid', @([UnicodeRanges]::Buhid))
$rangesMap2.Add('Cham', @([UnicodeRanges]::Cham))
$rangesMap.Add('Cherokee', @(
        @{ Name = 'Cherokee'; Range = [UnicodeRanges]::Cherokee }
        @{ Name = 'CherokeeSupplement'; Range = [UnicodeRanges]::CherokeeSupplement }
    ))
$rangesMap.Add('Cjk', @(
        @{ Name = 'CjkCompatibility'; Range = [UnicodeRanges]::CjkCompatibility }
        @{ Name = 'CjkCompatibilityForms'; Range = [UnicodeRanges]::CjkCompatibilityForms }
        @{ Name = 'CjkCompatibilityIdeographs'; Range = [UnicodeRanges]::CjkCompatibilityIdeographs }
        @{ Name = 'CjkRadicalsSupplement'; Range = [UnicodeRanges]::CjkRadicalsSupplement }
        @{ Name = 'CjkStrokes'; Range = [UnicodeRanges]::CjkStrokes }
        @{ Name = 'CjkSymbolsandPunctuation'; Range = [UnicodeRanges]::CjkSymbolsandPunctuation }
        @{ Name = 'CjkUnifiedIdeographs'; Range = [UnicodeRanges]::CjkUnifiedIdeographs }
        @{ Name = 'CjkUnifiedIdeographsExtensionA'; Range = [UnicodeRanges]::CjkUnifiedIdeographsExtensionA }
    ))
$rangesMap.Add('CombiningDiacriticalMarks', @(
        @{ Name = 'CombiningDiacriticalMarks'; Range = [UnicodeRanges]::CombiningDiacriticalMarks }
        @{ Name = 'CombiningDiacriticalMarksExtended'; Range = [UnicodeRanges]::CombiningDiacriticalMarksExtended }
        @{ Name = 'CombiningDiacriticalMarksforSymbols'; Range = [UnicodeRanges]::CombiningDiacriticalMarksforSymbols }
        @{ Name = 'CombiningDiacriticalMarksSupplement'; Range = [UnicodeRanges]::CombiningDiacriticalMarksSupplement }
    ))
$rangesMap2.Add('CombiningHalfMarks', @([UnicodeRanges]::CombiningHalfMarks))
$rangesMap2.Add('CommonIndicNumberForms', @([UnicodeRanges]::CommonIndicNumberForms))
$rangesMap2.Add('ControlPictures', @([UnicodeRanges]::ControlPictures))
$rangesMap2.Add('Coptic', @([UnicodeRanges]::Coptic))
$rangesMap2.Add('CurrencySymbols', @([UnicodeRanges]::CurrencySymbols))
$rangesMap.Add('Cyrillic', @(
        @{ Name = 'Cyrillic'; Range = [UnicodeRanges]::Cyrillic }
        @{ Name = 'CyrillicExtendedA'; Range = [UnicodeRanges]::CyrillicExtendedA }
        @{ Name = 'CyrillicExtendedB'; Range = [UnicodeRanges]::CyrillicExtendedB }
        @{ Name = 'CyrillicExtendedC'; Range = [UnicodeRanges]::CyrillicExtendedC }
        @{ Name = 'CyrillicSupplement'; Range = [UnicodeRanges]::CyrillicSupplement }
    ))
$rangesMap.Add('Devanagari', @(
        @{ Name = 'Devanagari'; Range = [UnicodeRanges]::Devanagari }
        @{ Name = 'DevanagariExtended'; Range = [UnicodeRanges]::DevanagariExtended }
    ))
$rangesMap2.Add('Dingbats', @([UnicodeRanges]::Dingbats))
$rangesMap2.Add('EnclosedAlphanumerics', @([UnicodeRanges]::EnclosedAlphanumerics))
$rangesMap2.Add('EnclosedCjkLettersandMonths', @([UnicodeRanges]::EnclosedCjkLettersandMonths))
$rangesMap.Add('Ethiopic', @(
        @{ Name = 'Ethiopic'; Range = [UnicodeRanges]::Ethiopic }
        @{ Name = 'EthiopicExtended'; Range = [UnicodeRanges]::EthiopicExtended }
        @{ Name = 'EthiopicExtendedA'; Range = [UnicodeRanges]::EthiopicExtendedA }
        @{ Name = 'EthiopicSupplement'; Range = [UnicodeRanges]::EthiopicSupplement }
    ))
$rangesMap2.Add('GeneralPunctuation', @([UnicodeRanges]::GeneralPunctuation))
$rangesMap2.Add('GeometricShapes', @([UnicodeRanges]::GeometricShapes))
$rangesMap.Add('Georgian', @(
        @{ Name = 'Georgian'; Range = [UnicodeRanges]::Georgian }
        @{ Name = 'GeorgianExtended'; Range = [UnicodeRanges]::GeorgianExtended }
        @{ Name = 'GeorgianSupplement'; Range = [UnicodeRanges]::GeorgianSupplement }
    ))
$rangesMap2.Add('Glagolitic', @([UnicodeRanges]::Glagolitic))
$rangesMap.Add('GreekandCoptic', @(
        @{ Name = 'GreekandCoptic'; Range = [UnicodeRanges]::GreekandCoptic }
        @{ Name = 'GreekExtended'; Range = [UnicodeRanges]::GreekExtended }
    ))
$rangesMap2.Add('Gujarati', @([UnicodeRanges]::Gujarati))
$rangesMap2.Add('Gurmukhi', @([UnicodeRanges]::Gurmukhi))
$rangesMap2.Add('HalfwidthandFullwidthForms', @([UnicodeRanges]::HalfwidthandFullwidthForms))
$rangesMap.Add('Hangul', @(
        @{ Name = 'HangulCompatibilityJamo'; Range = [UnicodeRanges]::HangulCompatibilityJamo }
        @{ Name = 'HangulJamo'; Range = [UnicodeRanges]::HangulJamo }
        @{ Name = 'HangulJamoExtendedA'; Range = [UnicodeRanges]::HangulJamoExtendedA }
        @{ Name = 'HangulJamoExtendedB'; Range = [UnicodeRanges]::HangulJamoExtendedB }
        @{ Name = 'HangulSyllables'; Range = [UnicodeRanges]::HangulSyllables }
    ))
$rangesMap2.Add('Hanunoo', @([UnicodeRanges]::Hanunoo))
$rangesMap2.Add('Hebrew', @([UnicodeRanges]::Hebrew))
$rangesMap2.Add('Hiragana', @([UnicodeRanges]::Hiragana))
$rangesMap2.Add('IdeographicDescriptionCharacters', @([UnicodeRanges]::IdeographicDescriptionCharacters))
$rangesMap2.Add('IpaExtensions', @([UnicodeRanges]::IpaExtensions))
$rangesMap2.Add('Javanese', @([UnicodeRanges]::Javanese))
$rangesMap2.Add('Kanbun', @([UnicodeRanges]::Kanbun))
$rangesMap2.Add('KangxiRadicals', @([UnicodeRanges]::KangxiRadicals))
$rangesMap2.Add('Kannada', @([UnicodeRanges]::Kannada))
$rangesMap.Add('Katakana', @(
        @{ Name = 'Katakana'; Range = [UnicodeRanges]::Katakana }
        @{ Name = 'KatakanaPhoneticExtensions'; Range = [UnicodeRanges]::KatakanaPhoneticExtensions }
    ))
$rangesMap2.Add('KayahLi', @([UnicodeRanges]::KayahLi))
$rangesMap.Add('Khmer', @(
        @{ Name = 'Khmer'; Range = [UnicodeRanges]::Khmer }
        @{ Name = 'KhmerSymbols'; Range = [UnicodeRanges]::KhmerSymbols }
    ))
$rangesMap2.Add('Lao', @([UnicodeRanges]::Lao))
$rangesMap2.Add('Lepcha', @([UnicodeRanges]::Lepcha))
$rangesMap2.Add('LetterlikeSymbols', @([UnicodeRanges]::LetterlikeSymbols))
$rangesMap2.Add('Limbu', @([UnicodeRanges]::Limbu))
$rangesMap2.Add('Lisu', @([UnicodeRanges]::Lisu))
$rangesMap2.Add('Malayalam', @([UnicodeRanges]::Malayalam))
$rangesMap2.Add('Mandaic', @([UnicodeRanges]::Mandaic))
$rangesMap2.Add('MathematicalOperators', @([UnicodeRanges]::MathematicalOperators))
$rangesMap2.Add('MeeteiMayek', @([UnicodeRanges]::MeeteiMayek))
$rangesMap2.Add('MeeteiMayekExtensions', @([UnicodeRanges]::MeeteiMayekExtensions))
$rangesMap.Add('Miscellaneous', @(
        @{ Name = 'MiscellaneousMathematicalSymbolsA'; Range = [UnicodeRanges]::MiscellaneousMathematicalSymbolsA }
        @{ Name = 'MiscellaneousMathematicalSymbolsB'; Range = [UnicodeRanges]::MiscellaneousMathematicalSymbolsB }
        @{ Name = 'MiscellaneousSymbols'; Range = [UnicodeRanges]::MiscellaneousSymbols }
        @{ Name = 'MiscellaneousSymbolsandArrows'; Range = [UnicodeRanges]::MiscellaneousSymbolsandArrows }
        @{ Name = 'MiscellaneousTechnical'; Range = [UnicodeRanges]::MiscellaneousTechnical }
    ))
$rangesMap2.Add('ModifierToneLetters', @([UnicodeRanges]::ModifierToneLetters))
$rangesMap2.Add('Mongolian', @([UnicodeRanges]::Mongolian))
$rangesMap.Add('Myanmar', @(
        @{ Name = 'Myanmar'; Range = [UnicodeRanges]::Myanmar }
        @{ Name = 'MyanmarExtendedA'; Range = [UnicodeRanges]::MyanmarExtendedA }
        @{ Name = 'MyanmarExtendedB'; Range = [UnicodeRanges]::MyanmarExtendedB }
    ))
$rangesMap2.Add('NewTaiLue', @([UnicodeRanges]::NewTaiLue))
$rangesMap2.Add('NKo', @([UnicodeRanges]::NKo))
$rangesMap2.Add('NumberForms', @([UnicodeRanges]::NumberForms))
$rangesMap2.Add('Ogham', @([UnicodeRanges]::Ogham))
$rangesMap2.Add('OlChiki', @([UnicodeRanges]::OlChiki))
$rangesMap2.Add('OpticalCharacterRecognition', @([UnicodeRanges]::OpticalCharacterRecognition))
$rangesMap2.Add('Oriya', @([UnicodeRanges]::Oriya))
$rangesMap2.Add('Phagspa', @([UnicodeRanges]::Phagspa))
$rangesMap.Add('PhoneticExtensions', @(
        @{ Name = 'PhoneticExtensions'; Range = [UnicodeRanges]::PhoneticExtensions }
        @{ Name = 'PhoneticExtensionsSupplement'; Range = [UnicodeRanges]::PhoneticExtensionsSupplement }
    ))
$rangesMap2.Add('Rejang', @([UnicodeRanges]::Rejang))
$rangesMap2.Add('Runic', @([UnicodeRanges]::Runic))
$rangesMap2.Add('Samaritan', @([UnicodeRanges]::Samaritan))
$rangesMap2.Add('Saurashtra', @([UnicodeRanges]::Saurashtra))
$rangesMap2.Add('Sinhala', @([UnicodeRanges]::Sinhala))
$rangesMap2.Add('SmallFormVariants', @([UnicodeRanges]::SmallFormVariants))
$rangesMap2.Add('SpacingModifierLetters', @([UnicodeRanges]::SpacingModifierLetters))
$rangesMap2.Add('Specials', @([UnicodeRanges]::Specials))
$rangesMap.Add('Sundanese', @(
        @{ Name = 'Sundanese'; Range = [UnicodeRanges]::Sundanese }
        @{ Name = 'SundaneseSupplement'; Range = [UnicodeRanges]::SundaneseSupplement }
    ))
$rangesMap2.Add('SuperscriptsandSubscripts', @([UnicodeRanges]::SuperscriptsandSubscripts))
$rangesMap.Add('Supplemental', @(
        @{ Name = 'SupplementalArrowsA'; Range = [UnicodeRanges]::SupplementalArrowsA }
        @{ Name = 'SupplementalArrowsB'; Range = [UnicodeRanges]::SupplementalArrowsB }
        @{ Name = 'SupplementalMathematicalOperators'; Range = [UnicodeRanges]::SupplementalMathematicalOperators }
        @{ Name = 'SupplementalPunctuation'; Range = [UnicodeRanges]::SupplementalPunctuation }
    ))
$rangesMap2.Add('SylotiNagri', @([UnicodeRanges]::SylotiNagri))
$rangesMap.Add('Syriac', @(
        @{ Name = 'Syriac'; Range = [UnicodeRanges]::Syriac }
        @{ Name = 'SyriacSupplement'; Range = [UnicodeRanges]::SyriacSupplement }
    ))
$rangesMap2.Add('Tagalog', @([UnicodeRanges]::Tagalog))
$rangesMap2.Add('Tagbanwa', @([UnicodeRanges]::Tagbanwa))
$rangesMap2.Add('TaiLe', @([UnicodeRanges]::TaiLe))
$rangesMap2.Add('TaiTham', @([UnicodeRanges]::TaiTham))
$rangesMap2.Add('TaiViet', @([UnicodeRanges]::TaiViet))
$rangesMap2.Add('Tamil', @([UnicodeRanges]::Tamil))
$rangesMap2.Add('Telugu', @([UnicodeRanges]::Telugu))
$rangesMap2.Add('Thaana', @([UnicodeRanges]::Thaana))
$rangesMap2.Add('Thai', @([UnicodeRanges]::Thai))
$rangesMap2.Add('Tibetan', @([UnicodeRanges]::Tibetan))
$rangesMap2.Add('Tifinagh', @([UnicodeRanges]::Tifinagh))
$rangesMap.Add('UnifiedCanadianAboriginalSyllabics', @(
        @{ Name = 'UnifiedCanadianAboriginalSyllabics'; Range = [UnicodeRanges]::UnifiedCanadianAboriginalSyllabics }
        @{ Name = 'UnifiedCanadianAboriginalSyllabicsExtended'; Range = [UnicodeRanges]::UnifiedCanadianAboriginalSyllabicsExtended }
    ))
$rangesMap2.Add('Vai', @([UnicodeRanges]::Vai))
$rangesMap2.Add('VariationSelectors', @([UnicodeRanges]::VariationSelectors))
$rangesMap2.Add('VedicExtensions', @([UnicodeRanges]::VedicExtensions))
$rangesMap2.Add('VerticalForms', @([UnicodeRanges]::VerticalForms))
$rangesMap2.Add('YijingHexagramSymbols', @([UnicodeRanges]::YijingHexagramSymbols))
$rangesMap.Add('Yi', @(
        @{ Name = 'YiRadicals'; Range = [UnicodeRanges]::YiRadicals }
        @{ Name = 'YiSyllables'; Range = [UnicodeRanges]::YiSyllables }
    ))


$biggestBlockNameLength = ($rangesMap2.Keys | Sort-Object -Property Length -Descending | Select-Object -First 1).Length
foreach ($blockName in $rangesMap2.Keys) {
    $currentRanges = $rangesMap2[$blockName]

    [Console]::WriteLine("$($blockName):  $([string]::new(' ', $biggestBlockNameLength - $blockName.Length))[$('0x{0:X4}' -f $currentRanges[0].FirstCodePoint)..$('0x{0:X4}' -f ($currentRanges[0].FirstCodePoint + $currentRanges[0].Length - 1))]")
}

foreach ($blockName in $rangesMap.Keys) {
    $currentRanges = $rangesMap[$blockName]
    [Console]::WriteLine("$($blockName):")
    $currentBiggest = ($currentRanges.Name | Sort-Object -Property Length -Descending | Select-Object -First 1).Length
    foreach ($range in $currentRanges) {
        [Console]::WriteLine("    $($range.Name):  $([string]::new(' ', $currentBiggest - $range.Name.Length))[$('0x{0:X4}' -f $range.Range.FirstCodePoint)..$('0x{0:X4}' -f ($range.Range.FirstCodePoint + $range.Range.Length - 1))]")
    }
}