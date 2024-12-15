// This file is part of the ManagedStrings project and repository.
// Project files are licensed under the MIT license.
// https://github.com/FranciscoNabas/ManagedStrings

using System;
using System.Threading;
using System.Runtime.CompilerServices;
using ManagedStrings.Lab;
using ManagedStrings.Engine;
using ManagedStrings.Decoders;
using ManagedStrings.Filtering;

namespace ManagedStrings.Handlers;

/// <summary>
/// Enumeration extensions.
/// </summary>
/// <remarks>
/// The motivation behind this is to have specialized methods to check
/// enumeration flags without using the slower <see cref="Enum.HasFlag(Enum)"/>.
/// </remarks>
internal static partial class EnumExtensions
{
    /// <summary>
    /// Checks if an <see cref="Enum"/> flags has only one flag set.
    /// </summary>
    /// <typeparam name="T">The <see cref="Enum"/> type.</typeparam>
    /// <param name="flags">The flags.</param>
    /// <returns>True if the flags contains only one flag set.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool HasOnlyOneFlagSet<T>(this T flags) where T : Enum
    {
        int value = (int)(object)flags;
        return value > 0 && (value & (value - 1)) == 0;
    }
}

/// <summary>
/// The base class for all handlers.
/// </summary>
internal abstract class Handler
{
    // The filter and delegate used for matching.
    private readonly Filter? m_filter;
    private readonly Func<string, bool> m_isMatch;
    
    // The command line options.
    protected readonly CommandLineOptions Options;
    
    // The decoders.
    protected readonly ASCIIDecoder ASCIIDecoder;
    protected readonly UnicodeDecoder UnicodeDecoder;
    protected readonly UTF8Decoder UTF8Decoder;
    
    // The printer.
    protected readonly Printer Printer;

    /// <summary>
    /// Constructs a <see cref="Handler"/> from the command line options and printer.
    /// </summary>
    /// <param name="options">The <see cref="CommandLineOptions"/>.</param>
    /// <param name="printer">The <see cref="Handlers.Printer"/>.</param>
    protected Handler(CommandLineOptions options, Printer printer)
    {
        Options = options;
        Printer = printer;
        ASCIIDecoder = new();
        UnicodeDecoder = new(options.UnicodeBlocks!);
        UTF8Decoder = new(options.UnicodeBlocks!);

        if (options.Filter is not null) {
            if (options.FilterType == FilterType.Regex)
                m_filter = new RegexFilter(options.Filter, options.RegexFilterOptions);
            else
                m_filter = new WildcardFilter(options.Filter, options.WildcardFilterOptions);

            m_isMatch = m_filter.IsMatch;
        }
        else
            m_isMatch = str => true;
    }

    /// <summary>
    /// Handles a run.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to be monitored.</param>
    internal abstract void Handle(CancellationToken cancellationToken);

    /// <summary>
    /// Handles the benchmark for a run.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to be monitored.</param>
    /// <returns>The <see cref="BenchmarkData"/> for this run.</returns>
    internal abstract BenchmarkData HandleBenchmark(CancellationToken cancellationToken);

    /// <summary>
    /// Checks if a string matches our filter.
    /// </summary>
    /// <param name="str">The input string.</param>
    /// <returns>True if the string matches our filter.</returns>
    protected bool IsMatch(string str)
        => m_isMatch(str);

    /// <summary>
    /// Returns the <see cref="Decoder"/> for a <see cref="ValidEncoding"/>.
    /// </summary>
    /// <param name="encoding">The encoding.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">The <see cref="ValidEncoding"/> contains more than one flag set.</exception>
    /// <exception cref="InvalidOperationException">Invalid <see cref="ValidEncoding"/> flag.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected Decoder GetDecoder(ValidEncoding encoding)
    {
        if (!encoding.HasOnlyOneFlagSet())
            throw new ArgumentException("Too many encodings.");

        return encoding switch {
            ValidEncoding.ASCII => ASCIIDecoder,
            ValidEncoding.UTF8 => UTF8Decoder,
            ValidEncoding.Unicode => UnicodeDecoder,
            _ => throw new InvalidOperationException($"Invalid encoding '{encoding}'.")
        };
    }
}