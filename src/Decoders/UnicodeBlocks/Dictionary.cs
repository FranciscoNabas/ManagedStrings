// This file is part of the ManagedStrings project and repository.
// Project files are licensed under the MIT license.
// https://github.com/FranciscoNabas/ManagedStrings

using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Generic;

/// <summary>
/// A rudimentary dictionary with two keys.
/// This object is not thread-safe.
/// </summary>
/// <typeparam name="TKey1">The first key type.</typeparam>
/// <typeparam name="TKey2">The second key type.</typeparam>
/// <typeparam name="TValue">The value type.</typeparam>
[CollectionBuilder(typeof(MultiKeyDictionaryBuilder), "Create")]
internal sealed class MultiKeyDictionary<TKey1, TKey2, TValue> : IEnumerable<KeyValueTriple<TKey1, TKey2, TValue>> where TKey1 : notnull where TKey2 : notnull
{
    // The idea behind this is simple and not necessarily efficient.
    // We keep two traditional dictionaries, one for each key type, this way we don't need to implement
    // the interfaces to get the dictionary functionality.
    // A list is also kept so we can iterate through the 'KeyValueTriple's if necessary.
    // This setup is only reliable if we have an entry for each value on each one of these objects,
    // to maintain symmetry.
    private readonly Dictionary<TKey1, TValue> m_key1Dictionary = [];
    private readonly Dictionary<TKey2, TValue> m_key2Dictionary = [];
    private readonly List<KeyValueTriple<TKey1, TKey2, TValue>> m_tripleList = [];

    // We can access a value with either key, but we only set a value with both keys
    // to ensure all stores are aligned.
    internal TValue this[TKey1 key] => m_key1Dictionary[key];
    internal TValue this[TKey2 key] => m_key2Dictionary[key];
    internal TValue this[TKey1 key1, TKey2 key2] {
        get => m_key1Dictionary[key1];
        set {
            m_key1Dictionary[key1] = value;
            m_key2Dictionary[key2] = value;
            int tripleIndex = m_tripleList.FindIndex(i => i.Key1.Equals(key1) && i.Key2.Equals(key2));
            if (tripleIndex >= 0) {
                m_tripleList.RemoveAt(tripleIndex);
                m_tripleList.Insert(tripleIndex, (key1, key2, value));

                return;
            }

            m_tripleList.Add((key1, key2, value));
        }
    }

    /// <summary>
    /// Constructs a <see cref="MultiKeyDictionary{TKey1, TKey2, TValue}"/> from a span of <see cref="KeyValueTriple{TKey1, TKey2, TValue}"/>.
    /// It's utilized by the <see cref="MultiKeyDictionaryBuilder"/> so we can use collection initializers.
    /// </summary>
    /// <param name="span">The span containing the <see cref="KeyValueTriple{TKey1, TKey2, TValue}"/>.</param>
    internal MultiKeyDictionary(ReadOnlySpan<KeyValueTriple<TKey1, TKey2, TValue>> span)
    {
        foreach (KeyValueTriple<TKey1, TKey2, TValue> triple in span)
            Add(triple.Key1, triple.Key2, triple.Value);
    }

    /// <summary>
    /// Adds a new item to the dictionary.
    /// </summary>
    /// <param name="key1">The first key.</param>
    /// <param name="key2">The second key.</param>
    /// <param name="value">The value.</param>
    internal void Add(TKey1 key1, TKey2 key2, TValue value)
    {
        m_key1Dictionary.Add(key1, value);
        m_key2Dictionary.Add(key2, value);
        m_tripleList.Add((key1, key2, value));
    }

    /// <summary>
    /// Attempts to add a new item to the dictionary.
    /// </summary>
    /// <param name="key1">The first key.</param>
    /// <param name="key2">The second key.</param>
    /// <param name="value">The value.</param>
    /// <returns>True if the item was added successfully, or false if values with the specified keys already exist.</returns>
    internal bool TryAdd(TKey1 key1, TKey2 key2, TValue value)
    {
        bool added = m_key1Dictionary.TryAdd(key1, value) && m_key2Dictionary.TryAdd(key2, value);
        if (added)
            m_tripleList.Add((key1, key2, value));

        return added;
    }

    /// <summary>
    /// Attempts to remove an item from the dictionary.
    /// </summary>
    /// <param name="key1">The first key.</param>
    /// <param name="key2">The second key.</param>
    /// <returns>True if the item was removed successfully, or false if it's not on the dictionary.</returns>
    internal bool Remove(TKey1 key1, TKey2 key2)
    {
        bool removed = m_key1Dictionary.Remove(key1) && m_key2Dictionary.Remove(key2);
        if (removed)
            m_tripleList.RemoveAll(i => i.Key1.Equals(key1) && i.Key2.Equals(key2));

        return removed;
    }

    /// <summary>
    /// Attempts to get the value with the first key.
    /// </summary>
    /// <param name="key">The first key.</param>
    /// <param name="value">The output value or null if the dictionary doesn't contain the key.</param>
    /// <returns>True if a value was found for the specified key.</returns>
    internal bool TryGetValue(TKey1 key, [NotNullWhen(true)] out TValue? value)
    {
        if (m_key1Dictionary.TryGetValue(key, out value) && value is not null)
            return true;

        return false;
    }

