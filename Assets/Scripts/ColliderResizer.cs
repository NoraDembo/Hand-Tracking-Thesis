using Oculus.Interaction.Surfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderResizer : MonoBehaviour
{

    [SerializeField] float padding = 500;

    BoundsClipper surfaceBounds;
    BoxCollider box;
    RectTransform canvasRect;

    // Start is called before the first frame update
    void Start()
    {
        surfaceBounds = GetComponentInChildren<BoundsClipper>();
        box = GetComponent<BoxCollider>();
        canvasRect = GetComponentInChildren<Canvas>().GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        // resize collider according to canvas size
        Vector3 boundingBoxSize = new Vector3(canvasRect.sizeDelta.x * canvasRect.localScale.x, canvasRect.sizeDelta.y * canvasRect.localScale.y, padding * canvasRect.localScale.z);
        surfaceBounds.Size = boundingBoxSize;
        box.size = boundingBoxSize;
    }
}
