using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(
    fileName = "NewCodexEntry",
    menuName = "Classified/Codex Entry"
)]
public sealed class CodexEntry : ScriptableObject
{
    [Header("Identity")]

    [SerializeField]
    private string entryId;

    [SerializeField]
    private int order;

    [Header("Localization")]

    [SerializeField]
    private LocalizedString localizedTitle;

    [SerializeField]
    private LocalizedString localizedDescription;

    public string EntryId => entryId;
    public int Order => order;

    public LocalizedString LocalizedTitle =>
        localizedTitle;

    public LocalizedString LocalizedDescription =>
        localizedDescription;
}
