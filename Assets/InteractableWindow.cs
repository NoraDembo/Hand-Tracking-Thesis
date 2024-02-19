using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableWindow : MonoBehaviour
{
    // how many hands are targeting this
    public bool Targeted { get; set; }
    public bool Faded { get; set; }

    Animator animator;

    bool grabbed = false;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // LateUpdate so the hand interactors are done calculating their targets
    void LateUpdate()
    {

        animator.SetBool("Targeted", Targeted);
        animator.SetBool("Faded", Faded);

        // reset window states (they will be reapplied in the projectedHands Update() if applicable)
        Targeted = false;
        Faded = false;
    }

    public void Hold(Transform grabbingPoint)
    {
        if (!grabbed)
        {
            // this is a new grab

            // set parent and lock to its position and rotation
            transform.SetParent(grabbingPoint);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            grabbed = true;
        }
        else
        {
            // this is a continued hold
        }

    }

    public void Release()
    {
        transform.parent = null;
        grabbed = false;
    }

}
