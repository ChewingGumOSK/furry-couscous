using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterController))]

public class KamaraBasicWandererAI : MonoBehaviour {

    [SerializeField] float currentSpeed = 0f;
    [SerializeField] float currentRotateSpeed = 0.0F;
    [SerializeField] float thoughtInterval = 10f;
    [Range(4f, 8f)][SerializeField] float thoughtIntervalBase = 6f;
    [Range(0.5f, 1.9f)][SerializeField] float potentialSpeed = 1.2f;
    [Range(40f, 120f)][SerializeField] float potentialRotateSpeed = 80.0f;
    [Range(0.7f, 0.99f)][SerializeField] float targetedAngleToAnchor = 0.845f;
    [Range(1f, 19f)][SerializeField] float targetedMaxdistanceFromAnchor = 10f;
    [Range(1.6f, 3.6f)][SerializeField] float wallAvoidanceDistance = 2.6f;
    [Range(1.4f, 1.8f)][SerializeField] float tooSteepEdgeLimit = 1.6f;
    [Range(0.5f, 2.5f)][SerializeField] float gravityMultiplier = 1.5f;
    [Range(1, 3)][SerializeField]int animationStateChangeThreshold = 2;
    [SerializeField] bool setControllerForKamaraCharacters = true;

    private int animationOffStateTimer;
    private int animationOffRotationTimer;
    private int activeAnimationState = 0;
    private int activeAnimatorRotation = 0;
    private int movementTypeIndicator = 1;
    private float downVelocity;
    private float offGroundTimer;
    private float randomMultiplier;
    private float forwardRayHitDistance;
    private CharacterController myController;
    private Animator myAnimator;
    private Vector3 anchorPoint;
    private RaycastHit forwardRayHit;
    private bool directionOverride = false;
    private bool noForwardHit;

    void Start()
    {
        myAnimator = GetComponent<Animator>();
        myController = GetComponent<CharacterController>();
        InvokeRepeating("RandomizerandomMultiplier", 0, 6.0f);
        ThoughtUpdate();
        DropAnchor();

        if (setControllerForKamaraCharacters)
        {
            ConfigureCharacterController(myController);
        }
    }

    void Update()
    {
        Routine();
        ApplyMovement();
    }

    void FixedUpdate()
    {
        AnimationCaster();
    }

    void HandleGravity()
    {
        //Erases everything that would move character down if it touches the ground.
        if (myController.isGrounded)
        {
            offGroundTimer = 0;
            downVelocity = 0;
        }
        //Accelerates character towards ground when it does not touch the ground.
        else
        {
            offGroundTimer++;
            downVelocity -= 9.81f * gravityMultiplier * Time.deltaTime;
        }
    }

    void ApplyMovement()
    {
        //Everything that moves the character can be found here.
        myController.Move(myForward() * deltaSpeed() + new Vector3(0, downVelocity, 0));

        //Positive currentRotateSpeed rotates character to right and negative rotates it to left.
        transform.Rotate(0, (currentRotateSpeed * Time.deltaTime), 0);

        HandleGravity();
    }

    private Vector3 myForward()
    {
        //Forward direction from characters point of view.
        return transform.TransformDirection(Vector3.forward);
    }

    private float deltaSpeed()
    {
        //Returns frame rate independent speed.
        return currentSpeed * Time.deltaTime;
    }

    void Routine()
    {
        ForwardRay();
        //Removes directionOverride if way ahead is blocked.
        if (!noForwardHit)
        {
            if (forwardBlocked())
            {
                directionOverride = false;
            }
        }
        //Removes directionOverride if way ahead is too steep to walk.
        if (forwardTooSteep())
        {
            directionOverride = false;
        }

        //Removes directionOverride if character is facing away from anchorPoint.
        if (anchorsAngleIndicator() > 0)
        {
            if (targetedAngleToAnchor < anchorsAngleIndicator())
            {
                directionOverride = false;
            }
        }
        else
        {
            if (targetedAngleToAnchor > anchorsAngleIndicator())
            {
                directionOverride = false;
            }
        }

        //If character is close to anchor sets random movement type.
        if (distanceFromAnchor() < targetedMaxdistanceFromAnchor)
        {
            RandomMovementTypeSwitch();
            directionOverride = false;
        }
        else
        {
            //If character is far for anchor and facing towards anchor this sets it to walk about forward.
            if ((distanceFromAnchor() > targetedMaxdistanceFromAnchor) && (targetedAngleToAnchor < anchorsAngleIndicator()))
            {
                MovementAboutForward();
                //If way ahead is not blocked sets directionOverride to keep character walking towards anchor.
                if (noForwardHit)
                {
                    directionOverride = true;
                }
                else
                {
                    if (!forwardBlocked())
                    {
                        directionOverride = true;
                    }
                    else
                    {
                        directionOverride = false;
                    }
                }
            }
            //If character is far for anchor and facing away from anchor this sets it to rotate while walking.
            else
            {
                if (!directionOverride)
                {
                    MovementAboutRotational();
                }
            }
        }      
    }

