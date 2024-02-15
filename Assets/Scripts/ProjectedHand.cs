using Oculus.Interaction;
using Oculus.Interaction.GrabAPI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectedHand : MonoBehaviour
{
    [Tooltip("The target position of the hand projection")]
    public Transform projectedPosition;

    [Tooltip("The window that is being targeted by this hand")]
    public InteractableWindow TargetWindow { get; private set; }

    [Tooltip("The minimum distance from the projected position where a window will be considered for targeting")]
    [SerializeField] float minSelectionDistance = 1;

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
        // Update positions
        UpdatePosition();

        // get Raycast targets
        RaycastHit[] hits = Physics.RaycastAll(transform.position, transform.forward, projectionRange + 1, layerMask);

        if (!grabAPI.IsHandPalmGrabbing(GrabbingRule.DefaultPalmRule))
        {
            // if the hand is open, release any held windows
            if (TargetWindow != null) TargetWindow.Release();

            // set the best suitable window as target
            TargetWindow = GetTargetWindow(hits);

        }else if(TargetWindow != null)
        {
            // this case only happens when we closed our fist while hovering over a suitable candidate (or we are already holding something)
            // otherwise, the target would have been set to null in the previous frame (this prevents picking up targets while passing through them with a closed fist)
            TargetWindow.PickUp(projectedPosition);
        }

        // fade untargeted windows in front of the target
        FadeWindows(hits);
        
        // draw debug lines
        if (debugMode) DrawDebugLines();
    }

    void UpdatePosition()
    {
        // follow the position of the RayInteractor origin
        transform.position = pointerPose.position;

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
    }


    // selects the best suitable target window based on hand projection
    InteractableWindow GetTargetWindow(RaycastHit[] hits)
    {
        if(hits.Length > 0)
        {
            // find the window along the ray that is closest to the projection target point and within minimum distance of the projected position
            InteractableWindow bestCandidate = null;
            float closestDistance = Mathf.Infinity;
            foreach (RaycastHit hit in hits)
            {
                float distance = (hit.point - projectedPosition.position).magnitude;
                if(distance < minSelectionDistance && distance < closestDistance)
                {
                    bestCandidate = hit.collider.GetComponent<InteractableWindow>();
                    closestDistance = distance;
                }
            }

            // check if we found a suitable candidate
            if(bestCandidate != null)
            {
                if(bestCandidate == TargetWindow)
                {
                    // we are already targeting this so there is nothing to do
                    return bestCandidate;
                }
                else
                {
                    // remove the previous target's targeted state and set the best candidate as the current target
                    if(TargetWindow) TargetWindow.Targeted--;
                    bestCandidate.Targeted++;
                    return bestCandidate;
                }         
            }
        }

        // no valid candidate was found, remove previous target's targeted state and reset target window
        if (TargetWindow) TargetWindow.Targeted--;
        return null;

    }

    void FadeWindows(RaycastHit[] hits)
    {
        foreach(RaycastHit hit in hits)
        {
            InteractableWindow hitWindow = hit.collider.GetComponent<InteractableWindow>();

            // fade all targets in front of the target, use projected position if there is no current target
            float hitDistance = transform.TransformPoint(hit.point).magnitude;
            Vector3 targetPosition = TargetWindow == null ? projectedPosition.position : TargetWindow.transform.position;
            float targetDistance = transform.TransformPoint(targetPosition).magnitude; 

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
