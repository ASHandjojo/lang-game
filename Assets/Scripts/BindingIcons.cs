using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BindingIcons", menuName = "Scriptable Objects/BindingIcons")]
public sealed class BindingIcons : ScriptableObject
{
    private readonly Dictionary<string, Sprite> icons = new();
    public IReadOnlyDictionary<string, Sprite> Icons => icons; 

    void OnEnable()
    {
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
