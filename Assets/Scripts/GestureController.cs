using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.GrabAPI;
using TMPro;
using System;

public class GestureController : MonoBehaviour
{

    public enum HandState
    {
        Open,       // Hand is completely open (target entire windows)
        Grabbing,   // Hand does a closed grab (interact with entire windows)
        Pointing,   // Hand is partially close (target inside of windows)
        Pinching    // Hand is pinching (interact inside of windows)
    }

    [SerializeField] HandGrabAPI grabAPI;

    [Tooltip("The time required to enter a 'pointing' state from an open hand. Closing a fist any slower than this will not trigger a grab")]
    [SerializeField] float grabTime = 0.2f;
    [Tooltip("How much the fingers need to be closed in order to switch from open hand state to pointing state.")]
    [SerializeField] float pinchingThreshold = 0.5f;
    float grabTimer = 0;


    [SerializeField] GrabbingRule indexRule;
    [SerializeField] GrabbingRule fingersRule;
    [SerializeField] GrabbingRule pinchRule;

    [SerializeField] TextMeshPro handStateDisplay, indexScoreDisplay, fingersScoreDisplay, pinchScoreDisplay;

    HandState previousState = HandState.Open;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public HandState GetHandState()
    {
        
        float indexScore = grabAPI.GetHandPalmScore(indexRule);
        float fingersScore = grabAPI.GetHandPalmScore(fingersRule);
        float pinchScore = grabAPI.GetHandPinchScore(pinchRule);

        // rules for determining new hand states depend on what state we are currently in
        // default to previous state so only changes to new states need to be specified
        HandState newState = previousState;
        switch (previousState)
        {
            case HandState.Open:
            case HandState.Pointing:

                if (grabAPI.IsHandPalmGrabbing(indexRule))
                {
                    // if the index finger is "grabbing", enter the hand grabbing state
                    // (basing this on the index finger only has proven to trigger very consistently when making a full fist, while not causing accidental acitvations for partially closed hands)
                    newState = HandState.Grabbing;
                }
                else if (grabAPI.IsHandPinchGrabbing(pinchRule))
                {
                    if (grabTimer > grabTime)
                    {
                        // if the grab condition was not satisfied as well before the timer ran out, enter pinching state
                        newState = HandState.Pinching;
                    }
                    else
                    {
                        // else remain in this state and increment grab timer
                        grabTimer += Time.deltaTime;
                    }
                }
                else
                {
                    // if the hand is neither grabbing nor pinching, reset grab timer
                    grabTimer = 0;

                    if (grabAPI.IsHandPalmGrabbing(fingersRule) || pinchScore > pinchingThreshold)
                    {
                        // if the hand is partially closed, enter pointing state
                        newState = HandState.Pointing;
                    }
                    else
                    {
                        // else enter open state
                        newState = HandState.Open;
                    }
                }
                break;

            case HandState.Grabbing:

                // release grabbing state when index finger is performing neither a grab nor a pinch
                // pinch state will always be active during a grab as well, while the index palm grab has proven to be a bit finnicky depending on hand rotation (probably due to camera view angle of the tracking cameras)
                // making the release depend on a pinch release as well makes the grab state a lot more stable
                if (!grabAPI.IsHandPalmGrabbing(indexRule) && !grabAPI.IsHandPinchGrabbing(pinchRule)){
                    grabTimer = 0;
                    newState = HandState.Open;
                }
                break;

            

                if (grabAPI.IsHandPinchGrabbing(pinchRule))
                {
                    // if the pinch is complete, go to pinching state
                    newState = HandState.Pinching;
                }
                else if (grabAPI.IsHandPalmGrabbing(indexRule))
                {
                    // if the hand is grabbing, go to grabbing state
                    newState = HandState.Grabbing;
                }
                else if (!grabAPI.IsHandPalmGrabbing(fingersRule) && pinchScore < pinchingThreshold)
                {
                    // if fingers are fully open, go to open hand state
                    grabTimer = 0;
                    newState = HandState.Open;
                }

                break;
            case HandState.Pinching:


                if (!grabAPI.IsHandPinchGrabbing(pinchRule))
                {
                    // if the hand is no longer pinching, go back to pointing state
                    newState = HandState.Pointing;
                }

                break;
        }

        // update debug text
        handStateDisplay.text = Enum.GetName(typeof(HandState), newState);

        indexScoreDisplay.text = indexScore.ToString("0.00");
        indexScoreDisplay.color = grabAPI.IsHandPalmGrabbing(indexRule) ? Color.green : Color.white;

        fingersScoreDisplay.text = fingersScore.ToString("0.00");
        fingersScoreDisplay.color = grabAPI.IsHandPalmGrabbing(fingersRule) ? Color.green : Color.white;

        pinchScoreDisplay.text = pinchScore.ToString("0.00");
        pinchScoreDisplay.color = grabAPI.IsHandPinchGrabbing(pinchRule) ? Color.green : Color.white;


        previousState = newState;
        return newState;

    }
}
