using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Unity.Collections;

using UnityEngine;
using UnityEngine.UIElements;

using static DialogueBox;

[DisallowMultipleComponent, RequireComponent(typeof(UIDocument))]
public class NpcDialogue : Interactable
{
    protected UIDocument document;
    [SerializeField] protected UIDocument hudDocument;
    private bool inDialogue;

    [SerializeField] protected CharacterData characterData;

    [SerializeField] protected VisualTreeAsset dialogueTreeAsset;
    protected DialogueBox dialogueBox;

    protected int index = 0;

    [Tooltip("Single lines shouldn't exceed 150 characters/20 words.")]
    [SerializeField] protected DialogueEntry[] entries;

    private VisualElement notebookContents;
    private Button notebookButton;

    private Button backButton;
    private Button dictionaryButton;
    private Button journalButton;
    private Label pageCount;

    private Button backPage;
    private Button forwardPage;

    private List<VisualElement> Slots = new List<VisualElement>();
    private int pageNumber = 0;

    private TextField journalPage;

    private bool journalOrDict = true; // true: dictionary, false: journal, will open by default

    public override PlayerContext TargetContext { get => PlayerContext.Interacting | PlayerContext.Dialogue; }

    public bool TryCheckInput(string content)
    {
        if (inDialogue && entries[index].hasResponse)
        {
            if (entries[index].responseData.line == content) // For when the content is equal to the expected
            {
                return true;
            }
            else // Otherwise, when it is invalid. This is temporary, incomplete logic handling.
            {
                index--;
            }
        }
        return false;
    }

    protected override void Start()
    {
        base.Start();
        document = GetComponent<UIDocument>();
        Debug.Assert(dialogueTreeAsset != null);

        dialogueBox = new DialogueBox(dialogueTreeAsset, characterData);
        document.rootVisualElement.Add(dialogueBox);

        Label word  = document.rootVisualElement.Q<Label>("Word");
        Label notes = document.rootVisualElement.Q<Label>("Notes");

        dialogueBox.SetDictionaryData(new DictionaryData() { dictWords = word, dictNotes = notes });

        document.rootVisualElement.style.visibility = Visibility.Hidden;
        document.rootVisualElement.style.display    = DisplayStyle.None;
        inDialogue = false;

        // Setup Notebook
        notebookButton = document.rootVisualElement.Q<Button>("NotebookButton");
        notebookButton.RegisterCallback<ClickEvent>((e) => ToggleNotebook());

        notebookContents = document.rootVisualElement.Q<VisualElement>("Notebook");

        for (int i = 0; i < 5; i++)
        {
            int slotNumber = i + 1;
            var item = notebookContents.Q("DictionarySlot" + slotNumber);

            if (item == null)
            {
                Debug.LogError("Could not find Slot" + slotNumber);
                continue;
            }

            var notesRow = item.Q<TextField>("Notes" + slotNumber);
            notes.RegisterValueChangedCallback(evt => {
                NotesUpdate(evt.newValue, slotNumber - 1);
            });
            notesRow.isDelayed = true;

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
        backButton.RegisterCallback<ClickEvent>((e) => ToggleNotebook());

        // Toggle to Dictionary
        dictionaryButton = notebookContents.Q<Button>("DictionaryButton");
        dictionaryButton.RegisterCallback<ClickEvent>((e) => ToggleJournalDictionary(true));

        // Toggle to Journal
        journalButton = notebookContents.Q<Button>("JournalButton");
        journalButton.RegisterCallback<ClickEvent>((e) => ToggleJournalDictionary(false));

        pageCount = notebookContents.Q<Label>("PageCount");

        ToggleJournalDictionary(journalOrDict);
    }

    protected override sealed IEnumerator InteractLogic(PlayerController player)
    {
        if (inDialogue)
        {
            // Interact key pressed when dialogue line is finished -> to next line/end dialogue
            yield return NextLine();
        }
        else
        {
            document.rootVisualElement.style.visibility = Visibility.Visible;
            document.rootVisualElement.style.display    = DisplayStyle.Flex;

            hudDocument.rootVisualElement.style.visibility = Visibility.Hidden;
            hudDocument.rootVisualElement.style.display    = DisplayStyle.None;
            worldPromptIcon.enabled = false;

            // Prevent Movement
            player.CanMove = false;
            inDialogue     = true;
            yield return NextLine();
        }
    }

    /// <summary>
    /// Works currently by completely rendering text rather than suspending any execution/co-routines.
    /// </summary>
    public void Advance()
    {
        //dialogueLabel.text = entries[index].line;
    }

    protected virtual IEnumerator OnLast()
    {
        yield return null;
    }

    private IEnumerator NextLine()
    {
        if (index < entries.Length)
        {
            dialogueBox.ClearDisplay();
            yield return dialogueBox.Display(entries[index]); // NOTE
            if (entries[index].hasResponse)
            {
                PlayerController.Instance.context |= PlayerContext.PlayerInput;
                InputController.Instance.OpenKeyboard();

                yield return new WaitUntil(() => (PlayerController.Instance.context & PlayerContext.PlayerInput) == 0);
            }
            else
            {
                InputController.Instance.CloseKeyboard();
                PlayerController.Instance.context &= ~PlayerContext.PlayerInput;
            }
            index++;
        }
        else
        {
            // Restore Movement
            PlayerController.Instance.CanMove = true;

            // Restore in-game UI
            document.rootVisualElement.Q<VisualElement>("NotebookBox").style.display = DisplayStyle.None;

            document.rootVisualElement.style.visibility = Visibility.Hidden;
            document.rootVisualElement.style.display    = DisplayStyle.None;

            hudDocument.rootVisualElement.style.visibility = Visibility.Visible;
            hudDocument.rootVisualElement.style.display    = DisplayStyle.Flex;

            worldPromptIcon.enabled = true;

            InputController.Instance.CloseKeyboard();
            PlayerController.Instance.context &= ~PlayerContext.PlayerInput;
            yield return OnLast();
        }
    }

    private void ToggleNotebook()
    {
        VisualElement notebookBox = document.rootVisualElement.Q<VisualElement>("NotebookBox");
        if (notebookBox.style.display == DisplayStyle.None)
        {
            notebookBox.style.display = DisplayStyle.Flex;
        }
        else
        {
            notebookBox.style.display = DisplayStyle.None;
        }
    }

    private void ToggleJournalDictionary(bool mode)
    {
        journalOrDict = mode;

        if (journalOrDict) // Toggle to Dictionary
        {
            journalPage.style.display        = DisplayStyle.None;
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
            journalPage.style.display        = DisplayStyle.Flex;
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

            var word  = slot.Q<Label>("Word" + ((index % Slots.Count) + 1));
            var notes = slot.Q<TextField>("Notes" + ((index % Slots.Count) + 1));

            word.text = LanguageTable.PhoneticProcessor.TranslateManaged(player.dictionary.dictionaryList[index].Word);

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
}