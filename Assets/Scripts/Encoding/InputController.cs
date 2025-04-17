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

    //private string inputStr;
    private string InputStr
    {
        set => inputField.text = value;
    }

    void Awake()
    {
        Debug.Assert(languageTable != null);
        document  = GetComponent<UIDocument>();
        processor = new Processor(languageTable.standardSigns, languageTable.compoundSigns);

        inputField = document.rootVisualElement.Q<Label>("Input");
        InputStr   = processor.Translate("an;na;an;");
    }

    void OnDestroy()
    {
        processor.Dispose();
    }
}