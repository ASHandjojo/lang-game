using UnityEngine;

public class AnswerChecker : MonoBehaviour
{
    public string correctAnswer = "UNITY";

    public void CheckAnswer(string input)
    {
        if (input.ToUpper().Trim() == correctAnswer)
        {
            Debug.Log("Correct!");
            // trigger correct animation or next step
        }
        else
        {
            Debug.Log($"Incorrect! Input Answer: {input}");
        }
    }
}