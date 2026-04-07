
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;


using UnityEngine;
using UnityEngine.UIElements;

using UnityEditor;
using UnityEditor.UIElements;

using Serialization;
using UnityEditor.Rendering;
using UnityEditor.Experimental.GraphView;
using UnityEngine.Rendering;
using System.Drawing.Printing;


[CustomPropertyDrawer(typeof(DialogueTreeList))]
internal sealed class DialogueTreeListDrawer : PropertyDrawer
{

    // all 3 of these lists will have the same size!

    // ids represents an id for a node
    private List<int> ids = new List<int>();

    // idxs represents an index for a node
    private List<int> idxs = new List<int>();

    // counts represents whether a node exists at a certain spot (mainly used for deleting unused indices)
    private List<bool> counts = new List<bool>();

    private bool verbose = false;

    // This function will take in the nodes array as a serialized property and return the index in which
    // node with id is or -1 if it does not exist
    private int GetIdxFromId(SerializedProperty array, int id)
    {
        int to_return = -1;
        for (int i = 0; i < array.arraySize; i++)
        {
            SerializedProperty node = array.GetArrayElementAtIndex(i);
            SerializedProperty node_id = node.FindPropertyRelative(nameof(DialogueTreeNode.NodeId));

            if (node_id.intValue == id)
            {
                to_return = i;
                break;
            }
        }
        return to_return;
    }

    // This function will check all success ids (outgoing edge) and map the success idx appropriately
    // It will also check all fail ids (outgoing edge) and map the fail idx appropriately
    // It will ensure also that every DialogueTreeNode has the correct parent ids and parent idxs array
    private int CheckAndSetNodeIdxs(SerializedProperty array)
    {
        int to_return = -1;
        for (int i = 0; i < array.arraySize; i++)
        {
            SerializedProperty node = array.GetArrayElementAtIndex(i);
            SerializedProperty node_id = node.FindPropertyRelative(nameof(DialogueTreeNode.NodeId));
            SerializedProperty type = node.FindPropertyRelative(nameof(DialogueTreeNode.Type));
            SerializedProperty succ_id = node.FindPropertyRelative(nameof(DialogueTreeNode.SuccId));
            SerializedProperty succ_idx = node.FindPropertyRelative(nameof(DialogueTreeNode.SuccIdx));
            SerializedProperty fail_id = node.FindPropertyRelative(nameof(DialogueTreeNode.FailId));
            SerializedProperty fail_idx = node.FindPropertyRelative(nameof(DialogueTreeNode.FailIdx));

            // This is for updating the success/fail ids to their indexes
            NodeType this_type = (NodeType) type.intValue;
            bool cond = (this_type & NodeType.Conditional) == NodeType.Conditional;
            if (cond || (this_type == NodeType.Default))
            {
                if (cond) {
                    int new_succ = GetIdxFromId(array, succ_id.intValue);
                    if (new_succ != -1)
                    {
                        to_return = 1;
                        succ_idx.intValue = new_succ;
                    } else
                    {
                        Debug.LogError("Success Node " + succ_id.intValue + " does not exist for node indexed at i: " + i);
                    }
                }

                int new_fail = GetIdxFromId(array, fail_id.intValue);  
                if (new_fail != -1)
                {
                    to_return = 1;
                    fail_idx.intValue = new_fail;
                } else
                {
                    Debug.LogError("Fail/Default Node " + fail_id.intValue + " does not exist for node indexed at i: " + i); 
                }
                
            }

            // This is for updating the idxs array
            int node_id_idx = GetIdxFromId(array, node_id.intValue);
            if (node_id_idx != -1)
            {
                if (verbose)
                {
                    Debug.Log("Changed Node Id " + node_id.intValue + " to be at index " + i);  
                }
                
                idxs[node_id_idx] = i;
            }
            
        }

        for (int i = 0; i < array.arraySize; i++)
        {
            SerializedProperty node = array.GetArrayElementAtIndex(i);
            SerializedProperty parIds = node.FindPropertyRelative(nameof(DialogueTreeNode.parentIds));
            SerializedProperty parIdxs = node.FindPropertyRelative(nameof(DialogueTreeNode.parentIdxs));
            //Debug.Log("Is element an array? " + (parIds.isArray));

            int to_go_to_size = parIds.arraySize;
            if (ids.Count > to_go_to_size)
            {
                to_go_to_size = ids.Count;
            }

            for (int j = 0; j < to_go_to_size; j++)
            {
                if (j < parIds.arraySize && j < ids.Count)
                {
                    // element is already allocated and is an element in ids list
                    SerializedProperty id_val = parIds.GetArrayElementAtIndex(j);
                    id_val.intValue = ids[j];
                    SerializedProperty idx_val = parIdxs.GetArrayElementAtIndex(j);
                    idx_val.intValue = idxs[j];
                } else if (j >= parIds.arraySize && j < ids.Count)
                {
                    // means we have to add a new element to the node array
                    parIds.InsertArrayElementAtIndex(j);
                    parIdxs.InsertArrayElementAtIndex(j);
                    SerializedProperty id_val = parIds.GetArrayElementAtIndex(j);
                    id_val.intValue = ids[j];
                    SerializedProperty idx_val = parIdxs.GetArrayElementAtIndex(j);
                    idx_val.intValue = idxs[j];
                } else if (j < parIds.arraySize && j >= ids.Count)
                {
                    // means we have to destroy the element in the node array
                    parIds.DeleteArrayElementAtIndex(j);
                    parIdxs.DeleteArrayElementAtIndex(j);
                    j--;
                    to_go_to_size--;
                } else
                {
                    break;
                }
            }


            
        }



        return to_return;
    }

