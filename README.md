<img align="left" src="Resources/ManagedStrings.ico" width="128" height="128" style="max-width: 100%;padding-right: 20px;">

<!-- omit in toc -->
## ManagedStrings

The ultimate binary string search utility!  
Searches for strings on binary files and processes virtual memory space.

<!-- omit in toc -->
## Table of contents

- [Command line options](#command-line-options)
- [Remarks](#remarks)
- [Supported values](#supported-values)
  - [-o](#-o)
  - [-fo](#-fo)
  - [-mt](#-mt)
  - [-pf](#-pf)
  - [-pp](#-pp)
  - [-ot](#-ot)
- [Unicode blocks](#unicode-blocks)
- [Examples](#examples)
- [Test parameters](#test-parameters)
  - [--TestDoBenchmark](#--testdobenchmark)
  - [--TestConsoleBufferSize](#--testconsolebuffersize)
  - [--TestConsoleUseDriver](#--testconsoleusedriver)
  - [--TestRunItemsAsync](#--testrunitemsasync)
- [Building and publishing](#building-and-publishing)
- [Code referencing](#code-referencing)
- [A very special thanks](#a-very-special-thanks)

## Command line options

ManagedStrings.exe \[-pid\] \[-mt\] \[-pp\] \[\<common parameters\>\]  
ManagedStrings.exe \<file or directory\> \[-r\] \[-pf\] \[\<common parameters\>\]  
  
Common parameters: \[-e\] \[-o\] \[-l\] \[-n\] \[-b\] \[-f\] \[-ft\] \[-fo\] \[-ug\] \[-ec\] \[-po\] \[-pe\] \[-ph\] \[-o\] \[-ot\] \[-d\] \[--sync\] \[--force\]  
  
| Parameter  | Description                                                                                                                                                             |
|:-----------|:------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| -?/-h      | Print help message. Use '-? UnicodeBlocks' to print a list of supported Unicode blocks.                                                                                 |
| -e         | Encoding. Default is 'UTF8' and 'Unicode'. See [Supported values](#supported-values) for a complete list.                                                               |
| -o         | Scan starting offset. Default is 0.                                                                                                                                     |
| -l         | Number of bytes to scan. Default is 0 (All).                                                                                                                            |
| -n         | Minimum string length. Default is 3.                                                                                                                                    |
| -b         | Buffer size in bytes. Default is 1Mb. See [Remarks](#remarks) for more info.                                                                                            |
| -f         | Filter string. The way it's used depends on the filter type.                                                                                                            |
| -ft        | Filter type. Accepted values are 'Regex' and 'PowerShell'. Default is 'Regex'. See [Remarks](#remarks) for more info.                                                   |
| -fo        | Filter options. If '-f' is not used this parameter is ignored. See [Supported values](#supported-values) for a complete list.                                           |
| -ub        | Unicode blocks. Default is 'BasicLatin'. Use '-? UnicodeBlocks' to see the list of supported Unicode blocks.                                                            |
| -pid       | One or more process IDs to scan the virtual memory, separated by a comma.                                                                                               |
| -mt        | Process virtual memory region type. Default is 'Private'. See [Supported values](#supported-values) for a complete list.                                                |
| -r         | Searches for files recursively. Path must be a directory or contain wildcards.                                                                                          |
| -ec        | Exclude the control code points 'HT', 'LF', 'CR' (Tab, Line Feed/New Line, and Carriage Return respectively).                                                           |
| -po        | Print offset where string was located.                                                                                                                                  |
| -pe        | Print the encoding of every string.                                                                                                                                     |
| -pf        | Print file name. If no input is provided it prints the file name. See [Supported values](#supported-values) for a complete list.                                        |
| -pp        | Print process information when searching for strings in a process memory space. Default is 'ProcessId'. See [Supported values](#supported-values) for a complete list.  |
| -ph        | Print header. To be used with the other '-p*' options, or if the output file is 'Csv' or 'PlainText'.                                                                   |
| -o         | Output file. It can be a file path, or 'Console' to write the serialized results to the console. See [Remarks](#remarks) for more info.                                 |
| -ot        | Output file type. When '-o' is used specifies the file type. Default is 'PlainText'. See [Supported values](#supported-values) for a complete list.                     |
| -d         | Delimiter character. Used with the other '-p*' options, or if the output file is 'Csv' or 'PlainText'. Default is a comma.                                              |
| --sync     | Forces synchronous string search. See [Remarks](#remarks) for more info.                                                                                                |
| --force    | When used with '-o' if the file already exists it overwrites it instead of returning an exception. If used without '-o' this parameter is ignored.                      |
  
## Remarks

- File name: You can use the wildcard characters '*' and '?' to filter for file or names.  
E.g., C:\Windows\System32\\*.dll. For more information check the remarks at [DirectoryInfo.GetFiles Method][01].

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

  | Option | Name                  | Description                                                                                                             |
  |:-------|:----------------------|:------------------------------------------------------------------------------------------------------------------------|
  | -o     | Start offset          | From '0' to '9223372036854775807'. Can't be bigger than the stream length.                                              |
  | -l     | Bytes to scan         | From '0' to '9223372036854775807', '0' means everything. Can't be bigger than the stream length minus the start offset. |
  | -n     | Minimum string length | From '1' to '4294967295', since '0' wouldn't print anything.                                                            |
  | -b     | Buffer size           | From '1' to '2147483647'.                                                                                               |
  | -pid   | Process ID            | From '0' to '4294967295'. Must be a valid currently running process ID.                                                 |
  
- Print header and delimiter character: These are used when one of the '-p*' parameters are used, or if the output file type is 'Csv'.  
They are ignored otherwise.

- Encoding: Since UTF8 is backwards compatible with ASCII by default we only include Unicode and UTF8. You can use them together,  
but if you do UTF8 is chosen by default to avoid printing repeated strings.

- Filter type: Defines which technic to use when filtering strings.  
If '-f' is not used this parameter is ignored.  

  | Option     | Description                                                         |
  |:-----------|:--------------------------------------------------------------------|
  | Regex      | Considers the filter string as a [Regex][02] pattern.               |
  | PowerShell | Considers the filter string as a [PowerShell wildcard pattern][03]. |

## Supported values

These are the supported values for some of the parameters.  
For combinable values you can combine them with '|'.

### -o

Encoding(s). The encoding(s) used to decode strings.  
Default: UTF8, Unicode.

| Value   | Description                         |
|:--------|:------------------------------------|
| ASCII   | Single byte ASCII encoding.         |
| UTF8    | Multi-byte UTF8 encoding.¹          |
| Unicode | Two-byte Unicode (UTF16) encoding.¹ |

¹ - UTF8 and Unicode only includes characters from the [Basic Multilingual Plane][04].  
Unicode characters without surrogate and UTF8 characters with a maximum of 3 bytes.

### -fo

Filter options.  
Regex default: None.  
PowerShell default: CultureInvariant, IgnoreCase.  
For more information on each option see [RegexOptions Enum][05] and [Regular expression options][06].

| Value                   | Accepted With     | Description                                                                                                                                                     |
|:------------------------|:------------------|:----------------------------------------------------------------------------------------------------------------------------------------------------------------|
| None                    | Regex, PowerShell | No options.                                                                                                                                                     |
| IgnoreCase              | Regex, PowerShell | Ignores case when matching strings.                                                                                                                             |
| Multiline               | Regex             | Changes the meaning of '^' and '$' so they match at the beginning and end, respectively, of any line, and not just the beginning and end of the entire string.  |
| ExplicitCapture         | Regex             | Specifies that the only valid captures are explicitly named or numbered groups of the form (?\<name\>...).                                                      |
| Compiled                | Regex, PowerShell | Specifies that the regular expression is compiled to MSIL code, instead of being interpreted.                                                                   |
| Singleline              | Regex             | Specifies single-line mode. Changes the meaning of the dot (.) so it matches every character (instead of every character except \\n).                           |
| IgnorePatternWhitespace | Regex             | Eliminates unescaped white space from the pattern and enables comments marked with #.                                                                           |
| RightToLeft             | Regex             | Specifies that the search will be from right to left instead of from left to right.                                                                             |
| ECMAScript              | Regex             | Enables ECMAScript-compliant behavior for the expression. This value can be used only in conjunction with the 'IgnoreCase', 'Multiline', and 'Compiled' values. |
| CultureInvariant        | Regex, PowerShell | Specifies that cultural differences in language is ignored.                                                                                                     |
| NonBacktracking         | Regex             | Enable matching using an approach that avoids backtracking and guarantees linear-time processing in the length of the input.                                    |

### -mt

Virtual memory region type(s). The virtual memory region type(s) to include while searching strings in processes virtual memory space.

| Value      | Description                                                                               |
|:-----------|:------------------------------------------------------------------------------------------|
| Stack      | Thread stack.                                                                             |
| Heap       | NT Heap, Segment Heap, and NT Heap segments.                                              |
| Private    | Includes Stack, Heap(s), PEB, TEB, other smaller regions and the remaining private data.¹ |
| MappedFile | Memory mapped files.                                                                      |
| Shareable  | Other 'MEM_MAPPED' regions that are not mapped files.                                     |
| Mapped     | All 'MEM_MAPPED' regions ('MappedFile' and 'Shareable').                                  |
| Image      | Mapped images. Essentially the same as mapped files, but these are PE images.             |
| All        | The processes entire readable memory virtual space.                                       |

¹ - The 'Private' option includes these regions:
Teb, Peb, UserSharedData, HypervisorSharedData, CfgBitmap, ApiSetMap, ReadOnlySharedMemory, CodePageData, GdiSharedHandleTable,  
ShimData, ActivationContextData, ProcessActivationContext, SystemActivationContext, WerRegistrationData, SiloSharedData, TelemetryCoverage,  
Stack, NtHeap, NtLfhHeap, SegmentHeap, NtHeapSegment, NtLfhSegment, SegmentHeapSegment, PrivateData
  
### -pf

Print file name. The format to print the file name.  
Default: Name.

| Value    | Description                                    |
|:---------|:-----------------------------------------------|
| Name     | Only the file name.                            |
| Relative | The file path relative to the root directory.¹ |
| FullPath | The file full path.                            |
  
¹ - E.g.:  
Root directory: C:\Windows\System32  
File path: C:\Windows\System32\wbem\Repository\OBJECTS.DATA  
Relative path: wbem\Repository\OBJECTS.DATA  
  
### -pp

Process information. Print information about the process and the memory regions.

| Value      | Description                                                                                                          |
|:-----------|:---------------------------------------------------------------------------------------------------------------------|
| ProcessId  | The process unique identifier.                                                                                       |
| MemoryType | The memory region type.                                                                                              |
| Details    | Details about the memory region. E.g., for heaps the heap ID, for stack the thread ID, for files the file name, etc. |
| All        | All of the above.                                                                                                    |

### -ot

Output file type.  
Default: PlainText.

| Value     | Description                                         |
|:----------|:----------------------------------------------------|
| PlainText | Prints/Saves the text following the '-p*' rules.¹   |
| Csv       | Prints/Saves the text following the '-p*' rules.¹   |
| Xml       | Prints/Saves the text in the XML format. Indented.  |
| Json      | Prints/Saves the text in the JSON format. Indented. |

¹ - As of now 'PlainText' and 'Csv' have the same effect.

## Unicode blocks

The tool supports the use of different Unicode blocks to determine what gets printed.  
Unicode blocks, also referred to as Unicode groups, or Unicode ranges are code point blocks that categorize characters.  
We only print strings with characters that belong to the same block, I.e., strings bigger than **-n** where each character code point belong to the same block.  
The exception is 'BasicLatin' and 'LatinExtensions'. Those can appear in the same string.  
Regardless of the input 'BasicLatin' is always included.  
  
You chose supported unicode blocks using the **-ub** parameter and combine them with '|'.  
To get the extensive list of supported groups run the program with `-? UnicodeBlocks`.  
  
ATTENTION: Most binary files don't contain meaningful strings with characters outside the `BasicLatin` block.  
The more blocks you add the more garbage will be printed to the screen.  
  
The Unicode blocks we support were extracted from the .NET [UnicodeRanges][08] and [UnicodeRange][09] classes.  
Due to the massive number of different ranges some of them were combined.  
When you run the program with `-? UnicodeBlocks` it shows the combined ranges.

## Examples

| Example                                                                           | Description                                                                                                                 |
|:----------------------------------------------------------------------------------|:----------------------------------------------------------------------------------------------------------------------------|
| `ManagedStrings.exe C:\Windows\System32\kernel32.dll`                             | Searches for binary strings on the file `kernel32.dll`. |
| `ManagedStrings.exe C:\Windows`                                                   | Searches for binary strings on all files under `C:\Windows`. |
| `ManagedStrings.exe C:\Windows -r -e Unicode`                                     | Searches for Unicode binary strings on files under `C:\Windows` recursively. |
| `ManagedStrings.exe C:\Windows\System32\*.dll -po -pe -pf`                        | Searches for binary strings on all files terminating in *.dll* under `C:\Windows\System32`, printing the offset, encoding, and file name. |
| `ManagedStrings.exe C:\Windows\System32\*.dll -r -pf Relative`                    | Searches for binary strings on all files terminating in *.dll* under `C:\Windows\System32` recursively, printing the relative file name. |
| `ManagedStrings.exe C:\SomeImage.exe -f "(?<=Tits).*?(?=Tits)"`                   | Searches for binary strings on `SomeImage.exe` using a Regex filter. |
| `ManagedStrings.exe C:\SomeImage.exe -f "*Tits*" -ft PowerShell -o C:\Output.txt` | Searches for binary strings on `SomeImage.exe` using a PowerShell filter and outputting to `C:\Output.txt`. |
| `ManagedStrings.exe -pid 666 -o C:\Output.xml -ot Xml`                            | Searches for binary strings on the process with ID 666 and outputs to `C:\Output.xml` in XML. |
| `ManagedStrings.exe -pid 666 -l 66642069 -n 5 -pp All -mt "Heap\|Stack"`          | Searches for binary strings on the process with ID 666 for a total of 66642069 bytes, only on Heap and Stack regions, outputting all process information. |
| `ManagedStrings.exe C:\SomeImage.exe -ub "BasicLatin\|LatinExtensions"`           | Searches for binary strings on `SomeImage.exe` including the Unicode blocks 'BasicLatin' and 'LatinExtensions'. |

## Test parameters

There are some parameters that were included for testing and ended up in the final release.  
These parameters are not visible with the help, but you can still use them.  

### --TestDoBenchmark

This switch enables a very rudimentary form of benchmarking which measures the time it took to process each file or process, and prints it at the end.

### --TestConsoleBufferSize

This parameter controls the buffer size of the console stream.  
With bigger console buffer sizes the application can perform better depending on the system, but it's not guaranteed.  
Note that with buffer sizes above 100Kb the output on the console may appear 'choppy', simply because we are flushing in bigger intervals.  
There's also a point where the performance will decrease if using a big enough buffer size. This happens because the cost of traversing this buffer became bigger than writing to the console itself.  
The default console buffer size is 80Kb. The theoretical maximum buffer size is 2147483647.  
ATTENTION: Using ultra big buffers were not tested and might break things.  

### --TestConsoleUseDriver

This switch enables writing to the console using the 'ConDrv' API.  
In other words it issues the write request directly to the console driver using an IO request.  
ATTENTION: During tests there was no performance gains writing directly to the console driver mainly because it increases the complexity immensely and we have to do a lot of unmanaged memory management and pointer arithmetic.  
To see how this works see [ConsoleAPI.cs][07].  

### --TestRunItemsAsync

This switch enables running multiple files or processes asynchronously.  
With this switch there will be two asynchronous operations, the one for each file/process, and the one for decoding each encoding.  
During tests there was no performance gains while running multiple items asynchronously mainly because we can only write to the console atomically.  
In some cases this might affect performance negatively because there's the extra overhead of managing the tasks.  
This switch has no effect if used with **--sync**.

## Building and publishing

This project doesn't have a special build procedure, if you have Visual Studio with the .NET SDK 9.0 you should be able to build it.  
For publishing since we are using options to trim the code you should use the [Publish.ps1][15] script.  
The output of the script will be at `out\Release\win-x64` or at the root as `ManagedStrings.zip` if you used the `-Compress` switch.

## Code referencing

Like almost everything on computer science these days this program contains a lot of code that was 'inspired' by already existing code.  
The application was originally developed for .NET Framework 4.7.2, which doesn't contain a lot of the improvements that the newer .NET versions have, and some of these things were ported to it.  
Furthermore in places where we had to implement our own version of an API, like the [WindowsConsole][10] or the [Console strategies][11] we based our code in already existing runtime libraries.  
In places where we based our code in existing runtime libraries the type was annotated with the *\<runtimefile\>\<\\runtimefile\>* tag, containing the file within the runtime used.  
There are places where this tag doesn't take you to a dotnet runtime file, but other MIT projects, like the case with [WildcardPattern][12], where we based our code from the PowerShell core project.  

## A very special thanks

In this project we don't reference code only from the runtime, or PowerShell.  
Like almost all my projects this one would most definitely not be possible without the help of one of the most well written software out there. The mighty [SystemInformer][13].  
This project has one of the most complete and well documented collection of undocumented Windows APIs and their usage.  
It's not only a source of knowledge, but a very pleasant and complete tool to use. I use it daily as a Systems Administrator.  
I recommend you to check it out and giving them some love. The official repository as of now is at [SystemInformer GitHub][14].  
A very special thanks to them!

<!-- Links -->

[01]: https://learn.microsoft.com/dotnet/api/system.io.directoryinfo.getfiles
[02]: https://learn.microsoft.com/dotnet/standard/base-types/regular-expressions
[03]: https://learn.microsoft.com/powershell/module/microsoft.powershell.core/about/about_wildcards
[04]: https://en.wikipedia.org/wiki/Plane_(Unicode)#Basic_Multilingual_Plane
[05]: https://learn.microsoft.com/dotnet/api/system.text.regularexpressions.regexoptions
[06]: https://learn.microsoft.com/dotnet/standard/base-types/regular-expression-options
[07]: src/Interop/Windows/ConsoleAPI.cs
[08]: https://learn.microsoft.com/dotnet/api/system.text.unicode.unicoderanges
[09]: https://learn.microsoft.com/dotnet/api/system.text.unicode.unicoderange
[10]: src/Engine/Console/Console.cs
[11]: src/Engine/Console/ConsoleStrategy.cs
[12]: src/Filtering/WildcardPattern.cs
[13]: https://systeminformer.com/
[14]: https://github.com/winsiderss/systeminformer
[15]: Tools/Publish.ps1
