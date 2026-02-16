using System;
using UnityEngine;
using UnityEngine.UIElements;


public enum HeadType
{
    Default,
    Conditional
    

}

[Serializable]
public struct DialogueTreeList
{
    // This will hold the first node in the tree list
    // This will help to know whether to go into the list or not
    public DialogueTreeHead FirstNode;

    // This will hold all the other nodes in the list
    public DialogueTreeNode[] OtherNodes;

    // This will be the current node in the list when traversing
    //   * It equals -1 if it is not being used 
    //   * It equals 0 if it is the first node
    //   * If it is greater than 0, then it is an index in the other_nodes list
    public int CurrNode;
}

[Serializable]
public struct DialogueTreeHead
{
    // type will be set to Default if it should be the default branch of diologue
    //      otherwise, type will be conditional (meaning it depends on some 
    //      condition to be chosen)
    public HeadType type; 

    // conditional value will have the value that should be compared with
    // to evaluate if this dialogue should be taken
    public int ConditionalValue;

    // This will hold the first node in the list's entry
    public DialogueTreeNode FirstNode;
}



// This will be a single Dialogue Tree node
// This will hold one line of dioluge and wil 
[Serializable]
public struct DialogueTreeNode
{
    

    // This will hold the index of the next dialogue in the list if successful
    // This will hold -2 if the node will immediately go to the next one!
    public int SuccIdx;

    // This will hold the index of the next dialogue if the user fails
    // i.e. the default next value!
    public int FailIdx;


    // This will hold the actual dialogue entry in this Node!
    public DialogueEntry Entry;
}

public class DialogueTree : MonoBehaviour
{

    
    // This will hold a list of diologue tree lists 
    [SerializeField] private DialogueTreeList[] NpcOptions;

    private int TreeSize = -1;

    // This will be true if we are currently in a diologue list (to be traversed)
    // otherwise it will be false (signaling we need to choose a new diologue list to be in)
    private bool InDialogueList = false;

    // This will be the current list that the diologue is in
    private int CurrListIdx = -1;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        TreeSize = NpcOptions.Length;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // This will return whether we are in a current dialogue tree or not
    public bool InDialogue()
    {
        return InDialogueList;
    }


    // This will print out errors if the Dialogue Tree entries are set up wrong! Will be useful for debugging
    // Could be removed in release to make it run faster with err replaced with indx!
    private int CurrListErrors()
    {
        if (!InDialogueList)
        {
            // Means the DialogueTree is not in a specific list
            Debug.Log("Error: DialogueTree has no Current entry");
            return -1;
        } 
        
        if (CurrListIdx < 0 || CurrListIdx >= TreeSize)
        {
            // Means the DialogueTree index stored is out of bounds in the list of all npc Dialogue lists
            Debug.Log("Error: DialogueTree Index is out of bounds: " + CurrListIdx.ToString());
            return -1;
        } 

        // This is the index of the currently in use Dialogue tree list
        int idx = NpcOptions[CurrListIdx].CurrNode;
            
        if (idx < 0)
        {
            // If the index of the node in the current tree is less than -1, the list is not in use
            Debug.Log("Error: Current Dialogue Tree is not in any current Node");
            return -1;
        }

        if (idx == 0)
        {
            // If the index of the node in the current tree is 0, then the list being used is the first node
            return 0;
        }

        if (idx >= NpcOptions[CurrListIdx].OtherNodes.Length)
        {
            // This checks the index to see if it is out of bounds
            Debug.Log("Error: Current Dialogue Tree is trying to access a Node that is out of bounds: " + idx.ToString());
            return -1;
        }

        
        // This will return the index which will be the exact node being used at the current!
        return idx;
        
    }


    // This will return the current entry of dialogue that will be said
    // or it will return null if there was an error
    public DialogueEntry? GetCurrentEntry()
    {
        int err = CurrListErrors();
        if (err == -1)
        {
            return null;
        }    

        if (err == 0)
        {
            return NpcOptions[CurrListIdx].FirstNode.FirstNode.Entry;
        }

        return NpcOptions[CurrListIdx].OtherNodes[err - 1].Entry;
    }



}