    void DropAnchor()
    {
        //anchorPoint is the point at the center of area character wanders around.
        anchorPoint = transform.position;
    }

    private Vector3 myPosition()
    {
        return transform.position;
    }

    private Vector3 frontalPoint()
    {
        return myPosition() + myForward();
    }

    public float distanceFromAnchor()
    {
        return Vector3.Distance(frontalPoint(), anchorPoint);
    }

    private float anchorsAngleIndicator()
    {
        //Used to target towards anchor point.
        return Vector3.Distance(myPosition(), anchorPoint) - distanceFromAnchor();
    }

    void RandomMovementTypeSwitch()
    {
        //Sets random movement type.
        switch (movementTypeIndicator)
        {
            case 1:
                ClearMovement();
                break;
            case 2:
                MovementAboutForward();
                break;
            case 3:
                MovementAboutRotational();
                break;
            case 4:
                MovementRotationalOnSpot();
                break;
            default:
                break;
        }
    }

    void MovementRotationalOnSpot()
    {
        //Rotates character on spot.
        currentRotateSpeed = potentialRotateSpeed * randomMultipliersCharge() + (randomMultiplier / 10);
        currentSpeed = 0f;
    }

    void MovementAboutForward()
    {
        //Moves character about forward if possible.
        if (!forwardTooSteep())
        {
            if (!forwardBlocked())
            {
                currentSpeed = potentialSpeed;
                currentRotateSpeed = potentialRotateSpeed / 30 * randomMultiplier;
            }
            else
            {
                MovementAboutRotational();
            }
        }
        else
        {
            MovementAboutRotational();
        }
    }

    void MovementAboutRotational()
    {
        //Moves character about forward and turns it to left or right.
        if (!forwardTooSteep())
        {
            if (!forwardBlocked())
            {
                currentSpeed = potentialSpeed;
            }
            else
            {
                if (!noForwardHit)
                {
                    currentSpeed = 0;
                }
                else
                {
                    currentSpeed = potentialSpeed;
                }
            }
        }
        else
        {
            currentSpeed = 0;
        }

        currentRotateSpeed = potentialRotateSpeed * randomMultipliersCharge() + (randomMultiplier / 10);
    }

    void ClearMovement()
    {
        if (!directionOverride)
        {
            //Stops character.
            currentSpeed = 0;
            currentRotateSpeed = 0;
        }
    }

    void ForwardRay()
    {
        //Used to avoid walking into walls.
        Ray ForwardRay = new Ray(transform.position + transform.TransformDirection(Vector3.up), transform.forward * 10);

        if (Physics.Raycast(ForwardRay, out forwardRayHit, 1000))
        {
            forwardRayHitDistance = forwardRayHit.distance;
            noForwardHit = false;
        }
        else
        {
            noForwardHit = true;
        }
    }

    private bool forwardBlocked()
    {
        //Returns true if way forward is blocked.
        if (forwardRayHitDistance > wallAvoidanceDistance)
            return false;
        else
            return true;
    }

    private bool forwardTooSteep()
    {
        //Returns true if way forward is too steep.
        if (forwardDownRayHitDistance() < tooSteepEdgeLimit)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    private float forwardDownRayHitDistance()
    {
        //Used to avoid falling off edges.
        RaycastHit IdleDownHit;
        Ray ForwardDownRay = new Ray(transform.position + transform.forward + transform.TransformDirection(Vector3.up), transform.TransformDirection(Vector3.down) * 2);

        if (Physics.Raycast(ForwardDownRay, out IdleDownHit, 100))
        {
            return IdleDownHit.distance;
        }
        else
        {
            return 10f;
        }
    }

    void AnimationCaster()
    {
        //Updates animation parameters for animator.
        int state = 0;
        int newRotation = 0;

        if (currentSpeed != 0)
        {
            state = 1;
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

    void ThoughtUpdate()
    {
        //Updates characters movement type.
        movementTypeIndicator = Random.Range(1, 5);
        thoughtInterval = thoughtIntervalBase + randomMultiplier;
        Invoke("ThoughtUpdate", thoughtInterval);
    }

    private void RandomizerandomMultiplier()
    {
        //Gets a new value on which to base the pseudorandom elements of character movement.
        randomMultiplier = Random.Range(-1.0f, 1.0f);
    }

    private int randomMultipliersCharge() {
        //Returns positive or negative one depending on randomMultipliers charge.
        if (randomMultiplier > 0)
            return 1;
        else
            return -1;
    }

    void ConfigureCharacterController(CharacterController C)
    {
        //Configures CharacterController for Kamara Characters.
        C.center = new Vector3(0, 0.88f, 0);
        C.radius = 0.32f;
        C.height = 1.6f;
    }
}
