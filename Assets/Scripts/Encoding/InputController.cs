using System;

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using UnityEngine.InputSystem;
using UnityEngine.UIElements;

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

    public readonly void InitAlphaNumeric(InnerInput input, PhoneticProcessor processor, Action<string> assignCallback)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].style.color = Color.black;
            string text = buttons[i].text; // Using text to infer the output
            buttons[i].RegisterCallback(
                (ClickEvent e) =>
                {
                    input.phoneticsStr += text.ToLower();
                    assignCallback?.Invoke(input.phoneticsStr);
                }
            ); // WATCH
        }
    }

    /// <summary>
    /// For the last row (a row that has submission
    /// </summary>
    public readonly void InitSpecial(InnerInput input, PhoneticProcessor processor, Action<string> assignCallback)
    {
        Button spacebar  = buttons.Where(x => x.name == "Spacebar").First();
        Button backspace = buttons.Where(x => x.name == "Backspace").First();
        Button enter     = buttons.Where(x => x.name == "Enter").First();

        Debug.Assert(spacebar != null && backspace != null && enter != null);

        spacebar.RegisterCallback(
            (ClickEvent e) =>
            {
                input.phoneticsStr += ' ';
                assignCallback?.Invoke(input.phoneticsStr);
            }
        );
        backspace.RegisterCallback(
            (ClickEvent e) =>
            {
                if (input.phoneticsStr.Length > 0)
                {
                    input.phoneticsStr = input.phoneticsStr[..^1];
                    assignCallback?.Invoke(input.phoneticsStr);
                }
            }
        );
        enter.RegisterCallback(
            (ClickEvent e) =>
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
                    string unicodeStr = processor.TranslateManaged(input.phoneticsStr);
                    (NPC as NpcDialogue).TryCheckInput(unicodeStr);
                }

                InputController.Instance.CloseKeyboard();
                PlayerController.Instance.context &= ~PlayerContext.PlayerInput;
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
        }
    }

    private PhoneticProcessor processor;
    public KeyboardRow[] rows;

    public Action<string> assignCallback;

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
            rows[i].InitAlphaNumeric(inner, processor, assignCallback);
        }
        rows[^1].InitSpecial(inner, processor, assignCallback);
    }

    public void ClearStrings()
    {
        inner.phoneticsStr = string.Empty;
        assignCallback?.Invoke(inner.phoneticsStr);
    }
}

[DisallowMultipleComponent, RequireComponent(typeof(Transform), typeof(UIDocument))]
public sealed class InputController : MonoBehaviour
{
    [SerializeField] private VisualTreeAsset keyboardAsset;
    private KeyboardUI keyboardUI;
    private UIDocument document;

    private Label inputField;

    [SerializeField] private float topPadding = 0.0f;

    public Label InputField => inputField;

    public static InputController Instance { get; private set; }

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

        keyboardUI.parent.BringToFront();
    }

    public void CloseKeyboard()
    {
        keyboardUI.style.visibility = Visibility.Hidden;
        keyboardUI.style.display    = DisplayStyle.None;
    }

    public void InsertCharacter(string character) => InputField.text += character;
}