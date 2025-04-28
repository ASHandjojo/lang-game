using UnityEngine;
using UnityEngine.UIElements;

[DisallowMultipleComponent, RequireComponent(typeof(UIDocument))]
public sealed class InputController : MonoBehaviour
{
    [SerializeField]
    private LanguageTable languageTable;
    [HideInInspector] public UIDocument document;

    public Processor processor;

    private Label inputField;

    private string InputStr
    {
        set => inputField.text = value;
    }

    void Awake()
    {
        Debug.Assert(languageTable != null);
        document  = GetComponent<UIDocument>();
        processor = new Processor(languageTable.StandardSigns, languageTable.CompoundSigns);

        inputField = document.rootVisualElement.Q<Label>("Input");
        InputStr   = processor.Translate("a;e;e;i;");
    }

    void OnDestroy()
    {
        processor.Dispose();
    }
}