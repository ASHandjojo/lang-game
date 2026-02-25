using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BindingIcons", menuName = "Scriptable Objects/BindingIcons")]
public class BindingIcons : ScriptableObject
{
    public static BindingIcons Instance { get; private set; }

    private readonly Dictionary<string, Sprite> icons = new();
    public IReadOnlyDictionary<string, Sprite> Icons => icons; 

    private void OnEnable()
    {
        // this gets run by Unity because this is set as a pre-loaded asset
        Instance = this;

        Debug.Log("Loading Binding Icons");

        icons.Clear();

        for (char key = 'a'; key <= 'z'; key++)
        {
            icons.Add(
                "<Keyboard>/" + key,
                Resources.Load<Sprite>("Keys/" + key + "_light")
            );
        }

        Debug.Log(icons["<Keyboard>/a"]);
    }
}
