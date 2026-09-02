using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public sealed class CodexUIController : MonoBehaviour
{
    [Header("Data")]

    [SerializeField]
    private CodexManager codexManager;

    [Header("Main UI")]

    [SerializeField]
    private Button codexButton;

    [SerializeField]
    private GameObject newEntryMarker;

    [Header("Codex Panel")]

    [SerializeField]
    private GameObject codexPanel;

    [SerializeField]
    private TMP_Text titleText;

    [SerializeField]
    private TMP_Text descriptionText;

    [SerializeField]
    private TMP_Text pageText;

    [SerializeField]
    private Button previousButton;

    [SerializeField]
    private Button nextButton;

    [Header("Sounds")]

    [SerializeField]
    private AudioClip openSound;

    [SerializeField]
    private AudioClip closeSound;

    [SerializeField]
    private AudioClip pageTurnSound;

    [SerializeField]
    [Range(0f, 1f)]
    private float soundVolume = 1f;

    public bool IsOpen
    {
        get
        {
            return codexPanel != null &&
                   codexPanel.activeSelf;
        }
    }

    private int currentIndex;

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged +=
            OnSelectedLocaleChanged;

        if (codexManager != null)
        {
            codexManager.EntriesChanged +=
                OnEntriesChanged;
        }
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -=
            OnSelectedLocaleChanged;

        if (codexManager != null)
        {
            codexManager.EntriesChanged -=
                OnEntriesChanged;
        }
    }

    private void Start()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        codexPanel.SetActive(false);
        RefreshButtonState();
    }

    public void OpenCodex()
    {
        if (codexManager.UnlockedEntries.Count == 0)
        {
            return;
        }

        currentIndex = Mathf.Clamp(
            currentIndex,
            0,
            codexManager.UnlockedEntries.Count - 1
        );

        codexPanel.SetActive(true);
        RefreshPage();

        SfxPlayer.Play(
            openSound,
            soundVolume
        );
    }

    public void CloseCodex()
    {
        codexPanel.SetActive(false);
        RefreshButtonState();

        SfxPlayer.Play(
            closeSound,
            soundVolume
        );
    }

    public void ShowPreviousEntry()
    {
        int count =
            codexManager.UnlockedEntries.Count;

        if (count == 0)
        {
            return;
        }

        currentIndex--;

        if (currentIndex < 0)
        {
            currentIndex = count - 1;
        }

        RefreshPage();

        SfxPlayer.Play(
            pageTurnSound,
            soundVolume
        );
    }

    public void ShowNextEntry()
    {
        int count =
            codexManager.UnlockedEntries.Count;

        if (count == 0)
        {
            return;
        }

        currentIndex++;

        if (currentIndex >= count)
        {
            currentIndex = 0;
        }

        RefreshPage();

        SfxPlayer.Play(
            pageTurnSound,
            soundVolume
        );
    }

    private void RefreshPage()
    {
        int count =
            codexManager.UnlockedEntries.Count;

        if (count == 0)
        {
            titleText.text = string.Empty;
            descriptionText.text = string.Empty;
            pageText.text = "0 / 0";

            previousButton.interactable = false;
            nextButton.interactable = false;

            return;
        }

        currentIndex = Mathf.Clamp(
            currentIndex,
            0,
            count - 1
        );

        CodexEntry entry =
            codexManager.UnlockedEntries[currentIndex];

        titleText.text =
            Localize(entry.LocalizedTitle);

        descriptionText.text =
            Localize(entry.LocalizedDescription);

        pageText.text =
            $"{currentIndex + 1} / {count}";

        bool hasSeveralEntries =
            count > 1;

        previousButton.interactable =
            hasSeveralEntries;

        nextButton.interactable =
            hasSeveralEntries;

        codexManager.MarkAsRead(entry);
        RefreshButtonState();
    }

    private void RefreshButtonState()
    {
        bool hasEntries =
            codexManager != null &&
            codexManager.UnlockedEntries.Count > 0;

        bool codexIsOpen =
            codexPanel != null &&
            codexPanel.activeSelf;

        if (codexButton != null)
        {
            bool shouldShowButton =
                hasEntries && !codexIsOpen;

            codexButton.gameObject.SetActive(
                shouldShowButton
            );

            codexButton.interactable =
                shouldShowButton;
        }

        if (newEntryMarker != null)
        {
            newEntryMarker.SetActive(
                hasEntries &&
                !codexIsOpen &&
                codexManager.HasUnreadEntries
            );
        }
    }

    private void OnEntriesChanged()
    {
        RefreshButtonState();

        if (codexPanel.activeSelf)
        {
            RefreshPage();
        }
    }

    private void OnSelectedLocaleChanged(
        Locale locale
    )
    {
        if (codexPanel != null &&
            codexPanel.activeSelf)
        {
            RefreshPage();
        }
    }

    private string Localize(
        LocalizedString localizedString
    )
    {
        if (localizedString == null ||
            localizedString.IsEmpty)
        {
            return string.Empty;
        }

        return localizedString.GetLocalizedString();
    }

    private bool ValidateReferences()
    {
        bool valid = true;

        if (codexManager == null)
        {
            Debug.LogError(
                "CodexUIController: не назначен Codex Manager.",
                this
            );
            valid = false;
        }

        if (codexButton == null)
        {
            Debug.LogError(
                "CodexUIController: не назначен Codex Button.",
                this
            );
            valid = false;
        }

        if (codexPanel == null)
        {
            Debug.LogError(
                "CodexUIController: не назначен Codex Panel.",
                this
            );
            valid = false;
        }

        if (titleText == null ||
            descriptionText == null ||
            pageText == null)
        {
            Debug.LogError(
                "CodexUIController: не назначены текстовые поля.",
                this
            );
            valid = false;
        }

        if (previousButton == null ||
            nextButton == null)
        {
            Debug.LogError(
                "CodexUIController: не назначены кнопки навигации.",
                this
            );
            valid = false;
        }

        return valid;
    }
}
