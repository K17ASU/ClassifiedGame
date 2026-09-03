using System.Collections.Generic;
using UnityEngine;

public readonly struct StoryRouteResult
{
    public string NextDocumentId { get; }

    public bool EndsCampaign =>
        string.IsNullOrWhiteSpace(
            NextDocumentId
        );

    public StoryRouteResult(
        string nextDocumentId)
    {
        NextDocumentId =
            nextDocumentId ?? string.Empty;
    }
}

public static class StoryRouter
{
    public static StoryRouteResult Resolve(
        StoryGraph storyGraph,
        StoryRouteContext context,
        string linearFallbackNextDocumentId)
    {
        if (storyGraph == null)
        {
            return new StoryRouteResult(
                linearFallbackNextDocumentId
            );
        }

        StoryNode node =
            storyGraph.FindNode(
                context.CurrentDocumentId
            );

        if (node == null)
        {
            return new StoryRouteResult(
                linearFallbackNextDocumentId
            );
        }

        IReadOnlyList<StoryBranch> branches =
            node.Branches;

        if (branches != null)
        {
            foreach (
                StoryBranch branch
                in branches)
            {
                if (branch == null ||
                    !branch.Matches(context))
                {
                    continue;
                }

                if (branch.EndsCampaign)
                {
                    return new StoryRouteResult(
                        string.Empty
                    );
                }

                if (!string.IsNullOrWhiteSpace(
                        branch.NextDocumentId))
                {
                    return new StoryRouteResult(
                        branch.NextDocumentId
                    );
                }

                Debug.LogWarning(
                    "StoryRouter: найден подходящий Branch без " +
                    "Next Document Id. Проверяю следующий Branch."
                );
            }
        }

        if (node.EndCampaignIfNoBranchMatches)
        {
            return new StoryRouteResult(
                string.Empty
            );
        }

        if (!string.IsNullOrWhiteSpace(
                node.DefaultNextDocumentId))
        {
            return new StoryRouteResult(
                node.DefaultNextDocumentId
            );
        }

        return new StoryRouteResult(
            linearFallbackNextDocumentId
        );
    }
}
