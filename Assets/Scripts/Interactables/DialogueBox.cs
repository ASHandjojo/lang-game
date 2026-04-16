using UnityEngine;
using UnityEngine.UIElements;

public sealed class DialogueBox : VisualElement
{
    private float textSpeed;

    private VisualElement wordTooltip;

    private Label   dictWords, dictNotes;
    private WordBox hoverPrefab, noHoverPrefab;

    private StyleColor originalColor;

    public DialogueBox(VisualTreeAsset asset, Label dictWords, Label dictNotes)
    {
        Debug.Assert(asset != null);

        Debug.Assert(dictWords != null);
        Debug.Assert(dictNotes != null);

        this.dictWords = dictWords;
        this.dictNotes = dictNotes;

        wordTooltip   = this.Q("WordTooltip");
        hoverPrefab   = new WordBox(asset, MetaHover());
        noHoverPrefab = new WordBox(asset);
    }

    private ToolTipProperties MetaHover() => new()
    {
        onPointerEnter = (evt) =>
        {
            Label target = (Label)evt.target;
            target.style.color = new StyleColor(Color.red);
            ShowTooltip(name, evt.position, target.style.unityFontDefinition);
        },
        onPointerMove  = (evt) => MoveTooltip(evt.position),
        onPointerLeave = (evt) =>
            {
                Label target = (Label)evt.target;
                target.style.color = originalColor;
                HideTooltip();
            },
        onPanelDetach  = (evt) => HideTooltip()
    };

    private string GetPlayerNotes(string word)
    {
        PlayerController player = PlayerController.Instance;
        Dictionary dictionary   = player.dictionary;

        foreach (DictionaryEntry entry in dictionary.dictionaryList) 
        {
            if (LanguageTable.PhoneticProcessor.Translate(entry.Word) == word) 
            {
                if (entry.Notes.Length == 0)
                {
                    return "No Notes Available For This Word";
                }
                return entry.Notes;
            }
        }

        return "No Notes Available For This Word";
    }

    private void ShowTooltip(string name, Vector2 mousePosition, StyleFontDefinition font)
    {
        dictWords.text = name;
        dictWords.style.unityFontDefinition = font;

        dictNotes.text = GetPlayerNotes(name);

        MoveTooltip(mousePosition);
        wordTooltip.style.display = DisplayStyle.Flex;
    }

    private void MoveTooltip(Vector2 mousePosition)
    {
        float offsetX = 12.0f;
        float offsetY = 12.0f;

        wordTooltip.style.left = mousePosition.x + offsetX;
        wordTooltip.style.top  = mousePosition.y + offsetY;
    }

    private void HideTooltip() => wordTooltip.style.display = DisplayStyle.None;
}