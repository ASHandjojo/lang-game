using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

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

    private IVisualElementScheduledItem bounceSchedule;
    private float bounceHeight = 30.0f;
    private float bounceSpeed  = 30.0f;
    private float bounceStartTime;

    public override PlayerContext TargetContext { get => PlayerContext.Interacting | PlayerContext.Dialogue; }

    void Start()
    {
        document        = GetComponent<UIDocument>();
        worldPromptIcon = GetComponentsInChildren<SpriteRenderer>(true)[1];

        keybindIcon = Keybinds.Instance.getKeyImage(Keybinds.Instance.getIntersKey());
        worldPromptIcon.sprite = ConvertToSprite(keybindIcon);

        // Set name and portrait
        document.rootVisualElement.Q<Label>("NpcName").text = npcName;
        document.rootVisualElement.Q("NpcImage").style.backgroundImage = npcImage;

        // Make sure text box begins empty
        dialogueLabel      = document.rootVisualElement.Q<Label>("DialogueText");
        dialogueLabel.text = "";

        textSpeed      = 0.02f;
        nextLinePrompt = document.rootVisualElement.Q("NextLinePrompt");
        document.rootVisualElement.style.visibility = Visibility.Hidden;
        nextLinePrompt.visible = false;
        inDialogue             = false;
    }

    protected override IEnumerator InteractLogic(PlayerController player)
    {
        if (inDialogue)
        {
            // Interact key pressed when dialogue line is finished -> to next line/end dialogue
            if (dialogueLabel.text == entries[index].line)
            {
                StopBounce();
                yield return NextLine();
            }
            else
            {
                StopAllCoroutines();
                dialogueLabel.text = entries[index].line;
                StartBounce();
            }
        }
        else
        {
            document.rootVisualElement.style.visibility    = Visibility.Visible;
            hudDocument.rootVisualElement.style.visibility = Visibility.Hidden;
            worldPromptIcon.enabled = false;
            
            // Prevent Movement
            player.CanMove = false;
            inDialogue     = true;
            yield return TypeLine();
        }
    }

    private IEnumerator TypeLine()
    {
        foreach (char c in entries[index].line)
        {
            dialogueLabel.text += c;
            yield return new WaitForSeconds(textSpeed);
        }

        StartBounce();
    }

    private IEnumerator NextLine()
    {
        if(index < entries.Length - 1)
        {
            index++;
            dialogueLabel.text = "";
            yield return TypeLine();

            if (entries[index - 1].hasResponse)
            {
                //InputController.Instance.OpenKeyboard();
            }
        }
        else
        {
            StopBounce();

            index                  = 0;
            dialogueLabel.text     = "";
            inDialogue             = false;
            nextLinePrompt.visible = false;
            // Stop listening for clicks
            //Actions.OnClick -= Interact;

            // Restore Movement
            PlayerController.Instance.CanMove = true;

            // Restore in-game UI
            document.rootVisualElement.style.visibility    = Visibility.Hidden;
            hudDocument.rootVisualElement.style.visibility = Visibility.Visible;

            worldPromptIcon.enabled = true;
        }
    }

    // Animates the prompt for informing the player that the current line is finished
    private void StartBounce()
    {
        nextLinePrompt.visible = true;
        bounceStartTime        = Time.time;

        bounceSchedule = nextLinePrompt.schedule.Execute(() =>
        {
            // Animate up and down movement
            float t = Time.time - bounceStartTime;

            float yOffset = Mathf.PingPong(t * bounceSpeed,  bounceHeight);
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
}