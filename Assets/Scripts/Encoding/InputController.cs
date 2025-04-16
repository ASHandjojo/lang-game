using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

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
        InputStr   = processor.Translate("an;na;");
    }

    void OnDestroy()
    {
        processor.Dispose();
    }
}

/**
#if UNITY_EDITOR
[CustomEditor(typeof(InputController))]
public sealed class InputControllerEditor : Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        VisualElement visualElement = new();

        visualElement.Add(new IMGUIContainer(base.OnInspectorGUI));

        return visualElement;
    }

    public override void OnInspectorGUI()
    {
        InputController controller = target as InputController;
        VisualElement inputElement = controller.GetComponent<UIDocument>().rootVisualElement;

        var inputField = inputElement.Q<TextField>("Input");
        inputField.Bind(serializedObject);
        inputField.bindingPath = "inputStr";
    }
}
#endif
*/