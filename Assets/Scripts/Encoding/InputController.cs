using UnityEngine;
using UnityEngine.UIElements;

[DisallowMultipleComponent, RequireComponent(typeof(UIDocument))]
public sealed class InputController : MonoBehaviour
{
    [HideInInspector] public UIDocument document;

    public Processor processor;

    private Label inputField;

    public Label InputField => inputField;

    public string InputStr
    {
        set => inputField.text = value;
    }

    public string TranslatedStr => processor.Translate(inputField.text);

    void Start()
    {
        document  = GetComponent<UIDocument>();
        processor = new Processor(LanguageTable.StandardSigns, LanguageTable.CompoundSigns);

        inputField = document.rootVisualElement.Q<Label>("Input");
        InputField.text = "";
    }

    void OnDestroy()
    {
        processor.Dispose();
    }
}