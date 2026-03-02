using System;
using UnityEngine;
using UnityEngine.UIElements;


public enum NodeType : uint
{
    Default = 1,
    Conditional = 2,
    Binary = 4 | Conditional,
    Multiheaded = 8 | Conditional
    
}

[Serializable]
public struct DialogueTreeList
{
    // This will hold the first node in the tree list
    // This will help to know whether to go into the list or not
    [SerializeField] public DialogueTreeHead FirstNode;

    // This will hold all the other nodes in the list
    [SerializeField] public DialogueTreeNode[] OtherNodes;

    // This will be the current node in the list when traversing
    //   * It equals -1 if it is not being used 
    //   * It equals 0 if it is the first node
    //   * If it is greater than 0, then it is an index in the other_nodes list
    [SerializeField] public int CurrNode;
}

[Serializable]
public struct DialogueTreeHead
{
    

    // conditional value will have the value that should be compared with
    // to evaluate if this dialogue should be taken
    // it is -1 if no node exists
    [SerializeField] public int ConditionalValue;

    // This will hold the first node in the list's entry
    [SerializeField] public DialogueTreeNode Node;
}



// This will be a single Dialogue Tree node
// This will hold one line of dioluge and wil 
[Serializable]
public struct DialogueTreeNode
{

    // type will be set to Default if it should be the default branch of diologue
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

public class DialogueTree : MonoBehaviour
{

    
    // This will hold a list of diologue tree lists 
    [SerializeField] private DialogueTreeList[] NpcOptions;

    [SerializeField] private int TreeSize = -1;

    // This will be true if we are currently in a diologue list (to be traversed)
    // otherwise it will be false (signaling we need to choose a new diologue list to be in)
    private bool InDialogueList = false;

    // This will be the current list that the diologue is in
    private int CurrListIdx = -1;

    private int CurrTreeLength = -1;


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
            

        if (idx == 0)
        {
            // If the index of the node in the current tree is 0, then the list being used is the first node
            return 0;
        }

        // This checks the index to see if it is out of bounds
        Debug.Assert(idx < NpcOptions[CurrListIdx].OtherNodes.Length + 1, "Error: Current Dialogue Tree is trying to access a Node that is out of bounds: " + idx.ToString());

        // This will return the index which will be the exact node being used at the current!
        return idx;
        
    }

    private DialogueTreeNode? GetCurrentNode()
    {
        int err = AssertGetNodeIndex(); // gets the index or less than 0 if there is an error
        
        if (err < 0)
        { // on error, return null
            return null;
        }    

        if (err == 0)
        { // if index is 0, this refers to the first node
            return NpcOptions[CurrListIdx].FirstNode.Node;
        }

        // otherwise, subtract one in the otherNodes list!
        return NpcOptions[CurrListIdx].OtherNodes[err - 1];
    }


    // This will return the current entry of dialogue that will be said
    // or it will return null if there was an error
    public DialogueEntry? GetCurrentEntry()
    {
        DialogueTreeNode? curr_node = GetCurrentNode(); // get current node
        if (curr_node == null)
        { // if there is an error, also return null
            return null;
        }
        return ((DialogueTreeNode)curr_node).Entry; // return the entry at the current node
        
    }

    public bool NeedPlayerInput()
    {
        if (!InDialogueList)
        {
            return false;
        } // if we are not in dialogue, return false
        DialogueTreeNode? curr = GetCurrentNode(); // get the node
        if (curr == null)
        { // if there is an error getting the node, return false
            return false;
        }

        // otherwise, return whether the current node is a conditional
        return (((DialogueTreeNode)curr).Type & NodeType.Conditional) != 0; 
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

        DialogueTreeNode? curr = GetCurrentNode(); // get the current node
        if (curr == null)
        { // if there was an error getting the current node, return an error
            return -3;
        }


        if (!useTest && (((DialogueTreeNode)curr).Type & NodeType.Conditional) != 0)
        { // if we are specifying not to use the string but the node is a conditional
            return -1;
        } else if (useTest && ((((DialogueTreeNode)curr).Type & NodeType.Conditional) == 0))
        { // if we are specifying to use the string but the node is not a conditional
            return -1;
        }

        int to_go_to = ((DialogueTreeNode)curr).FailIdx; // by default goes to failure

        if (useTest && (testing == ((DialogueTreeNode)curr).Entry.responseData.expectedInput))
        { // if we are using the string and the string matches the expected data!
            //Debug.Log("Testing string was same as data");
           to_go_to = ((DialogueTreeNode)curr).SuccIdx;
        } 

        //Debug.Log("Going to" + to_go_to);

        if (to_go_to == CurrTreeLength )
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
        int empty = 0; // to check whether the list is empty or not
        if (NpcOptions[GetStartIdx].FirstNode.ConditionalValue != -1)
        {
            empty = 1;
        }
        CurrTreeLength = empty +  NpcOptions[GetStartIdx].OtherNodes.Length; // initialize current tree length
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
