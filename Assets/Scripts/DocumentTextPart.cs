public sealed class DocumentTextPart
{
    public bool isWord;
    public int wordId;
    public string separatorText;

    public static DocumentTextPart CreateWord(int id)
    {
        return new DocumentTextPart
        {
            isWord = true,
            wordId = id,
            separatorText = string.Empty
        };
    }

    public static DocumentTextPart CreateSeparator(string text)
    {
        return new DocumentTextPart
        {
            isWord = false,
            wordId = -1,
            separatorText = text
        };
    }
}
