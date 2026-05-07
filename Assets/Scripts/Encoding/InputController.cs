using System;

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.EventSystems;

using Impl;

namespace Impl
{
    /// <summary>
    /// For mutability purposes. Strings are reference types but also immutable.
    /// </summary>
    public sealed class InnerInput
    {
        public string phoneticsStr = string.Empty; // Raw
    }
}

public struct KeyboardRow
{
    public VisualElement container;
    public Button[]      buttons;

    public KeyboardRow(VisualElement container)
    {
        Debug.Assert(container != null);

        VisualElement[] children = container.Children().ToArray();
        Debug.Assert(children.Length > 0);

        // Filters for all children that are buttons
        Button[] buttons = children.Select(x => x as Button)
            .Where(x => x != null)
            .ToArray();
        Debug.Assert(buttons.Length > 0); // Expects a non-zero amount of buttons per row

        this.container = container;
        this.buttons   = buttons;
    }

    public readonly void InitAlphaNumeric(KeyboardUI keyboardUI)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].style.color = Color.black;
            string text = buttons[i].text; // Using text to infer the output
            buttons[i].RegisterCallback(
                (ClickEvent e) =>
                {
                    keyboardUI.AddChar(text.ToLower());
                }
            ); // WATCH
        }
    }

    /// <summary>
    /// For the last row (a row that has submission
    /// </summary>
    public readonly void InitSpecial(KeyboardUI keyboardUI)
    {
        Button spacebar  = buttons.Where(x => x.name == "Spacebar").First();
        Button backspace = buttons.Where(x => x.name == "Backspace").First();
        Button enter     = buttons.Where(x => x.name == "Enter").First();

        Debug.Assert(spacebar != null && backspace != null && enter != null);

        spacebar.RegisterCallback(
            (ClickEvent e) =>
            {
                keyboardUI.AddChar(" ");
            }
        );
        backspace.RegisterCallback(
            (ClickEvent e) =>
            {
                keyboardUI.RemoveChar();
            }
        );
        enter.RegisterCallback(
            (ClickEvent e) =>
            {
                keyboardUI.Submit();
            }
        );
    }
}

public sealed class KeyboardUI : VisualElement
{
    private readonly InnerInput inner = new();

    public string PhoneticsString
    {
        get => inner.phoneticsStr;
        set
        {
            inner.phoneticsStr = value;
            assignCallback?.Invoke(inner.phoneticsStr);
        }
    }

    private PhoneticProcessor processor;
    public KeyboardRow[] rows;

    public Action<string> assignCallback;

    private bool inTypingMode = false;
    public bool InTypingMode { get { return inTypingMode; } }

    public KeyboardUI(VisualTreeAsset layout, in PhoneticProcessor processor, Action<string> assignCallback) : this(layout, processor, assignCallback, string.Empty) { }

    public KeyboardUI(VisualTreeAsset layout, in PhoneticProcessor processor, Action<string> assignCallback, string phoneticsStr)
    {
        Debug.Assert(phoneticsStr != null);
        inner.phoneticsStr = phoneticsStr;

        Debug.Assert(layout != null);
        layout.CloneTree(this);

        this.assignCallback = assignCallback;
        this.processor      = processor;

        VisualElement parent     = this.Q<VisualElement>("KeyboardParent");
        VisualElement[] children = parent.Children().ToArray(); // First one is input bar
        Debug.Assert(children.Length > 1);

        rows = new KeyboardRow[children.Length - 1];
        for (int i = 1; i < children.Length; i++) {
            rows[i - 1] = new KeyboardRow(children[i]);
        }

        for (int i = 0; i < rows.Length - 1; i++)
        {
            rows[i].InitAlphaNumeric(this);
        }
        rows[^1].InitSpecial(this);

        this.focusable = true;

        this.RegisterCallback<FocusInEvent>(evt => {
            EnterTypingMode();
        });


        this.RegisterCallback<FocusOutEvent>(evt => {
            LeaveTypingMode();
        });
    }

    public void ClearStrings()
    {
        inner.phoneticsStr = string.Empty;
    }

    public void AddChar(string ch)
    {
        inner.phoneticsStr += ch.ToLower();
        assignCallback?.Invoke(inner.phoneticsStr);
    }

    public void RemoveChar()
    {
        if (inner.phoneticsStr.Length > 0)
        {
            inner.phoneticsStr = inner.phoneticsStr[..^1];
            assignCallback?.Invoke(inner.phoneticsStr);
        }
    }

