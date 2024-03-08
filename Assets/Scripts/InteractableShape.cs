using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableShape : MonoBehaviour
{

    [SerializeField] AudioSource grab, release;

    Animator animator;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetHover(bool hovering)
    {
        animator.SetBool("Hover", hovering);
    }

    public void SetClick(bool clicking)
    {
        animator.SetBool("Click", clicking);

        if (clicking)
        {
            //grab.Play();
        }
        else
        {
            //release.Play();
        }
    }
}
