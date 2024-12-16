// This file is part of the ManagedStrings project and repository.
// Project files are licensed under the MIT license.
// https://github.com/FranciscoNabas/ManagedStrings

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using ManagedStrings.Interop.Windows;

namespace ManagedStrings.Engine.Console;

// For almost all applications the 'System.Console' class is perfectly fine. In fact
// the first tests we made we use it without problems.
// The motivation for writing our own console class is because 'System.Console' uses a
// 'StreamWriter' with AutoFlush on. That means there's no buffer between writes to the console.
// This affects performance heavily in our case, since we are writing thousands of strings to the console.
// Our console class is almost a perfect copy of the original 'System.Console', with the exception of the
// stream we are using to write and read.
// For the implementation of this buffered console stream check out 'ConsoleStrategy.cs'.

/// <summary>
/// Static methods for writing to the console.
/// </summary>
/// <runtimefile>src/libraries/System.Console/src/System/Console.cs</runtimefile>
/// <runtimefile>src/libraries/System.Console/src/System/ConsolePal.Windows.cs</runtimefile>
public static class WindowsConsole
{
    // Read buffer size. For more info see the comments on 'System.Console'.
    private const int ReadBufferSize = 4096;

    // The minimum write buffer length. Initially we also used a 'StreamWriter'
    // between our console and stream. The minimum buffer size for it is 128 bytes.
    private const int MinWriteBufferSize = 128;

    internal const int UnicodeCodePage = 1200;
    internal const ConsoleColor UnknownColor = (ConsoleColor)(-1);

    // The lock used to change certain properties.
    private static readonly Lock s_lock = new();

    private static int s_bufferSize;
    private static bool s_useDriver;
    private static bool s_autoFlush = true;

    private static StrongBox<bool>? s_isInputRedirected;
    private static StrongBox<bool>? s_isOutputRedirected;
    private static StrongBox<bool>? s_isErrorRedirected;
    private static ConsoleStreamStrategy? s_inStrategy, s_outStrategy, s_errStrategy;
    
    private static TextReader? s_in;

    /// <summary>
    /// The console input stream.
    /// </summary>
    /// <remarks>
    /// We don't use the 'Read' functionalities of this class on this program.
    /// So, to avoid the complications of writing our own read methods we use a 'StreamReader'.
    /// </remarks>
    internal static TextReader In {
        get {
            return Volatile.Read(ref s_in) ?? EnsureInitialized();

            static TextReader EnsureInitialized()
            {
                lock (s_lock) {
                    if (s_in is null)
                        Volatile.Write(ref s_in, GetInputReader());

                    return s_in!;
                }
            }
        }
    }

    /// <summary>
    /// The console output stream.
    /// </summary>
    internal static ConsoleStreamStrategy Out {
        get {
            return Volatile.Read(ref s_outStrategy) ?? EnsureInitialized();

            static ConsoleStreamStrategy EnsureInitialized()
            {
                lock (s_lock) {
                    if (s_outStrategy is null)
                        Volatile.Write(ref s_outStrategy, OpenStandardOutput());

                    return s_outStrategy!;
                }
            }
        }
    }

    /// <summary>
    /// The console error stream.
    /// </summary>
    /// <remarks>
    /// We don't use the error stream on this program.
    /// </remarks>
    internal static ConsoleStreamStrategy Error {
        get {
            return Volatile.Read(ref s_errStrategy) ?? EnsureInitialized();

            static ConsoleStreamStrategy EnsureInitialized()
            {
                lock (s_lock) {
                    if (s_errStrategy is null)
                        Volatile.Write(ref s_errStrategy, OpenStandardError());

                    return s_errStrategy!;
                }
            }
        }
    }

    /// <summary>
    /// The console input encoding.
    /// </summary>
    /// <remarks>
    /// We simply interface to the 'System.Console' encoding properties.
    /// It will take care of the unmanaged calls to 'SetConsole*CP' / 'GetConsole*CP'.
    /// </remarks>
    public static Encoding InputEncoding {
        get {
            lock (s_lock)
                return System.Console.InputEncoding;
        }
        set {
            lock (s_lock) {
                if (s_inStrategy is not null) {
                    if (System.Console.OutputEncoding.CodePage != UnicodeCodePage && value.CodePage == UnicodeCodePage)
                        s_inStrategy.IsUnicode = true;
                    else if (System.Console.OutputEncoding.CodePage == UnicodeCodePage && value.CodePage != UnicodeCodePage)
                        s_inStrategy.IsUnicode = false;
                }
         
                System.Console.InputEncoding = value;
            }
        }
    }

