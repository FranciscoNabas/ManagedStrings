// This file is part of the ManagedStrings project and repository.
// Project files are licensed under the MIT license.
// https://github.com/FranciscoNabas/ManagedStrings

using System;
using ManagedStrings.Interop.Windows;

namespace ManagedStrings.Engine;

/// <summary>
/// A disposable unmanaged memory buffer.
/// </summary>
/// <remarks>
/// This is used throughout the native APIs so we can easily allocate
/// memory without having to explicitly free it (while using the disposable pattern).
/// </remarks>
internal sealed class ScopedBuffer : IDisposable
{
    private nint m_buffer;
    private bool m_isDisposed;
    private ulong m_size;

    internal ulong Size => m_size;

    internal ScopedBuffer(int size)
        : this((ulong)size) { }

    internal ScopedBuffer(long size)
        : this((ulong)size) { }

    /// <summary>
    /// Allocates a buffer of the specified size.
    /// </summary>
    /// <param name="size">The buffer size.</param>
    internal ScopedBuffer(ulong size)
    {
        m_buffer = Heap.Alloc(size);
        m_size = size;
        m_isDisposed = false;
    }

    /// <summary>
    /// Allocates a buffer from a managed string.
    /// </summary>
    /// <param name="str">The input string.</param>
    /// <param name="ansi">True if the string is ANSI.</param>
    internal ScopedBuffer(string str, bool ansi = false)
    {
        if (ansi) {
            m_buffer = Heap.StringToHeapAllocAnsi(str);
            m_size = (ulong)str.Length;
            m_isDisposed = false;
        }
        else {
            m_buffer = Heap.StringToHeapAllocUni(str);
            m_size = (ulong)str.Length * 2;
            m_isDisposed = false;
        }
    }

    ~ScopedBuffer()
        => Dispose(disposing: false);

    /// <summary>
    /// Resizes the current buffer.
    /// </summary>
    /// <param name="newSize">The new size.</param>
    internal void Resize(ulong newSize)
    {
        ObjectDisposedException.ThrowIf(m_isDisposed, this);

        Heap.Free(m_buffer);

        m_buffer = Heap.Alloc(newSize);
        m_size = newSize;
    }

    /// <summary>
    /// Frees the buffer memory.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Frees the buffer memory.
    /// </summary>
    /// <param name="disposing">True to free the buffer memory.</param>
    private void Dispose(bool disposing)
    {
        if (disposing && !m_isDisposed) {
            Heap.Free(m_buffer);

            m_size = 0;
            m_buffer = nint.Zero;
            m_isDisposed = true;
        }
    }

    // Operators to make our lifes easier (sometimes harder).
    public static implicit operator nint(ScopedBuffer other)
    {
        ObjectDisposedException.ThrowIf(other.m_isDisposed, other);

        return other.m_buffer;
    }

    public static unsafe implicit operator void*(ScopedBuffer other)
    {
        ObjectDisposedException.ThrowIf(other.m_isDisposed, other);

        return other.m_buffer.ToPointer();
    }
}