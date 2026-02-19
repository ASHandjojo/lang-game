using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using static System.Collections.Specialized.BitVector32;

readonly struct RebindButton
{
    public string ID { get; }
    public string ActionID { get; }
    public int BindingIndex { get; }
        // for Button: 0; for Vector2: 1-4 (left: 3, right: 4)

    public RebindButton(string id, string actionID, int bindingIndex)
    {
        ID           = id;
        ActionID     = actionID;
        BindingIndex = bindingIndex;
    }
}

public sealed class SettingsMenuEvents : OpenClosable
{
    private static readonly RebindButton[] RebindButtons =
    {
        new("MoveRight", "Move", 4),
        new("MoveLeft", "Move", 3),
        new("Interact", "Interact", 0),
        new("Dictionary", "Dictionary", 0),
        new("Return", "Return", 0),
        new("SettingMenu", "Settings", 0),
    };

    private Dictionary<string, EventCallback<ClickEvent>> rebindButtonEventHandlers;
    
    private UIDocument selfDocument;

    private Button backButton;
    private SoundHandler sh;
    [Header("Audio")]
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip selectionClip;

    private Sprite[] azSprites = {null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null};

    void Awake()
    {
        selfDocument = GetComponent<UIDocument>();
        sh = GetComponent<SoundHandler>();

        char start = (char)97;
        for (int i = 0; i < 26; i++) {
            //Debug.Log(start);
            if (i % 23 == 0 || i % 23 == 1) {
                azSprites[i] = Resources.Load<Sprite>("Keys/" + start + "_key_light");
            } else {
                azSprites[i] = Resources.Load<Sprite>("Keys/" + start + "_light");
            }
            
            start = (char) (start + 1);
        }

        //vSprite.push(Resources.Load<Sprite>("Keys/v_light"));

        //if (vSprite != null) {
        //    Debug.Log("Sprite Loaded correctly");
        //}

        // Begin with settings menu not displayed
        selfDocument.rootVisualElement.style.display = DisplayStyle.None;
    }

    void Start()
    {
        var rebinds = PlayerPrefs.GetString("rebinds");
        if (!string.IsNullOrEmpty(rebinds))
        {
            InputSystem.actions.LoadBindingOverridesFromJson(rebinds);
        }

        //Initialize Keybinds on Settings Menu
        foreach (RebindButton rebindButton in RebindButtons)
        {
            // currently unused; TODO: display correct image based on this
            InputBinding binding = InputSystem.actions.FindAction(rebindButton.ActionID)
                .bindings[rebindButton.BindingIndex];

            // change ui image
            SetRebindKeyImage(rebindButton.ID, 97);
                // keycode 97; temporary fix to make it work with the old keycode system 
        } 
    } 

    void OnEnable()
    {
        // Add events to back button
        backButton = selfDocument.rootVisualElement.Q("BackButton") as Button;
        backButton.RegisterCallback<ClickEvent>(e => MenuToggler.Instance.ClearAllMenus());

        // Add input listeners for Keybinds
        rebindButtonEventHandlers = new();

        foreach (RebindButton rebindButton in RebindButtons)
        {
            void handler(ClickEvent e) => OnRebindButton(rebindButton);
            rebindButtonEventHandlers.Add(rebindButton.ID + "Button", handler);
            Button uiButton = selfDocument.rootVisualElement.Q(rebindButton.ID + "Button") as Button;
            uiButton.RegisterCallback<ClickEvent>(handler);
        }

        backButton.RegisterCallback<ClickEvent>(OnButtonClick);

        // Add sounds
        backButton.RegisterCallback<MouseEnterEvent>(OnButtonHover);
    }

    // Get rid of button events
    void OnDisable()
    {
        backButton.UnregisterCallback<ClickEvent>(OnButtonClick);
        backButton.UnregisterCallback<MouseEnterEvent>(OnButtonHover);

        foreach (var (buttonID, handler) in rebindButtonEventHandlers)
        {
            Button uiButton = selfDocument.rootVisualElement.Q(buttonID) as Button;
            uiButton.UnregisterCallback(handler);
        }
    }

    public override void Open()
    {
        selfDocument.rootVisualElement.style.display = DisplayStyle.Flex;
    }

    // Return to main menu or gameHud
    public override void Close()
    {
        backButton.SetEnabled(false);

        //otherDocument.rootVisualElement.style.display = DisplayStyle.Flex;
        selfDocument.rootVisualElement.style.display = DisplayStyle.None;

        backButton.SetEnabled(true);
    }


    // Play sound when a button is clicked
    private void OnButtonClick(ClickEvent e)
    {
        //Debug.Log("Click");
        sh.PlaySoundUI(selectionClip);
        MenuToggler.Instance.UseMenu(this);
    }

    // Play sound when cursor is over a button
    private void OnButtonHover(MouseEnterEvent e)
    {
        sh.PlaySoundUI(hoverClip);
    }

    private void OnRebindButton(RebindButton rebindButton)
    {
        Debug.Log("REBIND BUTTON: " + rebindButton.ID);

        Button uiButton = selfDocument.rootVisualElement.Q(rebindButton.ID + "Button") as Button;
        uiButton.AddToClassList("rebinding"); // rebinding styles

        InputAction action = InputSystem.actions.FindAction(rebindButton.ActionID);

        // action needs to be disabled before rebinding
        action.Disable();

        action.PerformInteractiveRebinding(rebindButton.BindingIndex)
            .OnComplete(operation => {
                string newKeyPath = operation.selectedControl.path;
                string newKeyeadableName = operation.selectedControl.displayName;

                // change ui image
                SetRebindKeyImage(rebindButton.ID, 97);
                    // keycode 97; temporary fix to make it work with the old keycode system 

                // save to playerprefs
                PlayerPrefs.SetString("rebinds", InputSystem.actions.SaveBindingOverridesAsJson());

                uiButton.RemoveFromClassList("rebinding"); // remove rebinding styles
                operation.Dispose();
                action.Enable();
            })
            .OnCancel(operation => {
                uiButton.RemoveFromClassList("rebinding"); // remove rebinding styles
                operation.Dispose();
                action.Enable();
            })
            .Start();
    }
    
    private void SetRebindKeyImage(string rebindID, int keycode)
    {
        //selfDocument.rootVisualElement.Q(rebindID + "Image").style.backgroundImage
        //    = Background.FromSprite(azSprites[keycode - 97]);
    }

    // Change dialogue text speed based on slider position 
}
