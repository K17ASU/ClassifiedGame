public sealed class DocumentEvaluationResult
{
    public int missedWords;
    public int extraRedactions;

    public bool IsCorrect =>
        missedWords == 0 &&
        extraRedactions == 0;
}