using UnityEngine;
using UnityEngine.UIElements;

public sealed class DialogueBox : VisualElement
{
    public struct DictionaryData
    {
        public Label dictWords;
        public Label dictNotes;
    }

    public struct CharacterData
    {
        public string    name;
        public Texture2D image;
    }

    private float textSpeed;

    private DictionaryData dictData;
    private CharacterData  charData;

    private Label         npcName;
    private VisualElement npcImage;

    private VisualElement wordTooltip;
    private WordBox hoverPrefab, noHoverPrefab;

    private StyleColor originalColor;

    public DialogueBox(VisualTreeAsset asset, in DictionaryData dictData, in CharacterData charData)
    {
        Debug.Assert(asset != null);
        asset.CloneTree(this);

        Debug.Assert(dictData.dictWords != null);
        Debug.Assert(dictData.dictNotes != null);

        this.dictData = dictData;
        this.charData = charData;

        npcName  = this.Q<Label>("NpcName");
        npcImage = this.Q<VisualElement>("NpcImage");
        Debug.Assert(npcName  != null);
        Debug.Assert(npcImage != null);

        npcName.text                   = charData.name;
        npcImage.style.backgroundImage = charData.image;

        wordTooltip   = this.Q("WordTooltip");
        hoverPrefab   = new WordBox(asset, MetaHover());
        noHoverPrefab = new WordBox(asset);

        originalColor = style.color;
    }

    private ToolTipProperties MetaHover() => new()
    {
        onPointerEnter = (evt) =>
        {
            Label target       = (Label) evt.target;
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
        dictData.dictWords.text = name;
        dictData.dictWords.style.unityFontDefinition = font;

        dictData.dictNotes.text = GetPlayerNotes(name);

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