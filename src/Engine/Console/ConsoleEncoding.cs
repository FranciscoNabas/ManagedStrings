// This file is part of the ManagedStrings project and repository.
// Project files are licensed under the MIT license.
// https://github.com/FranciscoNabas/ManagedStrings

using System.Text;

namespace ManagedStrings.Engine.Console;

/// <summary>
/// Encoding extensions.
/// </summary>
/// <runtimefile>src/libraries/Common/src/System/Text/ConsoleEncoding.cs</runtimefile>
internal static class EncodingExtensions
{
    /// <summary>
    /// Returns a <see cref="ConsoleEncoding"/> if the input <see cref="Encoding"/> has a preamble.
    /// </summary>
    /// <param name="encoding">The encoding.</param>
    /// <returns>The input <see cref="Encoding"/> or a <see cref="ConsoleEncoding"/> if the input has a preamble.</returns>
    /// <runtimefile>src/libraries/Common/src/System/Text/ConsoleEncoding.cs</runtimefile>
    internal static Encoding RemovePreamble(this Encoding encoding)
    {
        if (encoding.GetPreamble().Length == 0)
            return encoding;

        return new ConsoleEncoding(encoding);
    }
}

/// <summary>
/// Encapsulates a <see cref="Encoding"/> without preable.
/// <runtimefile>src/libraries/Common/src/System/Text/ConsoleEncoding.cs</runtimefile>
/// </summary>
internal sealed class ConsoleEncoding : Encoding
{
    private readonly Encoding m_encoding;

    internal ConsoleEncoding(Encoding encoding)
        => m_encoding = encoding;

    public override byte[] GetPreamble() => [];
    public override int CodePage => m_encoding.CodePage;
    public override bool IsSingleByte => m_encoding.IsSingleByte;
    public override string EncodingName => m_encoding.EncodingName;
    public override string WebName => m_encoding.WebName;

    public override int GetByteCount(char[] chars)
        => m_encoding.GetByteCount(chars);

    public override unsafe int GetByteCount(char* chars, int count)
        => m_encoding.GetByteCount(chars, count);

    public override int GetByteCount(char[] chars, int index, int count)
        => m_encoding.GetByteCount(chars, index, count);

    public override int GetByteCount(string s)
        => m_encoding.GetByteCount(s);

    public override unsafe int GetBytes(char* chars, int charCount, byte* bytes, int byteCount)
        => m_encoding.GetBytes(chars, charCount, bytes, byteCount);

    public override byte[] GetBytes(char[] chars)
        => m_encoding.GetBytes(chars);

    public override byte[] GetBytes(char[] chars, int index, int count)
        => m_encoding.GetBytes(chars, index, count);

    public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        => m_encoding.GetBytes(chars, charIndex, charCount, bytes, byteIndex);

    public override byte[] GetBytes(string s)
        => m_encoding.GetBytes(s);

    public override int GetBytes(string s, int charIndex, int charCount, byte[] bytes, int byteIndex)
        => m_encoding.GetBytes(s, charIndex, charCount, bytes, byteIndex);

    public override unsafe int GetCharCount(byte* bytes, int count)
        => m_encoding.GetCharCount(bytes, count);

    public override int GetCharCount(byte[] bytes)
        => m_encoding.GetCharCount(bytes);

    public override int GetCharCount(byte[] bytes, int index, int count)
        => m_encoding.GetCharCount(bytes, index, count);

    public override unsafe int GetChars(byte* bytes, int byteCount, char* chars, int charCount)
        => m_encoding.GetChars(bytes, byteCount, chars, charCount);

    public override char[] GetChars(byte[] bytes)
        => m_encoding.GetChars(bytes);

    public override char[] GetChars(byte[] bytes, int index, int count)
        => m_encoding.GetChars(bytes, index, count);

    public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        => m_encoding.GetChars(bytes, byteIndex, byteCount, chars, charIndex);

    public override Decoder GetDecoder()
        => m_encoding.GetDecoder();

    public override Encoder GetEncoder()
        => m_encoding.GetEncoder();

    public override int GetMaxByteCount(int charCount)
        => m_encoding.GetMaxByteCount(charCount);

    public override int GetMaxCharCount(int byteCount)
        => m_encoding.GetMaxCharCount(byteCount);

    public override string GetString(byte[] bytes)
        => m_encoding.GetString(bytes);

    public override string GetString(byte[] bytes, int index, int count)
        => m_encoding.GetString(bytes, index, count);
}