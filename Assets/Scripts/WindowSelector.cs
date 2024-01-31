using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindowSelector : MonoBehaviour
{

    Animator animator;

    int collisions = 0;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        animator.SetBool("Highlighted", collisions > 0);
    }

    private void OnTriggerEnter(Collider other)
    {
        collisions++;
    }

    private void OnTriggerExit(Collider other)
    {
        Mathf.Clamp(--collisions, 0, int.MaxValue);
    }
}
