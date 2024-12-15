// This file is part of the ManagedStrings project and repository.
// Project files are licensed under the MIT license.
// https://github.com/FranciscoNabas/ManagedStrings

using System;
using System.Linq;
using System.Reflection;
using ManagedStrings.Engine;
using ManagedStrings.Handlers;

namespace ManagedStrings;

/// <summary>
/// The main program class.
/// </summary>
internal class Program
{
    // Contains the help switches.
    private static readonly string[] m_helpStrings = ["-?", "--?", "/?", "-h", "-H", "--h", "--H", "/h", "/H"];

    /// <summary>
    /// The main method.
    /// </summary>
    /// <param name="args">A list of arguments passed to the executable.</param>
    static void Main(string[] args)
    {
        if (args.Length == 0) {
            Console.WriteLine($"""

            Insufficient argument count.
            Use 'ManagedStrings.exe -?' for more information.

            """);
            PrintUsage();

            return;
        }

        // Checking if the user requested help.
        if (m_helpStrings.Contains(args[0])) {
            if (args.Length > 1 && args[1].Equals("UnicodeBlocks", StringComparison.InvariantCultureIgnoreCase)) {
                
                // Print unicode block information.
                PrintUnicodeBlockInfo();
                return;
            }

            PrintUsage();
            return;
        }

        // Parsing the command line options.
        CommandLineOptions options;
        try {
            options = CommandLineOptions.Parse(args);
        }
        catch (CommandLineParserException ex) {
            Console.WriteLine($"""

            Invalid argument: {ex.Argument}.
            Use 'ManagedStrings.exe -?' to get the usage.

            """);
            return;
        }
        catch (InvalidOperationException ex) {
            Console.WriteLine($"""

            {ex.Message}
            Use 'ManagedStrings.exe -?' to get the usage.

            """);
            return;
        }
        catch (Exception) {
            throw;
        }

#if DEBUG
            PrintCommandLineOptions(options);
#endif

        // Processing the run.
        if (options.Benchmark)
            ProcessBenchmark(options);
        else
            ProcessRun(options);
    }

    /// <summary>
    /// Processes a single run.
    /// </summary>
    /// <param name="options">The command line options.</param>
    private static void ProcessRun(CommandLineOptions options)
    {
        // Creating the cancellation handler and printer.
        using WindowsCancellationHandler cancellationHandler = new();
        using Printer printer = new(options);
        try {
            if (options.Mode == Mode.SingleFile || options.Mode == Mode.Directory || options.Mode == Mode.FileSystemWildcard) {
                Handler handler = new FileHandler(options, printer);
                handler.Handle(cancellationHandler.Token);
            }
            else if (options.Mode == Mode.Process) {
                ProcessHandler handler = new(options, printer);
                handler.Handle(cancellationHandler.Token);
            }
        }
        catch (OperationCanceledException) { }
    }

    /// <summary>
    /// Processes a benchmark run.
    /// </summary>
    /// <param name="options">The command line options.</param>
    private static void ProcessBenchmark(CommandLineOptions options)
    {
        // Creating the cancellation handler and printer.
        using WindowsCancellationHandler cancellationHandler = new();
        using Printer printer = new(options);
        try {
            if (options.Mode == Mode.SingleFile || options.Mode == Mode.Directory || options.Mode == Mode.FileSystemWildcard) {
                Handler handler = new FileHandler(options, printer);
                handler.HandleBenchmark(cancellationHandler.Token).PrintData();
            }
            else if (options.Mode == Mode.Process) {
                ProcessHandler handler = new(options, printer);
                handler.HandleBenchmark(cancellationHandler.Token).PrintData();
            }
        }
        catch (OperationCanceledException) { }
    }

