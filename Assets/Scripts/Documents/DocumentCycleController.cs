using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class DocumentCycleController
{
    private sealed class RuntimeGroup
    {
        public readonly List<DocumentWord> words =
            new List<DocumentWord>();

        public float intervalMin;
        public float intervalMax;
        public int currentValueIndex = -1;
        public float timeUntilNextChange;
    }

    private readonly List<RuntimeGroup> groups =
        new List<RuntimeGroup>();

    public void Initialize(
        IReadOnlyList<DocumentWord> words)
    {
        groups.Clear();

        if (words == null)
        {
            return;
        }

        Dictionary<int, RuntimeGroup> byId =
            new Dictionary<int, RuntimeGroup>();

        foreach (DocumentWord word in words)
        {
            if (word == null || !word.HasCycle)
            {
                continue;
            }

            if (!byId.TryGetValue(
                    word.cycleGroupId,
                    out RuntimeGroup group))
            {
                group = new RuntimeGroup
                {
                    intervalMin = Mathf.Max(
                        0.05f,
                        word.cycleIntervalMin
                    ),
                    intervalMax = Mathf.Max(
                        0.05f,
                        word.cycleIntervalMax
                    )
                };

                if (group.intervalMax <
                    group.intervalMin)
                {
                    float temp = group.intervalMin;
                    group.intervalMin = group.intervalMax;
                    group.intervalMax = temp;
                }

                byId.Add(word.cycleGroupId, group);
                groups.Add(group);
            }

            group.words.Add(word);
        }

        foreach (RuntimeGroup group in groups)
        {
            group.currentValueIndex =
                FindInitialValueIndex(group);

            if (group.currentValueIndex >= 0)
            {
                ApplyValueIndex(
                    group,
                    group.currentValueIndex
                );
            }

            group.timeUntilNextChange =
                GetNextInterval(group);
        }
    }

    public bool Tick(float deltaTime)
    {
        if (groups.Count == 0 ||
            deltaTime <= 0f)
        {
            return false;
        }

        bool changed = false;

        foreach (RuntimeGroup group in groups)
        {
            group.timeUntilNextChange -=
                deltaTime;

            if (group.timeUntilNextChange > 0f)
            {
                continue;
            }

            int optionCount =
                GetOptionCount(group);

            if (optionCount < 2)
            {
                continue;
            }

            int nextIndex =
                GetNextValueIndex(
                    optionCount,
                    group.currentValueIndex
                );

            group.currentValueIndex = nextIndex;
            ApplyValueIndex(group, nextIndex);

            group.timeUntilNextChange =
                GetNextInterval(group);

            changed = true;
        }

        return changed;
    }

    public void Clear()
    {
        groups.Clear();
    }

    private int FindInitialValueIndex(
        RuntimeGroup group)
    {
        int optionCount =
            GetOptionCount(group);

        for (int optionIndex = 0;
             optionIndex < optionCount;
             optionIndex++)
        {
            bool matchesOriginal = true;

            foreach (DocumentWord word in group.words)
            {
                if (word.cycleValues == null ||
                    optionIndex >= word.cycleValues.Count ||
                    !string.Equals(
                        word.originalText,
                        word.cycleValues[optionIndex],
                        StringComparison.Ordinal))
                {
                    matchesOriginal = false;
                    break;
                }
            }

            if (matchesOriginal)
            {
                return optionIndex;
            }
        }

        return -1;
    }

    private int GetOptionCount(
        RuntimeGroup group)
    {
        if (group == null ||
            group.words.Count == 0 ||
            group.words[0].cycleValues == null)
        {
            return 0;
        }

        return group.words[0].cycleValues.Count;
    }

    private int GetNextValueIndex(
        int optionCount,
        int currentIndex)
    {
        if (currentIndex < 0 ||
            currentIndex >= optionCount)
        {
            return UnityEngine.Random.Range(
                0,
                optionCount
            );
        }

        int next =
            UnityEngine.Random.Range(
                0,
                optionCount - 1
            );

        if (next >= currentIndex)
        {
            next++;
        }

        return next;
    }

    private void ApplyValueIndex(
        RuntimeGroup group,
        int valueIndex)
    {
        foreach (DocumentWord word in group.words)
        {
            word.cycleValueIndex =
                valueIndex;
        }
    }

    private float GetNextInterval(
        RuntimeGroup group)
    {
        if (Mathf.Approximately(
                group.intervalMin,
                group.intervalMax))
        {
            return group.intervalMin;
        }

        return UnityEngine.Random.Range(
            group.intervalMin,
            group.intervalMax
        );
    }
}