    public void Submit()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            return;
        }
#endif
        Interactable NPC = null;
        Debug.Assert(PlayerController.Instance.currentInteraction.TryGet(out NPC));
        if (NPC is NpcDialogue)
        {
            string unicodeStr = processor.TranslateManaged(inner.phoneticsStr);
            (NPC as NpcDialogue).TryCheckInput(unicodeStr);
        }

        InputController.Instance.CloseKeyboard();
        PlayerController.Instance.context &= ~PlayerContext.PlayerInput;
    }

    public void EnterTypingMode()
    {
        inTypingMode = true;

        InputSystem.actions.FindActionMap("MenuToggles").Disable();
        InputSystem.actions.FindActionMap("Keyboard").Enable();
    }

    public void LeaveTypingMode()
    {
        inTypingMode = false;

        InputSystem.actions.FindActionMap("MenuToggles").Enable();
        InputSystem.actions.FindActionMap("Keyboard").Disable();
    }
}

[DisallowMultipleComponent, RequireComponent(typeof(Transform), typeof(UIDocument))]
public sealed class InputController : MonoBehaviour
{
    [SerializeField] private VisualTreeAsset keyboardAsset;
    public KeyboardUI keyboardUI;
    private UIDocument document;

    private Label inputField;

    [SerializeField] private float topPadding = 0.0f;

    public Label InputField => inputField;

    public static InputController Instance { get; private set; }

    private InputActionMap keyboardActionMap;

    private InputAction backspaceAction;
    private InputAction enterAction;
    private InputAction spaceAction;

    // Just shorter to get references lol
    private static ref readonly PhoneticProcessor PhoneticProcessor => ref LanguageTable.PhoneticProcessor;

    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogWarning($"Duplicate instance has been created of {nameof(InputController)}! Destroying duplicate instance.");
            Destroy(this);
            return;
        }

        Instance = this;
        Debug.Assert(keyboardAsset != null);

        document = GetComponent<UIDocument>();
        document.rootVisualElement.style.top   = new StyleLength(new Length(topPadding, LengthUnit.Percent));
        document.rootVisualElement.style.left  = new StyleLength(new Length(50.0f, LengthUnit.Percent));
        document.rootVisualElement.style.right = new StyleLength(new Length(50.0f, LengthUnit.Percent));

        keyboardActionMap = InputSystem.actions.FindActionMap("Keyboard");

        InputActionMap keyboardSpecialActionMap = InputSystem.actions.FindActionMap("KeyboardSpecial");
        backspaceAction = keyboardSpecialActionMap.FindAction("Backspace");
        enterAction     = keyboardSpecialActionMap.FindAction("Enter");
        spaceAction     = keyboardSpecialActionMap.FindAction("Space");
    }

    void Start()
    {
        keyboardUI = new KeyboardUI(keyboardAsset, PhoneticProcessor,
            (string phoneticsStr) =>
            {
                inputField.text = PhoneticProcessor.TranslateManaged(phoneticsStr);
            }
        );
        document.rootVisualElement.Add(keyboardUI);

        inputField = keyboardUI.Q<Label>("Input");
        Debug.Assert(inputField != null);

        CloseKeyboard();
    }

    public void OpenKeyboard()
    {
        keyboardUI.ClearStrings(); // Clears contents
        keyboardUI.style.visibility = Visibility.Visible;
        keyboardUI.style.display    = DisplayStyle.Flex;

    }

    public void CloseKeyboard()
    {
        keyboardUI.ClearStrings();
        keyboardUI.style.visibility = Visibility.Hidden;
        keyboardUI.style.display    = DisplayStyle.None;

        keyboardUI.LeaveTypingMode();
    }

    public void InsertCharacter(string character) => InputField.text += character;

    void Update()
    {
        if (keyboardUI.InTypingMode)
        {
            foreach (InputAction action in keyboardActionMap)
            {
                if (action.WasPerformedThisFrame())
                {
                    keyboardUI.AddChar(action.name);
                }
            }

            if (backspaceAction.WasPerformedThisFrame())
            {
                keyboardUI.RemoveChar();
            }

            if (enterAction.WasPerformedThisFrame())
            {
                keyboardUI.Submit();
            }

            if (spaceAction.WasPerformedThisFrame())
            {
                keyboardUI.AddChar(" ");
            }
        }
    }
}