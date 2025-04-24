using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PageContainer : MonoBehaviour
{
    private List<GameObject> wordList;
    private List<GameObject> definitionList;

    private int currPage;
    private bool isUnlocked;

    private bool nextAvailable, previousAvailable;


    public PageContainer(int currPage)
    {
        this.currPage = currPage;
    }

    void OnEnable()
    {
        wordList = new List<GameObject>();
        definitionList = new List<GameObject>();

        // check current page and populate words/definitions based on page index
        foreach(Transform child in this.gameObject.transform)
        {
            if(child.tag == "Tohgeri")
            {
                wordList.Add(child.gameObject);
            }
            else
            {
                definitionList.Add(child.gameObject);
            }
        }

    }

    public void TurnPage(string direction)
    {
        if(direction == null)
        {
            return;
        }

        else if(direction.Equals("Next") && nextAvailable)
        {
            currPage++;
        }
        else if(direction.Equals("Previous") && previousAvailable)
        {
            currPage--;
        }
    }

    private bool CheckAvailability()
    {
        int previous = currPage - 1;
        int next = currPage + 1;

        if(previous < 0)
        {
            return false;
        }

        return true;
    }

    public int GetCurrentPage()
    {
        return currPage;
    }

    public bool GetIsUnlocked()
    {
        return isUnlocked;
    }

}
