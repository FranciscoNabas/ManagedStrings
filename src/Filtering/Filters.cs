// This file is part of the ManagedStrings project and repository.
// Project files are licensed under the MIT license.
// https://github.com/FranciscoNabas/ManagedStrings

using System.Text.RegularExpressions;

namespace ManagedStrings.Filtering;

/// <summary>
/// The base class for all filters.
/// </summary>
internal abstract class Filter
{
    /// <summary>
    /// Checks if a string matches an expression.
    /// </summary>
    /// <param name="str">The input string.</param>
    /// <returns>True if the string matches the expression.</returns>
    internal abstract bool IsMatch(string str);
}

/// <summary>
/// A <see cref="Regex"/> filter.
/// </summary>
/// <param name="pattern">The Regex pattern.</param>
/// <param name="options">The Regex options.</param>
internal sealed class RegexFilter(string pattern, RegexOptions options) : Filter
{
    private readonly Regex m_regex = new(pattern, options);

    /// <summary>
    /// Checks if a string matches our <see cref="Regex"/> expression.
    /// </summary>
    /// <param name="str">The input string.</param>
    /// <returns>True if the string matches the expression.</returns>
    internal override bool IsMatch(string str)
        => m_regex.IsMatch(str);
}

/// <summary>
/// A <see cref="WildcardPattern"/> (PowerShell) filter.
/// </summary>
/// <param name="pattern">The WildcardPattern pattern.</param>
/// <param name="options">The WildcardPattern options.</param>
internal sealed class WildcardFilter(string pattern, WildcardOptions options) : Filter
{
    private readonly WildcardPattern m_wildcardPattern = WildcardPattern.Get(pattern, options);

    /// <summary>
    /// Checks if a string matches our <see cref="WildcardPattern"/> expression.
    /// </summary>
    /// <param name="str">The input string.</param>
    /// <returns>True if the string matches the expression.</returns>
    internal override bool IsMatch(string str)
        => m_wildcardPattern.IsMatch(str);
}