    /// <summary>
    /// Prints the usage.
    /// </summary>
    private static void PrintUsage()
    {
        Console.WriteLine($"""

        ManagedStrings v{Assembly.GetExecutingAssembly().GetName().Version!.ToString(3)} - Search for strings in binary files or processes address space.
        Copyright (C) 2024 Frank Nabas Labs
        https://github.com/FranciscoNabas
        
        ManagedStrings.exe [-pid] [-mt] [-pp] [<common parameters>]
        ManagedStrings.exe <file or directory> [-r] [-pf] [<common parameters>]
        
        Common parameters: [-e] [-o] [-l] [-n] [-b] [-f] [-ft] [-fo] [-ug] [-ec] [-po] [-pe] [-ph] [-o] [-ot] [-d] [--sync] [--force]
        
        -?/-h            Print help message. Use '-? UnicodeBlocks' to print a list of supported Unicode blocks.
        -e               Encoding. Accepted values: ASCII, Unicode, UTF8, All. You can specify multiple values with '|': ASCII|Unicode. Default: Unicode|UTF8.
        -o               Scan starting offset. Default: 0.
        -l               Number of bytes to scan. Default: 0 (All).
        -n               Minimum string length. Default: 3.
        -b               Buffer size in bytes. Default is 1Mb. See remarks for more info.
        -f               Filter string. The way it's used depends on the filter type. Default: no filter.
        -ft              Filter type. Accepted values: Regex, PowerShell. Default: Regex. See remarks for more info.
        -fo              Filter options. See remarks for more info. If '-f' is not used this parameter is ignored. Default: None.
        -ub              Unicode blocks. Default: BasicLatin. Use '-? UnicodeBlocks' to see the list of supported Unicode blocks.
        -pid             One or more process IDs to scan the virtual memory, separated by comma.
        -mt              Process virtual memory region type. Default is 'Private'. See remarks for all the possible options.
        -r               Searches for files recursively. Path must be a directory.
        -ec              Exclude the control code points 'HT', 'LF', 'CR' (Tab, Line Feed/New Line, and Carriage Return respectively).
        -po              Print offset where string was located.
        -pe              Print the encoding of every string.
        -pf              Print file name. If no input is provided it prints the file name. You can specify 'Name', 'Relative', or 'FullPath'. Can't be used with '-pid' or '-pp'.
        -pp              Print process information when searching for strings in a process memory space. Default is 'ProcessId'. See remarks for the all accepted values.
        -ph              Print header. To be used with the other '-p*' options, or if the output file is 'Csv' or 'PlainText'.
        -o               Output file. It can be a file path, or 'Console' to write the serialized results to the console. See remarks for more info.
        -ot              Output file type. When '-o' is used specifies the file type. Accepted values are 'PlainText', 'Csv', 'Xml', and 'Json'. Default is 'PlainText'.
        -d               Delimiter character. Used with the other '-p*' options, or if the output file is 'Csv' or 'PlainText'. Default is comma (,).
        --sync           Forces synchronous string search. See remarks for more info.
        --force          When used with '-o' if the file already exists it overwrites it instead of returning an exception. If used without '-o' this parameter is ignored.
        
        
        Remarks:
        
            - File name: You can use the wildcard characters '*' and '?' to filter for file or names.
              E.g., C:\Windows\System32\*.dll. For more information check the remarks at:
              https://learn.microsoft.com/dotnet/api/system.io.directoryinfo.getfiles
        
           - Output file: It needs to be a valid file path in an existing directory, or 'Console' to write the results to the console.
             Using 'Console' it's useful when you want to print 'Xml' or 'Json' to the console.
             When 'Console' is used with 'PlainText' or 'Csv' the parameter is ignored.
              
           - Output file type: When using 'PlainText' or 'Csv' the text saved will follow the '-p*' rules.
              When using 'Xml' or 'Json' these parameters are ignored with the exception of '-pf', and the following information is saved:
                 - Offset.
                 - Encoding.
                 - File. If '-pf' is not used the file name is saved, otherwise the type defined with '-pf'. (when used with files).
                 - Process. Id, name, memory region type, and details (when used with process ID).
        
            - Sync: When multiple encodings are used the string search is done in parallel for each one. This improves performance, but the results
              are printed out of order in relation to the offset. If you want to print the strings in the order they appear in the stream use the '--sync' parameter.
        
            - Buffer size: This is the size of the reading buffer. Regardless if we're reading a file or a process memory we read info in a buffer first,
              and use this buffer to search for strings. This speeds up things tremendously. Note that a buffer too big can make things slower. The default
              is 1Mb, which is pretty good for most cases. If the buffer size is bigger than the file, or process region it get's reduced to the stream size.
              Maximum size is 2147483647 bytes, which is the maximum array size in .NET, or the maximum 4-byte integer number value.
        
            - Start offset, Bytes to scan, Minimum length, Buffer size, and Process ID: These parameters accepts positive numbers in the decimal and hexadecimal formats.
              A hexadecimal value must start with '0x', otherwise will be treated as decimal. E.g., 0x10 = 16; 10 = 10; 1C = Invalid.
                  - Start offset [-o]    => From '0' to '9223372036854775807'. Can't be bigger than the stream length.
                  - Bytes to scan [-l]   => From '0' to '9223372036854775807', '0' means everything. Can't be bigger than the stream length minus the start offset.
                  - Minimum length [-n]  => From '1' to '4294967295', since '0' wouldn't print anything.
                  - Buffer size [-b]     => From '1' to '2147483647'.
                  - Process ID [-pid]    => From '0' to '4294967295'. Must be a valid currently running process ID.
        
            - Print header and delimiter character: These are used when one of the '-p*' parameters are used, or if the output file type is 'Csv'.
              They are ignored otherwise.
        
            - Encoding: Since UTF8 is backwards compatible with ASCII by default we only include Unicode and UTF8. You can use them together,
              but if you do UTF8 is chosen by default to avoid printing repeated strings.
        
            - Print process information: Print information about the process and the memory regions.
              Accepted values are:
                  - ProcessId   => The process unique identifier.
                  - MemoryType  => The memory region type.
                  - Details     => Details about the memory region. E.g., for heaps the heap ID, for stack the thread ID, for files the file name, etc.
                  - All         => All of the above.
        
              You can combine them with '|': ProcessId|MemoryType.
              Can't be used with files.
        
            - Memory region type: When reading from a process virtual memory we first categorize each region, because most of the data is not readable
              e.g., non-committed, free, or protected memory. This gives us the advantage of being able to identify where was the string read.
              Accepted values are:
                  - Stack       => Thread stack.
                  - Heap        => NT Heap, Segment Heap, and NT Heap segments.
                  - Private     => Includes Stack, Heap(s), PEB, TEB, other smaller regions and the remaining private data. To a full list see the 'MemoryRegionType' enumeration.
                  - MappedFile  => Memory mapped files.
                  - Shareable   => Other 'MEM_MAPPED' regions that are not mapped files.
                  - Mapped      => All 'MEM_MAPPED' regions ('MappedFile' and 'Shareable').
                  - Image       => Mapped images. Essentially the same as mapped files, but these are PE images.
                  - All         => All the regions.
        
              You can combine them with '|': Stack|Heap|MappedFile.
              Can't be used with files.
        
              ATTENTION: Have in mind that the more region types you chose the longer is going to take to read all of it.
              If you include images, mapped files, or shareable you're basically reading files that are mapped in the process memory space, or memory that is shareable between processes.
              There's going to be a LOT of useless data.
            
            - Filter type: Defines which technic to use when filtering strings.
              If '-f' is not used this parameter is ignored.
              The accepted values are:
                  - Regex       => Considers the filter string as a Regex pattern.
                  - PowerShell  => Considers the filter string as a PowerShell wildcard pattern.
        
            - Filter options: The accepted values depends on the type of filter being used:
                  - Regex       => None, IgnoreCase, Multiline, ExplicitCapture, Compiled, Singleline, IgnorePatternWhitespace, RightToLeft, ECMAScript, and CultureInvariant.
                  - PowerShell  => None, Compiled, IgnoreCase, and CultureInvariant. Default is 'CultureInvariant|IgnoreCase'.
        
              When parsing big files using 'Compiled' can introduce performance benefits.
              You can find more information about each options in the Microsoft documentation for the 'System.Text.RegularExpressions.RegexOptions' type:
              https://learn.microsoft.com/dotnet/api/system.text.regularexpressions.regexoptions
        
        
        Example:
        
            ManagedStrings.exe C:\Windows\System32\kernel32.dll
            ManagedStrings.exe C:\SomeFolder
            ManagedStrings.exe C:\SomeFolder -r -e Unicode -ub "BasicLatin|LatinExtensions"
            ManagedStrings.exe C:\Windows\System32\*.dll -po -pe -pf
            ManagedStrings.exe C:\Windows\System32\*.dll -po -pe -pf Relative
            ManagedStrings.exe C:\SomeImage.exe -f "(?<=Tits).*?(?=Tits)" -o C:\ManagedStringsOutput.json -ot Json
            ManagedStrings.exe -pid 666 -o C:\ManagedStringsOutput.txt
            ManagedStrings.exe -pid 666 -l 66642069 -n 5 -pp All -mt "Heap|Stack"
        
        """);
    }

