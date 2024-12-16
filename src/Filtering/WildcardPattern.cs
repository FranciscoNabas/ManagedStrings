// This file is part of the ManagedStrings project and repository.
// Project files are licensed under the MIT license.
// https://github.com/FranciscoNabas/ManagedStrings

using System;
using System.Text;
using System.Buffers;
using System.Globalization;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace ManagedStrings.Filtering;

// I am a PowerShell guy. I love the thing and one of the best features in
// PowerShell is the wildcard filtering. It's not as complex as Regex and
// covers your needs in 98.69% of the time.
// I wanted to have this globbing capabilities here, but including the
// PowerShell SDK seems excessive.
// So, I got only the WildcardPattern class and related APIs from it.
// At the end of the day it parses the expression into a Regex pattern.
// Simple, elegant, and effective.

/// <summary>
/// Wildcard options. These options maps to Regex options.
/// </summary>
[Flags]
internal enum WildcardOptions
{
    None              = 0x0,
    Compiled          = 0x1,
    IgnoreCase        = 0x2,
    CultureInvariant  = 0x4,
}

/// <summary>
/// A Wildcard pattern.
/// </summary>
/// <seealso href="https://learn.microsoft.com/powershell/module/microsoft.powershell.core/about/about_wildcards">about_Wildcards</seealso>
/// <seealso href="https://learn.microsoft.com/dotnet/api/system.management.automation.wildcardpattern">WildcardPattern Class</seealso>
/// <runtimefile>PowerShell/src/System.Management.Automation/engine/regex.cs</runtimefile>
internal sealed class WildcardPattern
{
    // Wildcard characters.
    private const string SpecialChars = "*?[]`";

    // Static default matchers.
    private static readonly Predicate<string> s_matchAll = _ => true;
    private static readonly WildcardPattern s_matchAllIgnoreCasePattern = new("*", WildcardOptions.None);

    // The predicate that matches a pattern.
    private Predicate<string>? m_isMatch;

    internal string Pattern { get; }

    // Options that control match behavior.
    // Default is WildcardOptions.None.
    internal WildcardOptions Options { get; }

    /// <summary>
    /// Initializes and instance of the WildcardPattern class
    /// for the specified wildcard pattern.
    /// </summary>
    /// <param name="pattern">The wildcard pattern to match.</param>
    /// <returns>The constructed WildcardPattern object.</returns>
    internal WildcardPattern(string pattern)
        : this(pattern, WildcardOptions.None) { }

    /// <summary>
    /// Initializes an instance of the WildcardPattern class for
    /// the specified wildcard pattern expression, with options
    /// that modify the pattern.
    /// </summary>
    /// <param name="pattern">The wildcard pattern to match.</param>
    /// <param name="options">Wildcard options.</param>
    /// <returns>The constructed WildcardPattern object.</returns>
    internal WildcardPattern(string pattern, WildcardOptions options)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        this.Pattern = pattern;
        this.Options = options;
    }

    /// <summary>
    /// A factory-style getter for a WildcardPattern.
    /// </summary>
    /// <param name="pattern">The Wildcard string pattern.</param>
    /// <param name="options">The Wildcard options.</param>
    /// <returns>A <see cref="WildcardPattern"/>.</returns>
    internal static WildcardPattern Get(string pattern, WildcardOptions options)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        if (pattern.Length == 1 && pattern[0] == '*')
            return s_matchAllIgnoreCasePattern;

        return new(pattern, options);
    }

    /// <summary>
    /// Matches a string to this <see cref="WildcardPattern"/>.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <returns>True if the string matches the pattern.</returns>
    internal bool IsMatch(string input)
    {
        Init();
        return input != null && m_isMatch!(input);
    }

    /// <summary>
    /// Initializes the <see cref="WildcardPattern"/>.
    /// </summary>
    private void Init()
    {
        StringComparison GetStringComparison()
        {
            StringComparison stringComparison;
            if ((Options & WildcardOptions.IgnoreCase) > 0) {
                stringComparison = (Options & WildcardOptions.CultureInvariant) > 0
                    ? StringComparison.InvariantCultureIgnoreCase
                    : CultureInfo.CurrentCulture.Name.Equals("en-US-POSIX", StringComparison.OrdinalIgnoreCase)
                        // The collation behavior of the POSIX locale (also known as the C locale) is case sensitive.
                        // For this specific locale, we use 'OrdinalIgnoreCase'.
                        ? StringComparison.OrdinalIgnoreCase
                        : StringComparison.CurrentCultureIgnoreCase;
            }
            else {
                stringComparison = (Options & WildcardOptions.CultureInvariant) > 0
                    ? StringComparison.InvariantCulture
                    : StringComparison.CurrentCulture;
            }

            return stringComparison;
        }

        if (m_isMatch is not null)
            return;

        if (Pattern.Length == 1 && Pattern[0] == '*') {
            m_isMatch = s_matchAll;
            return;
        }

        int index = Pattern.AsSpan().IndexOfAny(SpecialChars.AsSpan());
        if (index < 0) {

            // No special characters present in the pattern, so we can just do a string comparison.
            m_isMatch = str => string.Equals(str, Pattern, GetStringComparison());
            return;
        }

        if (index == Pattern.Length - 1 && Pattern[index] == '*') {

            // No special characters present in the pattern before last position and last character is asterisk.
            ReadOnlyMemory<char> patternWithoutAsterisk = Pattern.AsMemory(0, index);
            m_isMatch = str => str.AsSpan().StartsWith(patternWithoutAsterisk.Span, GetStringComparison());
            return;
        }

        m_isMatch = new WildcardPatternMatcher(this).IsMatch;
    }
}

