using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectedHand : MonoBehaviour
{

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

    [Tooltip("The transform that the Meta Ray Interaction bases its ray on")]
    [SerializeField] Transform pointerPose;

    [Tooltip("The layer mask used for ray collisions")]
    [SerializeField] LayerMask layerMask;

    // the target position of the hand projection
    Vector3 projectedPosition;

    // for visualization of rays
    LineRenderer debugLine;

    // Start is called before the first frame update
    void Start()
    {
        // (de)activate debug display
        debugLine = GetComponentInChildren<LineRenderer>();
        debugLine.enabled = debugMode;
    }


    // Update is called once per frame
    void Update()
    {

        // follow the position of the RayInteractor origin
        transform.position = pointerPose.position;

        // calculate projection values based on hand distance from the body
        // TODO: improve this?
        float horizontalDistance = new Vector3(transform.position.x-transform.parent.position.x, 0, transform.position.z - transform.parent.position.z).magnitude; // local position does not work because of possible head tilt
        float linearProjectionFactor = Mathf.Clamp01((horizontalDistance - minGrabDistance) / (maxGrabDistance - minGrabDistance));
        float quadraticProjectionFactor = Mathf.Pow(linearProjectionFactor, 2);

        // direction from center eye anchor through the hand ray origin
        Vector3 eyeDirection = (transform.position - transform.parent.position).normalized;

        // Interpolate between interactor direction and eye direction based on projection distance
        float interpolatedX = Mathf.Lerp(pointerPose.forward.x, eyeDirection.x, maxInterpolation.x * linearProjectionFactor);
        float interpolatedY = Mathf.Lerp(pointerPose.forward.y, eyeDirection.y, maxInterpolation.y * linearProjectionFactor);
        float interpolatedZ = Mathf.Lerp(pointerPose.forward.z, eyeDirection.z, maxInterpolation.z * linearProjectionFactor);
        Vector3 interpolatedDirection = new Vector3(interpolatedX, interpolatedY, interpolatedZ).normalized;

        // rotate to that direction
        transform.rotation = Quaternion.LookRotation(interpolatedDirection);

        // update projected hand position
        projectedPosition = transform.position + transform.forward * projectionRange * quadraticProjectionFactor;

        // select target window
        SelectWindow();

        // draw debug lines
        if (debugMode) DrawDebugLines();
    }

    void SelectWindow()
    {
        RaycastHit[] hits = Physics.RaycastAll(transform.position, transform.forward, projectionRange + 1, layerMask);
        if(hits.Length > 0)
        {
            // find the window along the ray that is closest to the projection target point and within minimum distance of the projected position
            InteractableWindow bestCandidate = null;
            float closestDistance = Mathf.Infinity;
            foreach (RaycastHit hit in hits)
            {
                float distance = (hit.point - projectedPosition).magnitude;
                if(distance < minSelectionDistance && distance < closestDistance)
                {
                    bestCandidate = hit.collider.GetComponent<InteractableWindow>();
                    closestDistance = distance;
                }
            }

            if(bestCandidate != null)
            {
                if(bestCandidate == TargetWindow)
                {
                    // we are already targeting this so there is nothing to do
                    return;
                }
                else
                {
                    // remove the previous target's targeted state and set the best candidate as the current target
                    if(TargetWindow) TargetWindow.Targeted--;
                    TargetWindow = bestCandidate;
                    TargetWindow.Targeted++;
                    return;
                }         
            }
        }

        // no valid candidate was found, remove previous target's targeted state and reset target window
        if (TargetWindow) TargetWindow.Targeted--;
        TargetWindow = null;

    }

    public void DrawDebugLines()
    {
        debugLine.SetPositions(new Vector3[] { Vector3.zero, transform.InverseTransformPoint(projectedPosition) });
    }

}
