using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Unity.Collections;

using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.EventSystems.EventTrigger;

[DisallowMultipleComponent, RequireComponent(typeof(UIDocument))]
public class NpcDialogue : Interactable
{
    protected UIDocument document;
    [SerializeField] protected UIDocument hudDocument;
    private bool inDialogue;

    [SerializeField] protected string    npcName;
    [SerializeField] protected Texture2D npcImage;
    protected int index = 0;

    [Tooltip("Single lines shouldn't exceed 150 characters/20 words.")]
    [SerializeField] protected DialogueEntry[] entries;

    protected Label dialogueLabel;
    protected float textSpeed;
    protected VisualElement nextLinePrompt;
    protected VisualElement wordTooltip;

    protected IVisualElementScheduledItem bounceSchedule;
    protected float bounceHeight = 30.0f;
    protected float bounceSpeed  = 30.0f;
    protected float bounceStartTime;

    public Font conlangFont;
    public Font englishFont;

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

        // Set name and portrait
        document.rootVisualElement.Q<Label>("NpcName").text = npcName;
        document.rootVisualElement.Q("NpcImage").style.backgroundImage = npcImage;

        //// Make sure text box begins empty
        //dialogueLabel      = document.rootVisualElement.Q<Label>("DialogueText");
        //dialogueLabel.text = "";

        textSpeed = 0.02f;
        nextLinePrompt = document.rootVisualElement.Q("NextLinePrompt");
        document.rootVisualElement.style.visibility = Visibility.Hidden;
        document.rootVisualElement.style.display    = DisplayStyle.None;
        nextLinePrompt.visible = false;
        inDialogue = false;

