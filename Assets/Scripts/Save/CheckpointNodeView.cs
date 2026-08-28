using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ссылки на элементы одной карточки checkpoint'а.
/// Компонент ставится на CheckpointButtonTemplate.
/// </summary>
public sealed class CheckpointNodeView : MonoBehaviour
{
    [SerializeField]
    private TMP_Text label;

    [SerializeField]
    private Button deleteButton;

    public TMP_Text Label => label;
    public Button DeleteButton => deleteButton;
}
