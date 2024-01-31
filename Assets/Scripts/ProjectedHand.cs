using Oculus.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectedHand : MonoBehaviour
{
    public float minGrabDistance = 0.25f;
    public float maxGrabDistance = 0.5f;
    public float projectionRange = 5;

    public float exponent = 2;

    // the RayInteractor this hand projection gets its direction from
    public RayInteractor rayInteractor;

    // the position that is used to calculate the hands distance from the body. Should be approximately inside the users chest.
    public Transform heart;

    Transform sphere;
    LineRenderer laser;

    // Start is called before the first frame update
    void Start()
    {
        sphere = transform.GetChild(0);
        laser = GetComponentInChildren<LineRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        // TODO calculate distance
        float distanceFromHeart = (transform.position-heart.position).magnitude;
        float projectionFactor = Mathf.Pow(Mathf.Clamp01((distanceFromHeart - minGrabDistance) / (maxGrabDistance - minGrabDistance)), exponent);

        float projectedDistance = projectionFactor * projectionRange;

        Vector3 projectedPosition = transform.position + rayInteractor.Ray.direction * projectedDistance;

        sphere.position = projectedPosition;
        sphere.localScale = Vector3.one * projectionFactor;
        laser.SetPositions( new Vector3[] { Vector3.zero, transform.InverseTransformPoint(projectedPosition) });

    }
}