        // Set Tooltip
        wordTooltip = document.rootVisualElement.Q("WordTooltip");

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
            var textContainer = document.rootVisualElement.Q("TextContainer");
            if (textContainer.childCount > 0)
            {
                StopBounce();
                yield return NextLine();
            }
            else
            {
                // dialogueLabel.text = entries[index].line;
                StartBounce();
            }
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
            yield return TypeLine();
        }
    }

    /// <summary>
    /// Works currently by completely rendering text rather than suspending any execution/co-routines.
    /// </summary>
    public void Advance()
    {
        //dialogueLabel.text = entries[index].line;
    }

    // Creates new text labels for each word to allow mouse events to be bound to each word independently
    private IEnumerator TypeLine()
    {
        var textContainer = document.rootVisualElement.Q("TextContainer");
        textContainer.Clear();

        string currentLine = entries[index].line;
        int i = 0;

        Font fontToUse = conlangFont;

        Label wordLabel = new();
        wordLabel.AddToClassList("dialogue-text");
        wordLabel.style.marginRight = 16;
        textContainer.Add(wordLabel);
        wordLabel.style.unityFont = fontToUse;

        while (i < currentLine.Length)
        {
            if (currentLine[i] == ' ')
            {
                if (wordLabel.text.Length > 0)
                {
                    string name = RemovePunctuationLinq(wordLabel.text.ToLower().Trim());
                    wordLabel.name = name;

                    StyleColor orig = wordLabel.style.color;

                    wordLabel.RegisterCallback<PointerEnterEvent>(evt =>
                    {
                        Label target = (Label)evt.target;
                        target.style.color = new StyleColor(Color.red);
                        ShowTooltip(name, evt.position, target.style.unityFontDefinition);
                    });

                    wordLabel.RegisterCallback<PointerMoveEvent>(evt =>
                    {
                        MoveTooltip(evt.position);
                    });

                    wordLabel.RegisterCallback<PointerLeaveEvent>(evt =>
                    {
                        Label target = (Label)evt.target;
                        target.style.color = orig;
                        HideTooltip();
                    });

                    wordLabel.RegisterCallback<DetachFromPanelEvent>(evt =>
                    {
                        HideTooltip();
                    });

                    wordLabel = new Label();
                    wordLabel.AddToClassList("dialogue-text");
                    wordLabel.style.marginRight = 16;
                    textContainer.Add(wordLabel);
                    wordLabel.style.unityFontDefinition = new StyleFontDefinition(fontToUse);
                }
            }
            else if (currentLine[i] == '<')
            {
                if (currentLine.Substring(i, 3) == "<e>")
                {
                    wordLabel.style.unityFontDefinition = new StyleFontDefinition(englishFont);
                    fontToUse = englishFont;
                    i += 2;
                }
                else if (currentLine.Substring(i, 3) == "<c>")
                {
                    wordLabel.style.unityFontDefinition = new StyleFontDefinition(conlangFont);
                    fontToUse = conlangFont;
                    i += 2;
                }
                else 
                {
                    if (fontToUse == conlangFont)
                    {
                        wordLabel.text = LanguageTable.PhoneticProcessor.Translate(wordLabel.text + currentLine[i]);
                    }
                    else
                    {
                        wordLabel.text += currentLine[i];
                    }
                    yield return new WaitForSeconds(textSpeed);
                }
            }
            else
            {
                if (fontToUse == conlangFont)
                {
                    wordLabel.text = LanguageTable.PhoneticProcessor.Translate(wordLabel.text + currentLine[i]);
                }
                else
                {
                    wordLabel.text += currentLine[i];
                }
                yield return new WaitForSeconds(textSpeed);
            }
            i++;
        }

        string word = RemovePunctuationLinq(wordLabel.text.ToLower().Trim());
        wordLabel.name = word;

        StyleColor originalColor = wordLabel.style.color;

        wordLabel.RegisterCallback<PointerEnterEvent>(evt =>
        {
            Label target = (Label)evt.target;
            target.style.color = new StyleColor(Color.red);
            ShowTooltip(word, evt.position, target.style.unityFontDefinition);
        });

        wordLabel.RegisterCallback<PointerMoveEvent>(evt =>
        {
            MoveTooltip(evt.position);
        });

        wordLabel.RegisterCallback<PointerLeaveEvent>(evt =>
        {
            Label target = (Label)evt.target;
            target.style.color = originalColor;
            HideTooltip();
        });

        StartBounce();
    }

    private string RemovePunctuationLinq(string input)
    {
        // Filters the string, keeping only characters that are not punctuation
        var result = new string(input.Where(c => !Char.IsPunctuation(c)).ToArray());
        return result;
    }

    protected virtual IEnumerator OnLast()
    {
        yield return null;
    }

    private IEnumerator NextLine()
    {
        if (index < entries.Length - 1)
        {
            var textContainer = document.rootVisualElement.Q("TextContainer");
            textContainer.Clear();
            index++;
            yield return TypeLine();

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
        }
        else
        {
            StopBounce();

            index = 0;
            var textContainer = document.rootVisualElement.Q("TextContainer");
            textContainer.Clear();
            inDialogue = false;
            nextLinePrompt.visible = false;

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

    // Animates the prompt for informing the player that the current line is finished
    private void StartBounce()
    {
        nextLinePrompt.visible = true;
        bounceStartTime = Time.time;

        bounceSchedule = nextLinePrompt.schedule.Execute(() =>
        {
            // Animate up and down movement
            float t = Time.time - bounceStartTime;

            float yOffset = Mathf.PingPong(t * bounceSpeed, bounceHeight);
            nextLinePrompt.style.translate = new Translate(0.0f, yOffset);

            // Animate size increase-decrease
            float normalizedT = yOffset / bounceHeight;
            float scaleFactor = Mathf.Lerp(1.3f, 1.0f, normalizedT);
            // float scaleFactor = Mathf.Lerp(0.7f, 1.3f, normalizedT);
            nextLinePrompt.transform.scale = new Vector3(scaleFactor, scaleFactor, 1.0f);
        }).Every(16);
    }

    private void StopBounce()
    {
        nextLinePrompt.visible = false;
        bounceSchedule?.Pause();

        // Reset position and scale
        nextLinePrompt.style.translate = new Translate(0.0f, 0.0f);
        nextLinePrompt.transform.scale = Vector3.one;
    }

    private void ShowTooltip(string name, Vector2 mousePosition, StyleFontDefinition font)
    {
        Label word = document.rootVisualElement.Q<Label>("Word");
        Label notes = document.rootVisualElement.Q<Label>("Notes");

        word.text = name;
        word.style.unityFontDefinition = font;

        notes.text = GetPlayerNotes(name);

        MoveTooltip(mousePosition);
        wordTooltip.style.display = DisplayStyle.Flex;
    }

    private void MoveTooltip(Vector2 mousePosition)
    {
        float offsetX = 12f;
        float offsetY = 12f;

        wordTooltip.style.left = mousePosition.x + offsetX;
        wordTooltip.style.top = mousePosition.y + offsetY;
    }

    private void HideTooltip()
    {
        wordTooltip.style.display = DisplayStyle.None;
    }

    private string GetPlayerNotes(string word)
    {
        PlayerController player = PlayerController.Instance;
        Dictionary dictionary = player.dictionary;

        foreach (DictionaryEntry entry in dictionary.dictionaryList) 
        {
            if (LanguageTable.PhoneticProcessor.Translate(entry.Word) == word) 
            {
                if (entry.Notes == "")
                {
                    return "No Notes Available For This Word";
                }
                return entry.Notes;
            }
        }

        return "No Notes Available For This Word";
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

            word.text = LanguageTable.PhoneticProcessor.Translate(player.dictionary.dictionaryList[index].Word);

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