/// <summary>
/// A base class for parsers of <see cref="WildcardPattern"/> patterns.
/// </summary>
/// /// <runtimefile>PowerShell/src/System.Management.Automation/engine/regex.cs</runtimefile>
internal abstract class WildcardPatternParser
{
    /// <summary>
    /// Called from <see cref="Parse"/> method to indicate that the next
    /// part of the pattern should match
    /// a literal character <paramref name="c"/>.
    /// </summary>
    protected abstract void AppendLiteralCharacter(char c);

    /// <summary>
    /// Called from <see cref="Parse"/> method to indicate that the next
    /// part of the pattern should match
    /// any string, including an empty string.
    /// </summary>
    protected abstract void AppendAsterix();

    /// <summary>
    /// Called from <see cref="Parse"/> method to indicate that the next
    /// part of the pattern should match
    /// any single character.
    /// </summary>
    protected abstract void AppendQuestionMark();

    /// <summary>
    /// Called from <see cref="Parse"/> method to indicate
    /// the beginning of a bracket expression.
    /// </summary>
    /// <remarks>
    /// Bracket expressions of <see cref="WildcardPattern"/> are
    /// a greatly simplified version of bracket expressions of POSIX wildcards
    /// (https://www.opengroup.org/onlinepubs/9699919799/functions/fnmatch.html).
    /// Only literal characters and character ranges are supported.
    /// Negation (with either '!' or '^' characters),
    /// character classes ([:alpha:])
    /// and other advanced features are not supported.
    /// </remarks>
    protected abstract void BeginBracketExpression();

    /// <summary>
    /// Called from <see cref="Parse"/> method to indicate that the bracket expression
    /// should include a literal character <paramref name="c"/>.
    /// </summary>
    protected abstract void AppendLiteralCharacterToBracketExpression(char c);

    /// <summary>
    /// Called from <see cref="Parse"/> method to indicate that the bracket expression
    /// should include all characters from character range
    /// starting at <paramref name="startOfCharacterRange"/>
    /// and ending at <paramref name="endOfCharacterRange"/>
    /// </summary>
    protected abstract void AppendCharacterRangeToBracketExpression(char start, char end);

    /// <summary>
    /// Called from <see cref="Parse"/> method to indicate the end of a bracket expression.
    /// </summary>
    protected abstract void EndBracketExpression();

