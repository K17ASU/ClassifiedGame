using UnityEngine;

/// <summary>
/// Хранит данные одного игрового документа.
/// Это не компонент сцены, а отдельный asset-файл.
/// </summary>
[CreateAssetMenu(
    fileName = "NewDocument",
    menuName = "Classified Documents/Document",
    order = 1
)]
public class DocumentData : ScriptableObject
{
    [Header("Информация о документе")]

    [SerializeField]
    private string documentTitle = "Неизвестное дело";

    [SerializeField]
    private string documentNumber = "ДОКЛАД № 001";

    [Header("Содержимое документа")]

    [TextArea(15, 40)]
    [SerializeField]
    private string documentText =
        "Введите сюда текст документа.\n\n" +
        "Секретные фрагменты заключайте в [[двойные скобки]].";

    public string DocumentTitle => documentTitle;

    public string DocumentNumber => documentNumber;

    public string DocumentText => documentText;
}