    /// <summary>
    /// The console output encoding.
    /// </summary>
    /// <remarks>
    /// When changing the output encoding we need to make sure we're also changing our
    /// stream encoding. This will determine which API is called to write to the console.
    /// </remarks>
    public static Encoding OutputEncoding {
        get {
            lock (s_lock)
                return System.Console.OutputEncoding;
        }
        set {
            lock (s_lock) {
                if (System.Console.OutputEncoding.CodePage != UnicodeCodePage && value.CodePage == UnicodeCodePage) {
                    if (s_outStrategy is not null)
                        s_outStrategy.IsUnicode = true;

                    if (s_errStrategy is not null)
                        s_errStrategy.IsUnicode = true;
                }
                else if (System.Console.OutputEncoding.CodePage == UnicodeCodePage && value.CodePage != UnicodeCodePage) {
                    if (s_outStrategy is not null)
                        s_outStrategy.IsUnicode = false;

                    if (s_errStrategy is not null)
                        s_errStrategy.IsUnicode = false;
                }
                
                System.Console.OutputEncoding = value;
            }
        }
    }

    /// <summary>
    /// The console background color.
    /// </summary>
    /// <remarks>
    /// Not entirely necessary since we could also use the 'System.Console' versions
    /// but at some point I built (copied) this for testing and stuck to it.
    /// </remarks>
    public static ConsoleColor BackgroundColor {
        get => TryGetBufferInfo(false, out CONSOLE_SCREEN_BUFFER_INFO info)
            ? ColorAttributeToConsoleColor((Color)info.wAttributes & Color.BackgroundMask) : ConsoleColor.Black;

        set {
            Color color = ConsoleColorToColorAttribute(value, true);
            if (!TryGetBufferInfo(false, out CONSOLE_SCREEN_BUFFER_INFO info))
                return;

            short attributes = info.wAttributes;
            attributes &= ~((short)Color.BackgroundMask);
            attributes = (short)(((uint)(ushort)attributes) | ((uint)(ushort)color));
            ConsoleAPI.SetConsoleTextAttribute(OutputHandle, attributes);
        }
    }

    /// <summary>
    /// The console foreground color.
    /// </summary>
    public static ConsoleColor ForegroundColor {
        get => TryGetBufferInfo(false, out CONSOLE_SCREEN_BUFFER_INFO info)
            ? ColorAttributeToConsoleColor((Color)info.wAttributes & Color.ForegroundMask) : ConsoleColor.Gray;

        set {
            Color color = ConsoleColorToColorAttribute(value, false);
            if (!TryGetBufferInfo(false, out CONSOLE_SCREEN_BUFFER_INFO info))
                return;

            short attributes = info.wAttributes;
            attributes &= ~((short)Color.ForegroundMask);
            attributes = (short)(((uint)(ushort)attributes) | ((uint)(ushort)color));
            ConsoleAPI.SetConsoleTextAttribute(OutputHandle, attributes);
        }
    }

    /// <summary>
    /// Gets or sets if we use the 'ConDrv' API to write to the console.
    /// </summary>
    /// <remarks>
    /// On our tests there was no noticeable performance increase calling the driver directly,
    /// and it increases the complexity immensely. However it works and we are keeping it as an option.
    /// We don't use it to read from the console. We implemented this reverse engineering 'WriteConsole'
    /// and 'ReadConsole', and for some reason reading breaks the console. Since we don't use reading
    /// I didn't bothered trying to fix it.
    /// </remarks>
    public static bool UseDriver {
        get => s_useDriver;
        set {
            lock (s_lock)
                SetDriverMode(value);
        }
    }

