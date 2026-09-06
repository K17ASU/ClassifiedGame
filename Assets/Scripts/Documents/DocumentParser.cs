using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

public sealed class DocumentParser
{
    private const float DefaultCycleInterval = 0.5f;
    private const float MinimumCycleInterval = 0.05f;

    private static readonly Regex WordPattern = new Regex(
        @"[\p{L}\p{N}]+(?:[-–—'][\p{L}\p{N}]+)*",
        RegexOptions.Compiled
    );

    private sealed class CycleDefinition
    {
        public int groupId = -1;

        public readonly List<string> alternatives =
            new List<string>();

        public float intervalMin =
            DefaultCycleInterval;

        public float intervalMax =
            DefaultCycleInterval;
    }

    private sealed class CharacterMetadata
    {
        public bool requiresRedaction;
        public RevealMethod revealMethods;
        public string decoderPayload;
        public bool isBold;
        public string storyFragmentId;
        public int cycleGroupId = -1;
    }

    private sealed class ActiveAnnotation
    {
        public bool requiresRedaction;
        public RevealMethod revealMethods;
        public string decoderPayload;
        public bool isBold;
        public string storyFragmentId;
        public int cycleGroupId = -1;
    }

    private sealed class ParsedSource
    {
        public string cleanText;

        public List<CharacterMetadata> metadata =
            new List<CharacterMetadata>();

        public readonly Dictionary<int, CycleDefinition>
            cycleDefinitions =
                new Dictionary<int, CycleDefinition>();

        public bool hasUnclosedMarker;
    }

    public DocumentParseResult Parse(
        string sourceText
    )
    {
        DocumentParseResult result =
            new DocumentParseResult();

        if (string.IsNullOrWhiteSpace(sourceText))
        {
            return result;
        }

        ParsedSource parsedSource =
            RemoveMarkers(sourceText);

        result.hasUnclosedSecretMarker =
            parsedSource.hasUnclosedMarker;

        CreateWordsAndTextParts(
            parsedSource,
            result
        );

        foreach (DocumentWord word in result.words)
        {
            if (word.requiresRedaction)
            {
                result.secretWordCount++;
            }
        }

        return result;
    }

    private ParsedSource RemoveMarkers(
        string sourceText
    )
    {
        ParsedSource result =
            new ParsedSource();

        StringBuilder cleanText =
            new StringBuilder();

        ActiveAnnotation activeAnnotation = null;
        string legacyClosingMarker = null;

        int nextCycleGroupId = 0;
        int position = 0;

        while (position < sourceText.Length)
        {
            if (activeAnnotation == null)
            {
                if (TryReadUnifiedOpeningTag(
                        sourceText,
                        position,
                        out ActiveAnnotation annotation,
                        out int openingLength,
                        out CycleDefinition cycleDefinition))
                {
                    if (cycleDefinition != null)
                    {
                        cycleDefinition.groupId =
                            nextCycleGroupId;

                        annotation.cycleGroupId =
                            nextCycleGroupId;

                        result.cycleDefinitions.Add(
                            nextCycleGroupId,
                            cycleDefinition
                        );

                        nextCycleGroupId++;
                    }

                    activeAnnotation = annotation;
                    legacyClosingMarker = null;
                    position += openingLength;
                    continue;
                }

                if (TryReadLegacyOpeningMarker(
                        sourceText,
                        position,
                        out annotation,
                        out legacyClosingMarker))
                {
                    activeAnnotation = annotation;
                    position += 2;
                    continue;
                }
            }
            else
            {
                if (legacyClosingMarker != null)
                {
                    if (StartsWith(
                            sourceText,
                            position,
                            legacyClosingMarker))
                    {
                        activeAnnotation = null;
                        legacyClosingMarker = null;
                        position += 2;
                        continue;
                    }
                }
                else if (StartsWith(
                             sourceText,
                             position,
                             "[/]"))
                {
                    activeAnnotation = null;
                    position += 3;
                    continue;
                }
            }

            cleanText.Append(
                sourceText[position]
            );

            result.metadata.Add(
                CreateCharacterMetadata(
                    activeAnnotation
                )
            );

            position++;
        }

        result.cleanText =
            cleanText.ToString();

        result.hasUnclosedMarker =
            activeAnnotation != null;

        return result;
    }

