using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableWindow : MonoBehaviour
{
    // how many hands are targeting this
    public int Targeted { get; set; }
    public bool Faded { get; set; }

    Animator animator;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // LateUpdate so the hand interactors are done calculating their targets
    void LateUpdate()
    {
        animator.SetBool("Targeted", Targeted > 0);
        animator.SetBool("Faded", Faded);

        // reset Faded state (it will be reapplied in the next frame's Update if necessary)
        Faded = false;
    }

    public void PickUp(Transform grabbingPoint)
    {
        transform.parent = grabbingPoint;
    }

    public void Release()
    {
        transform.parent = null;
    }

}