    /// <summary>
    /// Gets or sets the auto-flush.
    /// </summary>
    /// <remarks>
    /// This was mostly used when we had a 'StreamWriter' interfacing between our streams.
    /// We set this to true when not using the buffered console strategy.
    /// </remarks>
    public static bool AutoFlush {
        get => s_autoFlush;
        set {
            lock (s_lock)
                SetAutoFlush(value);
        }
    }

    /// <summary>
    /// Gets or sets the console stream buffer size.
    /// </summary>
    /// <remarks>
    /// If we are using an unbuffered console stream (default), and set the buffer size
    /// We must dispose of this stream and create a buffered one.
    /// </remarks>
    /// <seealso cref="WindowsConsoleStreamStrategy"/>
    /// <seealso cref="BufferedConsoleStreamStrategy"/>
    public static int BufferSize {
        get => s_bufferSize;
        set {
            lock (s_lock)
                SetBufferSize(value);
        }
    }

    // The native STD handles.
    private static IntPtr InputHandle  => Common.GetStdHandle(Constants.STD_INPUT_HANDLE);
    private static IntPtr OutputHandle => Common.GetStdHandle(Constants.STD_OUTPUT_HANDLE);
    private static IntPtr ErrorHandle  => Common.GetStdHandle(Constants.STD_ERROR_HANDLE);

    /// <summary>
    /// Gets if the console input stream is redirected.
    /// </summary>
    /// <remarks>
    /// This is not particularly useful on this application, since we always write to the physical console,
    /// but if using this class in a different application this helps determining which API we use to read and write.
    /// </remarks>
    private static bool IsInputRedirected {
        get {
            StrongBox<bool> redirected = Volatile.Read(ref s_isInputRedirected) ?? EnsureInitialized();
            return redirected.Value;

            static StrongBox<bool> EnsureInitialized()
            {
                Volatile.Write(ref s_isInputRedirected, new(IsHandleRedirected(InputHandle)));
                return s_isInputRedirected!;
            }
        }
    }

    /// <summary>
    /// Gets if the console output stream is redirected.
    /// </summary>
    /// <seealso cref="IsInputRedirected"/>
    private static bool IsOutputRedirected {
        get {
            StrongBox<bool> redirected = Volatile.Read(ref s_isOutputRedirected) ?? EnsureInitialized();
            return redirected.Value;

            static StrongBox<bool> EnsureInitialized()
            {
                Volatile.Write(ref s_isOutputRedirected, new(IsHandleRedirected(OutputHandle)));
                return s_isOutputRedirected!;
            }
        }
    }

    /// <summary>
    /// Gets if the console error stream is redirected.
    /// </summary>
    /// <seealso cref="IsInputRedirected"/>
    private static bool IsErrorRedirected {
        get {
            StrongBox<bool> redirected = Volatile.Read(ref s_isErrorRedirected) ?? EnsureInitialized();
            return redirected.Value;

            static StrongBox<bool> EnsureInitialized()
            {
                Volatile.Write(ref s_isErrorRedirected, new(IsHandleRedirected(ErrorHandle)));
                return s_isErrorRedirected!;
            }
        }
    }

#if DEBUG
    static WindowsConsole() => Debug.Assert(UnicodeCodePage == Encoding.Unicode.CodePage);
#endif

    /// <summary>
    /// Reads the next character from the standard input stream.
    /// </summary>
    /// <returns>The next character from the input stream, or negative one (-1) if there are currently no more characters to be read.</returns>
    public static int Read() => In.Read();

    /// <summary>
    /// Reads the next line of characters from the standard input stream.
    /// </summary>
    /// <returns>The next line of characters from the input stream, or null if no more lines are available.</returns>
    public static string? ReadLine() => In.ReadLine();

    /// <summary>
    /// Writes the text representation of the specified value to the output stream.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public static void Write(string? value) => Out.Write(value);

    /// <summary>
    /// Writes the formatted text representation of the specified value to the output stream.
    /// </summary>
    /// <param name="value">The value to write.</param>
    /// <param name="arguments">The arguments to format the string.</param>
    public static void Write(string? value, params object[] arguments) => Out.Write(value, arguments);

    /// <summary>
    /// Writes the text representation of the specified value to the output stream asynchronously.
    /// </summary>
    /// <param name="value">The value to write.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public static Task WriteAsync(string? value) => Out.WriteAsync(value);

