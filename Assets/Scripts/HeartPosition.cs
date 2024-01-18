using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeartPosition : MonoBehaviour
{

    public float offset;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // position below  the center eye anchor (even if it gets tilted)
        transform.position = transform.parent.position + Vector3.down * offset;

        // TODO: maybe calculate additional horizontal offset based on head tilt
    }
}
