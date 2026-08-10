using System.Collections.Generic;

public sealed class DocumentEvaluator
{
    public DocumentEvaluationResult Evaluate(
        IReadOnlyList<DocumentWord> words
    )
    {
        DocumentEvaluationResult result =
            new DocumentEvaluationResult();

        if (words == null)
        {
            return result;
        }

        foreach (DocumentWord word in words)
        {
            if (word == null)
            {
                continue;
            }

            if (word.requiresRedaction &&
    !word.isRedacted)
            {
                result.missedWords++;
            }

            if (!word.requiresRedaction &&
                word.isRedacted)
            {
                result.extraRedactions++;
            }
        }

        return result;
    }
}