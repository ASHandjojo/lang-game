using UnityEngine;
using UnityEngine.UIElements;

public sealed class DialogueBox : VisualElement
{
    private float textSpeed;

    private VisualElement wordTooltip;

    private WordBox hoverPrefab, noHoverPrefab;

    public DialogueBox(VisualTreeAsset asset, Label dictWords, Label notes)
    {
        Debug.Assert(asset != null);

        wordTooltip   = this.Q("WordTooltip");
        hoverPrefab   = new WordBox(asset, wordTooltip, dictWords, notes, useHover: true);
        noHoverPrefab = new WordBox(asset, wordTooltip, dictWords, notes, useHover: false);
    }
}
