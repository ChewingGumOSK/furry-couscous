using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterController))]

public class KamaraBasicPlayerController : MonoBehaviour {

    [SerializeField] float currentSpeed = 0f;
    [SerializeField] float currentRotateSpeed = 0.0F;
    [Range(0.5f, 1.9f)][SerializeField] float potentialSpeed = 1.2f;
    [Range(40f, 120f)][SerializeField] float potentialRotateSpeed = 80.0f;
    [Range(0.5f, 2.5f)][SerializeField] float gravityMultiplier = 1.5f;
    [Range(3.0f, 4.2f)][SerializeField] float runspeedMultiplier = 3.6f;
    [Range(1, 3)][SerializeField]int animationStateChangeThreshold = 2;
    [SerializeField] private float downVelocity;

    private int animationOffStateTimer;
    private int animationOffRotationTimer;
    private int activeAnimationState = 0;
    private int activeAnimatorRotation = 0;
    private float randomMultiplier;
    private CharacterController myController;
    private Animator myAnimator;
    private bool jumping;

	private void Start () {
		myController = GetComponent<CharacterController>();
		myAnimator = GetComponent<Animator>();
		downVelocity = 0.0f;
	}
	
	private void Update () {
		HandleInput();
		ApplyMovement();
	}

	private void FixedUpdate() {
		AnimationCaster();
	}

	private void HandleInput() {
		if (Input.GetKey (KeyCode.W)) {

			currentSpeed = potentialSpeed;

			if (Input.GetKey (KeyCode.LeftShift)) {
				currentSpeed *= runspeedMultiplier;
			}
		} else if (Input.GetKey (KeyCode.S)) {
			currentSpeed = (-1) * potentialSpeed;
		} else {
			currentSpeed = 0.0f;
		}
		
		if (Input.GetKey (KeyCode.A)) {
			currentRotateSpeed = -potentialRotateSpeed;
			if (Input.GetKey (KeyCode.LeftShift)) {
				currentRotateSpeed *= (runspeedMultiplier / 2);
			}
		} else if (Input.GetKey (KeyCode.D)) {
			currentRotateSpeed = potentialRotateSpeed;
			if (Input.GetKey (KeyCode.LeftShift)) {
				currentRotateSpeed *= (runspeedMultiplier / 2);
			}
		}	else {
			currentRotateSpeed = 0;
		}

		if (Input.GetKeyDown (KeyCode.Space)) {
			jumping = true;
		} else {
			jumping = false;
		}
	}

    private float deltaSpeed()
    {
        //Returns frame rate independent speed.
        return currentSpeed * Time.deltaTime;
    }

    private Vector3 myForward()
    {
        //Forward direction from characters point of view.
        return transform.TransformDirection(Vector3.forward);
    }

    void ApplyMovement()
    {

        HandleGravity();

        //Everything that moves the character can be found here.
        myController.Move(myForward() * deltaSpeed() + new Vector3(0, downVelocity, 0) * Time.deltaTime);

        //Positive currentRotateSpeed rotates character to right and negative rotates it to left.
        transform.Rotate(0, (currentRotateSpeed * Time.deltaTime), 0);


    }

	private void HandleGravity()
    {
        //Erases everything that would move character down if it touches the ground.

        if (myController.isGrounded) {

            downVelocity = 0.0f;
            if (jumping) {
            	downVelocity = 5.0f;
            }
        }
        //Accelerates character towards ground when it does not touch the ground.
        downVelocity -= 9.81f * gravityMultiplier * Time.deltaTime;
    }

    void AnimationCaster()
    {
        //Updates animation parameters for animator.
        int state = 0;
        int newRotation = 0;

        if (Mathf.Abs(currentSpeed) == potentialSpeed)
        {
            if(currentSpeed > 0f) { 
                state = 1;
            }else
            {
                state = -1;
            }

        } else if (Mathf.Abs(currentSpeed) > potentialSpeed) {
        	state = 2;
        }
        if (!myController.isGrounded) {
            state = 3;
        }

        if (currentRotateSpeed > 10f)
        {
            newRotation = 1;
        }
        else if (currentRotateSpeed < -10f)
        {
            newRotation = -1;
        }
        else
        {
            newRotation = 0;
        }

        if (state != activeAnimationState)
        {
            animationOffStateTimer++;
        }
        else
        {
            animationOffStateTimer = 0;
        }

        if (newRotation != activeAnimatorRotation)
        {
            animationOffRotationTimer++;
        }
        else
        {
            animationOffRotationTimer = 0;
        }

        if (animationOffStateTimer > animationStateChangeThreshold)
        {
            animationOffStateTimer = 0;
            activeAnimationState = state;
            myAnimator.SetInteger("state", state);
        }

        if (animationOffRotationTimer > animationStateChangeThreshold)
        {
            animationOffRotationTimer = 0;
            activeAnimatorRotation = newRotation;
            myAnimator.SetInteger("rotation", newRotation);
        }
    }
}
