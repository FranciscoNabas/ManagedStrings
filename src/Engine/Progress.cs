// This file is part of the ManagedStrings project and repository.
// Project files are licensed under the MIT license.
// https://github.com/FranciscoNabas/ManagedStrings

using System;
using System.Threading;
using ManagedStrings.Engine.Console;

namespace ManagedStrings.Engine;

/// <summary>
/// A handler to change terminal progress with Virtual Terminal Sequences.
/// </summary>
/// <seealso href="https://learn.microsoft.com/windows/terminal/tutorials/progress-bar-sequences">Set the progress bar in the Windows Terminal</seealso>
public sealed class ProgressHandler : IDisposable
{
    private const byte DefaultState          = 0;
    private const byte ProgressDefaultState  = 1;
    private const byte IndeterminateState    = 3;

    private static readonly string s_vtsTemplate = "\x1b]9;4;{0};{1}\x07";

    private long m_progress;
    private long m_totalWorkLength;

    /// <summary>
    /// Constructs a handler for the initial total length.
    /// </summary>
    /// <param name="totalWorkLength">The total work length.</param>
    /// <exception cref="ArgumentException">Total work length is negative.</exception>
    internal ProgressHandler(long totalWorkLength)
    {
        if (totalWorkLength < 0)
            throw new ArgumentException("Total work length can't be negative.");

        m_totalWorkLength = totalWorkLength;
        
        // Avoid dividing by zero.
        if (m_totalWorkLength == 0)
            m_totalWorkLength = 1;
    }

    /// <summary>
    /// Sets the terminal to indeterminate, I.e., the progress 'circle' keeps spinning.
    /// </summary>
    internal static void SetToIndeterminate()
        => WindowsConsole.Write(s_vtsTemplate, IndeterminateState, 0);

    /// <summary>
    /// Sets the terminal to default. No progress information.
    /// </summary>
    internal static void SetToDefault()
        => WindowsConsole.Write(s_vtsTemplate, DefaultState, 0);

    /// <summary>
    /// Increments the progress and handles it to the terminal.
    /// </summary>
    /// <param name="progress">The progress increment.</param>
    internal void IncrementProgress(long progress)
    {
        Interlocked.Add(ref m_progress, progress);
        int percentComplete = (int)Math.Round(((decimal)m_progress / m_totalWorkLength) * 100, 0);
        if (percentComplete < 0)
            percentComplete = 0;

        if (percentComplete > 100)
            percentComplete = 100;

        WindowsConsole.Write(s_vtsTemplate, ProgressDefaultState, percentComplete);
    }

    /// <summary>
    /// Resets the handler to a new total work length.
    /// </summary>
    /// <param name="totalWorkLength">The new total work length.</param>
    internal void Reset(long totalWorkLength)
        => m_totalWorkLength = totalWorkLength;

    /// <summary>
    /// Disposes of the handler and sets the terminal to default (no progress information).
    /// </summary>
    public void Dispose() {
        WindowsConsole.Write(s_vtsTemplate, DefaultState, 0);
    }
}