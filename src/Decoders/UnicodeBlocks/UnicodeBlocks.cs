// This file is part of the ManagedStrings project and repository.
// Project files are licensed under the MIT license.
// https://github.com/FranciscoNabas/ManagedStrings

using System;
using System.Linq;
using System.Reflection;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace ManagedStrings.Decoders;

/// <summary>
/// A Unicode range enumeration item.
/// </summary>
internal abstract class UnicodeBlock : IEquatable<UnicodeBlock?>, IComparable<UnicodeBlock?>
{
    // This is the bit index of this block in our 'enumeration'.
    internal readonly byte Index;

    /// <summary>
    /// Constructs the <see cref="UnicodeBlock"/> from the index.
    /// </summary>
    /// <param name="index">The bit index.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is bigger than our maximum 'enumeration' capacity.</exception>
    protected UnicodeBlock(byte index)
    {
        // 0xFE is used to represent all bits set.
        if (index >= 128 && index != 0xFE)
            throw new ArgumentOutOfRangeException(nameof(index), "Index can't be bigger than 127.");

        Index = index;
    }

    public override bool Equals(object? obj)
        => this.Equals(obj as UnicodeBlock);

    public bool Equals(UnicodeBlock? other)
        => other is not null && Index == other.Index;

    public int CompareTo(UnicodeBlock? other)
        => other is null ? 1 : Index.CompareTo(other.Index);

    public override int GetHashCode()
        => Index.GetHashCode();

    public static bool operator ==(UnicodeBlock? left, UnicodeBlock? right)
        => (left is null && right is null) || (left is not null ? left.Equals(right) : right!.Equals(left));

    public static bool operator !=(UnicodeBlock? left, UnicodeBlock? right)
        => !(left == right);
}

/// <summary>
/// A <see cref="UnicodeBlock"/> 'enumeration'.
/// </summary>
/// <seealso href="https://gamedev.stackexchange.com/questions/71767/how-can-i-efficiently-implement-a-bitmask-larger-than-64-bits-for-component-exis">Implement bit-mask bigger than 64-bits</seealso>
internal sealed partial class UnicodeBlocks : IEquatable<UnicodeBlocks?>, IComparable<UnicodeBlocks?>
{
    /// <summary>
    /// A <see cref="UnicodeBlock"/> friend of this class, so we can hide its construction.
    /// </summary>
    /// <param name="index">The bit index.</param>
    private sealed class FriendUnicodeBlock(byte index) : UnicodeBlock(index) { }

    /// <summary>
    /// A record containing the <see cref="UnicodeBlock"/> name and its getter, so we can cache it.
    /// </summary>
    private record UnicodeBlockPropertyRecord
    {
        internal required string StrongName { get; init; }
        internal required Func<UnicodeBlock> Getter { get; init; }
    }

    // Math.Log(BitSize + 1, 2);
    private const int ByteSize = 6;
    private const int BitSize = (sizeof(ulong) * 8) - 1;

    private const byte LastBitIndex = 0x69;
    private const byte AllFlagsIndex = 0xFE;
    private const ulong AllFlagsLow = ulong.MaxValue;
    private const ulong AllFlagsHigh = 0x3FFFFFFFFFF;

    // This dictionary contains the chached static 'UnicodeBlock's.
    // This is used when parsing a block from a string, or to print the 'enumeration' representation string.
    // We have a cache because we use Linq Expressions to retrieve the 'enumeration values'.
    private static readonly MultiKeyDictionary<byte, string, UnicodeBlockPropertyRecord> s_blockExpressionMap = [];

    // This array contains the actual numbers storing our flags.
    private readonly ulong[] m_bits = new ulong[2];

    // We cache the currently set flags to easily print the representation string with 'ToString()'.
    private readonly HashSet<byte> m_activeBlocks = [];

    /// <summary>
    /// Constructs a <see cref="UnicodeBlocks"/> with zeroed values.
    /// </summary>
    internal UnicodeBlocks() { }

    /// <summary>
    /// Constructs a <see cref="UnicodeBlocks"/> from a <see cref="UnicodeBlock"/>.
    /// </summary>
    /// <param name="block"></param>
    internal UnicodeBlocks(UnicodeBlock block)
        => SetBlock(block);

    /// <summary>
    /// Constructs a <see cref="UnicodeBlocks"/> from another block's bit values.
    /// </summary>
    /// <param name="bits">The bit value array.</param>
    private UnicodeBlocks(ulong[] bits)
    {
        m_bits = bits;
        m_activeBlocks.UnionWith(GetAllSetIndexes());
    }

    /// <summary>
    /// Checks whether a <see cref="UnicodeBlock"/> bit is set.
    /// </summary>
    /// <param name="block">The <see cref="UnicodeBlock"/>.</param>
    /// <returns>True if the bit is set.</returns>
    internal bool HasBlock(UnicodeBlock block)
        => HasBlockInternal(block);

