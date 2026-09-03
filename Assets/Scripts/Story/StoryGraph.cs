using System;
using System.Collections.Generic;
using UnityEngine;

public enum StoryConditionType
{
    Always,
    DocumentPassed,
    DocumentFailed,
    DocumentScoreAtLeast,
    CampaignPercentAtLeast,
    CampaignPercentBelow
}

[Serializable]
public sealed class StoryCondition
{
    [SerializeField]
    private StoryConditionType type =
        StoryConditionType.Always;

    [SerializeField]
    private float value;

    public bool IsMet(
        StoryRouteContext context)
    {
        switch (type)
        {
            case StoryConditionType.Always:
                return true;

            case StoryConditionType.DocumentPassed:
                return context.DocumentPassed;

            case StoryConditionType.DocumentFailed:
                return !context.DocumentPassed;

            case StoryConditionType.DocumentScoreAtLeast:
                return context.DocumentScore >= value;

            case StoryConditionType.CampaignPercentAtLeast:
                return context.CampaignPercent >= value;

            case StoryConditionType.CampaignPercentBelow:
                return context.CampaignPercent < value;

            default:
                return false;
        }
    }
}

[Serializable]
public sealed class StoryBranch
{
    [SerializeField]
    private string debugName;

    [SerializeField]
    private List<StoryCondition> conditions =
        new List<StoryCondition>();

    [SerializeField]
    private string nextDocumentId;

    [SerializeField]
    private bool endsCampaign;

    public string DebugName => debugName;

    public string NextDocumentId =>
        nextDocumentId;

    public bool EndsCampaign =>
        endsCampaign;

    public bool Matches(
        StoryRouteContext context)
    {
        if (conditions == null ||
            conditions.Count == 0)
        {
            return true;
        }

        foreach (
            StoryCondition condition
            in conditions)
        {
            if (condition == null ||
                !condition.IsMet(context))
            {
                return false;
            }
        }

        return true;
    }
}

[Serializable]
public sealed class StoryNode
{
    [SerializeField]
    private string currentDocumentId;

    [SerializeField]
    private List<StoryBranch> branches =
        new List<StoryBranch>();

    [SerializeField]
    private string defaultNextDocumentId;

    [SerializeField]
    private bool endCampaignIfNoBranchMatches;

    public string CurrentDocumentId =>
        currentDocumentId;

    public IReadOnlyList<StoryBranch> Branches =>
        branches;

    public string DefaultNextDocumentId =>
        defaultNextDocumentId;

    public bool EndCampaignIfNoBranchMatches =>
        endCampaignIfNoBranchMatches;
}

[CreateAssetMenu(
    fileName = "StoryGraph",
    menuName = "Classified/Story Graph"
)]
public sealed class StoryGraph : ScriptableObject
{
    [Header("Отображение прогресса")]

    [Tooltip(
        "Количество обычных документов в одном полном прохождении. " +
        "0 = использовать старый подсчёт из списка Documents."
    )]
    [SerializeField]
    [Min(0)]
    private int campaignPlayableDocumentCount;

    [Header("Маршруты")]

    [SerializeField]
    private List<StoryNode> nodes =
        new List<StoryNode>();

    public int CampaignPlayableDocumentCount =>
        campaignPlayableDocumentCount;

    public StoryNode FindNode(
        string documentId)
    {
        if (string.IsNullOrWhiteSpace(
                documentId) ||
            nodes == null)
        {
            return null;
        }

        foreach (StoryNode node in nodes)
        {
            if (node != null &&
                node.CurrentDocumentId ==
                documentId)
            {
                return node;
            }
        }

        return null;
    }
}

public readonly struct StoryRouteContext
{
    public string CurrentDocumentId { get; }

    public bool DocumentPassed { get; }

    public int DocumentScore { get; }

    public int TotalScore { get; }

    public int CompletedPlayableDocuments { get; }

    public int MaximumDocumentScore { get; }

    public float CampaignPercent
    {
        get
        {
            int maximumScore =
                CompletedPlayableDocuments *
                MaximumDocumentScore;

            if (maximumScore <= 0)
            {
                return 0f;
            }

            return Mathf.Clamp(
                TotalScore * 100f /
                maximumScore,
                0f,
                100f
            );
        }
    }

    public StoryRouteContext(
        string currentDocumentId,
        bool documentPassed,
        int documentScore,
        int totalScore,
        int completedPlayableDocuments,
        int maximumDocumentScore)
    {
        CurrentDocumentId =
            currentDocumentId;

        DocumentPassed =
            documentPassed;

        DocumentScore =
            documentScore;

        TotalScore =
            totalScore;

        CompletedPlayableDocuments =
            Mathf.Max(
                0,
                completedPlayableDocuments
            );

        MaximumDocumentScore =
            Mathf.Max(
                1,
                maximumDocumentScore
            );
    }
}