    /// <summary>
    /// Writes the current line terminator to the output stream.
    /// </summary>
    public static void WriteLine() => Out.WriteLine();

    /// <summary>
    /// Writes the specified string value, followed by the current line terminator, to the output stream.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public static void WriteLine(string? value) => Out.WriteLine(value);

    /// <summary>
    /// Writes the current line terminator to the output stream asynchronously.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public static Task WriteLineAsync() => Out.WriteLineAsync();

    /// <summary>
    /// Writes the specified string value, followed by the current line terminator, to the output stream asynchronously.
    /// </summary>
    /// <param name="value">The value to write.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public static Task WriteLineAsync(string? value) => Out.WriteLineAsync(value);

    /// <summary>
    /// Flushes the stream bytes into the console.
    /// </summary>
    /// <remarks>
    /// This method only has effect when using a buffered console stream strategy.
    /// Otherwise it does nothing.
    /// </remarks>
    public static void Flush()
    {
        if (s_outStrategy is BufferedConsoleStreamStrategy && !AutoFlush) {
            Out.Flush();
            s_outStrategy.Flush();
        }

        if (s_errStrategy is BufferedConsoleStreamStrategy && !AutoFlush) {
            Error.Flush();
            s_errStrategy.Flush();
        }
    }

    /// <summary>
    /// Changes if we're using the 'ConDrv' API to write to the console in the underlying stream.
    /// </summary>
    /// <param name="enable">True to enable using the driver.</param>
    /// <remarks>
    /// It's important to set the underlying stream to use Unicode appropriately.
    /// Calling the 'ConDrv' API requires us to tell if we're using Unicode or not,
    /// in contrast to 'WriteConsoleA' / 'WriteConsoleW'.
    /// </remarks>
    /// <seealso cref="UseDriver"/>
    private static void SetDriverMode(bool enable)
    {
        // Although we change the driver mode in the underlying input stream we currently don't
        // use the 'ConDrv' API to read from the console.
        if (s_inStrategy is not null) {
            if (s_inStrategy.IoType != ConsoleIoType.Driver && enable && !IsInputRedirected) {
                if (InputEncoding.CodePage == UnicodeCodePage && !s_inStrategy.IsUnicode)
                    s_inStrategy.IsUnicode = true;

                s_inStrategy.IoType = ConsoleIoType.Driver;
            }
            else if (s_inStrategy.IoType == ConsoleIoType.Driver && !enable) {
                if (InputEncoding.CodePage != UnicodeCodePage || IsInputRedirected)
                    s_inStrategy.IoType = ConsoleIoType.File;
                else
                    s_inStrategy.IoType = ConsoleIoType.Console;
            }
        }

        // Changing the output stream strategy.
        if (s_outStrategy is not null) {
            if (s_outStrategy.IoType != ConsoleIoType.Driver && enable && !IsOutputRedirected) {
                if (OutputEncoding.CodePage == UnicodeCodePage && !s_outStrategy.IsUnicode)
                    s_outStrategy.IsUnicode = true;

                s_outStrategy.IoType = ConsoleIoType.Driver;
            }
            else if (s_outStrategy.IoType == ConsoleIoType.Driver && !enable) {
                if (OutputEncoding.CodePage != UnicodeCodePage || IsOutputRedirected)
                    s_outStrategy.IoType = ConsoleIoType.File;
                else
                    s_outStrategy.IoType = ConsoleIoType.Console;
            }
        }

        // Changing the error stream strategy.
        if (s_errStrategy is not null) {
            if (s_errStrategy.IoType != ConsoleIoType.Driver && enable && !IsErrorRedirected) {
                if (OutputEncoding.CodePage == UnicodeCodePage && !s_errStrategy.IsUnicode)
                    s_errStrategy.IsUnicode = true;

                s_errStrategy.IoType = ConsoleIoType.Driver;
            }
            else if (s_errStrategy.IoType == ConsoleIoType.Driver && !enable) {
                if (OutputEncoding.CodePage != UnicodeCodePage || IsErrorRedirected)
                    s_errStrategy.IoType = ConsoleIoType.File;
                else
                    s_errStrategy.IoType = ConsoleIoType.Console;
            }
        }

        s_useDriver = enable;
    }

