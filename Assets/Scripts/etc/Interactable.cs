using UnityEngine;

public class Interactable : MonoBehaviour
{
    public string interactName;

    public virtual void Interact()
    {
        Debug.Log(interactName + " 상호작용됨");
    }
}