    public void UpdateWholeArray(SerializedProperty nodes)
    {
        for (int j = 0; j < counts.Count; j++)
        {
            counts[j] = false;
        }

        if (verbose)
        {
          Debug.Log("Called Back");  
        }
        
        for (int i = 0; i < nodes.arraySize; i++)
        {
            SerializedProperty node = nodes.GetArrayElementAtIndex(i);
            SerializedProperty node_id = node.FindPropertyRelative(nameof(DialogueTreeNode.NodeId));
            if (ids.Contains(node_id.intValue))
            {
                int idx = ids.IndexOf(node_id.intValue);
                if (verbose)
                {
                    Debug.Log("Ids Size: " + ids.Count + "; Counts Size: " + counts.Count);
                    
                }
                
                if (counts[idx] == false)
                {
                    counts[idx] = true;
                } else
                {         
                    int random = UnityEngine.Random.Range(0, int.MaxValue);
                    if (verbose)
                    {
                        Debug.Log("New Id " + random + " at Index " + i);       
                    }
                    
                    node_id.intValue = random;
                    ids.Add(random);
                    counts.Add(true);
                    idxs.Add(i);
                }
                        
            } else
            {
                if (verbose)
                {
                    Debug.Log("Added ID " + node_id.intValue + " at Index " + i);                   
                }
                
                ids.Add(node_id.intValue);
                counts.Add(true);
                idxs.Add(i);
            }
        }


        for (int k = 0; k < counts.Count; k++)
        {
            if (counts[k] == false)
            {
                if (verbose)
                {
                    Debug.Log("Removed ID"); 
                }
                
                ids.RemoveAt(k);
                counts.RemoveAt(k);
                idxs.RemoveAt(k);
                k--;
            }
        }

        CheckAndSetNodeIdxs(nodes);
                
    }