    /// <summary>
    /// Sets the auto-flush on the underlying streams.
    /// </summary>
    /// <param name="enable">True to enable auto-flush.</param>
    /// <remarks>
    /// We need to check if we're using a buffered console stream strategy and change it accordingly.
    /// </remarks>
    /// <seealso cref="AutoFlush"/>
    private static void SetAutoFlush(bool enable)
    {
        bool recreateOutput = false;
        bool recreateError = false;

        // Since it makes no sense having a buffered console read stream we only change the output ones.
        if (s_outStrategy is not null) {
            if (!s_autoFlush && enable && s_outStrategy is BufferedConsoleStreamStrategy) {
                s_outStrategy.Dispose();
                recreateOutput = true;
            }
            else if (s_autoFlush && !enable && BufferSize > 1 && s_outStrategy is WindowsConsoleStreamStrategy) {
                s_outStrategy.Dispose();
                recreateOutput = true;
            }
        }

        if (s_errStrategy is not null) {
            if (!s_autoFlush && enable && s_errStrategy is BufferedConsoleStreamStrategy) {
                s_errStrategy.Dispose();
                recreateError = true;
            }
            else if (s_autoFlush && !enable && BufferSize > 1 && s_errStrategy is WindowsConsoleStreamStrategy) {
                s_errStrategy.Dispose();
                recreateError = true;
            }
        }

        // Setting the auto-flush to create the correct strategy.
        s_autoFlush = enable;
        if (recreateOutput) s_outStrategy = OpenStandardOutput();
        if (recreateError) s_errStrategy = OpenStandardError();
    }

    /// <summary>
    /// Sets the console buffer size.
    /// </summary>
    /// <param name="size">The new console stream buffer size.</param>
    /// <remarks>
    /// If we are using an unbuffered console stream strategy and we want to increase the buffer size
    /// we need to dispose of it and create a buffered console stream strategy.
    /// </remarks>
    /// <seealso cref="BufferSize"/>
    private static void SetBufferSize(int size)
    {
        if (size > 0 && size < MinWriteBufferSize)
            size = MinWriteBufferSize;

        // Again, it makes no sense having a buffered console read stream, so we only change the output ones.
        bool recreateOutput = false;
        bool recreateError = false;
        if (s_outStrategy is not null) {
            if (size > 1) {
                if (s_outStrategy is BufferedConsoleStreamStrategy) {
                    s_outStrategy.SetBufferSize(size);
                }
                else {
                    s_outStrategy.Dispose();
                    recreateOutput = true;
                }
            }
            else if (size <= 1 && s_outStrategy is BufferedConsoleStreamStrategy) {
                s_outStrategy.Dispose();
                recreateOutput = true;
            }
        }

        if (s_errStrategy is not null) {
            if (size > 1) {
                if (s_errStrategy is BufferedConsoleStreamStrategy) {
                    s_errStrategy.SetBufferSize(size);
                }
                else {
                    s_errStrategy.Dispose();
                    recreateError = true;
                }
            }
            else if (size <= 1 && s_errStrategy is BufferedConsoleStreamStrategy) {
                s_errStrategy.Dispose();
                recreateError = true;
            }
        }

        // Setting the auto-flush according to the buffer size.
        s_autoFlush = size <= 1;
        
        s_bufferSize = size;
        if(recreateOutput) s_outStrategy = OpenStandardOutput();
        if (recreateError) s_errStrategy = OpenStandardError();
    }

    /// <summary>
    /// Opens the console standard input.
    /// </summary>
    /// <returns>The <see cref="ConsoleStreamStrategy"/> representing the standard input.</returns>
    /// <remarks>We decide on the <see cref="ConsoleIoType"/> based on the input encoding, and if we are using the 'ConDrv' API.</remarks>
    private static ConsoleStreamStrategy OpenStandardInput()
        => GetStandardFile(
            Constants.STD_INPUT_HANDLE,
            FileAccess.Read,
            UseDriver ? ConsoleIoType.Driver : InputEncoding.CodePage != UnicodeCodePage || IsInputRedirected ? ConsoleIoType.File : ConsoleIoType.Console,
            InputEncoding.CodePage == UnicodeCodePage
        );

