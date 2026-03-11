using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.EventSystems.EventTrigger;

[DisallowMultipleComponent, RequireComponent(typeof(UIDocument))]
public sealed class NpcDialogue : Interactable
{
    private UIDocument document;
    [SerializeField] private UIDocument hudDocument;
    private bool inDialogue;

    [SerializeField] private string npcName;
    [SerializeField] private Texture2D npcImage;
    private int index = 0;

    [Tooltip("Single lines shouldn't exceed 150 characters/20 words.")]
    [SerializeField] private DialogueEntry[] entries;

    private Label dialogueLabel;
    private float textSpeed;
    private VisualElement nextLinePrompt;
    private VisualElement wordTooltip;

    private IVisualElementScheduledItem bounceSchedule;
    private float bounceHeight = 30.0f;
    private float bounceSpeed = 30.0f;
    private float bounceStartTime;

    public override PlayerContext TargetContext { get => PlayerContext.Interacting | PlayerContext.Dialogue; }

    public bool TryCheckInput(string content)
    {
        if (inDialogue && entries[index].hasResponse)
        {
            if (entries[index].responseData.expectedInput == content) // For when the content is equal to the expected
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
        document.rootVisualElement.style.display = DisplayStyle.None;
        nextLinePrompt.visible = false;
        inDialogue = false;

        // Set Tooltip
        wordTooltip = document.rootVisualElement.Q("WordTooltip");
    }

    protected override IEnumerator InteractLogic(PlayerController player)
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
            document.rootVisualElement.style.display = DisplayStyle.Flex;

            hudDocument.rootVisualElement.style.visibility = Visibility.Hidden;
            hudDocument.rootVisualElement.style.display = DisplayStyle.None;
            worldPromptIcon.enabled = false;

            // Prevent Movement
            player.CanMove = false;
            inDialogue = true;
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

    private IEnumerator TypeLine()
    {
        var textContainer = document.rootVisualElement.Q("TextContainer");
        textContainer.Clear();

        string currentLine = entries[index].line;
        int i = 0;

        Label wordLabel = new Label();
        wordLabel.AddToClassList("dialogue-text");
        wordLabel.style.marginRight = 12;
        textContainer.Add(wordLabel);

        while (i < currentLine.Length)
        {
            if (currentLine[i] == ' ')
            {
                string name = RemovePunctuationLinq(wordLabel.text.ToLower().Trim());
                wordLabel.name = name;

                StyleColor orig = wordLabel.style.color;

                wordLabel.RegisterCallback<PointerEnterEvent>(evt =>
                {
                    Label target = (Label)evt.target;
                    target.style.color = new StyleColor(Color.red);
                    ShowTooltip(name, evt.position);
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
                wordLabel.style.marginRight = 12;
                textContainer.Add(wordLabel);
            }
            else
            {
                wordLabel.text += currentLine[i];
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
            ShowTooltip(word, evt.position);
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

    private IEnumerator NextLine()
    {
        if (index < entries.Length - 1)
        {
            index++;
            var textContainer = document.rootVisualElement.Q("TextContainer");
            textContainer.Clear();
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
            document.rootVisualElement.style.visibility = Visibility.Hidden;
            document.rootVisualElement.style.display = DisplayStyle.None;

            hudDocument.rootVisualElement.style.visibility = Visibility.Visible;
            hudDocument.rootVisualElement.style.display = DisplayStyle.Flex;

            worldPromptIcon.enabled = true;

            InputController.Instance.CloseKeyboard();
            PlayerController.Instance.context &= ~PlayerContext.PlayerInput;
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

    private void ShowTooltip(string name, Vector2 mousePosition)
    {
        Label word = document.rootVisualElement.Q<Label>("Word");
        Label notes = document.rootVisualElement.Q<Label>("Notes");

        word.text = name;
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
            if (entry.Word == word) 
            {
                return entry.Notes;
            }
        }

        return "No Notes Available For This Word";
    }
}