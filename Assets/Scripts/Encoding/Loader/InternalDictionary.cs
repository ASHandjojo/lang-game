using System;
using Unity.Collections;

using UnityEngine;
using UnityEngine.UIElements;

[Serializable]
public struct DictEntry
{
    [Tooltip("The raw phonetics representation.")]
    public string rawString;
    [Tooltip("The processed unicode expression.")]
    public string unicodeString;
}

[CreateAssetMenu(menuName = "Linguistics/Internal Dictionary")]
public sealed class InternalDictionary : ScriptableObject
{
#if UNITY_EDITOR
    // Used to properly populate tables through a reference.
    // Editor mode LigatureSub table has references to also underlying StandardSignTable.
    [SerializeField] private LigatureSub ligatureSub;
#endif

    public DictEntry[] entries;
}