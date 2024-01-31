using Oculus.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectedHand : MonoBehaviour
{
    public Transform projectedPosition;

    [SerializeField] bool debugMode = false;

    [SerializeField] Vector3 maxInterpolation = Vector3.zero;
    [SerializeField] float minGrabDistance = 0.25f;
    [SerializeField] float maxGrabDistance = 0.5f;
    [SerializeField] float projectionRange = 5;

    [SerializeField] float exponent = 2;

    // the RayInteractor that provideds position and direction
    [SerializeField] RayInteractor rayInteractor;

    MeshRenderer debugSphere;
    LineRenderer[] debugLines;

    // Start is called before the first frame update
    void Start()
    {
        debugSphere = GetComponentInChildren<MeshRenderer>();
        debugSphere.enabled = debugMode;

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
        Ray handRay = rayInteractor.Ray;
        transform.position = handRay.origin;

        // calculate projection distance
        // TODO: improve this?
        float horizontalDistance = new Vector3(transform.position.x-transform.parent.position.x, 0, transform.position.z - transform.parent.position.z).magnitude; // local position does not work because of possible head tilt
        float linearProjectionFactor = Mathf.Clamp01((horizontalDistance - minGrabDistance) / (maxGrabDistance - minGrabDistance));
        float quadraticProjectionFactor = Mathf.Pow(linearProjectionFactor, 2);
        float projectedDistance = quadraticProjectionFactor * projectionRange;

        // Interpolate direction between handRay and direction from eye through the hand based on projection distance
        Vector3 eyeDirection = (handRay.origin - transform.parent.position).normalized;
        float interpolatedX = Mathf.Lerp(handRay.direction.x, eyeDirection.x, maxInterpolation.x * linearProjectionFactor);
        float interpolatedY = Mathf.Lerp(handRay.direction.y, eyeDirection.y, maxInterpolation.y * linearProjectionFactor);
        float interpolatedZ = Mathf.Lerp(handRay.direction.z, eyeDirection.z, maxInterpolation.z * linearProjectionFactor);
        Vector3 interpolatedDirection = new Vector3(interpolatedX, interpolatedY, interpolatedZ);
        transform.rotation = Quaternion.LookRotation(interpolatedDirection);

        // update projected position
        projectedPosition.localPosition = Vector3.forward * projectedDistance;
        projectedPosition.localScale = Vector3.one * quadraticProjectionFactor;

        if (debugMode)
        {
            debugLines[0].SetPositions(new Vector3[] { Vector3.zero, Vector3.forward * projectedDistance });
            debugLines[1].SetPositions(new Vector3[] { Vector3.zero, transform.InverseTransformDirection(eyeDirection) * projectedDistance });
            debugLines[2].SetPositions(new Vector3[] { Vector3.zero, transform.InverseTransformDirection(handRay.direction) * projectedDistance });
        }
        

    }
}