    /// <summary>
    /// Called from <see cref="Parse"/> method to indicate
    /// the beginning of the wildcard pattern.
    /// Default implementation simply returns.
    /// </summary>
    /// <param name="pattern">
    /// <see cref="WildcardPattern"/> object that includes both
    /// the text of the pattern (<see cref="WildcardPattern.Pattern"/>)
    /// and the pattern options (<see cref="WildcardPattern.Options"/>)
    /// </param>
    protected virtual void BeginWildcardPattern(WildcardPattern pattern) { }

    /// <summary>
    /// Called from <see cref="Parse"/> method to indicate the end of the wildcard pattern.
    /// Default implementation simply returns.
    /// </summary>
    protected virtual void EndWildcardPattern() { }

    /// <summary>
    /// PowerShell v1 and v2 treats all characters inside
    /// <paramref name="bracketContents"/> as literal characters,
    /// except '-' sign which denotes a range.  In particular it means that
    /// '^', '[', ']' are escaped within the bracket expression and don't
    /// have their regex-y meaning.
    /// </summary>
    /// <param name="bracketContents"></param>
    /// <param name="operators"></param>
    /// <param name="pattern"></param>
    /// <remarks>
    /// This method should be kept "internal"
    /// </remarks>
    internal void AppendBracketExpression(string bracketContents, string operators, string pattern)
    {
        this.BeginBracketExpression();

        int i = 0;
        while (i < bracketContents.Length) {
            if (((i + 1) < bracketContents.Length) && (operators[i + 1] == '-')) {
                char lowerBound = bracketContents[i];
                char upperBound = bracketContents[i + 2];
                
                i += 3;
                if (lowerBound > upperBound)
                    throw new WildcardPatternException($"Invalid wildcard pattern '{pattern}'.");

                this.AppendCharacterRangeToBracketExpression(lowerBound, upperBound);
            }
            else
                this.AppendLiteralCharacterToBracketExpression(bracketContents[i++]);
        }

        this.EndBracketExpression();
    }

    /// <summary>
    /// Parses <paramref name="pattern"/>, calling appropriate overloads
    /// in <paramref name="parser"/>
    /// </summary>
    /// <param name="pattern">Pattern to parse.</param>
    /// <param name="parser">Parser to call back.</param>
    internal static void Parse(WildcardPattern pattern, WildcardPatternParser parser)
    {
        parser.BeginWildcardPattern(pattern);

        bool insideCharacterRange = false;
        bool previousCharacterIsAnEscape = false;
        bool previousCharacterStartedBracketExpression = false;
        StringBuilder? characterRangeContents = null;
        StringBuilder? characterRangeOperators = null;
        foreach (char c in pattern.Pattern) {
            if (insideCharacterRange) {
                if (c == ']' && !previousCharacterStartedBracketExpression && !previousCharacterIsAnEscape) {

                    // An unescaped closing square bracket closes the character set.  In other
                    // words, there are no nested square bracket expressions
                    // This is different than the POSIX spec
                    // (at https://www.opengroup.org/onlinepubs/9699919799/functions/fnmatch.html),
                    // but we are keeping this behavior for back-compatibility.
                    insideCharacterRange = false;
                    parser.AppendBracketExpression(characterRangeContents!.ToString(), characterRangeOperators!.ToString(), pattern.Pattern);
                    characterRangeContents = null;
                    characterRangeOperators = null;
                }
                else if (c != '`' || previousCharacterIsAnEscape) {
                    characterRangeContents!.Append(c);
                    characterRangeOperators!.Append((c == '-') && !previousCharacterIsAnEscape ? '-' : ' ');
                }

                previousCharacterStartedBracketExpression = false;
            }
            else {
                if (c == '*' && !previousCharacterIsAnEscape)
                    parser.AppendAsterix();

                else if (c == '?' && !previousCharacterIsAnEscape)
                    parser.AppendQuestionMark();

                else if (c == '[' && !previousCharacterIsAnEscape) {
                    insideCharacterRange = true;
                    characterRangeContents = new StringBuilder();
                    characterRangeOperators = new StringBuilder();
                    previousCharacterStartedBracketExpression = true;
                }

                else if (c != '`' || previousCharacterIsAnEscape)
                    parser.AppendLiteralCharacter(c);
            }

            previousCharacterIsAnEscape = (c == '`') && (!previousCharacterIsAnEscape);
        }

        if (insideCharacterRange)
            throw new WildcardPatternException($"Invalid wildcard pattern '{pattern.Pattern}'.");

        if (previousCharacterIsAnEscape) {
            if (!pattern.Pattern.Equals("`", StringComparison.Ordinal))  // Win7 backwards-compatibility requires treating '`' pattern as '' pattern.
                parser.AppendLiteralCharacter(pattern.Pattern[^1]);
        }

        parser.EndWildcardPattern();
    }
}

