using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

[DisallowMultipleComponent]
public sealed class LanguageTable : MonoBehaviour
{
    public StandardSignTable signTable;
    public LigatureSub ligatureSub;

    public StandardSign[] StandardSigns => signTable.entries;
    public CompoundSign[] CompoundSigns => ligatureSub.entries;
}