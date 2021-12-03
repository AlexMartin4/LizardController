using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LizardSpace
{


    public class GrapplingState : LizardState ,HeadState, BodyState, TailState
    {

        HeadController head;
        BodyController body;
        TailController tail;

        Transform target;



        Vector3 lastPosition;
        float stuckDetectorDelay;

        Coroutine stuckDetector;




        #region Constructor And Interface Implementation
        public HeadController getHeadController()
        {
            return head;
        }


        public BodyController getBodyController()
        {
            return body;
        }

        public TailController getTailController()
        {
            return tail;
        }

        public GrapplingState(LizardController c, Transform newTarget) : base(c)
        {
            head = c.head;
            body = c.body;
            tail = c.tail;

            target = newTarget;
        }
        #endregion

        public void HandleTakeDamage(float amt)
        {
            if(amt < 0)
            {
                //Cancel out of the fly state
            }
        }

        public override void OnStateEnter()
        {
            base.OnStateEnter();
            controller.healthManager.OnHealthChange += HandleTakeDamage;
            head.tongue.gameObject.SetActive(true);
            head.tongue.LatchTongue(target);
            controller.SetGravity(false);

            head.tongue.tongueRend.Normalize();
        }

        public override void OnStateExit()
        {
            base.OnStateExit();
            controller.healthManager.OnHealthChange -= HandleTakeDamage;
            head.tongue.gameObject.SetActive(false);
            head.tongue.unlatchTongue();
            controller.SetGravity(true);

            head.tongue.clearAttached();

            if (BodyController.dettachFromGrapple != null) BodyController.dettachFromGrapple();
        }

        public override void Tick(ref List<LizardState> resultStates)
        {
            base.Tick(ref resultStates);

            #region Stuck Detection
            /*if ((lastPosition - controller.transform.position).magnitude < 0.01f)
            {
                if (stuckDetector != null) controller.StopCoroutine(StuckDetector());
            }
            lastPosition = controller.transform.position;
            */
            #endregion

            Vector3 groundPos = new Vector3(target.position.x, body.transform.position.y, target.position.z);

            if (Vector3.Distance(body.transform.position, groundPos) > body.grappleDistance)
            {

                Vector3 lookDirection = controller.GetNeckOrientation();

                Vector3 flyDirection = target.position - body.transform.position;
                Vector3 flatFlyDirection = Vector3.ProjectOnPlane(flyDirection, Vector3.up).normalized;

                float TotalDiffAngle = Vector3.SignedAngle(lookDirection, flatFlyDirection, Vector3.up);

                //Rotate to match the max angular speed of the controller
                float ActualRotation = Mathf.Clamp(Time.fixedDeltaTime * head.headRotateSpeed, 0, Mathf.Abs(TotalDiffAngle)) * Mathf.Sign(TotalDiffAngle);

                
                //Rotate body to look toward the direction of travel
                body.transform.RotateAround(Vector3.up, ActualRotation * Mathf.Deg2Rad);

                //Rotate the head back 
                head.transform.RotateAround(Vector3.up, -ActualRotation * Mathf.Deg2Rad);

                //Align the joints to unfurl nicely
                body.AlignJoints(body.neckBone, 1- body.damping);
                

                body.transform.position += flatFlyDirection * Time.fixedDeltaTime * body.grappleSpeed;
                
            }
            else
            {
                resultStates.Add(new BodyIdle(controller));
                resultStates.Add(new HeadIdle(controller));
                resultStates.Add(new TailIdle(controller));

            }



        }

        public IEnumerator StuckDetector()
        {
            yield return null;
        }



    }
}
