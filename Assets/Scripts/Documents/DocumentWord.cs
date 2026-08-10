using System.Collections.Generic;

public sealed class DocumentWord
{
    public int id;
    public string originalText;

    // Должен ли игрок засекретить это слово.
    public bool requiresRedaction;

    // Какими инструментами слово можно обнаружить.
    public RevealMethod revealMethods;

    // Текущее состояние игрока.
    public bool isRedacted;

    // Пометка игрока карандашом.
    // Не влияет на проверку документа и работу инструментов анализа.
    public bool isStruckThrough;

    // Временное состояние UV-визуализации.
    public bool isUltravioletRevealed;

    // Дополнительное выделение текста для создания акцента
    public bool isBold;

    // Дополнительные данные для инструментов анализа.
    // Например:
    // RevealMethod.Decoder -> "ВОЛКОВ"
    private readonly Dictionary<RevealMethod, string>
        analysisPayloads =
            new Dictionary<RevealMethod, string>();

    public bool CanBeRevealedBy(
        RevealMethod method
    )
    {
        return (revealMethods & method) != 0;
    }

    public void SetAnalysisPayload(
        RevealMethod method,
        string payload
    )
    {
        if (method == RevealMethod.None)
        {
            return;
        }

        if (string.IsNullOrEmpty(payload))
        {
            analysisPayloads.Remove(method);
            return;
        }

        analysisPayloads[method] = payload;
    }

    public bool TryGetAnalysisPayload(
        RevealMethod method,
        out string payload
    )
    {
        return analysisPayloads.TryGetValue(
            method,
            out payload
        );
    }
}