    /// <summary>
    /// Opens the console standard output.
    /// </summary>
    /// <returns>The <see cref="ConsoleStreamStrategy"/> representing the standard output.</returns>
    /// <remarks>We decide on the <see cref="ConsoleIoType"/> based on the output encoding, and if we are using the 'ConDrv' API.</remarks>
    private static ConsoleStreamStrategy OpenStandardOutput()
        => GetStandardFile(
            Constants.STD_OUTPUT_HANDLE,
            FileAccess.Write,
            UseDriver ? ConsoleIoType.Driver : OutputEncoding.CodePage != UnicodeCodePage || IsOutputRedirected ? ConsoleIoType.File : ConsoleIoType.Console,
            OutputEncoding.CodePage == UnicodeCodePage
        );

    /// <summary>
    /// Opens the console standard error.
    /// </summary>
    /// <returns>The <see cref="ConsoleStreamStrategy"/> representing the standard error.</returns>
    /// <remarks>We decide on the <see cref="ConsoleIoType"/> based on the output encoding, and if we are using the 'ConDrv' API.</remarks>
    private static ConsoleStreamStrategy OpenStandardError()
        => GetStandardFile(
            Constants.STD_ERROR_HANDLE,
            FileAccess.Write,
            UseDriver ? ConsoleIoType.Driver : OutputEncoding.CodePage != UnicodeCodePage || IsErrorRedirected ? ConsoleIoType.File : ConsoleIoType.Console,
            OutputEncoding.CodePage == UnicodeCodePage
        );

    /// <summary>
    /// Gets the standard stream representing the console handle.
    /// </summary>
    /// <param name="handleType">The handle type. 'STD_INPUT_HANDLE', 'STD_OUTPUT_HANDLE', or 'STD_ERROR_HANDLE'.</param>
    /// <param name="access">The access to the stream. Read for input and write for output.</param>
    /// <param name="ioType">The console IO type. Determines which API will be used to interface with the console.</param>
    /// <param name="isUnicode">True if we're using Unicode.</param>
    /// <returns>A <see cref="ConsoleStreamStrategy"/> representing the console handle.</returns>
    /// <exception cref="ArgumentException">The input handle type is invalid.</exception>
    /// <remarks>
    /// We decide which <see cref="ConsoleStreamStrategy"/> to use based on the buffer size.
    /// If the buffer size is greater than zero we use a <see cref="BufferedConsoleStreamStrategy"/>
    /// otherwise a <see cref="WindowsConsoleStreamStrategy"/>.
    /// </remarks>
    private static ConsoleStreamStrategy GetStandardFile(int handleType, FileAccess access, ConsoleIoType ioType, bool isUnicode)
    {
        IntPtr handle = Common.GetStdHandle(handleType);
        if (handle == IntPtr.Zero || handle == Constants.INVALID_HANDLE_VALUE || (access != FileAccess.Read && !ConsoleHandleIsWritable(handle)))
            return ConsoleStreamStrategy.NullStrategy;

        WindowsConsoleStreamStrategy strategy = new(handle, access, ioType, isUnicode);
        switch (handleType) {
            case Constants.STD_INPUT_HANDLE:

                // There is no buffered strategy for reading.
                s_inStrategy = strategy;
                return s_inStrategy;
            case Constants.STD_OUTPUT_HANDLE:
                s_outStrategy = BufferSize > 1 && !AutoFlush ? new BufferedConsoleStreamStrategy(strategy, BufferSize) : strategy;
                return s_outStrategy;
            case Constants.STD_ERROR_HANDLE:
                s_errStrategy = BufferSize > 1 && !AutoFlush ? new BufferedConsoleStreamStrategy(strategy, BufferSize) : strategy;
                return s_errStrategy;
            default:
                throw new ArgumentException($"Invalid handle type '{handleType}'.");
        }
    }

