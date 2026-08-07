public sealed class DocumentWord
{
    public int id;
    public string originalText;

    // ƒолжен ли игрок засекретить это слово.
    public bool requiresRedaction;

    //  акими инструментами слово можно обнаружить.
    public RevealMethod revealMethods;

    // “екущее состо€ние игрока.
    public bool isRedacted;

    // ¬ременно ли слово про€влено сейчас.
    public bool isUltravioletRevealed;

    public bool CanBeRevealedBy(
        RevealMethod method
    )
    {
        return (revealMethods & method) != 0;
    }
}