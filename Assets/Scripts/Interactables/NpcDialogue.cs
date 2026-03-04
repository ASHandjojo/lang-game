using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering.Universal.ShaderGraph;
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
    //private int index = 0; 

    [Tooltip("Single lines shouldn't exceed 150 characters/20 words.")]
    [SerializeField] private DialogueEntry[] entries;
    [SerializeField] private DialogueTree npcTree;
    
    private bool alreadyIncrDiag = false;

    private Label dialogueLabel;
    private float textSpeed;
    private VisualElement nextLinePrompt;

    private IVisualElementScheduledItem bounceSchedule;
    private float bounceHeight = 30.0f;
    private float bounceSpeed  = 30.0f;
    private float bounceStartTime;

    public override PlayerContext TargetContext { get => PlayerContext.Interacting | PlayerContext.Dialogue; }

    public bool TryCheckInput(string content)
    {
        
        if (inDialogue && npcTree.NeedPlayerInput()) // if we are in dialogue and we need a response from the player
        {
            DialogueEntry currDiag = (DialogueEntry)npcTree.GetCurrentEntry(); // This will get the current dialogue entry
            int errno = npcTree.DialogueForward(content); // This will increment the dialogue accordingly or return error
            alreadyIncrDiag = true; // make sure to set that we have already progressed the dialogue!
            if (errno < 0) 
            {
                Debug.Log("Error in Moving Dialogue Forward");
            }

            

            if (currDiag.responseData.expectedInput == content) // For when the content is equal to the expected
            {
                return true;
            }

            // old code for reference!
            //else // Otherwise, when it is invalid. This is temporary, incomplete logic handling.
            //{
            //    index--;
            //}
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

        // Make sure text box begins empty
        dialogueLabel      = document.rootVisualElement.Q<Label>("DialogueText");
        dialogueLabel.text = "";

        textSpeed      = 0.02f;
        nextLinePrompt = document.rootVisualElement.Q("NextLinePrompt");
        document.rootVisualElement.style.visibility = Visibility.Hidden;
        document.rootVisualElement.style.display    = DisplayStyle.None;
        nextLinePrompt.visible = false;
        inDialogue             = false;
    }

    protected override IEnumerator InteractLogic(PlayerController player)
    {
        if (inDialogue)
        {
            //Debug.Log("Interact Logic Called");
            // Interact key pressed when dialogue line is finished -> to next line/end dialogue
            if (npcTree.InDialogue()) { // Ensure our tree is in dialogue before getting the current entry
                DialogueEntry currDiag = (DialogueEntry) npcTree.GetCurrentEntry();
                
                if (dialogueLabel.text == currDiag.line || alreadyIncrDiag)
                {
                    //Debug.Log("New Line");
                    StopBounce();
                    yield return NextLine();
                }
                else
                {
                    //Debug.Log("Setting");
                    dialogueLabel.text = currDiag.line;
                    StartBounce();
                }
            } else
            {
                // If we are not in dialogue, end the dialogue
                EndDialogue();
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

            int errno = npcTree.InitializeTree(); // initialize the current dialogue tree!
            if (errno < 0) // If there is an error initiLizing the tree, take note!
            {
                Debug.Log("Error with NPC Tree Initialize");
            }

            yield return TypeLine();

            if (npcTree.NeedPlayerInput())
            {
                PlayerController.Instance.context |= PlayerContext.PlayerInput;
                InputController.Instance.OpenKeyboard();
                
                yield return new WaitUntil(() => (PlayerController.Instance.context & PlayerContext.PlayerInput) == 0); 
            } else
            {
                InputController.Instance.CloseKeyboard();
                PlayerController.Instance.context &= ~PlayerContext.PlayerInput;
            }
        }
    }

    /// <summary>
    /// Works currently by completely rendering text rather than suspending any execution/co-routines.
    /// </summary>
    public void Advance()
    {
        //dialogueLabel.text = entries[index].line;
        //Debug.Log("Advance Called");
        if (npcTree.InDialogue())
        {
            dialogueLabel.text = ((DialogueEntry) npcTree.GetCurrentEntry()).line; // 
        } else
        {
            EndDialogue();
        }
        
    }

    private IEnumerator TypeLine()
    {
        //Debug.Log("Type Line Called");
        if (npcTree.InDialogue()) // Ensure we are in dialogue before getting the current entry
        {
           string currentLine = ((DialogueEntry) npcTree.GetCurrentEntry()).line;
            int i = 0;
            while (dialogueLabel.text.Length < currentLine.Length)
            {
                dialogueLabel.text += currentLine[i++];
                yield return new WaitForSeconds(textSpeed);
            }
            StartBounce(); 
        } else
        {
            EndDialogue();
        } 
    }

    private void EndDialogue() // This will be a function that is called when the Dialogue has ended!
    {
        //Debug.Log("End Dialogue Called");
        StopBounce();

        //index                  = 0;
        dialogueLabel.text     = "";
        inDialogue             = false;
        nextLinePrompt.visible = false;

        // Restore Movement
        PlayerController.Instance.CanMove = true;

        // Restore in-game UI
        document.rootVisualElement.style.visibility = Visibility.Hidden;
        document.rootVisualElement.style.display    = DisplayStyle.None;

        hudDocument.rootVisualElement.style.visibility = Visibility.Visible;
        hudDocument.rootVisualElement.style.display    = DisplayStyle.Flex;

        worldPromptIcon.enabled = true;

        InputController.Instance.CloseKeyboard();
        PlayerController.Instance.context &= ~PlayerContext.PlayerInput;
    }

    private IEnumerator NextLine()
    {
        //Debug.Log("Next Line Called");
        if (!npcTree.InDialogue()) // If we are currently not in dialogue, end it!
        {
            EndDialogue();
        } else
        {
            if (!alreadyIncrDiag) // If we haven't already incremented the dialogue, ensure to increment it
            {
                int err = npcTree.DialogueForward(); // This will increment the dialogue accordingly
                if (err < 0) // reports if there is an error!
                {
                    Debug.Log("Error in Moving Dialogue Forward");
                }
            } else
            {
                alreadyIncrDiag = false;
            }

            dialogueLabel.text = "";
            yield return TypeLine();

            if (npcTree.NeedPlayerInput())
            {
                PlayerController.Instance.context |= PlayerContext.PlayerInput;
                InputController.Instance.OpenKeyboard();
                
                yield return new WaitUntil(() => (PlayerController.Instance.context & PlayerContext.PlayerInput) == 0); 
            } else
            {
                InputController.Instance.CloseKeyboard();
                PlayerController.Instance.context &= ~PlayerContext.PlayerInput;
            }
        }
        // if(index < entries.Length - 1) old code for viewing
        // {
        //     index++;
        //     dialogueLabel.text = "";
        //     yield return TypeLine();

        //     if (entries[index].hasResponse)
        //     {
        //         PlayerController.Instance.context |= PlayerContext.PlayerInput;
        //         InputController.Instance.OpenKeyboard();

        //         yield return new WaitUntil(() => (PlayerController.Instance.context & PlayerContext.PlayerInput) == 0);
        //     }
        //     else
        //     {
        //         InputController.Instance.CloseKeyboard();
        //         PlayerController.Instance.context &= ~PlayerContext.PlayerInput;
        //     }
        // }
        // else
        // {
        //   EndDialogue()
        // }
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