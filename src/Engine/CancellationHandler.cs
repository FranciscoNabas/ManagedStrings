// This file is part of the ManagedStrings project and repository.
// Project files are licensed under the MIT license.
// https://github.com/FranciscoNabas/ManagedStrings

using System;
using System.Threading;
using ManagedStrings.Interop.Windows;

namespace ManagedStrings.Engine;

/// <summary>
/// A disposable object that manages cancellation requests.
/// </summary>
/// <seealso cref="Common.SetCtrlHandler(HandlerRoutine, bool)"/>
/// <seealso href="https://learn.microsoft.com/windows/console/setconsolectrlhandler">SetConsoleCtrlHandler function</seealso>
internal sealed class WindowsCancellationHandler : IDisposable
{
    // The cancellation handler routine and the cancellation token source.
    private readonly HandlerRoutine m_handlerRoutine;
    private readonly CancellationTokenSource m_tokenSource;

    private bool m_isDisposed;

    /// <summary>
    /// Gets the token associated with this handler.
    /// </summary>
    internal CancellationToken Token => m_tokenSource.Token;

    /// <summary>
    /// Constructs the handler and registers the handler delegate.
    /// </summary>
    internal WindowsCancellationHandler()
    {
        m_tokenSource = new();
        m_handlerRoutine = new(HandleControl);
        Common.SetCtrlHandler(m_handlerRoutine, true);
        m_isDisposed = false;
    }

    ~WindowsCancellationHandler()
        => Dispose(disposing: false);

    /// <summary>
    /// Disposes of unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes of unmanaged resources and unregisters the handler delegate.
    /// </summary>
    /// <param name="disposing">True to dispose of unmanaged resources.</param>
    private void Dispose(bool disposing)
    {
        if (disposing && m_isDisposed) {
            m_tokenSource.Dispose();
            m_isDisposed = true;
            Common.SetCtrlHandler(m_handlerRoutine, false);
        }
    }

    /// <summary>
    /// The handler method to be called.
    /// </summary>
    /// <param name="type">The control type.</param>
    /// <returns>True if the control was handled.</returns>
    /// <remarks>
    /// We only handle 'Ctrl + C' and 'Ctrl + Break'.
    /// </remarks>
    /// <seealso href="https://learn.microsoft.com/windows/console/handlerroutine">HandlerRoutine callback function</seealso>
    private bool HandleControl(CtrlType type)
    {
        switch (type) {
            case CtrlType.CtrlC:
            case CtrlType.CtrlBreak:
                if (!m_isDisposed && !m_tokenSource.IsCancellationRequested) {
                    m_tokenSource.Cancel();
                    return true;
                }
                return false;

            default:
                return false;
        }
    }
}