    public override VisualElement CreatePropertyGUI(SerializedProperty property) 
    {
        VisualElement element = new();

        SerializedProperty condVal = property.FindPropertyRelative(nameof(DialogueTreeList.ConditionalValue));
        // condVal.
        // condVal.boolValue = true;
        // EditorUtility.SetDirty(property.serializedObject.targetObject);
        // property.serializedObject.ApplyModifiedProperties();

        PropertyField condValProp = new(condVal);
        element.Add(condValProp);

        SerializedProperty startId = property.FindPropertyRelative(nameof(DialogueTreeList.startingId));
        // startId.intValue = 55;
        // EditorUtility.SetDirty(property.serializedObject.targetObject);
        // property.serializedObject.ApplyModifiedProperties();
        IntegerField startIdProp = new();
        startIdProp.label = "Start Id";
        startIdProp.BindProperty(startId);
        element.Add(startIdProp);

        SerializedProperty startIdx = property.FindPropertyRelative(nameof(DialogueTreeList.startIdx));
        PropertyField startIdxProp = new(startIdx);
        startIdxProp.style.visibility = Visibility.Visible;
        startIdxProp.style.display = DisplayStyle.Flex;
        startIdxProp.enabledSelf = false;
        element.Add(startIdxProp);

        


        SerializedProperty currNode = property.FindPropertyRelative(nameof(DialogueTreeList.CurrNode));
        PropertyField currNodeProp = new(currNode);
        currNodeProp.style.visibility = Visibility.Hidden;
        currNodeProp.style.display = DisplayStyle.None;
        element.Add(currNodeProp);

        SerializedProperty numNodes = property.FindPropertyRelative(nameof(DialogueTreeList.NumNodes));
        IntegerField numNodesProp = new();
        numNodesProp.style.visibility = Visibility.Hidden;
        numNodesProp.style.display = DisplayStyle.None;
        numNodesProp.BindProperty(numNodes);
        element.Add(numNodesProp);


        SerializedProperty nodes = property.FindPropertyRelative(nameof(DialogueTreeList.Nodes));
        PropertyField nodesProp = new();
        nodesProp.BindProperty(nodes);
        element.Add(nodesProp);

        // This callback will detect a change in the nodes order (either insert, deletion, or rearrange of a new node)
        nodesProp.RegisterCallback((ChangeEvent<UnityEngine.Object> e) =>
        {
            
            if (counts.Count != nodes.arraySize)
            {
                if (verbose)
                {
                    Debug.Log("1Nodes Callback ; Arr Size: " + nodes.arraySize);  
                }
                
                numNodes.intValue = nodes.arraySize;
                EditorUtility.SetDirty(property.serializedObject.targetObject);
                property.serializedObject.ApplyModifiedProperties();
            } else
            {
                // This runs a lot but does not update new information!
                // May want to implement a timer to not update if it calls this again within 2 seconds or something
                // Couldn't figure out how to make that timer!
                UpdateWholeArray(nodes);
                EditorUtility.SetDirty(property.serializedObject.targetObject);
                property.serializedObject.ApplyModifiedProperties();
                if (verbose)
                {
                    Debug.Log("2Nodes Callback ; Arr Size: " + nodes.arraySize);
                }
                
            }
        });

        // This callback detects a change in the set num Nodes property
        // This will execute a change in either adding a node, or removing a node (ensuring all ids and idxs are 
        // updated appropriately)
        numNodesProp.RegisterCallback((ChangeEvent<int> e) =>
        {
            //Debug.Log("New E Value num Nodes: " + e.newValue);
            // if (counts.Count != e.newValue)
            // {
            UpdateWholeArray(nodes);
            EditorUtility.SetDirty(property.serializedObject.targetObject);
            property.serializedObject.ApplyModifiedProperties();
                
            // } 
        });

        
        // This ensures to set the value of the current array size 
        numNodes.intValue = nodes.arraySize;
        EditorUtility.SetDirty(property.serializedObject.targetObject);
        property.serializedObject.ApplyModifiedProperties();

        
        // This detects a change in the start Id property and will update its underlying index appropriately
        startIdProp.RegisterCallback((ChangeEvent<int> e) =>
        {
            int new_val = e.newValue;
            int idx = GetIdxFromId(nodes, new_val);
            bool found = idx != -1;
            Debug.Assert(found, "Error: starting id is not in Tree List!");
            if (found)
            {
                startIdx.intValue = idx;
            }
            EditorUtility.SetDirty(property.serializedObject.targetObject);
            property.serializedObject.ApplyModifiedProperties();
        });

        




        return element;
        
    }
}






[CustomPropertyDrawer(typeof(DialogueTreeNode))]
internal sealed class DialogueTreeNodeDrawer : PropertyDrawer
{

