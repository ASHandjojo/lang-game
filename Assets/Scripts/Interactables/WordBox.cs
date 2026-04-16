using UnityEngine;
using UnityEngine.UIElements;

public sealed class WordBox : Label
{
    private VisualElement wordTooltip;
    private Label word, notes;

    private StyleColor originalColor;

    public WordBox(VisualTreeAsset asset, VisualElement wordTooltip, Label word, Label notes, bool useHover)
    {
        Debug.Assert(asset != null);
        asset.CloneTree(this);

        Debug.Assert(wordTooltip != null);
        Debug.Assert(word        != null);
        Debug.Assert(notes       != null);

        this.wordTooltip = wordTooltip;
        this.word        = word;
        this.notes       = notes;

        originalColor = style.color;

        enableRichText       = true;
        parseEscapeSequences = true;
        AddToClassList("dialogue-text");

        if (useHover)
        {
            MetaHover();
        }
    }

    private void MetaHover()
    {
        RegisterCallback<PointerEnterEvent>(evt =>
        {
            Label target       = (Label) evt.target;
            target.style.color = new StyleColor(Color.red);
            ShowTooltip(name, evt.position, target.style.unityFontDefinition);
        });

        RegisterCallback<PointerMoveEvent>(evt =>
        {
            MoveTooltip(evt.position);
        });

        RegisterCallback<PointerLeaveEvent>(evt =>
        {
            Label target       = (Label) evt.target;
            target.style.color = originalColor;
            HideTooltip();
        });
        RegisterCallback<DetachFromPanelEvent>(evt =>
        {
            HideTooltip();
        });
    }

    private string GetPlayerNotes(string word)
    {
        PlayerController player = PlayerController.Instance;
        Dictionary dictionary   = player.dictionary;

        foreach (DictionaryEntry entry in dictionary.dictionaryList) 
        {
            if (LanguageTable.PhoneticProcessor.Translate(entry.Word) == word) 
            {
                if (entry.Notes == "")
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
        word.text = name;
        word.style.unityFontDefinition = font;

        notes.text = GetPlayerNotes(name);

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

    private void HideTooltip()
    {
        wordTooltip.style.display = DisplayStyle.None;
    }
}