    /// <summary>
    /// Flips the bit from a <see cref="UnicodeBlock"/> off.
    /// </summary>
    /// <param name="block">The <see cref="UnicodeBlock"/>.</param>
    internal void RemoveBlock(UnicodeBlock block)
        => RemoveBlockInternal(block);
    
    /// <summary>
    /// Gets a <see cref="UnicodeBlock"/> for a bit index.
    /// </summary>
    /// <param name="index">The bit index.</param>
    /// <returns><see cref="UnicodeBlock"/></returns>
    internal static UnicodeBlock GetBlock(byte index)
        => new FriendUnicodeBlock(index); // TODO: Does constructing a new block cause any issues?

    /// <summary>
    /// Flips all bits on.
    /// </summary>
    internal void SetAll()
    {
        m_bits[0] = AllFlagsLow;
        m_bits[1] = AllFlagsHigh;
        m_activeBlocks.UnionWith(GetIndexRange());
    }

    /// <summary>
    /// Flips all bits off.
    /// </summary>
    internal void Clear()
    {
        m_bits[0] = 0ul;
        m_bits[1] = 0ul;
        m_activeBlocks.Clear();
    }

    /// <summary>
    /// Attempts to get a <see cref="UnicodeBlock"/> from a string.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="block"></param>
    /// <returns></returns>
    internal static bool TryParse(string? value, [NotNullWhen(true)] out UnicodeBlock? block)
    {
        block = default;
        if (string.IsNullOrEmpty(value))
            return false;

        return TryGetBlockAndCache(value, out block);
    }

    // IEquatable implementation.
    public override bool Equals(object? obj)
        => this.Equals(obj as UnicodeBlocks);

    public bool Equals(UnicodeBlocks? other)
        => other is not null && other.m_bits[0] == m_bits[0] && other.m_bits[1] == m_bits[1];


    // IComparable implementation.
    public int CompareTo(UnicodeBlocks? other)
        => other is null ? 1 : (m_bits[0] + m_bits[1]).CompareTo(other.m_bits[0] + other.m_bits[1]);

    // Overrides.
    public override int GetHashCode()
        => HashCode.Combine(m_bits);

    public override string ToString()
    {
        if (IsAllSet())
            return "All";

        string? output = null;
        foreach (byte index in m_activeBlocks) {
            if (TryGetBlockAndCache(index, out UnicodeBlockPropertyRecord? record))
                output = output is null ? record.StrongName : string.Join(", ", output, record.StrongName);
        }

        return output is null ? string.Empty : output;
    }

    // Operators.
    public static bool operator ==(UnicodeBlocks? left, UnicodeBlocks? right)
        => (left is null && right is null) || (left is not null ? left.Equals(right) : right!.Equals(left));

    public static bool operator !=(UnicodeBlocks? left, UnicodeBlocks? right)
        => !(left == right);

    public static UnicodeBlocks operator &(UnicodeBlocks left, UnicodeBlock right)
    {
        ulong[] bits;
        if (right.Index == AllFlagsIndex) {
            if (left.IsAllSet())
                return new UnicodeBlocks().SetBlock(right);

            bits = [left.m_bits[0], left.m_bits[1]];
            bits[0] &= AllFlagsLow;
            bits[1] &= AllFlagsHigh;

            return new(bits);
        }

        bits = [left.m_bits[0], left.m_bits[1]];
        bits[right.Index >> ByteSize] &= 1ul << (right.Index & BitSize);
        
        return new(bits);
    }

    public static UnicodeBlocks operator |(UnicodeBlocks left, UnicodeBlock right)
    {
        if (right.Index == AllFlagsIndex)
            return new UnicodeBlocks().SetBlock(right);

        ulong[] bits = [left.m_bits[0], left.m_bits[1]];
        bits[right.Index >> ByteSize] |= 1ul << (right.Index & BitSize);

        return new(bits);
    }

    /// <summary>
    /// Checks whether a <see cref="UnicodeBlock"/> bit is set.
    /// </summary>
    /// <param name="block">The <see cref="UnicodeBlock"/>.</param>
    /// <returns>True if the bit is set.</returns>
    private bool HasBlockInternal(UnicodeBlock block)
    {
        if (block.Index == AllFlagsIndex)
            return (m_bits[0] & AllFlagsLow) == AllFlagsLow && (m_bits[1] & AllFlagsHigh) == AllFlagsHigh;

        return (m_bits[block.Index >> ByteSize] & (1ul << (block.Index & BitSize))) != 0;
    }

