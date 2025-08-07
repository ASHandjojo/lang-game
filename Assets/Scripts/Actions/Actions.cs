using System;

using UnityEngine;

public static class Actions
{
    // Contextual actions 
    public static Action<PlayerController> OnInteract;
    public static Action OnSettingsMenuCalled;
    public static Action OnDictionaryMenuCalled;
    public static Action<PlayerController> OnClick;
}
