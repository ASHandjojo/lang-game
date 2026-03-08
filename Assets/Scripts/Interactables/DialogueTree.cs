using System;
using UnityEngine;
using UnityEngine.UIElements;

public enum NodeType : uint
{
    Default     = 1,
    Conditional = 2,
    // New node type added to be the end node (will only end at an end node)
    End         = 4  | Default,
    Binary      = 8  | Conditional,
    Multiheaded = 16 | Conditional
}

public enum TraverseStatus : uint
{
    Successful = 1,
    IsAtEnd    = 2 | Successful
}

[Serializable]
public struct DialogueTreeList
{
    // conditional value will have the value that should be compared with
    // to evaluate if this dialogue should be taken
    // it is -1 if no node exists
    [SerializeField] public int ConditionalValue;
    // The value of the starting id!
    [SerializeField] public int startingId;
    // This will hold all the nodes in the list
    [SerializeField] public DialogueTreeNode[] Nodes;
    // This will be the current node in the list when traversing
    //   * It equals -1 if it is not being used 
    //   * It equals 0 if it is the first node
    //   * If it is greater than 0, then it is an index in the other_nodes list
    [SerializeField] public int CurrNode;
}



// This will be a single Dialogue Tree node
// This will hold one line of dioluge and wil 
[Serializable]
public struct DialogueTreeNode
{
    // type will be set to Default if it should be the default branch of dialogue
    //      otherwise, type will be conditional (meaning it depends on some 
    //      condition to be chosen)
    [SerializeField] public NodeType Type;
    // This will hold the index of the next dialogue in the list if successful
    // This will hold -1 if the node will immediately go to the next one!
    // This will hold the size of the list if this transition ends the dialogue!
    [SerializeField] public int SuccIdx;
    // This will hold the index of the next dialogue if the user fails
    // i.e. the default next value!
    // This will hold the size of the list if this transition ends the dialogue!
    [SerializeField] public int FailIdx;
    // This will hold the actual dialogue entry in this Node!
    [SerializeField] public DialogueEntry Entry;
}

public sealed class DialogueTree : MonoBehaviour
{
    // This will hold a list of dialogue tree lists 
    [SerializeField] private DialogueTreeList[] NpcOptions;
    [SerializeField] private int TreeSize = -1;

    // This will be true if we are currently in a dialogue list (to be traversed)
    // otherwise it will be false (signaling we need to choose a new dialogue list to be in)
    private bool InDialogueList = false;

    // This will be the current list that the dialogue is in
    private int CurrListIdx    = -1;
    private int CurrTreeLength = -1;

    void Start()
    {
        TreeSize = NpcOptions.Length;
    }

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
    private int AssertGetNodeIndex()
    {
        // Means the DialogueTree is not in a specific list
        Debug.Assert(InDialogueList, "Error: DialogueTree has no Current entry");
        // Means the DialogueTree index stored is out of bounds in the list of all npc Dialogue lists
        Debug.Assert(CurrListIdx >= 0 && CurrListIdx < TreeSize, "Error: DialogueTree Index is out of bounds: " + CurrListIdx.ToString());
        // This is the index of the currently in use Dialogue tree list
        int idx = NpcOptions[CurrListIdx].CurrNode;
        // If the index of the node in the current tree is less than -1, the list is not in use
        Debug.Assert(idx >= 0, "Error: Current Dialogue Tree is not in any current Node");
        // This checks the index to see if it is out of bounds
        Debug.Assert(idx < NpcOptions[CurrListIdx].Nodes.Length, "Error: Current Dialogue Tree is trying to access a Node that is out of bounds: " + idx.ToString());
        // This will return the index which will be the exact node being used at the current!
        return idx;
        
    }

    public bool TryGetCurrentNode(out DialogueTreeNode node)
    {
        int index = AssertGetNodeIndex(); // gets the index or less than 0 if there is an error
        node = index switch
        {
            < 0 => default,
            >= 0 => NpcOptions[CurrListIdx].Nodes[index]
        };
        return index >= 0;
    }

    public bool NeedsPlayerInput() => InDialogueList && TryGetCurrentNode(out var curr) && (curr.Type & NodeType.Conditional) != 0;

    // This will return the current entry of dialogue that will be said
    // or it will return null if there was an error
    public bool TryGetCurrentEntry(out DialogueEntry entry)
    {
        if (TryGetCurrentNode(out DialogueTreeNode node))
        {
            entry = node.Entry;
            return true;
        }
        entry = default;
        return false;
    }