/// <summary>
/// Convert a string with wild cards into its equivalent regex.
/// </summary>
/// <remarks>
/// A list of glob patterns and their equivalent regexes
///
///  glob pattern      regex
/// -------------     -------
/// *foo*              foo
/// foo                ^foo$
/// foo*bar            ^foo.*bar$
/// foo`*bar           ^foo\*bar$
///
/// This class is based on the original 'WildcardPatternToRegexParser'. Since we are not going to
/// use its functionalities as a Wildcard Pattern parser we just declare the static methods.
/// </remarks>
/// /// <runtimefile>PowerShell/src/System.Management.Automation/engine/regex.cs</runtimefile>
internal static class RegexParserExtensions
{
    private const string RegexChars = "()[.?*{}^$+|\\";

    /// <summary>
    /// Appends a literal character to the <see cref="Regex"/> pattern.
    /// </summary>
    /// <param name="regexPattern">The <see cref="Regex"/> pattern.</param>
    /// <param name="c">The character to append.</param>
    internal static void AppendLiteralCharacter(StringBuilder regexPattern, char c)
    {
        if (IsRegexChar(c))
            regexPattern.Append('\\');

        regexPattern.Append(c);
    }

    /// <summary>
    /// Appends a literal character to a bracket expression.
    /// </summary>
    /// <param name="regexPattern">The <see cref="Regex"/> pattern.</param>
    /// <param name="c">The character to append.</param>
    internal static void AppendLiteralCharacterToBracketExpression(StringBuilder regexPattern, char c)
    {
        switch (c) {
            case '[':
                regexPattern.Append('[');
                break;

            case ']':
                regexPattern.Append(@"\]");
                break;

            case '-':
                regexPattern.Append(@"\x2d");
                break;

            default:
                AppendLiteralCharacter(regexPattern, c);
                break;
        }
    }

    /// <summary>
    /// Appends a character range to a bracket expression.
    /// </summary>
    /// <param name="regexPattern">The <see cref="Regex"/> pattern.</param>
    /// <param name="start">The starting character.</param>
    /// <param name="end">The end character.</param>
    internal static void AppendCharacterRangeToBracketExpression(StringBuilder regexPattern, char start, char end)
    {
        AppendLiteralCharacterToBracketExpression(regexPattern, start);
        regexPattern.Append('-');
        AppendLiteralCharacterToBracketExpression(regexPattern, end);
    }

    /// <summary>
    /// Translates <see cref="WildcardOptions"/> to <see cref="RegexOptions"/>.
    /// </summary>
    /// <param name="options">The <see cref="WildcardOptions"/>.</param>
    /// <returns>The equivalent <see cref="RegexOptions"/>.</returns>
    internal static RegexOptions TranslateWildcardOptionsToRegexOptions(WildcardOptions options)
    {
        RegexOptions regexOptions = RegexOptions.Singleline;

        if ((options & WildcardOptions.Compiled) > 0)
            regexOptions |= RegexOptions.Compiled;

        if ((options & WildcardOptions.IgnoreCase) > 0)
            regexOptions |= RegexOptions.IgnoreCase;

        if ((options & WildcardOptions.CultureInvariant) > 0)
            regexOptions |= RegexOptions.CultureInvariant;

        return regexOptions;
    }

