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
        Pinching    // Hand does a closed pinch (interact within windows)
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

                // if the index finger is "grabbing", enter the hand grabbing state
                // (basing this on the index finger only has proven to trigger very consistently when making a full fist, while not causing accidental acitvations for partially closed hands)
                if (grabAPI.IsHandPalmGrabbing(indexRule))
                {
                    newState = HandState.Grabbing;
                }
                else if (fingersScore > pinchingThreshold || pinchScore > 0)
                {
                    // if the hand is partially closed and it has been this way for longer than the grabTimer, enter pointing state
                    // this prevents accidental clicks when a "pinching" position is reached while going for a full grab, since the ray interactor will only be enabled once a pointing state is registered
                    if(grabTimer > grabTime)
                    {
                        newState = HandState.Pinching;
                    }
                    else
                    {
                        // else remain in open state and increment grab timer
                        grabTimer += Time.deltaTime;
                    }
                }
                else
                {
                    // if the hand is fully open, remain in open state and reset grab timer
                    grabTimer = 0;
                }
                break;

            case HandState.Grabbing:

                // only release the grabbing state if ALL fingers are released
                if (!grabAPI.IsHandPalmGrabbing(fingersRule)){
                    grabTimer = 0;
                    newState = HandState.Open;
                }
                break;

            case HandState.Pinching:


                if (grabAPI.IsHandPalmGrabbing(indexRule))
                {
                    // if the hand is grabbing, go to grabbing state
                    newState = HandState.Grabbing;
                }
                else if (fingersScore < pinchingThreshold && pinchScore == 0)
                {
                    // if fingers are fully open, go to open hand state
                    grabTimer = 0;
                    newState = HandState.Open;
                }else if (grabAPI.IsHandPinchGrabbing(pinchRule))
                {
                    // if fingers are pinching, go to pinching state
                    newState = HandState.Pinching;
                }

                // going from pointing to grabbing always requires opening the hand completely first to ensure it is a deliberate gesture

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
