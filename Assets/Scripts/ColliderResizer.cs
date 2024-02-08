using Oculus.Interaction.Surfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderResizer : MonoBehaviour
{

    BoundsClipper surfaceBounds;
    BoxCollider collisionBox;
    RectTransform canvasRect;

    // Start is called before the first frame update
    void Start()
    {
        surfaceBounds = GetComponentInChildren<BoundsClipper>();
        collisionBox = GetComponent<BoxCollider>();
        canvasRect = GetComponentInChildren<Canvas>().GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        // resize collider according to canvas size
        Vector3 boundingBoxSize = new Vector3(canvasRect.sizeDelta.x * canvasRect.localScale.x, canvasRect.sizeDelta.y * canvasRect.localScale.y, 1 * canvasRect.localScale.z);
        surfaceBounds.Size = boundingBoxSize;
        collisionBox.size = boundingBoxSize;
    }
}
