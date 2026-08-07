using System.Collections.Generic;

public sealed class DocumentParseResult
{
    public List<DocumentWord> words = new List<DocumentWord>();
    public List<DocumentTextPart> textParts = new List<DocumentTextPart>();
    public bool hasUnclosedSecretMarker;
    public int secretWordCount;
}
