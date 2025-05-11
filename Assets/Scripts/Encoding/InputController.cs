using UnityEngine;
using UnityEngine.UIElements;

[DisallowMultipleComponent, RequireComponent(typeof(RectTransform))]
public sealed class InputController : MonoBehaviour
{
    public GameObject inputPrefab;
    private UIDocument document;
    private Label inputField;

    private RectTransform transCast;

    public Label InputField => inputField;

    public string InputStr
    {
        set => inputField.text = value;
    }

    // Just shorter to get references lol
    private static ref readonly Processor Processor => ref LanguageTable.Processor;

    public string TranslatedStr => Processor.Translate(inputField.text);

    void Awake()
    {
        transCast = transform as RectTransform;
        Debug.Assert(transCast != null);

        document = inputPrefab.GetComponent<UIDocument>();

        inputField = document.rootVisualElement.Q<Label>("Input");
        InputField.text = "";
    }

    void Start()
    {
        CloseKeyboard();
    }

    void Update()
    {
        Resolution screenDims  = Screen.currentResolution;
        Vector3 parentPosition = transCast.position;

        inputField.style.left  = parentPosition.x / 2.0f;
        inputField.style.top   = (screenDims.height - parentPosition.y) / 2.0f;
        inputField.style.width = new StyleLength(transCast.rect.width);
    }

    public void OpenKeyboard()
    {
        inputField.text = ""; // Clears contents

        inputField.SetEnabled(true);
        inputField.style.visibility = Visibility.Visible;
    }

    public void CloseKeyboard()
    {
        gameObject.SetActive(false);
        inputField.style.visibility = Visibility.Hidden;
    }

    public void InsertCharacter(string character) => inputField.text += character;

    public void Backspace()
    {
        if (inputField.text.Length > 0)
        {
            inputField.text = inputField.text[..^1];
        }
    }

    public void SubmitInput()
    {
        FindFirstObjectByType<AnswerChecker>().CheckAnswer(TranslatedStr);
        CloseKeyboard();
    }
}