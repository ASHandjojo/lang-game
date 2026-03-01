using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public enum RebindableInput
{
    // don't rename values
    MoveRight,
    MoveLeft,
    Interact,
    Dictionary,
    Return,
    Settings
}

[System.Serializable]
public struct RebindableInputPath
{
    public string ActionId { get; }
    public int BindingIndex { get; }

    public RebindableInputPath(string actionId, int bindingIndex)
    {
        ActionId = actionId;
        BindingIndex = bindingIndex;
    }

    public readonly InputAction GetAction()
    {
        return InputSystem.actions.FindAction(ActionId);
    }
    public readonly InputBinding GetBinding()
    {
        return GetAction().bindings[BindingIndex];
    }

    public readonly bool Equals(string actionId, int bindingIndex)
    {
        return ActionId == actionId && BindingIndex == bindingIndex;
    }
}

[DisallowMultipleComponent]
public sealed class SettingsMenuEvents : UIMenuController
{
    public static readonly IReadOnlyDictionary<RebindableInput, RebindableInputPath>
        RebindableInputPaths = new Dictionary<RebindableInput, RebindableInputPath>()
    {
        { RebindableInput.MoveRight, new("Move", 4) },
        { RebindableInput.MoveLeft, new("Move", 3) },
        { RebindableInput.Interact, new("Interact", 0) },
        { RebindableInput.Dictionary, new("Dictionary", 0) },
        { RebindableInput.Return, new("Return", 0) },
        { RebindableInput.Settings, new("Settings", 0) },
    };

    private Dictionary<string, EventCallback<ClickEvent>> rebindButtonEventHandlers;
    
    private UIDocument selfDocument;

    private Button backButton;
    private Button saveButton;
    [SerializeField] private UIDocument saveDocument;
    private SoundHandler sh;
    [Header("Audio")]
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip selectionClip;

    void Awake()
    {
        selfDocument = GetComponent<UIDocument>();
        sh = GetComponent<SoundHandler>();

        // Begin with settings menu not displayed
        selfDocument.rootVisualElement.style.display = DisplayStyle.None;
    }

    void Start()
    {
        string rebinds = PlayerPrefs.GetString("rebinds");
        if (!string.IsNullOrEmpty(rebinds))
        {
            InputSystem.actions.LoadBindingOverridesFromJson(rebinds);
        }

        // initialize ui images
        UpdateAllRebindBindingIcons();
    } 

    void OnEnable()
    {
        // Add events to back button
        backButton = selfDocument.rootVisualElement.Q("BackButton") as Button;
        backButton.RegisterCallback<ClickEvent>(e => MenuToggler.Instance.ClearAllMenus());

        saveButton = selfDocument.rootVisualElement.Q("SaveButton") as Button;
        var saveComponent = saveDocument.gameObject.GetComponent<SaveMenuEvents>();
        saveButton.RegisterCallback<ClickEvent>((e) => MenuToggler.Instance.UseMenu(saveComponent));

        // Add input listeners for Keybinds
        rebindButtonEventHandlers = new();

        foreach (var (input, path) in RebindableInputPaths)
        {
            void handler(ClickEvent e) => OnRebindButton(input, path);

            string buttonId = input + "Button";
            rebindButtonEventHandlers.Add(buttonId, handler);

            Button uiButton = selfDocument.rootVisualElement.Q(buttonId) as Button;
            uiButton.RegisterCallback<ClickEvent>(handler);
        }

        // Add sounds
        backButton.RegisterCallback<MouseEnterEvent>(OnButtonHover);

        InputSystem.onActionChange += HandleActionChange;
    }

    // Get rid of button events
    void OnDisable()
    {
        backButton.UnregisterCallback<ClickEvent>(OnButtonClick);
        backButton.UnregisterCallback<MouseEnterEvent>(OnButtonHover);

        saveButton.UnregisterCallback<ClickEvent>(OnButtonClick);
        saveButton.UnregisterCallback<MouseEnterEvent>(OnButtonHover);

        /**
        foreach (var (buttonID, handler) in rebindButtonEventHandlers)
        {
            Button uiButton = selfDocument.rootVisualElement.Q(buttonID) as Button;
            uiButton.UnregisterCallback(handler);
        }
        */

        InputSystem.onActionChange -= HandleActionChange;
    }

    private void HandleActionChange(object actionOrMap, InputActionChange change)
    {
        // ignore if not a rebinding update
        if (change != InputActionChange.BoundControlsChanged) return;
        UpdateAllRebindBindingIcons();
    }

    public override IEnumerator Open()
    {
        selfDocument.rootVisualElement.style.display = DisplayStyle.Flex;
        yield break;
    }

    // Return to main menu or gameHud
    public override IEnumerator Close()
    {
        backButton.SetEnabled(false);
        saveButton.SetEnabled(false);
        selfDocument.rootVisualElement.style.display = DisplayStyle.None;

        backButton.SetEnabled(true);
        saveButton.SetEnabled(true);

        yield break;
    }


    // Play sound when a button is clicked
    private void OnButtonClick(ClickEvent e)
    {
        sh.PlaySoundUI(selectionClip);
        MenuToggler.Instance.UseMenu(this);
    }

    // Play sound when cursor is over a button
    private void OnButtonHover(MouseEnterEvent e)
    {
        sh.PlaySoundUI(hoverClip);
    }

    private void OnRebindButton(RebindableInput input, RebindableInputPath path)
    {
        Debug.Log("REBIND BUTTON: " + input);

        Button uiButton = selfDocument.rootVisualElement.Q(input + "Button") as Button;
        uiButton.AddToClassList("rebinding"); // rebinding styles

        InputAction action = path.GetAction();

        // action needs to be disabled before rebinding
        action.Disable();

        action.PerformInteractiveRebinding(path.BindingIndex)
            .OnComplete(operation => {
                string newKeyPath = operation.selectedControl.path;
                string newKeyeadableName = operation.selectedControl.displayName;

                // change ui image
                //SetRebindKeyImage(rebindButton.ID, 97);
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
    
    private void SetRebindBindingIcon(RebindableInput input, string controlPath)
    {
        if (MenuToggler.BindingIcons.Icons.TryGetValue(controlPath, out Sprite iconSprite)) 
        {
            selfDocument.rootVisualElement.Q(input + "Image").style.backgroundImage 
                = Background.FromSprite(iconSprite);
        }
        else
        {
            Debug.LogWarning("No icon found for control path '" + controlPath + "'");
        }
    }

    private void UpdateAllRebindBindingIcons()
    {
        Debug.Log("Updating Settings Menu UI Binding Icons");

        foreach (var (input, path) in RebindableInputPaths)
        {
            SetRebindBindingIcon(input, path.GetBinding().effectivePath);
        }
    }

    // Change dialogue text speed based on slider position 
}
