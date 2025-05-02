using UnityEngine;
using UnityEngine.UIElements;

[DisallowMultipleComponent, RequireComponent(typeof(UIDocument))]
public sealed class InputController : MonoBehaviour
{
    [HideInInspector] public UIDocument document;

    public Processor processor;

    private Label inputField;

    private string InputStr
    {
        set => inputField.text = value;
    }

    void Start()
    {
        document  = GetComponent<UIDocument>();
        processor = new Processor(LanguageTable.StandardSigns, LanguageTable.CompoundSigns);

        inputField = document.rootVisualElement.Q<Label>("Input");
        InputStr   = processor.Translate("aeeiio+hgo+");
    }

    void OnDestroy()
    {
        processor.Dispose();
    }
}