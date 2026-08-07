using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

public sealed class DocumentParser
{
    private static readonly Regex WordPattern = new Regex(
        @"[\p{L}\p{N}]+(?:[-–—'][\p{L}\p{N}]+)*",
        RegexOptions.Compiled
    );

    private enum MarkerType
    {
        None,
        RedactAndUltraviolet,
        RedactOnly,
        UltravioletOnly
    }

    public DocumentParseResult Parse(string sourceText)
    {
        DocumentParseResult result = new DocumentParseResult();

        if (string.IsNullOrWhiteSpace(sourceText))
        {
            return result;
        }

        ParsedSource parsedSource = RemoveMarkers(sourceText);

        result.hasUnclosedSecretMarker =
            parsedSource.hasUnclosedMarker;

        CreateWordsAndTextParts(
            parsedSource.cleanText,
            parsedSource.redactionCharacters,
            parsedSource.ultravioletCharacters,
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

    private ParsedSource RemoveMarkers(string sourceText)
    {
        StringBuilder cleanText = new StringBuilder();

        List<bool> redactionCharacters =
            new List<bool>();

        List<bool> ultravioletCharacters =
            new List<bool>();

        MarkerType currentMarker = MarkerType.None;
        int position = 0;

        while (position < sourceText.Length)
        {
            if (currentMarker == MarkerType.None)
            {
                if (StartsWith(sourceText, position, "[["))
                {
                    currentMarker =
                        MarkerType.RedactAndUltraviolet;

                    position += 2;
                    continue;
                }

                if (StartsWith(sourceText, position, "{{"))
                {
                    currentMarker =
                        MarkerType.RedactOnly;

                    position += 2;
                    continue;
                }

                if (StartsWith(sourceText, position, "(("))
                {
                    currentMarker =
                        MarkerType.UltravioletOnly;

                    position += 2;
                    continue;
                }
            }
            else if (IsClosingMarker(
                         sourceText,
                         position,
                         currentMarker))
            {
                currentMarker = MarkerType.None;
                position += 2;
                continue;
            }

            cleanText.Append(sourceText[position]);

            redactionCharacters.Add(
                currentMarker ==
                    MarkerType.RedactAndUltraviolet ||
                currentMarker ==
                    MarkerType.RedactOnly
            );

            ultravioletCharacters.Add(
                currentMarker ==
                    MarkerType.RedactAndUltraviolet ||
                currentMarker ==
                    MarkerType.UltravioletOnly
            );

            position++;
        }

        return new ParsedSource
        {
            cleanText = cleanText.ToString(),

            redactionCharacters =
                redactionCharacters,

            ultravioletCharacters =
                ultravioletCharacters,

            hasUnclosedMarker =
                currentMarker != MarkerType.None
        };
    }

    private void CreateWordsAndTextParts(
        string cleanText,
        List<bool> redactionCharacters,
        List<bool> ultravioletCharacters,
        DocumentParseResult result
    )
    {
        MatchCollection matches =
            WordPattern.Matches(cleanText);

        int currentPosition = 0;
        int wordId = 0;

        foreach (Match match in matches)
        {
            if (match.Index > currentPosition)
            {
                string separator =
                    cleanText.Substring(
                        currentPosition,
                        match.Index - currentPosition
                    );

                result.textParts.Add(
                    DocumentTextPart.CreateSeparator(
                        separator
                    )
                );
            }

            bool requiresRedaction =
                IsWordMarked(
                    match.Index,
                    match.Length,
                    redactionCharacters
                );

            bool ultravioletVisible =
                IsWordMarked(
                    match.Index,
                    match.Length,
                    ultravioletCharacters
                );

            RevealMethod revealMethods =
                ultravioletVisible
                    ? RevealMethod.Ultraviolet
                    : RevealMethod.None;

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

                    isUltravioletRevealed = false
                };

            result.words.Add(word);

            result.textParts.Add(
                DocumentTextPart.CreateWord(wordId)
            );

            wordId++;

            currentPosition =
                match.Index + match.Length;
        }

        if (currentPosition < cleanText.Length)
        {
            string remainingText =
                cleanText.Substring(currentPosition);

            result.textParts.Add(
                DocumentTextPart.CreateSeparator(
                    remainingText
                )
            );
        }
    }

    private bool IsWordMarked(
        int startIndex,
        int length,
        List<bool> markedCharacters
    )
    {
        int endIndex = startIndex + length;

        for (int i = startIndex;
             i < endIndex &&
             i < markedCharacters.Count;
             i++)
        {
            if (markedCharacters[i])
            {
                return true;
            }
        }

        return false;
    }

    private bool IsClosingMarker(
        string sourceText,
        int position,
        MarkerType markerType
    )
    {
        switch (markerType)
        {
            case MarkerType.RedactAndUltraviolet:
                return StartsWith(
                    sourceText,
                    position,
                    "]]"
                );

            case MarkerType.RedactOnly:
                return StartsWith(
                    sourceText,
                    position,
                    "}}"
                );

            case MarkerType.UltravioletOnly:
                return StartsWith(
                    sourceText,
                    position,
                    "))"
                );

            default:
                return false;
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

    private sealed class ParsedSource
    {
        public string cleanText;

        public List<bool> redactionCharacters;

        public List<bool> ultravioletCharacters;

        public bool hasUnclosedMarker;
    }
}