    /// <summary>
    /// Checks if a character is a <see cref="Regex"/> character.
    /// </summary>
    /// <param name="c">The input character.</param>
    /// <returns>True if the input character is a <see cref="Regex"/> character.</returns>
    private static bool IsRegexChar(char c)
    {
        for (int i = 0; i < RegexChars.Length; i++) {
            if (c == RegexChars[i])
                return true;
        }

        return false;
    }
}

/// <summary>
/// A WildcardPattern matcher.
/// </summary>
internal sealed class WildcardPatternMatcher
{
    private readonly PatternElement[] m_patternElements;
    private readonly CharacterNormalizer m_characterNormalizer;

    /// <summary>
    /// Constructs a <see cref="WildcardPatternMatcher"/> from a <see cref="WildcardPattern"/>.
    /// </summary>
    /// <param name="wildcardPattern">The input <see cref="WildcardPattern"/>.</param>
    internal WildcardPatternMatcher(WildcardPattern wildcardPattern)
    {
        m_characterNormalizer = new(wildcardPattern.Options);
        m_patternElements = MyWildcardPatternParser.Parse(wildcardPattern, m_characterNormalizer);
    }

    /// <summary>
    /// Checks if a string matches our <see cref="WildcardPattern"/>.
    /// </summary>
    /// <param name="str">The input string.</param>
    /// <returns>True if the string matches the pattern.</returns>
    internal bool IsMatch(string str)
    {
        // - each state of NFA is represented by (patternPosition, stringPosition) tuple
        //     - state transitions are documented in
        //       ProcessStringCharacter and ProcessEndOfString methods
        // - the algorithm below tries to see if there is a path
        //   from (0, 0) to (lengthOfPattern, lengthOfString)
        //    - this is a regular graph traversal
        //    - there are O(1) edges per node (at most 2 edges)
        //      so the whole graph traversal takes O(number of nodes in the graph) =
        //      = O(lengthOfPattern * lengthOfString) time
        //    - for efficient remembering which states have already been visited,
        //      the traversal goes methodically from beginning to end of the string
        //      therefore requiring only O(lengthOfPattern) memory for remembering
        //      which states have been already visited
        //  - Wikipedia calls this algorithm the "NFA" algorithm at
        //    https://en.wikipedia.org/wiki/Regular_expression#Implementations_and_running_times

        PatternPositionsVisitor nextStringVisitor = new(m_patternElements.Length);
        PatternPositionsVisitor currentStringVisitor = new(m_patternElements.Length);
        currentStringVisitor.Add(0);
        try {
            for (int currentStringPosition = 0; currentStringPosition < str.Length; currentStringPosition++) {
                char currentStringCharacter = m_characterNormalizer.Normalize(str[currentStringPosition]);
                currentStringVisitor.StringPosition = currentStringPosition;
                nextStringVisitor.StringPosition = currentStringPosition + 1;

                while (currentStringVisitor.MoveNext(out int patternPosition))
                    m_patternElements[patternPosition].ProcessStringCharacter(currentStringCharacter, patternPosition, currentStringVisitor, nextStringVisitor);

                (nextStringVisitor, currentStringVisitor) = (currentStringVisitor, nextStringVisitor);
            }

            while (currentStringVisitor.MoveNext(out int patternPosition2))
                m_patternElements[patternPosition2].ProcessEndOfString(patternPosition2, currentStringVisitor);

            return currentStringVisitor.ReachedEndOfPattern;
        }
        finally {
            nextStringVisitor.Dispose();
            currentStringVisitor.Dispose();
        }
    }

    private abstract class PatternElement
    {
        internal abstract void ProcessStringCharacter(char currentCharacter, int currentPosition, PatternPositionsVisitor currentStringVisitor, PatternPositionsVisitor nextStringVisitor);
        internal abstract void ProcessEndOfString(int currentPosition, PatternPositionsVisitor endStringVisitor);
    }

    private class QuestionMarkElement : PatternElement
    {
        internal override void ProcessStringCharacter(char currentCharacter, int currentPosition, PatternPositionsVisitor currentStringVisitor, PatternPositionsVisitor nextStringVisitor)
            => nextStringVisitor.Add(currentPosition + 1);

        internal override void ProcessEndOfString(int currentPosition, PatternPositionsVisitor endStringVisitor) { }
    }

