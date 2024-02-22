using Oculus.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableWindow : MonoBehaviour
{
    // how many hands are targeting this
    public bool Targeted { get; set; }
    public bool Faded { get; set; }
    public bool Hidden { get; set; }

    Animator animator;
    CanvasGroup canvasGroup;
    RayInteractable rayInteractable;
    PointableCanvas pointableCanvas;

    bool grabbed = false;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        canvasGroup = GetComponentInChildren<CanvasGroup>();
        rayInteractable = GetComponentInChildren<RayInteractable>();
        pointableCanvas = GetComponentInChildren<PointableCanvas>();
    }

    // LateUpdate so the hand interactors are done calculating their targets
    void LateUpdate()
    {

        animator.SetBool("Targeted", Targeted);
        animator.SetBool("Faded", Faded);
        animator.SetBool("Hidden", Hidden);
        animator.SetBool("Grabbed", grabbed);

        // disable window interactability when either faded or hidden
        canvasGroup.interactable = !(Faded || Hidden);
        canvasGroup.blocksRaycasts = !(Faded || Hidden);
        rayInteractable.enabled = !(Faded || Hidden);
        pointableCanvas.enabled = !(Faded || Hidden);

        // reset window states (they will be reapplied in the projectedHands Update() if appliccable)
        Targeted = false;
        Faded = false;
        Hidden = false;
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
