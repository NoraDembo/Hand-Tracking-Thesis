using Oculus.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectedHand : MonoBehaviour
{
    // the target position of the hand projection
    Transform projectionTarget;

    // display visualizations of rays and target collider
    [SerializeField] bool debugMode = false;

    // maximum interpolation for each xyz component beween eye direction vector and Meta interaction ray
    [SerializeField] Vector3 maxInterpolation = Vector3.zero;

    // distance (from body to hand) at which hand position starts to be projected outwards
    [SerializeField] float minGrabDistance = 0.25f;

    // distance (from body to hand) where maximum projection distance is reached
    [SerializeField] float maxGrabDistance = 0.5f;

    // maximum distance the pojected position can reach (if the physical hand is at maxGrabDistance, the projected position will be this much further)
    [SerializeField] float projectionRange = 5;

    // the transform that the Meta Ray Interaction bases its ray on
    [SerializeField] Transform pointerPose;

    // for visualization of rays and target collider
    MeshRenderer debugSphere;
    LineRenderer[] debugLines;

    // Start is called before the first frame update
    void Start()
    {
        // get reference to target transform
        projectionTarget = transform.GetChild(0);

        // get references to the debug displays
        debugSphere = GetComponentInChildren<MeshRenderer>();
        debugLines = GetComponentsInChildren<LineRenderer>();

        // (de)activate debug displays
        debugSphere.enabled = debugMode;
        foreach (LineRenderer line in debugLines)
        {
            line.enabled = debugMode;
        }
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
        Vector3 interpolatedDirection = new(interpolatedX, interpolatedY, interpolatedZ);

        // rotate to that direction
        transform.rotation = Quaternion.LookRotation(interpolatedDirection);

        // update projected hand position
        float projectedDistance = projectionRange * quadraticProjectionFactor;
        projectionTarget.localPosition = Vector3.forward * projectedDistance;
        projectionTarget.localScale = Vector3.one * quadraticProjectionFactor;

        // draw debug lines
        if (debugMode)
        {
            debugLines[0].SetPositions(new Vector3[] { Vector3.zero, projectionTarget.localPosition });
            debugLines[1].SetPositions(new Vector3[] { Vector3.zero, transform.InverseTransformDirection(eyeDirection) * projectedDistance });
            debugLines[2].SetPositions(new Vector3[] { Vector3.zero, transform.InverseTransformDirection(pointerPose.forward) * projectedDistance });
        }
    }

}