    private sealed class LiteralCharacterElement(char literalCharacter) : QuestionMarkElement
    {
        private readonly char m_literalCharacter = literalCharacter;

        internal override void ProcessStringCharacter(char currentCharacter, int currentPosition, PatternPositionsVisitor currentStringVisitor, PatternPositionsVisitor nextStringVisitor)
        {
            if (currentCharacter == m_literalCharacter)
                base.ProcessStringCharacter(currentCharacter, currentPosition, currentStringVisitor, nextStringVisitor);
        }
    }

    private sealed class BracketExpressionElement(Regex regex) : QuestionMarkElement
    {
        private readonly Regex m_regex = regex;

        internal override void ProcessStringCharacter(char currentCharacter, int currentPosition, PatternPositionsVisitor currentStringVisitor, PatternPositionsVisitor nextStringVisitor)
        {
            if (m_regex.IsMatch(new(currentCharacter, 1)))
                base.ProcessStringCharacter(currentCharacter, currentPosition, currentStringVisitor, nextStringVisitor);
        }
    }

    private sealed class AsterixElement : PatternElement
    {
        internal override void ProcessStringCharacter(char currentCharacter, int currentPosition, PatternPositionsVisitor currentStringVisitor, PatternPositionsVisitor nextStringVisitor)
        {
            currentStringVisitor.Add(currentPosition + 1);
            nextStringVisitor.Add(currentPosition);
        }

        internal override void ProcessEndOfString(int currentPosition, PatternPositionsVisitor endStringVisitor)
            => endStringVisitor.Add(currentPosition + 1);
    }

    private sealed class PatternPositionsVisitor : IDisposable
    {
        private readonly int m_lengthOfPattern;
        private readonly int[] m_isPatternPositionVisitedMarker;
        private readonly int[] m_patternPositionsForFurtherProcessing;
        
        private int m_patternPositionsForFurtherProcessingCount;

        internal int StringPosition { private get; set; }
        internal bool ReachedEndOfPattern => m_isPatternPositionVisitedMarker[m_lengthOfPattern] >= this.StringPosition;

        internal PatternPositionsVisitor(int lengthOfPattern)
        {
            m_lengthOfPattern = lengthOfPattern;

            m_isPatternPositionVisitedMarker = ArrayPool<int>.Shared.Rent(m_lengthOfPattern + 1);
            for (int i = 0; i <= m_lengthOfPattern; i++)
                m_isPatternPositionVisitedMarker[i] = -1;

            m_patternPositionsForFurtherProcessing = ArrayPool<int>.Shared.Rent(m_lengthOfPattern);
            m_patternPositionsForFurtherProcessingCount = 0;
        }

        public void Dispose()
        {
            ArrayPool<int>.Shared.Return(m_isPatternPositionVisitedMarker, clearArray: true);
            ArrayPool<int>.Shared.Return(m_patternPositionsForFurtherProcessing, clearArray: true);
        }


        internal void Add(int patternPosition)
        {
            if (m_isPatternPositionVisitedMarker[patternPosition] == this.StringPosition)
                return;

            m_isPatternPositionVisitedMarker[patternPosition] = this.StringPosition;
            if (patternPosition < m_lengthOfPattern) {
                m_patternPositionsForFurtherProcessing[m_patternPositionsForFurtherProcessingCount] = patternPosition;
                m_patternPositionsForFurtherProcessingCount++;
            }
        }

        internal bool MoveNext(out int patternPosition)
        {
            if (m_patternPositionsForFurtherProcessingCount == 0) {
                patternPosition = -1;
                return false;
            }

            m_patternPositionsForFurtherProcessingCount--;
            patternPosition = m_patternPositionsForFurtherProcessing[m_patternPositionsForFurtherProcessingCount];

            return true;
        }
    }

    private sealed class MyWildcardPatternParser : WildcardPatternParser
    {
        private readonly List<PatternElement> m_patternElements = [];
        
        private RegexOptions m_regexOptions;
        private StringBuilder? m_bracketExpressionBuilder;
        private CharacterNormalizer m_characterNormalizer;

