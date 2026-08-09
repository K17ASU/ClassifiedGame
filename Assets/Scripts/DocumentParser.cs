using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

public sealed class DocumentParser
{
    private static readonly Regex WordPattern = new Regex(
        @"[\p{L}\p{N}]+(?:[-–—'][\p{L}\p{N}]+)*",
        RegexOptions.Compiled
    );

    private sealed class CharacterMetadata
    {
        public bool requiresRedaction;
        public RevealMethod revealMethods;
        public string decoderPayload;
    }

    private sealed class ActiveAnnotation
    {
        public bool requiresRedaction;
        public RevealMethod revealMethods;
        public string decoderPayload;
    }

    private sealed class ParsedSource
    {
        public string cleanText;

        public List<CharacterMetadata> metadata =
            new List<CharacterMetadata>();

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

        int position = 0;

        while (position < sourceText.Length)
        {
            if (activeAnnotation == null)
            {
                if (TryReadUnifiedOpeningTag(
                        sourceText,
                        position,
                        out ActiveAnnotation annotation,
                        out int openingLength))
                {
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
        out int openingLength
    )
    {
        annotation = null;
        openingLength = 0;

        if (position >= sourceText.Length ||
            sourceText[position] != '[')
        {
            return false;
        }

        // Старый UV-маркер [[...]] должен
        // обрабатываться legacy-парсером.
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

        foreach (string rawToken in tokens)
        {
            string token =
                rawToken.Trim();

            if (token.Equals(
                    "redact",
                    StringComparison
                        .OrdinalIgnoreCase))
            {
                parsedAnnotation
                    .requiresRedaction = true;

                hasKnownToken = true;
                continue;
            }

            if (token.Equals(
                    "uv",
                    StringComparison
                        .OrdinalIgnoreCase))
            {
                parsedAnnotation.revealMethods |=
                    RevealMethod.Ultraviolet;

                hasKnownToken = true;
                continue;
            }

            if (token.Equals(
                    "magnifier",
                    StringComparison
                        .OrdinalIgnoreCase) ||
                token.Equals(
                    "mag",
                    StringComparison
                        .OrdinalIgnoreCase))
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
                    StringComparison
                        .OrdinalIgnoreCase))
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
        }

        if (!hasKnownToken)
        {
            return false;
        }

        annotation = parsedAnnotation;

        openingLength =
            closingBracket - position + 1;

        return true;
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

        if (StartsWith(
                sourceText,
                position,
                "[["))
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

        if (StartsWith(
                sourceText,
                position,
                "{{"))
        {
            annotation =
                new ActiveAnnotation
                {
                    requiresRedaction = true
                };

            closingMarker = "}}";
            return true;
        }

        if (StartsWith(
                sourceText,
                position,
                "(("))
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

        if (StartsWith(
                sourceText,
                position,
                "<<"))
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

        if (StartsWith(
                sourceText,
                position,
                "##"))
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
                annotation.decoderPayload
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

                if (!string.IsNullOrEmpty(
                        metadata.decoderPayload))
                {
                    decoderPayload =
                        metadata.decoderPayload;
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

                    isRedacted = false,

                    isUltravioletRevealed =
                        false
                };

            if (!string.IsNullOrEmpty(
                    decoderPayload))
            {
                word.SetAnalysisPayload(
                    RevealMethod.Decoder,
                    decoderPayload
                );
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