    /// <summary>
    /// Prints the unicode block information.
    /// </summary>
    private static void PrintUnicodeBlockInfo()
    {
        Console.WriteLine($"""

        ManagedStrings v{Assembly.GetExecutingAssembly().GetName().Version!.ToString(3)} - Search for strings in binary files or processes address space.
        Copyright (C) 2024 Frank Nabas Labs
        https://github.com/FranciscoNabas
        
        Unicode Block Information.
            
            Unicode blocks, also refered to as Unicode groups, or Unicode ranges are code point blocks that categorize characters.
            The default block is 'BasicLatin', that covers all ASCII characters (0x00..0x7F).
            You can combine multiple blocks with '|', E.g., 'LatinExtensions|Cyrillic', or 'All' to include all blocks.
            
            We only print strings that belong to the same block, I.e., stringgs bigger than '-n' that are from the same block.
            The exception is 'BasicLatin' and 'LatinExtensions'. Those can appear in the same string.
            Regardless of the input 'BasicLatin' is always included.
        
            ATTENTION: The more groups you input the bigger is the garbage output, simply because the character set is bigger.
            Due to the extensive number of blocks, some of them were combined into a single block. Those will be depicted below.
        
        
        Supported options. The ones indented denote a group. Only the parent name is valid.
        
            BasicLatin:                   [0x0000..0x007F]    EnclosedAlphanumerics:                  [0x2460..0x24FF]    LetterlikeSymbols:                               [0x2100..0x214F]
            AlphabeticPresentationForms:  [0xFB00..0xFB4F]    EnclosedCjkLettersandMonths:            [0x3200..0x32FF]    Limbu:                                           [0x1900..0x194F]
            Armenian:                     [0x0530..0x058F]    GeneralPunctuation:                     [0x2000..0x206F]    Lisu:                                            [0xA4D0..0xA4FF]
            Arrows:                       [0x2190..0x21FF]    GeometricShapes:                        [0x25A0..0x25FF]    Malayalam:                                       [0x0D00..0x0D7F]
            Balinese:                     [0x1B00..0x1B7F]    Glagolitic:                             [0x2C00..0x2C5F]    Mandaic:                                         [0x0840..0x085F]
            Bamum:                        [0xA6A0..0xA6FF]    Gujarati:                               [0x0A80..0x0AFF]    MathematicalOperators:                           [0x2200..0x22FF]
            Batak:                        [0x1BC0..0x1BFF]    Gurmukhi:                               [0x0A00..0x0A7F]    MeeteiMayek:                                     [0xABC0..0xABFF]
            Bengali:                      [0x0980..0x09FF]    HalfwidthandFullwidthForms:             [0xFF00..0xFFEF]    MeeteiMayekExtensions:                           [0xAAE0..0xAAFF]
            BlockElements:                [0x2580..0x259F]    Hanunoo:                                [0x1720..0x173F]    ModifierToneLetters:                             [0xA700..0xA71F]
            BoxDrawing:                   [0x2500..0x257F]    Hebrew:                                 [0x0590..0x05FF]    Mongolian:                                       [0x1800..0x18AF]
            BraillePatterns:              [0x2800..0x28FF]    Hiragana:                               [0x3040..0x309F]    NewTaiLue:                                       [0x1980..0x19DF]
            Buginese:                     [0x1A00..0x1A1F]    IdeographicDescriptionCharacters:       [0x2FF0..0x2FFF]    NKo:                                             [0x07C0..0x07FF]
            Buhid:                        [0x1740..0x175F]    IpaExtensions:                          [0x0250..0x02AF]    NumberForms:                                     [0x2150..0x218F]
            Cham:                         [0xAA00..0xAA5F]    Javanese:                               [0xA980..0xA9DF]    Ogham:                                           [0x1680..0x169F]
            CombiningHalfMarks:           [0xFE20..0xFE2F]    Kanbun:                                 [0x3190..0x319F]    OlChiki:                                         [0x1C50..0x1C7F]
            CommonIndicNumberForms:       [0xA830..0xA83F]    KangxiRadicals:                         [0x2F00..0x2FDF]    OpticalCharacterRecognition:                     [0x2440..0x245F]
            ControlPictures:              [0x2400..0x243F]    Kannada:                                [0x0C80..0x0CFF]    Oriya:                                           [0x0B00..0x0B7F]
            Coptic:                       [0x2C80..0x2CFF]    KayahLi:                                [0xA900..0xA92F]    Phagspa:                                         [0xA840..0xA87F]
            CurrencySymbols:              [0x20A0..0x20CF]    Lao:                                    [0x0E80..0x0EFF]    Rejang:                                          [0xA930..0xA95F]
            Dingbats:                     [0x2700..0x27BF]    Lepcha:                                 [0x1C00..0x1C4F]    Runic:                                           [0x16A0..0x16FF]
            Samaritan:                    [0x0800..0x083F]    VariationSelectors:                     [0xFE00..0xFE0F]    Bopomofo:
            Saurashtra:                   [0xA880..0xA8DF]    VedicExtensions:                        [0x1CD0..0x1CFF]        Bopomofo:                                    [0x3100..0x312F]
            Sinhala:                      [0x0D80..0x0DFF]    VerticalForms:                          [0xFE10..0xFE1F]        BopomofoExtended:                            [0x31A0..0x31BF]
            SmallFormVariants:            [0xFE50..0xFE6F]    YijingHexagramSymbols:                  [0x4DC0..0x4DFF]    Cherokee:
            SpacingModifierLetters:       [0x02B0..0x02FF]    LatinExtensions:                                                Cherokee:                                    [0x13A0..0x13FF]
            Specials:                     [0xFFF0..0xFFFF]        Latin1Supplement:                   [0x0080..0x00FF]        CherokeeSupplement:                          [0xAB70..0xABBF]
            SuperscriptsandSubscripts:    [0x2070..0x209F]        LatinExtendedA:                     [0x0100..0x017F]    Cjk:
            SylotiNagri:                  [0xA800..0xA82F]        LatinExtendedAdditional:            [0x1E00..0x1EFF]        CjkCompatibility:                            [0x3300..0x33FF]
            Tagalog:                      [0x1700..0x171F]        LatinExtendedB:                     [0x0180..0x024F]        CjkCompatibilityForms:                       [0xFE30..0xFE4F]
            Tagbanwa:                     [0x1760..0x177F]        LatinExtendedC:                     [0x2C60..0x2C7F]        CjkCompatibilityIdeographs:                  [0xF900..0xFAFF]
            TaiLe:                        [0x1950..0x197F]        LatinExtendedD:                     [0xA720..0xA7FF]        CjkRadicalsSupplement:                       [0x2E80..0x2EFF]
            TaiTham:                      [0x1A20..0x1AAF]        LatinExtendedE:                     [0xAB30..0xAB6F]        CjkStrokes:                                  [0x31C0..0x31EF]
            TaiViet:                      [0xAA80..0xAADF]    Arabic:                                                         CjkSymbolsandPunctuation:                    [0x3000..0x303F]
            Tamil:                        [0x0B80..0x0BFF]        Arabic:                             [0x0600..0x06FF]        CjkUnifiedIdeographs:                        [0x4E00..0x9FFF]
            Telugu:                       [0x0C00..0x0C7F]        ArabicExtendedA:                    [0x08A0..0x08FF]        CjkUnifiedIdeographsExtensionA:              [0x3400..0x4DBF]
            Thaana:                       [0x0780..0x07BF]        ArabicExtendedB:                    [0x0870..0x089F]    CombiningDiacriticalMarks:
            Thai:                         [0x0E00..0x0E7F]        ArabicPresentationFormsA:           [0xFB50..0xFDFF]        CombiningDiacriticalMarks:                   [0x0300..0x036F]
            Tibetan:                      [0x0F00..0x0FFF]        ArabicPresentationFormsB:           [0xFE70..0xFEFF]        CombiningDiacriticalMarksExtended:           [0x1AB0..0x1AFF]
            Tifinagh:                     [0x2D30..0x2D7F]        ArabicSupplement:                   [0x0750..0x077F]        CombiningDiacriticalMarksforSymbols:         [0x20D0..0x20FF]
            Vai:                          [0xA500..0xA63F]                                                                    CombiningDiacriticalMarksSupplement:         [0x1DC0..0x1DFF]
            Cyrillic:                                         Hangul:                                                     PhoneticExtensions:
                Cyrillic:                 [0x0400..0x04FF]        HangulCompatibilityJamo:            [0x3130..0x318F]        PhoneticExtensions:                          [0x1D00..0x1D7F]
                CyrillicExtendedA:        [0x2DE0..0x2DFF]        HangulJamo:                         [0x1100..0x11FF]        PhoneticExtensionsSupplement:                [0x1D80..0x1DBF]
                CyrillicExtendedB:        [0xA640..0xA69F]        HangulJamoExtendedA:                [0xA960..0xA97F]    Sundanese:
                CyrillicExtendedC:        [0x1C80..0x1C8F]        HangulJamoExtendedB:                [0xD7B0..0xD7FF]        Sundanese:                                   [0x1B80..0x1BBF]
                CyrillicSupplement:       [0x0500..0x052F]        HangulSyllables:                    [0xAC00..0xD7AF]        SundaneseSupplement:                         [0x1CC0..0x1CCF]
            Devanagari:                                       Katakana:                                                   Supplemental:
                Devanagari:               [0x0900..0x097F]        Katakana:                           [0x30A0..0x30FF]        SupplementalArrowsA:                         [0x27F0..0x27FF]
                DevanagariExtended:       [0xA8E0..0xA8FF]        KatakanaPhoneticExtensions:         [0x31F0..0x31FF]        SupplementalArrowsB:                         [0x2900..0x297F]
            Ethiopic:                                         Khmer:                                                          SupplementalMathematicalOperators:           [0x2A00..0x2AFF]
                Ethiopic:                 [0x1200..0x137F]        Khmer:                              [0x1780..0x17FF]        SupplementalPunctuation:                     [0x2E00..0x2E7F]
                EthiopicExtended:         [0x2D80..0x2DDF]        KhmerSymbols:                       [0x19E0..0x19FF]    Syriac:
                EthiopicExtendedA:        [0xAB00..0xAB2F]    Miscellaneous:                                                  Syriac:                                      [0x0700..0x074F]
                EthiopicSupplement:       [0x1380..0x139F]        MiscellaneousMathematicalSymbolsA:  [0x27C0..0x27EF]        SyriacSupplement:                            [0x0860..0x086F]
            Georgian:                                             MiscellaneousMathematicalSymbolsB:  [0x2980..0x29FF]    UnifiedCanadianAboriginalSyllabics:
                Georgian:                 [0x10A0..0x10FF]        MiscellaneousSymbols:               [0x2600..0x26FF]        UnifiedCanadianAboriginalSyllabics:          [0x1400..0x167F]
                GeorgianExtended:         [0x1C90..0x1CBF]        MiscellaneousSymbolsandArrows:      [0x2B00..0x2BFF]        UnifiedCanadianAboriginalSyllabicsExtended:  [0x18B0..0x18FF]
                GeorgianSupplement:       [0x2D00..0x2D2F]        MiscellaneousTechnical:             [0x2300..0x23FF]    Yi:
            GreekandCoptic:                                   Myanmar:                                                        YiRadicals:                                  [0xA490..0xA4CF]
                GreekandCoptic:           [0x0370..0x03FF]        Myanmar:                            [0x1000..0x109F]        YiSyllables:                                 [0xA000..0xA48F]
                GreekExtended:            [0x1F00..0x1FFF]        MyanmarExtendedA:                   [0xAA60..0xAA7F]
                                                                  MyanmarExtendedB:                   [0xA9E0..0xA9FF]
        
        """);
    }

