using Oculus.Interaction;
using Oculus.Interaction.GrabAPI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class ProjectedHand : MonoBehaviour
{
    enum HandState
    {
        Open,       // Hand is completely open (target entire windows)
        Grabbing,   // Hand does a closed grab (interact with entire windows)
        Pointing,   // Hand is in preparing a pinch (target within windows)
        Pinching    // Hand does a closed pinch (interact within windows)
    }

    

    [Tooltip("Display visualizations of rays and target collider")]
    [SerializeField] bool debugMode = false;

    [Tooltip("Distance (from body to hand) at which hand position starts to be projected outwards")]
    [SerializeField] float minGrabDistance = 0.25f;

    [Tooltip("Distance (from body to hand) where maximum projection distance is reached")]
    [SerializeField] float maxGrabDistance = 0.5f;

    [Tooltip("Maximum distance the pojected position can reach (if the physical hand is at maxGrabDistance, the projected position will be this much further)")]
    [SerializeField] float projectionRange = 5;

    [Tooltip("The minimum distance from the projected position where a window will be considered for targeting")]
    [SerializeField] float maxSelectionDistance = 1;

    [Tooltip("The RayInteractor used for 'pinch' interactions as well as for calculating direction")]
    [SerializeField] RayInteractor rayInteractor;

    // the PointerPose calculates the ray origin and direction for the interactor
    Transform pointerPose;
    Transform rayInteractorVisuals;

    [Tooltip("Maximum interpolation weights for each xyz component beween eye direction vector and Meta interaction ray")]
    [SerializeField] Vector3 maxInterpolation = Vector3.zero;

    [Tooltip("GrabScore to consider the hand grabbing")]
    [SerializeField] float grabThreshold;

    [Tooltip("PinchScore to consider the hand pointing")]
    [SerializeField] float pointThreshold;

    

    [Tooltip("Hand grab API to determine hand states.")]
    [SerializeField] HandGrabAPI grabAPI;

    [Tooltip("The layer mask used for ray collisions")]
    [SerializeField] LayerMask layerMask;

    HandState handState = HandState.Open;

    // the target position of the hand projection
    [SerializeField] Transform projectedPosition;

    // the windows that are targeted or held by this projected hand or are in touch range
    InteractableWindow hoverTarget;
    InteractableWindow heldWindow;
    InteractableWindow touchedWindow;

    // the last known distance to a hover target
    float targetDistance = 0;

    // for visualization of rays and hand states
    LineRenderer[] debugLines;
    TextMeshPro[] debugText;

    


    // Start is called before the first frame update
    void Start()
    {
        // get reference to pointer pose and interactor visuals
        pointerPose = rayInteractor.GetComponentInChildren<HandPointerPose>().transform;
        rayInteractorVisuals = rayInteractor.transform.GetChild(2);

        // get reference to debug displays
        debugLines = GetComponentsInChildren<LineRenderer>();
        debugText = GetComponentsInChildren<TextMeshPro>();

    }


    void OnTriggerEnter(Collider other)
    {
        // if there is no touchedWindow yet, set the triggering window as touch window (ignore any additional triggers)
        // because we reach closer windows first, we will only be interacting with the frontmost window, even if nearby windows also touch the hand collider

        if(touchedWindow == null)
        {
            InteractableWindow triggerWindow = other.GetComponent<InteractableWindow>();
            touchedWindow = triggerWindow;
        }
        
    }

    void OnTriggerExit(Collider other)
    {
        // if the touchedWindow leaves the collider, reset touchedWindow
        InteractableWindow triggerWindow = other.GetComponent<InteractableWindow>();
        if(triggerWindow == touchedWindow)
        {
            touchedWindow = null;
        }
    }

    // Update is called once per frame
    void Update()
    {
        //show (or hide) debug displays
        ToggleDebugDisplay(debugMode);

        // follow the position of the RayInteractor origin
        transform.position = pointerPose.position;

        // Get hand projection based on distance from body
        float projectionStrength = ProjectHand();

        // Get Hand state (Open, Grabbing, Pointing or Pinching)
        handState = GetHandState();

        // get target window
        hoverTarget = GetTargetWindow(projectionStrength);

        switch (handState)
        {
            case HandState.Open:

                // release any held windows
                ReleaseWindow();

                // notify the hoverTarget that it is being targeted for grabbing
                if (hoverTarget != null) hoverTarget.Targeted = true;

                // disable ray interaction
                ToggleRayInteraction(false);               
                break;

            case HandState.Grabbing:

                // if there is a Hover Target and we are not yet holding anything else, pick it up
                // notably, this state does not look for new targets, so we won't accidentally pick things up when passing through them with a closed hand
                if(hoverTarget != null && heldWindow == null)
                {
                    heldWindow = hoverTarget;
                    // if the target is in touch range, attatch to hand, otherwise attach to projected position
                    if (hoverTarget == touchedWindow)
                    {
                        heldWindow.Grab(transform);
                    }
                    else
                    {
                        heldWindow.Grab(projectedPosition);
                    }
                }
                break;

            case HandState.Pointing:

                // enable ray interaction
                ToggleRayInteraction(true);

                break;

            case HandState.Pinching:
                
                // nothing to do?
                // we should always pass through the "pointing" state before we get here, so the RayInteractor should already be enabled.
                // not enabling it here again may prevent accidental shifts from grabbing directly to pinching

                break;

        }     
        
    }



    float ProjectHand()
    {
        
        // calculate projection values based on hand distance from the body
        // (TODO: improve this?)
        float horizontalDistance = new Vector3(transform.position.x - transform.parent.position.x, 0, transform.position.z - transform.parent.position.z).magnitude; // local position does not work because of possible head tilt
        float linearProjectionFactor = Mathf.Clamp01((horizontalDistance - minGrabDistance) / (maxGrabDistance - minGrabDistance));
        float quadraticProjectionFactor = Mathf.Pow(linearProjectionFactor, 2);

        // direction from center eye anchor through the hand ray origin (in local space)
        Vector3 eyeDirection = transform.InverseTransformDirection((transform.position - transform.parent.position).normalized);
        // Interactor ray direction (in local space)
        Vector3 rayDirection = transform.InverseTransformDirection(pointerPose.forward);

        // Interpolate between interactor direction and eye direction based on projection distance
        float interpolatedX = Mathf.Lerp(rayDirection.x, eyeDirection.x, maxInterpolation.x * linearProjectionFactor);
        float interpolatedY = Mathf.Lerp(rayDirection.y, eyeDirection.y, maxInterpolation.y * linearProjectionFactor);
        float interpolatedZ = Mathf.Lerp(rayDirection.z, eyeDirection.z, maxInterpolation.z * linearProjectionFactor);
        Vector3 interpolatedDirection = new Vector3(interpolatedX, interpolatedY, interpolatedZ).normalized;

        // rotate to that direction
        Vector3 globalDirection = transform.TransformDirection(interpolatedDirection);
        transform.rotation = Quaternion.LookRotation(globalDirection);

        // update projected hand position
        projectedPosition.localPosition = Vector3.forward * projectionRange * quadraticProjectionFactor;

        // Update Ray visualizations
        if (debugMode)
        {
            debugLines[0].SetPositions(new Vector3[] { Vector3.zero, projectedPosition.localPosition });
            //debugLines[1].SetPositions(new Vector3[] { Vector3.zero, transform.InverseTransformDirection(pointerPose.forward * projectionRange) });
        }


        // return current projection strength
        return quadraticProjectionFactor;
    }





    HandState GetHandState()
    {

        float grabScore = grabAPI.GetHandPalmScore(GrabbingRule.FullGrab);
        float pinchScore = grabAPI.GetHandPinchScore(GrabbingRule.DefaultPinchRule);

        // TODO: Improve this!

        HandState state;

        if(grabScore > grabThreshold)
        {
            state = HandState.Grabbing;
        }
        else if(pinchScore == 1 && grabScore < 0.1f)
        {
            // while doing a full hand grab, pinch score will usually reach 1 before our grab score reaches its own threshhold 
            // count it as a pinch only if grab score is still close to zero
            state = HandState.Pinching;
               
        }else if(pinchScore > pointThreshold && grabScore == 0)
        {
            state = HandState.Pointing;
        }
        else
        {
            state = HandState.Open;
        }


        if(debugMode)
        {
            debugText[0].text = "G: " + grabScore.ToString("0.00");
            debugText[1].text = "P: " + pinchScore.ToString("0.00");
            debugText[2].text = Enum.GetName(typeof(HandState), state);
        }

        return state;
        
    }





    // selects the best suitable target window based on hand projection
    InteractableWindow GetTargetWindow(float projectionStrength)
    {

        
        if (touchedWindow != null)
        {
            // if a window is in touch range, that window should be the target
            if (handState == HandState.Open)
            {
                // if the hand is open, select it
                return touchedWindow;
            }
            else
            {
                // if the hand is not open, keep the previous target window
                return hoverTarget;
            }
                
        }
        else
        {
            // otherwse, find target window based on raycast
            RaycastHit[] hits = Physics.RaycastAll(transform.position, transform.forward, projectionRange + 1, layerMask);

            InteractableWindow bestCandidate = null;

            if (handState == HandState.Open)
            {
                // "forget" previously known target distance since we are looking for new targets
                targetDistance = 0;

                // If the hand is open find the window along the ray that is closest to the projection target point and within maximum distance of the projected position
                float closestDistance = Mathf.Infinity;
                foreach (RaycastHit hit in hits)
                {
                    // distance from origin to projected position
                    float projectionDistance = projectedPosition.localPosition.magnitude;
                    // distance from origin to window hit
                    float hitDistance = transform.InverseTransformPoint(hit.point).magnitude;
                    // distance between hit position and projectedPosition (doing it this way instead of calculating the raw distance between points allows for negative values)
                    float hitDifference = hitDistance - projectionDistance;
                    float absDistance = Mathf.Abs(hitDifference);

                    // if this target is within distance of the projected position (scaled by projection strength) and closer to it than our previous best candidate, choose it
                    // if the projected position is behind a window, selectionDistance will be negative, i. e. windows in front of the projected position are always "in range" and reaching TOO far through a window is impossible
                    if (hitDifference < maxSelectionDistance * projectionStrength && absDistance < closestDistance)
                    {
                        bestCandidate = hit.collider.GetComponent<InteractableWindow>();
                        targetDistance = transform.InverseTransformPoint(hit.point).magnitude;
                        closestDistance = absDistance;
                    }
                }

            }
            else
            {
                // for all other hand states, keep the previous hover target and use the raycast only to find its distance (used for fading of foreground windows)
                bestCandidate = hoverTarget;

                foreach (RaycastHit hit in hits)
                {
                    InteractableWindow hitWindow = hit.collider.GetComponent<InteractableWindow>();
                    if(hitWindow == hoverTarget)
                    {
                        targetDistance = transform.InverseTransformPoint(hit.point).magnitude;
                        break;
                    }
                    
                }

                // notably, if this did not update targetDistance (because the hoverTarget was not any of the raycast hits), the previously known distance is still used
                // this can happen if the pointer moves off the target window during a pinch interaction, because the target will not be updated but the raycast may still hit other windows
                // this ensures any windows closer than the current target are still hidden for the duration of an interaction, even if we move off the target window during that time

            }


            // fade untargeted windows in front of the target
            FadeWindows(hits);

            return bestCandidate;
        }
    }




    // Fades or Hides all windows closer than the last known target distance, depending on hand state
    void FadeWindows(RaycastHit[] hits)
    {

        // fade all windows that are closer than the target window's distance
        foreach (RaycastHit hit in hits)
        {
            InteractableWindow hitWindow = hit.collider.GetComponent<InteractableWindow>();
            float hitDistance = transform.InverseTransformPoint(hit.point).magnitude;

            if (hitDistance < targetDistance)
            {
                // when pinching (interacting within a window) completely hide windows in front, otherwise fade them
                if (hoverTarget != null && (int) handState > 1)
                {
                    hitWindow.Hidden = true;
                }
                else
                {
                    hitWindow.Faded = true;
                }
            }
        }

    }



    // (de)activate the ray interaction
    void ToggleRayInteraction(bool enabled)
    {

        // we cannot simply disable the entire game object since we still need the pointerPose script to be active
        rayInteractor.enabled = enabled;
        rayInteractorVisuals.gameObject.SetActive(enabled);
        
    }



    // release any held window
    void ReleaseWindow()
    {
        if (heldWindow != null)
        {
            heldWindow.Release();
            heldWindow = null;
        }
    }




    void ToggleDebugDisplay(bool enabled)
    {
        foreach(TextMeshPro text in debugText)
        {
            text.enabled = enabled;
        }

        foreach (LineRenderer line in debugLines)
        {
            line.enabled = enabled;
        }
    }



}