    /// <summary>
    /// Sets the bit from a <see cref="UnicodeBlock"/> on.
    /// </summary>
    /// <param name="block">The block.</param>
    /// <returns>This object.</returns>
    private UnicodeBlocks SetBlock(UnicodeBlock block)
    {
        if (block.Index == AllFlagsIndex) {
            SetAll();
            return this;
        }

        m_bits[block.Index >> ByteSize] |= 1ul << (block.Index & BitSize);
        m_activeBlocks.Add(block.Index);

        return this;
    }

    /// <summary>
    /// Gets the indexes of all the bits set.
    /// </summary>
    /// <returns>An IEnumerable containing the set bit indexes.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IEnumerable<byte> GetAllSetIndexes()
    {
        for (byte index = 0; index <= LastBitIndex; index++) {
            if ((m_bits[index >> ByteSize] & (1ul << (index & BitSize))) != 0)
                yield return index;
        }
    }

    /// <summary>
    /// Gets the index of all possible bits.
    /// </summary>
    /// <returns>An IEnumerable containing all possible bit indexes.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IEnumerable<byte> GetIndexRange()
    {
        for (byte b = 0; b <= LastBitIndex; b++)
            yield return b;
    }

    /// <summary>
    /// Flips the bit from a <see cref="UnicodeBlock"/> off.
    /// </summary>
    /// <param name="block">The <see cref="UnicodeBlock"/>.</param>
    private void RemoveBlockInternal(UnicodeBlock block)
    {
        if (block.Index == AllFlagsIndex) {
            Clear();
            return;
        }

        m_bits[block.Index >> ByteSize] &= ~(1ul << (block.Index & BitSize));
        m_activeBlocks.Remove(block.Index);
    }

    /// <summary>
    /// Checks whether all bits are set.
    /// </summary>
    /// <returns>True if all bits are set.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsAllSet()
        => m_bits[0] == AllFlagsLow && m_bits[1] == AllFlagsHigh;

    /// <summary>
    /// Attempts to get a <see cref="UnicodeBlock"/> by name from our static properties.
    /// </summary>
    /// <param name="name">The block name.</param>
    /// <param name="block">The output block</param>
    /// <returns>True if the block was found.</returns>
    private static bool TryGetBlockAndCache(string name, [NotNullWhen(true)] out UnicodeBlock? block)
    {
        block = default;
        string nameToUpper = name.ToUpperInvariant();

        // Checking if the block info is already cached.
        if (s_blockExpressionMap.TryGetValue(nameToUpper, out UnicodeBlockPropertyRecord? propertyRecord)) {
            block = propertyRecord.Getter();
            return true;
        }

        // Creating an expression to avoid using reflection.
        Expression<Func<UnicodeBlock>> expression;
        try { expression = Expression.Lambda<Func<UnicodeBlock>>(Expression.Property(null, typeof(UnicodeBlocks), name)); }
        catch (ArgumentException) { return false; }
        
        // Checking if we have a MemberExpression for this expression so we can get the actual property name.
        // This is useful for when 'ToString()' is called we print the actual property name, not what the user input on 'TryParse()'.
        if (expression.Body is not MemberExpression memberExpression)
            return false;

        // Caching the record.
        Func<UnicodeBlock> getter = expression.Compile();
        block = getter();
        s_blockExpressionMap.Add(block.Index, nameToUpper, new() {
            StrongName = memberExpression.Member.Name,
            Getter = getter
        });

        return true;
    }

    /// <summary>
    /// Attempts to get a <see cref="UnicodeBlock"/> by bit index from our static properties.
    /// </summary>
    /// <param name="index">The bit index.</param>
    /// <param name="record">The output block.</param>
    /// <returns>True if the block was found.</returns>
    private static bool TryGetBlockAndCache(byte index, [NotNullWhen(true)] out UnicodeBlockPropertyRecord? record)
    {
        record = default;
        if (s_blockExpressionMap.TryGetValue(index, out record))
            return true;

        // The method is similar to the previous method, but here we build the expression with a
        // 'PropertyInfo', since we don't have the name.
        var test = typeof(UnicodeBlocks).GetProperties(BindingFlags.Static | BindingFlags.NonPublic);
        PropertyInfo? info = typeof(UnicodeBlocks).GetProperties(BindingFlags.Static | BindingFlags.NonPublic)
            .Where(p => p.PropertyType == typeof(UnicodeBlock) && ((UnicodeBlock?)p.GetValue(null))?.Index == index)
            .FirstOrDefault();

        if (info is null)
            return false;

        Expression<Func<UnicodeBlock>> expression = Expression.Lambda<Func<UnicodeBlock>>(Expression.Property(null, info));
        if (expression.Body is not MemberExpression memberExpression)
            return false;

        string name = memberExpression.Member.Name;
        Func<UnicodeBlock> getter = expression.Compile();
        record = new() {
            StrongName = name,
            Getter = getter
        };
        
        s_blockExpressionMap.Add(index, name.ToUpperInvariant(), record);

        return true;
    }
}