using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

public sealed class DocumentParser
{
    private static readonly Regex WordPattern = new Regex(
        @"[\p{L}\p{N}]+(?:[-–—'][\p{L}\p{N}]+)*",
        RegexOptions.Compiled
    );

    public DocumentParseResult Parse(string sourceText)
    {
        DocumentParseResult result = new DocumentParseResult();

        if (string.IsNullOrWhiteSpace(sourceText))
        {
            return result;
        }

        ParsedSource parsedSource = RemoveSecretMarkers(sourceText);

        result.hasUnclosedSecretMarker =
            parsedSource.hasUnclosedSecretMarker;

        CreateWordsAndTextParts(
            parsedSource.cleanText,
            parsedSource.secretCharacters,
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

    private ParsedSource RemoveSecretMarkers(string sourceText)
    {
        StringBuilder cleanText = new StringBuilder();
        List<bool> secretCharacters = new List<bool>();

        bool insideSecretFragment = false;
        int position = 0;

        while (position < sourceText.Length)
        {
            bool startsSecretFragment =
                position + 1 < sourceText.Length &&
                sourceText[position] == '[' &&
                sourceText[position + 1] == '[';

            bool endsSecretFragment =
                position + 1 < sourceText.Length &&
                sourceText[position] == ']' &&
                sourceText[position + 1] == ']';

            if (startsSecretFragment)
            {
                insideSecretFragment = true;
                position += 2;
                continue;
            }

            if (endsSecretFragment)
            {
                insideSecretFragment = false;
                position += 2;
                continue;
            }

            cleanText.Append(sourceText[position]);
            secretCharacters.Add(insideSecretFragment);
            position++;
        }

        return new ParsedSource
        {
            cleanText = cleanText.ToString(),
            secretCharacters = secretCharacters,
            hasUnclosedSecretMarker = insideSecretFragment
        };
    }

    private void CreateWordsAndTextParts(
        string cleanText,
        List<bool> secretCharacters,
        DocumentParseResult result
    )
    {
        MatchCollection matches = WordPattern.Matches(cleanText);

        int currentPosition = 0;
        int wordId = 0;

        foreach (Match match in matches)
        {
            if (match.Index > currentPosition)
            {
                string separator = cleanText.Substring(
                    currentPosition,
                    match.Index - currentPosition
                );

                result.textParts.Add(
                    DocumentTextPart.CreateSeparator(separator)
                );
            }

            bool requiresRedaction =
                IsWordSecret(
                    match.Index,
                    match.Length,
                    secretCharacters
                );

            DocumentWord word =
                new DocumentWord
                {
                    id = wordId,
                    originalText = match.Value,
                    requiresRedaction = requiresRedaction,
                    revealMethods = requiresRedaction
                        ? RevealMethod.Ultraviolet
                        : RevealMethod.None,
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
                DocumentTextPart.CreateSeparator(remainingText)
            );
        }
    }

    private bool IsWordSecret(
        int startIndex,
        int length,
        List<bool> secretCharacters
    )
    {
        int endIndex = startIndex + length;

        for (int i = startIndex;
             i < endIndex &&
             i < secretCharacters.Count;
             i++)
        {
            if (secretCharacters[i])
            {
                return true;
            }
        }

        return false;
    }

    private sealed class ParsedSource
    {
        public string cleanText;
        public List<bool> secretCharacters;
        public bool hasUnclosedSecretMarker;
    }
}