        internal static PatternElement[] Parse(WildcardPattern pattern, CharacterNormalizer characterNormalizer)
        {
            var parser = new MyWildcardPatternParser {
                m_characterNormalizer = characterNormalizer,
                m_regexOptions = RegexParserExtensions.TranslateWildcardOptionsToRegexOptions(pattern.Options),
            };

            Parse(pattern, parser);

            return [.. parser.m_patternElements];
        }

        protected override void AppendLiteralCharacter(char c)
            => m_patternElements.Add(new LiteralCharacterElement(m_characterNormalizer.Normalize(c)));

        protected override void AppendAsterix()
            => m_patternElements.Add(new AsterixElement());

        protected override void AppendQuestionMark()
            => m_patternElements.Add(new QuestionMarkElement());

        protected override void BeginBracketExpression()
        {
            m_bracketExpressionBuilder = new StringBuilder();
            m_bracketExpressionBuilder.Append('[');
        }

        protected override void AppendLiteralCharacterToBracketExpression(char c)
            => RegexParserExtensions.AppendLiteralCharacterToBracketExpression(m_bracketExpressionBuilder!, c);

        protected override void AppendCharacterRangeToBracketExpression(char startOfCharacterRange, char endOfCharacterRange)
            => RegexParserExtensions.AppendCharacterRangeToBracketExpression(m_bracketExpressionBuilder!, startOfCharacterRange, endOfCharacterRange);

        protected override void EndBracketExpression()
        {
            m_bracketExpressionBuilder!.Append(']');
            m_patternElements.Add(new BracketExpressionElement(ParserOps.NewRegex(m_bracketExpressionBuilder.ToString(), m_regexOptions)));
        }
    }

    private readonly struct CharacterNormalizer
    {
        private readonly CultureInfo? m_cultureInfo;
        private readonly bool m_caseInsensitive;

        internal CharacterNormalizer(WildcardOptions options)
        {
            m_caseInsensitive = (options & WildcardOptions.IgnoreCase) != 0;
            if (m_caseInsensitive) {
                m_cultureInfo = (options & WildcardOptions.CultureInvariant) != 0
                    ? CultureInfo.InvariantCulture
                    : CultureInfo.CurrentCulture;
            }
            else
                m_cultureInfo = null;
        }

        internal char Normalize(char x)
        {
            if (m_caseInsensitive)
                return m_cultureInfo!.TextInfo.ToLower(x);

            return x;
        }
    }
}

internal static class ParserOps
{
    private const int MinCache = -100;
    private const int MaxCache = 1000;

    private static readonly string[] s_chars = new string[255];
    private static readonly object[] s_integerCache = new object[MaxCache - MinCache];
    private static readonly ConcurrentDictionary<RegexOptions, ConcurrentDictionary<string, Regex>> s_regexCache = [];
    private static readonly Func<RegexOptions, ConcurrentDictionary<string, Regex>> s_regexCacheCreationDelegate = key => new(StringComparer.Ordinal);

    static ParserOps()
    {
        for (int i = 0; i < (MaxCache - MinCache); i++)
            s_integerCache[i] = i + MinCache;

        for (char c = (char)0; c < 255; c++)
            s_chars[c] = new(c, 1);
    }

    internal static Regex NewRegex(string pattern, RegexOptions options)
    {
        ConcurrentDictionary<string, Regex> subordinateRegexCache = s_regexCache.GetOrAdd(options, s_regexCacheCreationDelegate);
        if (subordinateRegexCache.TryGetValue(pattern, out Regex? value)) {
            return value;
        }
        else {
            if (subordinateRegexCache.Count > MaxCache)
                subordinateRegexCache.Clear();

            Regex regex = new(pattern, options);
            return subordinateRegexCache.GetOrAdd(pattern, regex);
        }
    }
}

/// <summary>
/// Thrown when a wildcard pattern is invalid.
/// </summary>
public sealed class WildcardPatternException : Exception
{
    public WildcardPatternException()
        : base() { }

    public WildcardPatternException(string message)
        : base(message) { }
}