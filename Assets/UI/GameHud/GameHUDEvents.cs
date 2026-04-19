using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Collections;

using UnityEngine;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public sealed class GameHUDEvents : UIMenuController
{
    [Header("Audio")]
    [SerializeField] private AudioClip openClip;
    [SerializeField] private AudioClip closeClip;
    [SerializeField] private AudioClip hoverClip;
    private SoundHandler sh;

    [Header("Sprites")]
    [SerializeField] private Texture2D closedImage;
    [SerializeField] private Texture2D openImage;

    private UIDocument selfDocument;
    [SerializeField] private UIDocument settingsDocument;

    public VisualElement hudContainer;
    private VisualElement notebookContainer;
    private VisualElement notebook;
    [SerializeField] private VisualTreeAsset notebookUIDocument;
    private TemplateContainer notebookContents;

    private Button notebookButton;
    private Button settingsButton;

    private Button backButton;
    private Button dictionaryButton;
    private Button journalButton;
    private Label pageCount;

    private Button backPage;
    private Button forwardPage;

    private readonly List<VisualElement> Slots = new();
    private int pageNumber = 0;

    private TextField journalPage;

    private bool journalOrDict = true; // true: dictionary, false: journal, will open by default

    private const string RootImportDir = "Assets/Scripts/Encoding";
    private const string LigatureSubDir = RootImportDir + "/Loader/Ligature Sub Table.asset";

    [SerializeField] private LigatureSub ligatureSub;
    [SerializeField] private StandardSignTable standardSignTable;
    [SerializeField] private PhoneticProcessor processor;

    public override IEnumerator Open()
    {
        yield return EnterDictionary(notebook, openImage);
        PlayerController.Instance.context |= PlayerContext.InDictionary;
    }

    public override IEnumerator Close() 
    {
        PlayerController.Instance.context &= ~PlayerContext.InDictionary;
        yield return ExitDictionary(notebook, closedImage);
    }

    void Awake()
    {
        selfDocument = GetComponent<UIDocument>();
        sh           = GetComponent<SoundHandler>();

        hudContainer = selfDocument.rootVisualElement.Q("ScreenContainer");

        notebookContainer = selfDocument.rootVisualElement.Q("NotebookContainer");
        notebook = selfDocument.rootVisualElement.Q("Notebook");

        // Add events to all buttons
        notebookButton = selfDocument.rootVisualElement.Q<Button>("NotebookButton");
        notebookButton.RegisterCallback<ClickEvent>((e) => MenuToggler.Instance.UseMenu(this));
        notebookButton.RegisterCallback<MouseEnterEvent>(OnButtonHover);
       
        var settingsComponent = settingsDocument.gameObject.GetComponent<SettingsMenuEvents>();
        settingsButton = selfDocument.rootVisualElement.Q<Button>("SettingsButton");
        settingsButton.RegisterCallback<ClickEvent>((e) => MenuToggler.Instance.UseMenu(settingsComponent));
        settingsButton.RegisterCallback<MouseEnterEvent>(OnButtonHover);

        notebookContainer.visible = false;
        notebookContents = notebookUIDocument.Instantiate();
        notebookContents.style.flexGrow = 1;

        for (int i = 0; i < 5; i++)
        {
            int slotNumber = i + 1;
            var item = notebookContents.Q("DictionarySlot" + slotNumber);

            if (item == null)
            {
                Debug.LogError("Could not find Slot" + slotNumber);
                continue;
            }

            var notes = item.Q<TextField>("Notes" + slotNumber);
            notes.RegisterValueChangedCallback(evt => {
                NotesUpdate(evt.newValue, slotNumber - 1);
            });
            notes.isDelayed = true;

            Slots.Add(item);
        }

        journalPage = notebookContents.Q<TextField>("JournalPage");
        journalPage.RegisterValueChangedCallback(evt =>
        {
            PageUpdate(evt.newValue);
        });

        backPage = notebookContents.Q<Button>("BackPage");
        backPage.RegisterCallback<ClickEvent>((e) => LoadPage(pageNumber - 1));

        forwardPage = notebookContents.Q<Button>("ForwardPage");
        forwardPage.RegisterCallback<ClickEvent>((e) => LoadPage(pageNumber + 1));

        // Back button for dictionary
        backButton = notebookContents.Q<Button>("BackButton");
        backButton.RegisterCallback<ClickEvent>((e) => MenuToggler.Instance.ClearAllMenus());

        // Toggle to Dictionary
        dictionaryButton = notebookContents.Q<Button>("DictionaryButton");
        dictionaryButton.RegisterCallback<ClickEvent>((e) => ToggleJournalDictionary(true));

        // Toggle to Journal
        journalButton = notebookContents.Q<Button>("JournalButton");
        journalButton.RegisterCallback<ClickEvent>((e) => ToggleJournalDictionary(false));

        pageCount = notebookContents.Q<Label>("PageCount");

        notebookContents.visible = false;
        foreach (VisualElement slot in Slots) 
        {
            slot.visible = false;
        }

        ToggleJournalDictionary(journalOrDict);
    }

    //public void OpenSettings(ClickEvent e) => OpenSettings();

    private IEnumerator EnterDictionary(VisualElement notebook, Texture2D closedImage)
    {
        notebookContainer.style.backgroundColor = new StyleColor(new Color(0.08f, 0.08f, 0.08f, 0.8f));
        // Disable box collider to prevent interactions & freeze position to prevent movement
        //DisableWorldActions();
        
        // Enable Dictionary elements, Disable HUD
        notebookContainer.visible = true;
        hudContainer.visible        = false;

        // Enter the screen
        sh.PlaySoundUI(openClip);
        yield return Translate(notebook, -1500.0f, -350.0f, 1.0f);
        // Play transition animations
        yield return Fade(notebook, 1.0f, 0.0f, 0.45f);
        notebook.style.backgroundImage = new StyleBackground(openImage);
        notebookContainer.Remove(notebook);
        notebookContainer.Add(notebookContents);
        notebookContents.visible = true;
        LoadPage(pageNumber);
        yield return Fade(notebookContents, 0.0f, 1.0f, 1.5f);
    }

    private IEnumerator ExitDictionary(VisualElement notebook, Texture2D closedImage)
    {
        // Play transition animations
        sh.PlaySoundUI(closeClip);
        yield return Fade(notebookContents, 1.0f, 0.0f, 0.45f);
        notebookContainer.Remove(notebookContents);
        notebookContainer.Add(notebook);
        foreach (VisualElement slot in Slots)
        {
            slot.visible = false;
        }
        notebook.style.backgroundImage = new StyleBackground(closedImage);
        yield return Fade(notebook, 0.0f, 1.0f, 1.5f);

        // Leave the screen
        yield return Translate(notebook, -350f, -1500f, 1f);

        // Disable Dictionary elements, Re-enable HUD
        notebookContainer.style.backgroundColor = new StyleColor(new Color(0.8f, 0.8f, 0.8f, 0f));
        notebookContainer.visible = false;
        hudContainer.visible        = true;
        
        // Restore movement, Re-enable box collider, listen for menu keys
        //EnableWorldActions();
    }

    // Fade the alpha style property of a visual element
    private IEnumerator Fade(VisualElement dict, float start, float end, float duration)
    {
        float timeElapsed = 0.0f;
        while (duration > timeElapsed)
        {
            float t      = timeElapsed / duration;
            float eased  = Mathf.SmoothStep(start, end, t);
            timeElapsed += Time.deltaTime;

            dict.style.opacity = eased;
            yield return null;
        }
        dict.style.opacity = end;
    }

    // Translate the left style property of a visual element and button
    private IEnumerator Translate(VisualElement dict, float start, float end, float duration)
    {
        float buttonStart;
        float buttonEnd;

        if (start == -1500)
        {
            buttonStart = -start;
            buttonEnd   = 0.0f;
        }
        else
        {
            buttonStart = 0.0f;
            buttonEnd   = -end;
        }

        float timeElapsed = 0.0f;
        while (duration > timeElapsed)
        {
            float t = timeElapsed / duration;

            float easedDictionary = Mathf.SmoothStep(start, end, t);
            float easedButton     = Mathf.SmoothStep(buttonStart, buttonEnd, t);

            dict.style.left = easedDictionary;
            timeElapsed    += Time.deltaTime;

            yield return null;
        }

        dict.style.left = end;
    }

    private void ToggleJournalDictionary(bool mode) {
        journalOrDict = mode;

        if (journalOrDict) // Toggle to Dictionary
        {
            journalPage.style.display = DisplayStyle.None;
            journalPage.parent.style.display = DisplayStyle.None;

            foreach (VisualElement slot in Slots)
            {
                slot.style.display = DisplayStyle.Flex;
            }
        }
        else // Toggle to Journal
        {
            foreach (VisualElement slot in Slots)
            {
                slot.style.display = DisplayStyle.None;
            }
            journalPage.style.display = DisplayStyle.Flex;
            journalPage.parent.style.display = DisplayStyle.Flex;
        }

        LoadPage(0);
    }

    private void LoadPage(int pageNum)
    {
        if (pageNum < 0) 
        {
            return;
        }

        if (journalOrDict)
        {
            LoadDictPage(pageNum);
        }
        else 
        {
            LoadJournalPage(pageNum);
        }
    }

    private void LoadDictPage(int pageNum)
    {
        PlayerController player = PlayerController.Instance;
        if (pageNum > (int)player.dictionary.dictionaryList.Length / Slots.Count)
        {
            return;
        }

        pageCount.text = (pageNum + 1) + "/" + (((int)player.dictionary.dictionaryList.Length / Slots.Count) + 1);

        int index = pageNum * Slots.Count;

        pageNumber = pageNum;

        foreach (VisualElement slot in Slots)
        {
            if (index >= player.dictionary.dictionaryList.Length)
            {
                slot.visible = false;
                continue;
            }

            slot.visible = true;

            var word = slot.Q<Label>("Word" + ((index % Slots.Count) + 1));
            var notes = slot.Q<TextField>("Notes" + ((index % Slots.Count) + 1));

            processor = PhoneticProcessor.Create(standardSignTable.entries, ligatureSub.entries, Allocator.Temp);
            word.text = processor.TranslateManaged(player.dictionary.dictionaryList[index].Word);

            if (player.dictionary.dictionaryList[index].Notes == "")
            {
                notes.value = "";
                notes.textEdition.placeholder = "Notes...";
            }
            else
            {
                notes.value = player.dictionary.dictionaryList[index].Notes;
            }
            index++;
        }
    }

    private void LoadJournalPage(int pageNum)
    {
        PlayerController player = PlayerController.Instance;
        if (pageNum > player.dictionary.journalPages.Length) 
        {
            return;
        }

        pageCount.text = (pageNum + 1) + "/" + (player.playerJournalSize);

        pageNumber = pageNum;

        journalPage.value = player.dictionary.journalPages[pageNum].Content;
    }

    private void NotesUpdate(string newValue, int index)
    {
        var notes = Slots[index].Q<TextField>("Notes" + (index + 1));
        notes.value = newValue;

        PlayerController player = PlayerController.Instance;
        player.dictionary.dictionaryList[(pageNumber * Slots.Count) + index].Notes = newValue;
    }

    private void PageUpdate(string newValue)
    {
        journalPage.value = newValue;

        PlayerController player = PlayerController.Instance;
        player.dictionary.journalPages[pageNumber].Content = newValue;
    }

    private void OnButtonHover(MouseEnterEvent e)
    {
        sh.PlaySoundUI(hoverClip);
    }
}