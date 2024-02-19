using Oculus.Interaction;
using Oculus.Interaction.GrabAPI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectedHand : MonoBehaviour
{
    [Tooltip("The target position of the hand projection")]
    public Transform projectedPosition;

    [Tooltip("The minimum distance from the projected position where a window will be considered for targeting")]
    [SerializeField] float maxSelectionDistance = 1;

    [Tooltip("Display visualizations of rays and target collider")]
    [SerializeField] bool debugMode = false;

    [Tooltip("Maximum interpolation weights for each xyz component beween eye direction vector and Meta interaction ray")]
    [SerializeField] Vector3 maxInterpolation = Vector3.zero;

    [Tooltip("Distance (from body to hand) at which hand position starts to be projected outwards")]
    [SerializeField] float minGrabDistance = 0.25f;

    [Tooltip("Distance (from body to hand) where maximum projection distance is reached")]
    [SerializeField] float maxGrabDistance = 0.5f;

    [Tooltip("Maximum distance the pojected position can reach (if the physical hand is at maxGrabDistance, the projected position will be this much further)")]
    [SerializeField] float projectionRange = 5;

    [Tooltip("The RayInteractor used for 'pinch' interactions as well as for caulculating direction")]
    [SerializeField] RayInteractor rayInteractor;
    // the PointerPose calculates the ray origin and direction for the interactor
    Transform pointerPose;

    [Tooltip("Hand grab API to determine hand states.")]
    [SerializeField] HandGrabAPI grabAPI;

    [Tooltip("The layer mask used for ray collisions")]
    [SerializeField] LayerMask layerMask;

    InteractableWindow hoverTarget;
    InteractableWindow heldWindow;


    // for visualization of rays
    LineRenderer[] debugLines;

    // Start is called before the first frame update
    void Start()
    {
        // get reference to pointer pose
        pointerPose = rayInteractor.GetComponentInChildren<HandPointerPose>().transform;

        // (de)activate debug display
        debugLines = GetComponentsInChildren<LineRenderer>();
        foreach(LineRenderer line in debugLines)
        {
            line.enabled = debugMode;
        }
    }


    // Update is called once per frame
    void Update()
    {
        // follow the position of the RayInteractor origin
        transform.position = pointerPose.position;

        float projectionStrength = ProjectHand();

        // get Raycast targets
        RaycastHit[] hits = Physics.RaycastAll(transform.position, transform.forward, projectionRange + 1, layerMask);

        if (!grabAPI.IsHandPalmGrabbing(GrabbingRule.DefaultPalmRule))
        {
            // if the hand is open, release any held windows
            if (heldWindow != null)
            {
                heldWindow.Release();
                heldWindow = null;
            }

            // set the best suitable window as target
            hoverTarget = GetTargetWindow(hits, projectionStrength);

        }else if(hoverTarget != null)
        {
            // this case only happens when we closed our fist while hovering over a suitable candidate (or we are already holding something)
            // otherwise, the target would have been set to null in the previous frame (this prevents picking up targets while passing through them with a closed fist)
            heldWindow = hoverTarget;
            heldWindow.Hold(projectedPosition);
        }

        // fade untargeted windows in front of the target
        FadeWindows(hits);
        
        // draw debug lines
        if (debugMode) DrawDebugLines();
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

        // return current projection strength
        return quadraticProjectionFactor;
    }


    // selects the best suitable target window based on hand projection
    InteractableWindow GetTargetWindow(RaycastHit[] hits, float projectionStrength)
    {

        // find the window along the ray that is closest to the projection target point and within minimum distance of the projected position
        InteractableWindow bestCandidate = null;
        float closestDistance = Mathf.Infinity;
        foreach (RaycastHit hit in hits)
        {
            float distance = (hit.point - projectedPosition.position).magnitude;
            // if this target is within distance of the projected position (scaled by projection strength) and closer to it than our previous best candidate, choose it
            if (distance < maxSelectionDistance*projectionStrength && distance < closestDistance)
            {
                bestCandidate = hit.collider.GetComponent<InteractableWindow>();
                closestDistance = distance;
            }
        }

        if (bestCandidate != null) bestCandidate.Targeted = true;

        return bestCandidate;
    }

    void FadeWindows(RaycastHit[] hits)
    {

        float targetDistance = 0;

        // get distance to selected window
        if(hoverTarget == null)
        {
            // if there is no current hover target, use projected hand position (held windows are always hover targets as well)
            targetDistance = projectedPosition.position.magnitude;
        }
        else
        {
            // find distance to the current hover target
            foreach(RaycastHit hit in hits)
            {
                InteractableWindow hitWindow = hit.collider.GetComponent<InteractableWindow>();
                if (hitWindow == hoverTarget)
                {
                    // distance = magnitude of position in local space
                    targetDistance = transform.InverseTransformPoint(hit.point).magnitude;
                }
            }
        }

        // fade all windows that are closer than that distance
        foreach(RaycastHit hit in hits)
        {
            InteractableWindow hitWindow = hit.collider.GetComponent<InteractableWindow>();
            float hitDistance = transform.InverseTransformPoint(hit.point).magnitude;

            if (hitDistance < targetDistance)
            {
                hitWindow.Faded = true;
            }
        }
    }

    public void DrawDebugLines()
    {
        if (grabAPI.IsHandPalmGrabbing(GrabbingRule.DefaultPalmRule))
        {
            debugLines[0].startColor = new Color(0, 1, 0, 0);
            debugLines[0].endColor = Color.green;
        }
        else if(grabAPI.IsHandPinchGrabbing(GrabbingRule.DefaultPinchRule))
        {
            debugLines[0].startColor = new Color(1, 1, 0, 0);
            debugLines[0].endColor = Color.yellow;
        }
        else
        {
            debugLines[0].startColor = new Color(1, 0, 0, 0);
            debugLines[0].endColor = Color.red;
        }

        debugLines[0].SetPositions(new Vector3[] { Vector3.zero, projectedPosition.localPosition });
        debugLines[1].SetPositions(new Vector3[] { Vector3.zero, transform.InverseTransformDirection(pointerPose.forward * projectionRange) });
    }

}
