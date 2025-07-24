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

            Button[] buttons = children.Select((x) => x as Button)
                .Where(x => x != null)
                .ToArray();
            Debug.Assert(buttons.Length > 0);

            this.container = container;
            this.buttons   = buttons;
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
        Debug.Assert(children.Length > 0);

        inputField = this.Q<Label>("Input");
        rows = new KeyboardRow[children.Length - 1];
        for (int i = 1; i < children.Length; i++) {
            rows[i - 1] = new KeyboardRow(children[i]);
        }

        inputField.text = "";
    }
}

[DisallowMultipleComponent, RequireComponent(typeof(Transform), typeof(UIDocument))]
public sealed class InputController : MonoBehaviour
{
    [SerializeField] private VisualTreeAsset keyboardAsset;
    private KeyboardUI keyboardUI;
    private UIDocument document;

    public Label InputField => keyboardUI.inputField;

    public string InputStr
    {
        set => InputField.text = value;
    }

    private static InputController Instance { get; set; }
    // Just shorter to get references lol
    private static ref readonly Processor Processor => ref LanguageTable.Processor;
    public string TranslatedStr => Processor.Translate(InputField.text);

    void Awake()
    {
        Debug.Assert(keyboardAsset != null);
        keyboardUI = new KeyboardUI(keyboardAsset);

        document = GetComponent<UIDocument>();
        document.rootVisualElement.Add(keyboardUI);

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

    void Update()
    {

    }

    public void OpenKeyboard()
    {
        InputField.text = ""; // Clears contents
        InputField.SetEnabled(true);

        InputField.style.visibility = Visibility.Visible;
    }

    public void CloseKeyboard()
    {
        gameObject.SetActive(false);
        InputField.style.visibility = Visibility.Hidden;
    }

    public void InsertCharacter(string character) => InputField.text += character;

    public void Backspace()
    {
        if (InputField.text.Length > 0)
        {
            InputField.text = InputField.text[..^1];
        }
    }

    public void SubmitInput()
    {
        FindFirstObjectByType<AnswerChecker>().CheckAnswer(TranslatedStr);
        CloseKeyboard();
    }
}