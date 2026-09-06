using System.Collections.Generic;

public sealed class DocumentWord
{
    public int id;
    public string originalText;
    public bool requiresRedaction;
    public RevealMethod revealMethods;
    public string storyFragmentId;
    public bool isRedacted;
    public bool isStruckThrough;
    public bool isUltravioletRevealed;
    public bool isBold;

    public int cycleGroupId = -1;
    public List<string> cycleValues = new List<string>();
    public float cycleIntervalMin = 0.5f;
    public float cycleIntervalMax = 0.5f;
    public int cycleValueIndex = -1;

    public bool HasCycle =>
        cycleGroupId >= 0 &&
        cycleValues != null &&
        cycleValues.Count >= 2;

    public string GetDisplayText()
    {
        if (HasCycle &&
            cycleValueIndex >= 0 &&
            cycleValueIndex < cycleValues.Count)
        {
            return cycleValues[cycleValueIndex];
        }

        return originalText;
    }

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