    /// <summary>
    /// Attempts to get the value with the second key.
    /// </summary>
    /// <param name="key">The second key.</param>
    /// <param name="value">The output value or null if the dictionary doesn't contain the key.</param>
    /// <returns>True if a value was found for the specified key.</returns>
    internal bool TryGetValue(TKey2 key, [NotNullWhen(true)] out TValue? value)
    {
        if (m_key2Dictionary.TryGetValue(key, out value) && value is not null)
            return true;

        return false;
    }

    /// <summary>
    /// Checks if the dictionary contains the first key.
    /// </summary>
    /// <param name="key">The first key.</param>
    /// <returns>True if the dictionary contains the key.</returns>
    internal bool ContainsKey(TKey1 key)
        => m_key1Dictionary.ContainsKey(key);

    /// <summary>
    /// Checks if the dictionary contains the second key.
    /// </summary>
    /// <param name="key">The second key.</param>
    /// <returns>True if the dictionary contains the key.</returns>
    internal bool ContainsKey(TKey2 key)
        => m_key2Dictionary.ContainsKey(key);

    /// <summary>
    /// Checks if the dictionary contains a value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>True if the dictionary contains the value.</returns>
    internal bool ContainsValue(TValue value)
        => m_key1Dictionary.ContainsValue(value); // We only check one dictionary because they should be symmetric.

    /// <summary>
    /// Clears the dictionary.
    /// </summary>
    internal void Clear()
    {
        m_key1Dictionary.Clear();
        m_key2Dictionary.Clear();
        m_tripleList.Clear();
    }

    /// <summary>
    /// Retrieves the enumerator of <see cref="KeyValueTriple{TKey1, TKey2, TValue}"/>.
    /// </summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<KeyValueTriple<TKey1, TKey2, TValue>> GetEnumerator()
        => m_tripleList.GetEnumerator();

    /// <summary>
    /// Retrieves the enumerator of <see cref="KeyValueTriple{TKey1, TKey2, TValue}"/>.
    /// </summary>
    /// <returns>The enumerator.</returns>
    IEnumerator IEnumerable.GetEnumerator()
        => this.GetEnumerator();
}

/// <summary>
/// A key-value triple, with two keys.
/// </summary>
/// <typeparam name="TKey1">The first key type.</typeparam>
/// <typeparam name="TKey2">The second key type.</typeparam>
/// <typeparam name="TValue">The value type.</typeparam>
internal readonly struct KeyValueTriple<TKey1, TKey2, TValue>
{
    // This structure is not entirely necessary as we could accomplish the same with tuples.
    // However this makes our API more familiar to a traditional dictionary.
    private readonly TKey1 m_key1;
    private readonly TKey2 m_key2;
    private readonly TValue m_value;

    /// <summary>
    /// Constructs the triple from separate values.
    /// </summary>
    /// <param name="key1">The first key.</param>
    /// <param name="key2">The second key.</param>
    /// <param name="value">The value.</param>
    internal KeyValueTriple(TKey1 key1, TKey2 key2, TValue value)
        => (m_key1, m_key2, m_value) = (key1, key2, value);

    internal TKey1 Key1 => m_key1;
    internal TKey2 Key2 => m_key2;
    internal TValue Value => m_value;

    /// <summary>
    /// Returns the string representation of the triple.
    /// Follows the pattern: '[Key1, Key2, Value]'.
    /// </summary>
    /// <returns>A string representing the triple.</returns>
    public override string ToString()
        => string.Create(null, stackalloc char[256], $"[{m_key1}, {m_key2}, {m_value}]");

    /// <summary>
    /// Deconstructs the triple.
    /// </summary>
    /// <param name="key1">The first key.</param>
    /// <param name="key2">The second key.</param>
    /// <param name="value">The value.</param>
    internal void Deconstruct(out TKey1 key1, out TKey2 key2, out TValue value)
    {
        key1 = m_key1;
        key2 = m_key2;
        value = m_value;
    }

    /// <summary>
    /// Constructs a triple from a tuple.
    /// Used to facilitate the <see cref="MultiKeyDictionary{TKey1, TKey2, TValue}"/> construction with collection expressions.
    /// </summary>
    /// <param name="tuple">The tuple to build the triple from.</param>
    public static implicit operator KeyValueTriple<TKey1, TKey2, TValue>((TKey1, TKey2, TValue) tuple)
        => new(tuple.Item1, tuple.Item2, tuple.Item3);
}

/// <summary>
/// Contains methods to create a <see cref="MultiKeyDictionary{TKey1, TKey2, TValue}"/>.
/// </summary>
/// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/collection-expressions">Collection expressions</seealso>
/// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-12.0/collection-expressions">Collection expressions - Feature specification</seealso>
internal static class MultiKeyDictionaryBuilder
{
    internal static MultiKeyDictionary<TKey1, TKey2, TValue> Create<TKey1, TKey2, TValue>(ReadOnlySpan<KeyValueTriple<TKey1, TKey2, TValue>> value)
         where TKey1 : notnull where TKey2 : notnull => new(value);
}