    /// <summary>
    /// Gets the console standard input <see cref="TextReader"/>.
    /// </summary>
    /// <returns>A synchronized <see cref="TextReader"/> to read from the console standard input.</returns>
    private static TextReader GetInputReader()
    {
        ConsoleStreamStrategy strategy = OpenStandardInput();
        return TextReader.Synchronized(strategy == ConsoleStreamStrategy.NullStrategy
            ? StreamReader.Null : new StreamReader(strategy, new ConsoleEncoding(InputEncoding), false, ReadBufferSize, true));
    }

    /// <summary>
    /// Determines if the input console handle is redirected.
    /// </summary>
    /// <param name="handle">The console handle.</param>
    /// <returns>True if the handle is redirected.</returns>
    private static bool IsHandleRedirected(IntPtr handle)
    {
        FileType fileType = NativeIO.GetFileType(handle);
        if ((fileType & FileType.CHAR) != FileType.CHAR)
            return true;

        return (!ConsoleAPI.GetConsoleMode(handle, out _));
    }

    /// <summary>
    /// Determines if the input console handle is writable.
    /// </summary>
    /// <param name="handle">The console handle.</param>
    /// <returns>True if the console handle is writable.</returns>
    private static unsafe bool ConsoleHandleIsWritable(IntPtr handle)
    {
        byte junkByte = 0x41;
        return NativeIO.WriteFile(handle, &junkByte, 0, out _, IntPtr.Zero);
    }

    /// <summary>
    /// Attempts to get the console buffer information.
    /// </summary>
    /// <param name="throwOnNoConsole">True to throw an error if the handle is not a console handle.</param>
    /// <param name="info">The output <see cref="CONSOLE_SCREEN_BUFFER_INFO"/>.</param>
    /// <returns>True if we successfully retrieved the console screen buffer information.</returns>
    /// <exception cref="IOException">The output handle is not a console handle.</exception>
    /// <exception cref="NativeException">The native API returned false.</exception>
    private static bool TryGetBufferInfo(bool throwOnNoConsole, out CONSOLE_SCREEN_BUFFER_INFO info)
    {
        info = default;
        IntPtr outputHandle = OutputHandle;
        if (outputHandle == Constants.INVALID_HANDLE_VALUE) {
            if (throwOnNoConsole)
                throw new IOException("Output handle is not a console handle.");

            return false;
        }

        if (!ConsoleAPI.GetConsoleScreenBufferInfo(outputHandle, out info) &&
            !ConsoleAPI.GetConsoleScreenBufferInfo(ErrorHandle, out info) &&
            !ConsoleAPI.GetConsoleScreenBufferInfo(InputHandle, out info)
        ) {
            int errorCode = Marshal.GetLastWin32Error();
            if (errorCode == ErrorCodes.ERROR_INVALID_HANDLE && !throwOnNoConsole)
                return false;

            throw new NativeException(errorCode);
        }

        Debug.Assert((int)Color.ColorMask == 0xFF, "Make sure one byte is large enough to store a Console color value!");
        
        return true;
    }

    /// <summary>
    /// Converts a <see cref="ConsoleColor"/> to the native <see cref="Color"/> attribute.
    /// </summary>
    /// <param name="color">The <see cref="ConsoleColor"/>.</param>
    /// <param name="isBackground">True if it's the background color.</param>
    /// <returns>The native <see cref="Color"/> attribute.</returns>
    /// <exception cref="ArgumentException">The color is not a valid <see cref="ConsoleColor"/>.</exception>
    private static Color ConsoleColorToColorAttribute(ConsoleColor color, bool isBackground)
    {
        if ((((int)color) & ~0xf) != 0)
            throw new ArgumentException($"'{color}' is not a valid console color.");

        Color c = (Color)color;
        if (isBackground)
            c = (Color)((int)c << 4);
        
        return c;
    }

    /// <summary>
    /// Converts a native <see cref="Color"/> attribute to a <see cref="ConsoleColor"/>.
    /// </summary>
    /// <param name="color">The native <see cref="Color"/> attribute.</param>
    /// <returns>The converted <see cref="ConsoleColor"/>.</returns>
    private static ConsoleColor ColorAttributeToConsoleColor(Color color)
    {
        if ((color & Color.BackgroundMask) != 0)
            color = (Color)(((int)color) >> 4);

        return (ConsoleColor)color;
    }
}