    /// <summary>
    /// Prints the debug header.
    /// </summary>
    /// <param name="options">The command line options.</param>
    /// <exception cref="ArgumentException">Invalid <see cref="Mode"/>.</exception>
    private static void PrintCommandLineOptions(CommandLineOptions options)
    {
        string bytesToScan = options.BytesToScan == 0 ? "All" : options.BytesToScan.ToString();

        string optionSpecificText = options.Mode switch {
            Mode.SingleFile => $"""
            Path: {options.Path}.
                Print file name: {options.PrintFileName}.
            """,
            Mode.Process => $"""
            Process ID: {options.ProcessId.IntegerListToString()}.
                Process memory region: {options.MemoryRegionType}.
                Process info: {options.ProcessInfoType}.
            """,
            Mode.Directory or Mode.FileSystemWildcard => $"""
            Path: {options.Path}.
                Recurse: {options.Recurse}.
                Print file name: {options.PrintFileName}.
            """,
            _ => throw new ArgumentException($"Invalid mode '{options.Mode}'."),
        };

        string filterOptions = options.FilterType == FilterType.Regex ? $"Filter options: {options.RegexFilterOptions}." : $"Filter options: {options.WildcardFilterOptions}.";
        string filterSpecificText = options.Filter is null ? "Filter: None."
            : $"""
              Filter: {options.Filter}.
                  Filter type: {options.FilterType}.
                  {filterOptions}
              """;

        string unicodeBlocksText = (!options.Encoding.HasEncodingFlag(ValidEncoding.Unicode) && !options.Encoding.HasEncodingFlag(ValidEncoding.UTF8))
            ? "Unicode blocks: N/A." : $"Unicode blocks: {options.UnicodeBlocks}.";

        string outFileText = options.OutFile is null ? "Out file: N/A." : $"""
        Out file: {options.OutFile}.
            Out file type: {options.OutputFileType}.
        """;

        Console.WriteLine($"""

        ManagedStrings v{Assembly.GetExecutingAssembly().GetName().Version!.ToString(3)} - Search for strings in binary files or processes address space.
        Copyright (C) 2024 Frank Nabas Labs
        https://github.com/FranciscoNabas
        
        Options:
            Mode: {options.Mode}.
            {optionSpecificText}
            {filterSpecificText}
            Exclude control code points: {options.ExcludeControlCp}.
            Encoding: {options.Encoding}.
            {unicodeBlocksText}
            Start offset: {options.StartOffset}.
            Bytes to scan: {bytesToScan}.
            Minimum string length: {options.MinStringLength}.
            Buffer size: {options.BufferSize}.
            Print offset: {options.PrintOffset}.
            Print encoding: {options.PrintEncoding}.
            Print header: {options.PrintHeader}.
            Print file name: {options.PrintFileName}.
            {outFileText}
            Delimiter char: {options.DelimiterChar}.
            Force: {options.Force}.
            Synchronous: {options.Synchronous}.

        Test options:
            Do benchmark: {options.Benchmark}.
            Run itens async: {options.RunMultipleItensAsync}.
            Console buffer size: {options.ConsoleBufferSize}.
            Console use driver: {options.ConsoleUseDriver}.

        ---- END OF DEBUG HEADER ----


        """);
    }
}