    public override VisualElement CreatePropertyGUI(SerializedProperty property) 
    {
        VisualElement element = new();

        SerializedProperty nodeIdProp = property.FindPropertyRelative(nameof(DialogueTreeNode.NodeId));
        PropertyField nodeIdProp2 = new(nodeIdProp);
        element.Add(nodeIdProp2);

        SerializedProperty nodeTypeProp = property.FindPropertyRelative(nameof(DialogueTreeNode.Type));
        EnumField nodeTypeProp2 = new();
        nodeTypeProp2.BindProperty(nodeTypeProp);
        element.Add(nodeTypeProp2);

        SerializedProperty succIdxp = property.FindPropertyRelative(nameof(DialogueTreeNode.SuccIdx));
        PropertyField succIdxProp = new(succIdxp);
        // succIdxProp.style.visibility = Visibility.Hidden;
        // succIdxProp.style.display = DisplayStyle.None;
        succIdxProp.enabledSelf = false;
        element.Add(succIdxProp);

        SerializedProperty failIdxp = property.FindPropertyRelative(nameof(DialogueTreeNode.FailIdx));
        PropertyField failIdxProp = new(failIdxp);
        // failIdxProp.style.visibility = Visibility.Hidden;
        // failIdxProp.style.display = DisplayStyle.None;
        failIdxProp.enabledSelf = false;
        element.Add(failIdxProp);

        SerializedProperty succIdp = property.FindPropertyRelative(nameof(DialogueTreeNode.SuccId));
        IntegerField succIdProp = new();
        succIdProp.label = "Succ Id";
        succIdProp.BindProperty(succIdp);
        element.Add(succIdProp);

        SerializedProperty failIdp = property.FindPropertyRelative(nameof(DialogueTreeNode.FailId));
        IntegerField failIdProp = new();
        failIdProp.label = "Fail Id";
        failIdProp.BindProperty(failIdp);
        element.Add(failIdProp);

        SerializedProperty parIds = property.FindPropertyRelative(nameof(DialogueTreeNode.parentIds));
        PropertyField parIdsProp = new(parIds);
        parIdsProp.style.visibility = Visibility.Hidden;
        parIdsProp.style.display = DisplayStyle.None;
        element.Add(parIdsProp);

        SerializedProperty parIdxs = property.FindPropertyRelative(nameof(DialogueTreeNode.parentIdxs));
        PropertyField parIdxsProp = new(parIdxs);
        parIdxsProp.style.visibility = Visibility.Hidden;
        parIdxsProp.style.display = DisplayStyle.None;
        element.Add(parIdxsProp);

        SerializedProperty vincTalk = property.FindPropertyRelative(nameof(DialogueTreeNode.VincentTalking));
        PropertyField vincTalkProp = new(vincTalk);
        element.Add(vincTalkProp);

        


        SerializedProperty entryp = property.FindPropertyRelative(nameof(DialogueTreeNode.Entry));
        PropertyField entryProp = new(entryp);
        element.Add(entryProp);

        SerializedProperty hasRes = entryp.FindPropertyRelative(nameof(DialogueTreeNode.Entry.hasResponse));

        // This will detect a change in the success id property and update its corresponding index
        succIdProp.RegisterCallback((ChangeEvent<int> e) =>
        {
            //Debug.Log("Array Size: " + parIds.arraySize);
            if (((NodeType)nodeTypeProp.intValue & NodeType.Conditional) != 0)
            {
                bool found = false;
                for (int i = 0; i < parIds.arraySize; i++)
                {
                    SerializedProperty ele = parIds.GetArrayElementAtIndex(i);
                    if (ele.intValue == e.newValue)
                    {
                        SerializedProperty eleIdx = parIdxs.GetArrayElementAtIndex(i);
                        // found the id
                        found = true;

                        // set the index appropriately
                        succIdxp.intValue = eleIdx.intValue;

                        break;
                    }
                }

                Debug.Assert(found, "Error: New Success Id " + e.newValue + " Does Not Exist + Node Type is " + nodeTypeProp.intValue);
                EditorUtility.SetDirty(property.serializedObject.targetObject);
                property.serializedObject.ApplyModifiedProperties();
            }
            
            
        });

        // This will detect a change in the fail/default id property and update its corresponding index
        failIdProp.RegisterCallback((ChangeEvent<int> e) =>
        {
            if (((NodeType)nodeTypeProp.intValue & NodeType.End) != NodeType.End)
            {
                bool found = false;
                for (int i = 0; i < parIds.arraySize; i++)
                {
                    SerializedProperty ele = parIds.GetArrayElementAtIndex(i);
                    if (ele.intValue == e.newValue)
                    {
                        SerializedProperty eleIdx = parIdxs.GetArrayElementAtIndex(i);
                        // found the id
                        found = true;

                        // set the index appropriately
                        failIdxp.intValue = eleIdx.intValue;

                        break;
                        
                    }
                }

                Debug.Assert(found, "Error: New Fail/Default Id " + e.newValue + " Does Not Exist + Node Type is " + nodeTypeProp.intValue);
                EditorUtility.SetDirty(property.serializedObject.targetObject);
                property.serializedObject.ApplyModifiedProperties();
            }  
        });


        // This detects a change in the node type property and will hide entries that don't need to be shown!
        nodeTypeProp2.RegisterCallback((ChangeEvent<Enum> e) =>
        {
            NodeType newtype = (NodeType) e.newValue;
            Visibility visibilityState1 = Visibility.Visible;
            DisplayStyle displayStyle1 = DisplayStyle.Flex;
            Visibility visibilityState2 = Visibility.Visible;
            DisplayStyle displayStyle2 = DisplayStyle.Flex;
            //Debug.Log("EnumType: " + nodeTypeProp.intValue);
            if (newtype == NodeType.Default || newtype == NodeType.End)
            {
                visibilityState1 = Visibility.Hidden;
                displayStyle1 = DisplayStyle.None;

                hasRes.boolValue = false;

                if (newtype == NodeType.End)
                {
                    visibilityState2 = Visibility.Hidden;
                    displayStyle2 = DisplayStyle.None;
                } else
                {
                    failIdProp.label = "GoTo Id";
                }

            } else
            {
                hasRes.boolValue = true;
                failIdProp.label = "Fail Id";
            }

            succIdProp.style.visibility = visibilityState1;
            succIdProp.style.display = displayStyle1;
            failIdProp.style.visibility = visibilityState2;
            failIdProp.style.display = displayStyle2;

            
            EditorUtility.SetDirty(property.serializedObject.targetObject);
            property.serializedObject.ApplyModifiedProperties();
        });


        return element;
        
    }
}
