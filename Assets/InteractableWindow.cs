using Oculus.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableWindow : MonoBehaviour
{
    
    public bool Targeted { get; set; }
    public bool Faded { get; set; }
    public bool Hidden { get; set; }

    Animator animator;
    CanvasGroup canvasGroup;
    RayInteractable rayInteractable;
    PointableCanvas pointableCanvas;
    Transform centerEyeAnchor;

    Vector3 previousGrabPosition = Vector3.zero;
    Transform grabTransform;
    [Tooltip("Distance the grabbing point needs to move before the window will follow it 1-1.")]
    [SerializeField] static float followThreshold = 1f;
    float accumulatedDistance = 0;
    bool grabbed = false;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        canvasGroup = GetComponentInChildren<CanvasGroup>();
        rayInteractable = GetComponentInChildren<RayInteractable>();
        pointableCanvas = GetComponentInChildren<PointableCanvas>();
        centerEyeAnchor = GameObject.Find("CenterEyeAnchor").transform;
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

        if(grabbed) FollowGrabPosition();
    }

    public void Grab(Transform grabPoint)
    {

        grabTransform = grabPoint;
        previousGrabPosition = grabTransform.position;
        grabbed = true;

    }

    void FollowGrabPosition()
    {

        // follow the grab point based on movement
        float frameDistance = (grabTransform.position - previousGrabPosition).magnitude;
        accumulatedDistance += frameDistance;
        transform.position = Vector3.Lerp(transform.position, grabTransform.position, accumulatedDistance / followThreshold);

        // rotate towards users face, also based on traveled distance
        Quaternion lookRotation = Quaternion.LookRotation(transform.position - centerEyeAnchor.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, accumulatedDistance / followThreshold);

        previousGrabPosition = grabTransform.position;
    }

    public void Release()
    {
        grabbed = false;
        accumulatedDistance = 0;
    }

}
