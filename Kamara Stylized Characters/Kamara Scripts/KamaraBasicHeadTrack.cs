using UnityEngine;
using System;
using System.Collections;

[RequireComponent(typeof(Animator))]

public class KamaraBasicHeadTrack : MonoBehaviour
{

    [Range(1f, 1.72f)][SerializeField] float targetHeadOffset = 1.36f;
    [Range(0f, 100f)][SerializeField] float chanceToTrackATarget = 50f;
    [Range(0.8f, 1.2f)][SerializeField] float shiftSpeedMult = 1f;
    [Range(4, 12)][SerializeField] int rayAmount = 8;
    [Range(-0.8f, -1.2f)][SerializeField] float scanHeightOffset = -1.0f;
    [Range(0.3f, 0.9f)][SerializeField] float offsetFromCenterMultiplier = 0.6f;
    [Range(1f, 4f)][SerializeField] float scanDistance = 2.5f;
    [Range(0.2f, 1f)][SerializeField] float maxLookAtWeight = 0.6f;

    protected Animator animator;
    private bool trackingActive = true;
    private Transform lookObj = null;
    private Transform closestPotentialLookObj = null;
    private int lookAtStage = 0;
    private Transform newTarget = null;
    private Transform oldTarget = null;
    private int shiftCounter = 1;
    private int trackingShiftCounter = 0;
    private Ray[] rays;
    private RaycastHit[] hits;
    private float[] hitDistances;

    void Start()
    {
        animator = GetComponent<Animator>();
        ToTrackOrNotToTrack();
    }

    void Update()
    {
        CastRayCircle();
        UpdateTrackedTarget();
    }

    //If chanceToTrackATarget is set lower than 100 this may randomly turn trackingActive to false
    void ToTrackOrNotToTrack()
    {
        float random = UnityEngine.Random.Range(0f, 100f);

        if (random < chanceToTrackATarget)
        {
            trackingActive = true;
            trackingShiftCounter = 0;
        }
        else
        {
            trackingActive = false;
        }

        Invoke("ToTrackOrNotToTrack", 5f + (random / 100));
    }

    //a callback for calculating IK
    void OnAnimatorIK()
    {
        if (animator)
        {
            if (trackingActive)
            { 
                if (closestPotentialLookObj != null)
                {
                    //lookAtStage is defined by functions it leads to here
                    switch (lookAtStage)
                    {
                        case 0:
                            LookAtNothing();
                            break;
                        case 1:
                            LookAtTarget();
                            break;
                        case 2:
                            ShiftFromNothingToTarget();
                            break;
                        case 3:
                            ShiftFromTargetToTarget();
                            break;
                        default:
                            break;
                    }
                }
            }
            //If tracking is not active shifts to look at nothing
            else
            {
                ShiftFromTargetToNothing();
            }
        }
    }

    //Looks at nothing unless closestPotentialLookObj is not null
    private void LookAtNothing()
    {
        animator.SetLookAtWeight(0);
        shiftCounter = 0;

        if (closestPotentialLookObj != null)
        {
            lookAtStage = 2;
            lookObj = closestPotentialLookObj;
        }
    }

    //Looks at a target until other potential target is found to be closer
    private void LookAtTarget()
    {
        shiftCounter = 0;
        animator.SetLookAtWeight(maxLookAtWeight);
        animator.SetLookAtPosition(lookObj.position + lookObjOffset(targetHeadOffset));

        if(closestPotentialLookObj != lookObj)
        {
            newTarget = closestPotentialLookObj;
            oldTarget = lookObj;
            lookAtStage = 3;
        }
    }

    //Shifts LookAtWeight from 0 to set max value
    private void ShiftFromNothingToTarget()
    {
        if (shiftCounter == 0)
        {
            animator.SetLookAtPosition(closestPotentialLookObj.position + lookObjOffset(targetHeadOffset));
            lookObj = closestPotentialLookObj;
        }

        if(shiftCounter <= 100)
        {

            float w = (shiftCounter * (shiftSpeedMult / 100)) * maxLookAtWeight;

            animator.SetLookAtWeight(w);

            shiftCounter++;
        }
        else
        {
            animator.SetLookAtWeight(maxLookAtWeight);
            lookAtStage = 1;
        }
    }

    //Shifts LookAtWeight from set max value to 0
    private void ShiftFromTargetToNothing()
    {

        if (trackingShiftCounter <= 100)
        {
            animator.SetLookAtWeight(((100f - trackingShiftCounter) * (shiftSpeedMult / 100)) * maxLookAtWeight);
            trackingShiftCounter++;
        }
        else
        {
            lookObj = null;
            lookAtStage = 0;
        }
    }

    //Linearly interpolates the position to look at between oldTarget and newTarget
    private void ShiftFromTargetToTarget()
    {
        animator.SetLookAtWeight(maxLookAtWeight);

        if (shiftCounter <= 100)
        {
            animator.SetLookAtPosition(Vector3.Lerp(oldTarget.position + lookObjOffset(targetHeadOffset), newTarget.position + lookObjOffset(targetHeadOffset), (float)shiftCounter/100));
            shiftCounter++;
        }
        else
        {
            lookObj = newTarget;
            shiftCounter = 0;
            lookAtStage = 1;
        }
    }

    //Casts a circle of rays which density depends on set rayAmount
    private void CastRayCircle()
    {

        rays = new Ray[rayAmount];
        hits = new RaycastHit[rayAmount];

        for (int i = 0; i < rayAmount; i++)
        {
            DrawRayOnCircle(i);
        }
    }

    //A single ray on the circle
    private void DrawRayOnCircle(int step)
    {

        rays[step] = new Ray(vectorOnCircle(step), outerEdgePoint(step));
        Physics.Raycast(rays[step], out hits[step]);

    }

    //Returns a point on circle around character
    private Vector3 vectorOnCircle(int step)
    {

        float myAngle = step * (360.0f / rayAmount) * Mathf.Deg2Rad;
        return scanStartHeight() + new Vector3(Mathf.Sin(myAngle), 0f, Mathf.Cos(myAngle)) * offsetFromCenterMultiplier;

    }

    //A point where rays end
    private Vector3 outerEdgePoint(int step)
    {
    
       float myAngle = step * (360.0f / rayAmount) * Mathf.Deg2Rad;
       return new Vector3(Mathf.Sin(myAngle), 0f, Mathf.Cos(myAngle)) * (offsetFromCenterMultiplier + scanDistance);
       
    }

    //Scan circle start height offset by scanHeightOffset
    private Vector3 scanStartHeight()
    {

        return transform.position + transform.TransformDirection(Vector3.down) * scanHeightOffset;

    }

    private Vector3 lookObjOffset(float targetHeadOffset)
    {
        return new Vector3(0f, targetHeadOffset, 0f);
    }

    //Updates closestPotentialLookObj
    private void UpdateTrackedTarget()
    {

        float shortest = offsetFromCenterMultiplier + scanDistance;

        for (int i = 0; i < rayAmount; i++)
        {
            if (hits[i].transform)
            {
                if (hits[i].transform.gameObject.GetComponent<KamaraBasicHeadTrack>())
                {
                    if (hits[i].distance < shortest)
                    {
                        shortest = hits[i].distance;
                        closestPotentialLookObj = hits[i].transform;
                    }
                }
            }
        }
    }
}
