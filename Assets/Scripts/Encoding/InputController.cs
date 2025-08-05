using System.Linq;

using UnityEngine;
using UnityEngine.UIElements;

public sealed class KeyboardUI : VisualElement
{
    public struct KeyboardRow
    {
        public VisualElement container;
        public Button[] buttons;

        public KeyboardRow(VisualElement container)
        {
            Debug.Assert(container != null);

            VisualElement[] children = container.Children().ToArray();
            Debug.Assert(children.Length > 0);

            Button[] buttons = children.Select(x => x as Button)
                .Where(x => x != null)
                .ToArray();
            Debug.Assert(buttons.Length > 0);

            this.container = container;
            this.buttons   = buttons;
        }

        public readonly void InitAlphaNumeric(Label inputField)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                string text = buttons[i].text; // Using text to infer the output
                buttons[i].RegisterCallback((ClickEvent e) => inputField.text += text); // WATCH
            }
        }

        /// <summary>
        /// For the last row (a row that has submission
        /// </summary>
        public readonly void InitSpecial(Label inputField)
        {
            Button spacebar  = buttons.Where(x => x.name == "Spacebar").First();
            Button backspace = buttons.Where(x => x.name == "Backspace").First();
            Button enter     = buttons.Where(x => x.name == "Enter").First();

            Debug.Assert(spacebar != null && backspace != null && enter != null);

            spacebar.RegisterCallback((ClickEvent e) => inputField.text += ' ');
            backspace.RegisterCallback(
                (ClickEvent e) =>
                {
                    if (inputField.text.Length > 0)
                    {
                        inputField.text = inputField.text[..^1];
                    }
                }
            );
            enter.RegisterCallback((ClickEvent e) => InputController.Instance.SubmitInput());
        }
    }

    public Label inputField;
    public KeyboardRow[] rows;

    public KeyboardUI(VisualTreeAsset layout)
    {
        Debug.Assert(layout != null);
        layout.CloneTree(this);

        VisualElement parent     = this.Q<VisualElement>("KeyboardParent");
        VisualElement[] children = parent.Children().ToArray(); // First one is input bar
        Debug.Assert(children.Length > 1);

        inputField = this.Q<Label>("Input");
        rows = new KeyboardRow[children.Length - 1];
        for (int i = 1; i < children.Length; i++) {
            rows[i - 1] = new KeyboardRow(children[i]);
        }

        for (int i = 0; i < rows.Length - 1; i++)
        {
            rows[i].InitAlphaNumeric(inputField);
        }
        rows[^1].InitSpecial(inputField);
    }
}

[DisallowMultipleComponent, RequireComponent(typeof(Transform), typeof(UIDocument))]
public sealed class InputController : MonoBehaviour
{
    [SerializeField] private VisualTreeAsset keyboardAsset;
    private KeyboardUI keyboardUI;
    private UIDocument document;

    [SerializeField] private float topPadding = 0.0f;

    public Label InputField => keyboardUI.inputField;

    public string InputStr
    {
        set => InputField.text = value;
    }

    public static InputController Instance { get; private set; }
    // Just shorter to get references lol
    private static ref readonly Processor Processor => ref LanguageTable.Processor;
    public string TranslatedStr => Processor.Translate(InputField.text);

    void Awake()
    {
        Debug.Assert(keyboardAsset != null);
        keyboardUI = new KeyboardUI(keyboardAsset);

        document = GetComponent<UIDocument>();
        document.rootVisualElement.Add(keyboardUI);
        document.rootVisualElement.style.top   = new StyleLength(new Length(topPadding, LengthUnit.Percent));
        document.rootVisualElement.style.left  = new StyleLength(new Length(50.0f, LengthUnit.Percent));
        document.rootVisualElement.style.right = new StyleLength(new Length(50.0f, LengthUnit.Percent));

        if (Instance != null)
        {
            Debug.LogWarning($"Duplicate instance has been created of {nameof(InputController)}! Destroying duplicate instance.");
            Destroy(this);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        CloseKeyboard();
    }

    public void OpenKeyboard()
    {
        InputField.text = ""; // Clears contents
        keyboardUI.style.visibility = Visibility.Visible;
    }

    public void CloseKeyboard()
    {
        keyboardUI.style.visibility = Visibility.Hidden;
    }

    public void InsertCharacter(string character) => InputField.text += character;

    public void SubmitInput()
    {
        FindFirstObjectByType<AnswerChecker>().CheckAnswer(TranslatedStr);
        CloseKeyboard();
    }
}