    private bool TryReadUnifiedOpeningTag(
        string sourceText,
        int position,
        out ActiveAnnotation annotation,
        out int openingLength,
        out CycleDefinition cycleDefinition
    )
    {
        annotation = null;
        openingLength = 0;
        cycleDefinition = null;

        if (position >= sourceText.Length ||
            sourceText[position] != '[')
        {
            return false;
        }

        if (StartsWith(
                sourceText,
                position,
                "[["))
        {
            return false;
        }

        int closingBracket =
            sourceText.IndexOf(
                ']',
                position + 1
            );

        if (closingBracket < 0)
        {
            return false;
        }

        string tagContent =
            sourceText.Substring(
                position + 1,
                closingBracket - position - 1
            );

        if (string.IsNullOrWhiteSpace(
                tagContent) ||
            tagContent == "/")
        {
            return false;
        }

        ActiveAnnotation parsedAnnotation =
            new ActiveAnnotation();

        string[] tokens =
            tagContent.Split(',');

        bool hasKnownToken = false;
        List<string> cycleAlternatives = null;
        string intervalTokenValue = null;

        foreach (string rawToken in tokens)
        {
            string token =
                rawToken.Trim();

            if (token.Equals(
                    "redact",
                    StringComparison.OrdinalIgnoreCase))
            {
                parsedAnnotation.requiresRedaction = true;
                hasKnownToken = true;
                continue;
            }

            if (token.Equals(
                    "bold",
                    StringComparison.OrdinalIgnoreCase))
            {
                parsedAnnotation.isBold = true;
                hasKnownToken = true;
                continue;
            }

            if (token.Equals(
                    "uv",
                    StringComparison.OrdinalIgnoreCase))
            {
                parsedAnnotation.revealMethods |=
                    RevealMethod.Ultraviolet;

                hasKnownToken = true;
                continue;
            }

            if (token.Equals(
                    "magnifier",
                    StringComparison.OrdinalIgnoreCase) ||
                token.Equals(
                    "mag",
                    StringComparison.OrdinalIgnoreCase))
            {
                parsedAnnotation.revealMethods |=
                    RevealMethod.Magnifier;

                hasKnownToken = true;
                continue;
            }

            const string decoderPrefix =
                "decoder=";

            if (token.StartsWith(
                    decoderPrefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                parsedAnnotation.revealMethods |=
                    RevealMethod.Decoder;

                parsedAnnotation.decoderPayload =
                    token.Substring(
                        decoderPrefix.Length
                    );

                hasKnownToken = true;
                continue;
            }

            const string storyIdPrefix =
                "id=";

            if (token.StartsWith(
                    storyIdPrefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                string storyFragmentId =
                    token.Substring(
                            storyIdPrefix.Length
                        )
                        .Trim();

                if (!string.IsNullOrWhiteSpace(
                        storyFragmentId))
                {
                    parsedAnnotation.storyFragmentId =
                        storyFragmentId;
                }

                hasKnownToken = true;
                continue;
            }

            const string cyclePrefix =
                "cycle=";

            if (token.StartsWith(
                    cyclePrefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                string rawCycle =
                    token.Substring(
                            cyclePrefix.Length
                        )
                        .Trim();

                cycleAlternatives =
                    ParseCycleAlternatives(
                        rawCycle
                    );

                hasKnownToken = true;
                continue;
            }

            const string intervalPrefix =
                "interval=";

            if (token.StartsWith(
                    intervalPrefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                intervalTokenValue =
                    token.Substring(
                            intervalPrefix.Length
                        )
                        .Trim();

                hasKnownToken = true;
                continue;
            }
        }

        if (!hasKnownToken)
        {
            return false;
        }

        if (cycleAlternatives != null)
        {
            if (cycleAlternatives.Count < 2)
            {
                Debug.LogWarning(
                    "DocumentParser: cycle= должен содержать " +
                    "как минимум два значения через |. " +
                    "Cycle для этого фрагмента отключён."
                );
            }
            else
            {
                cycleDefinition =
                    new CycleDefinition();

                cycleDefinition.alternatives.AddRange(
                    cycleAlternatives
                );

                if (!string.IsNullOrWhiteSpace(
                        intervalTokenValue))
                {
                    if (!TryParseCycleInterval(
                            intervalTokenValue,
                            out float intervalMin,
                            out float intervalMax))
                    {
                        Debug.LogWarning(
                            $"DocumentParser: не удалось разобрать " +
                            $"interval={intervalTokenValue}. " +
                            $"Использую {DefaultCycleInterval:0.##} сек."
                        );
                    }
                    else
                    {
                        cycleDefinition.intervalMin =
                            intervalMin;

                        cycleDefinition.intervalMax =
                            intervalMax;
                    }
                }
            }
        }
        else if (!string.IsNullOrWhiteSpace(
                     intervalTokenValue))
        {
            Debug.LogWarning(
                "DocumentParser: interval= указан без cycle=. " +
                "Параметр проигнорирован."
            );
        }

        annotation = parsedAnnotation;

        openingLength =
            closingBracket - position + 1;

        return true;
    }

    private List<string> ParseCycleAlternatives(
        string rawCycle)
    {
        List<string> result =
            new List<string>();

        if (string.IsNullOrWhiteSpace(
                rawCycle))
        {
            return result;
        }

        string[] values =
            rawCycle.Split('|');

        foreach (string rawValue in values)
        {
            string value =
                rawValue.Trim();

            if (!string.IsNullOrWhiteSpace(
                    value))
            {
                result.Add(value);
            }
        }

        return result;
    }

    private bool TryParseCycleInterval(
        string rawValue,
        out float intervalMin,
        out float intervalMax)
    {
        intervalMin =
            DefaultCycleInterval;

        intervalMax =
            DefaultCycleInterval;

        if (string.IsNullOrWhiteSpace(
                rawValue))
        {
            return false;
        }

        string normalized =
            rawValue.Trim();

        int separatorIndex =
            normalized.IndexOf(
                '-',
                1
            );

        if (separatorIndex < 0)
        {
            if (!TryParseInvariantFloat(
                    normalized,
                    out float fixedInterval))
            {
                return false;
            }

            fixedInterval =
                Mathf.Max(
                    MinimumCycleInterval,
                    fixedInterval
                );

            intervalMin =
                fixedInterval;

            intervalMax =
                fixedInterval;

            return true;
        }

        string minText =
            normalized.Substring(
                    0,
                    separatorIndex
                )
                .Trim();

        string maxText =
            normalized.Substring(
                    separatorIndex + 1
                )
                .Trim();

        if (!TryParseInvariantFloat(
                minText,
                out float parsedMin) ||
            !TryParseInvariantFloat(
                maxText,
                out float parsedMax))
        {
            return false;
        }

        parsedMin =
            Mathf.Max(
                MinimumCycleInterval,
                parsedMin
            );

        parsedMax =
            Mathf.Max(
                MinimumCycleInterval,
                parsedMax
            );

        if (parsedMax < parsedMin)
        {
            float temp = parsedMin;
            parsedMin = parsedMax;
            parsedMax = temp;
        }

        intervalMin = parsedMin;
        intervalMax = parsedMax;

        return true;
    }

    private bool TryParseInvariantFloat(
        string value,
        out float result)
    {
        return float.TryParse(
            value,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out result
        );
    }

    private bool TryReadLegacyOpeningMarker(
        string sourceText,
        int position,
        out ActiveAnnotation annotation,
        out string closingMarker
    )
    {
        annotation = null;
        closingMarker = null;

        if (StartsWith(sourceText, position, "[["))
        {
            annotation =
                new ActiveAnnotation
                {
                    requiresRedaction = true,
                    revealMethods =
                        RevealMethod.Ultraviolet
                };

            closingMarker = "]]";
            return true;
        }

        if (StartsWith(sourceText, position, "{{"))
        {
            annotation =
                new ActiveAnnotation
                {
                    requiresRedaction = true
                };

            closingMarker = "}}";
            return true;
        }

        if (StartsWith(sourceText, position, "(("))
        {
            annotation =
                new ActiveAnnotation
                {
                    revealMethods =
                        RevealMethod.Ultraviolet
                };

            closingMarker = "))";
            return true;
        }

        if (StartsWith(sourceText, position, "<<"))
        {
            annotation =
                new ActiveAnnotation
                {
                    requiresRedaction = true,
                    revealMethods =
                        RevealMethod.Magnifier
                };

            closingMarker = ">>";
            return true;
        }

        if (StartsWith(sourceText, position, "##"))
        {
            annotation =
                new ActiveAnnotation
                {
                    revealMethods =
                        RevealMethod.Magnifier
                };

            closingMarker = "##";
            return true;
        }

        return false;
    }

    private CharacterMetadata
        CreateCharacterMetadata(
            ActiveAnnotation annotation
        )
    {
        if (annotation == null)
        {
            return new CharacterMetadata();
        }

        return new CharacterMetadata
        {
            requiresRedaction =
                annotation.requiresRedaction,

            revealMethods =
                annotation.revealMethods,

            decoderPayload =
                annotation.decoderPayload,

            isBold =
                annotation.isBold,

            storyFragmentId =
                annotation.storyFragmentId,

            cycleGroupId =
                annotation.cycleGroupId
        };
    }

    private void CreateWordsAndTextParts(
        ParsedSource parsedSource,
        DocumentParseResult result
    )
    {
        MatchCollection matches =
            WordPattern.Matches(
                parsedSource.cleanText
            );

        Dictionary<int, List<DocumentWord>>
            cycleWordsByGroup =
                new Dictionary<
                    int,
                    List<DocumentWord>>();

        int currentPosition = 0;
        int wordId = 0;

        foreach (Match match in matches)
        {
            if (match.Index > currentPosition)
            {
                string separator =
                    parsedSource.cleanText.Substring(
                        currentPosition,
                        match.Index - currentPosition
                    );

                result.textParts.Add(
                    DocumentTextPart.CreateSeparator(
                        separator
                    )
                );
            }

            bool requiresRedaction = false;
            RevealMethod revealMethods =
                RevealMethod.None;

            string decoderPayload = null;
            string storyFragmentId = null;
            bool isBold = false;
            int cycleGroupId = -1;

            int endIndex =
                match.Index + match.Length;

            for (int i = match.Index;
                 i < endIndex &&
                 i < parsedSource.metadata.Count;
                 i++)
            {
                CharacterMetadata metadata =
                    parsedSource.metadata[i];

                requiresRedaction |=
                    metadata.requiresRedaction;

                revealMethods |=
                    metadata.revealMethods;

                isBold |=
                    metadata.isBold;

                if (!string.IsNullOrEmpty(
                        metadata.decoderPayload))
                {
                    decoderPayload =
                        metadata.decoderPayload;
                }

                if (!string.IsNullOrWhiteSpace(
                        metadata.storyFragmentId))
                {
                    storyFragmentId =
                        metadata.storyFragmentId;
                }

                if (metadata.cycleGroupId >= 0)
                {
                    cycleGroupId =
                        metadata.cycleGroupId;
                }
            }

            DocumentWord word =
                new DocumentWord
                {
                    id = wordId,
                    originalText = match.Value,

                    requiresRedaction =
                        requiresRedaction,

                    revealMethods =
                        revealMethods,

                    storyFragmentId =
                        storyFragmentId,

                    isBold =
                        isBold,

                    cycleGroupId =
                        cycleGroupId,

                    isRedacted = false,
                    isUltravioletRevealed = false
                };

            if (!string.IsNullOrEmpty(
                    decoderPayload))
            {
                word.SetAnalysisPayload(
                    RevealMethod.Decoder,
                    decoderPayload
                );
            }

            if (cycleGroupId >= 0)
            {
                if (!cycleWordsByGroup.TryGetValue(
                        cycleGroupId,
                        out List<DocumentWord> cycleWords))
                {
                    cycleWords =
                        new List<DocumentWord>();

                    cycleWordsByGroup.Add(
                        cycleGroupId,
                        cycleWords
                    );
                }

                cycleWords.Add(word);
            }

            result.words.Add(word);

            result.textParts.Add(
                DocumentTextPart.CreateWord(wordId)
            );

            wordId++;

            currentPosition =
                match.Index + match.Length;
        }

        if (currentPosition <
            parsedSource.cleanText.Length)
        {
            string remainingText =
                parsedSource.cleanText.Substring(
                    currentPosition
                );

            result.textParts.Add(
                DocumentTextPart.CreateSeparator(
                    remainingText
                )
            );
        }

        ApplyCycleDefinitions(
            parsedSource,
            cycleWordsByGroup
        );
    }

    private void ApplyCycleDefinitions(
        ParsedSource parsedSource,
        Dictionary<int, List<DocumentWord>>
            cycleWordsByGroup)
    {
        foreach (
            KeyValuePair<int, CycleDefinition> pair
            in parsedSource.cycleDefinitions)
        {
            int groupId = pair.Key;
            CycleDefinition definition = pair.Value;

            if (!cycleWordsByGroup.TryGetValue(
                    groupId,
                    out List<DocumentWord> groupWords) ||
                groupWords.Count == 0)
            {
                continue;
            }

            List<List<string>>
                tokenizedAlternatives =
                    new List<List<string>>();

            bool valid = true;

            foreach (
                string alternative
                in definition.alternatives)
            {
                List<string> alternativeWords =
                    TokenizeCycleAlternative(
                        alternative
                    );

                if (alternativeWords.Count !=
                    groupWords.Count)
                {
                    Debug.LogWarning(
                        $"DocumentParser: cycle-фрагмент " +
                        $"«{alternative}» содержит " +
                        $"{alternativeWords.Count} слов, " +
                        $"а текст между тегами — " +
                        $"{groupWords.Count}. " +
                        $"Все варианты cycle= должны иметь " +
                        $"одинаковое количество слов. " +
                        $"Cycle отключён."
                    );

                    valid = false;
                    break;
                }

                tokenizedAlternatives.Add(
                    alternativeWords
                );
            }

            if (!valid ||
                tokenizedAlternatives.Count < 2)
            {
                DisableCycleForGroup(
                    groupWords
                );

                continue;
            }

            for (int wordIndex = 0;
                 wordIndex < groupWords.Count;
                 wordIndex++)
            {
                DocumentWord word =
                    groupWords[wordIndex];

                word.cycleValues.Clear();

                foreach (
                    List<string> alternativeWords
                    in tokenizedAlternatives)
                {
                    word.cycleValues.Add(
                        alternativeWords[wordIndex]
                    );
                }

                word.cycleIntervalMin =
                    definition.intervalMin;

                word.cycleIntervalMax =
                    definition.intervalMax;

                word.cycleValueIndex = -1;
            }
        }
    }

    private List<string> TokenizeCycleAlternative(
        string alternative)
    {
        List<string> result =
            new List<string>();

        MatchCollection matches =
            WordPattern.Matches(
                alternative
            );

        foreach (Match match in matches)
        {
            result.Add(match.Value);
        }

        return result;
    }

    private void DisableCycleForGroup(
        IReadOnlyList<DocumentWord> words)
    {
        if (words == null)
        {
            return;
        }

        foreach (DocumentWord word in words)
        {
            if (word == null)
            {
                continue;
            }

            word.cycleGroupId = -1;
            word.cycleValues.Clear();
            word.cycleValueIndex = -1;
        }
    }

    private bool StartsWith(
        string sourceText,
        int position,
        string marker
    )
    {
        return position + marker.Length <=
               sourceText.Length &&
               sourceText.Substring(
                   position,
                   marker.Length
               ) == marker;
    }
}