    // The function will return 1 if it has successfully incremented to the next dialogue
    // The function will return 0 if it has successfully incremented to the end of the dialogue
    // The function will return a number < 0 if it has unsuccessfully incremented to the en
    //          - returns -1 if useTest does not match testing or dialogue type
    //          - returns -2 if the player is not in any dialogue tree (use tree initializer function) 
    //          - returns -3 if the current node is not defined
    //          - returns -4 if the node to go to is out of bounds
    //          - returns () ...
    // testing is the string to test against and useTest specifies whether we should check against a string 
    // or ignore the string
    private int DialogueForwardWithText(string testing, bool useTest)
    {
        if (!InDialogueList) // if we are not in dialogue, we cannot continue
        {
            return -2;
        }

        bool hasCurrentNode = TryGetCurrentNode(out DialogueTreeNode curr); // get the current node
        if (!hasCurrentNode)
        { // if there was an error getting the current node, return an error
            return -3;
        }

        if (useTest ^ (curr.Type & NodeType.Conditional) != 0) // XOR op
        {
            return -1;
        }

        int to_go_to = curr.FailIdx; // by default goes to failure
        if (useTest && (testing == curr.Entry.responseData.line))
        { // if we are using the string and the string matches the expected data!
            //Debug.Log("Testing string was same as data");
           to_go_to = curr.SuccIdx;
        }

        if (curr.Type == NodeType.End)
        { // If the index of the next one is the end of the list, then we are officially done!
            //Debug.Log("Ending ha ha ha");
            NpcOptions[CurrListIdx].CurrNode = -1; // Set the current node to no current node
            InDialogueList = false; // Set that you are not in the dialogue anymore 
            CurrListIdx = -1; // Sets that we are not in any current list!
            return 0; // return that you hit the end
        }

        if (to_go_to < 0 || to_go_to >= CurrTreeLength)
        { // If where you are going to is out of bounds, return an error
            return -4;
        }

        NpcOptions[CurrListIdx].CurrNode = to_go_to; // If index is valid, then we will set this as our index
        return 1;  // return that you went to a next node   
    }

    // The function will return 1 if it has successfully incremented to the next dialogue
    // The function will return 0 if it has successfully incremented to the end of the dialogue
    // The function will return a number < 0 if it has unsuccessfully incremented to the next dialogue
    //          - returns -1 if dialogue option needs player input to move forward
    //          - returns -2 if the player is not in any dialogue tree (use tree initializer function) 
    //          - returns -3 if the current node is not defined
    //          - returns -4 if the node to go to is out of bounds
    //          - returns () ...
    public int DialogueForward()
    {
        return DialogueForwardWithText("", false); // call helper function noting you have no text input
    }

    // The function will return 1 if it has successfully incremented to the next dialogue
    // The function will return 0 if it has successfully incremented to the end of the dialogue
    // The function will return a number < 0 if it has unsuccessfully incremented to the next dialogue
    //          - returns -1 if dialogue option does not need player input to move forward
    //          - returns -2 if the player is not in any dialogue tree (use tree initializer function) 
    //          - returns -3 if the current node is not defined
    //          - returns -4 if the node to go to is out of bounds
    //          - returns () ...
    public int DialogueForward(string testingText)
    {
        return DialogueForwardWithText(testingText, true); // call helper function noting you do have text input
    }

    // The function will return 1 if it has successfully incremented to the next dialogue
    // The function will return a number < 0 if it has unsuccessfully incremented to the next dialogue
    //          - returns -1 if the dialogue index is out of bounds
    //          - returns -2 if the node to go to is out of bounds
    //          - returns () ...
    public int InitializeTree()
    {
        // TODO: will need to get some data from the player data to decide what tree to use 
        //       based on the player's current actions!
        int GetStartIdx = 0;

        if (GetStartIdx < 0 || GetStartIdx >= TreeSize)
        { // check the start index bounds and return -1 if there is an error
            return -1;
        }

        CurrListIdx = GetStartIdx;
        CurrTreeLength = NpcOptions[GetStartIdx].Nodes.Length; // initialize current tree length
        //Debug.Log("Current Tree Length" + CurrTreeLength);
        if (CurrTreeLength == 0)
        { // if no tree, then return an error
            return -2;
        }
        NpcOptions[GetStartIdx].CurrNode = 0; // otherwise your current node is the first node
        InDialogueList = true;
        return 1; // return success!

    }
}