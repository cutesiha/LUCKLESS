using UnityEngine;

public class DialogueInput : MonoBehaviour
{
    public Typewriter typewriter;

    void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            typewriter.OnNextInput();
        }
    }
}