using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace LizardSpace
{
    public class TongueChargeState : LizardState, HeadState, BodyState
    {
        HeadController head;
        BodyController body;

        bool fire = false;
        bool cancel = false;


        float bornTime;



        public HeadController getHeadController()
        {
            return head;
        }


        public BodyController getBodyController()
        {
            return body;
        }


        public TongueChargeState(LizardController c) : base(c)
        {
            controller = c;
            head = c.head;
            body = c.body;
        }
        public TongueChargeState(LizardController c, Vector2 initial) : base(c)
        {
            controller = c;
            head = c.head;
            body = c.body;
        }




        public override void OnStateEnter()
        {
            base.OnStateEnter();
            InputHandler.instance.ReleaseTongueInput += HandleFireInput;
            InputHandler.instance.CancelAimTongueInput += HandleCancelInput;
            bornTime = Time.time;
            if(head.arrow)
                head.arrow.enabled = true;

            head.SetLookPosition(head.transform.position + controller.GetNeckOrientation(), true);

            TargettingReticle.target = null;

        }

        public override void OnStateExit()
        {
            base.OnStateExit();
            InputHandler.instance.ReleaseTongueInput -= HandleFireInput;
            InputHandler.instance.CancelAimTongueInput-= HandleCancelInput;
            if(head.arrow)
             head.arrow.enabled = false;
        }



        public void HandleFireInput()
        {
            fire = true;
            
        }
        public void HandleCancelInput()
        {
            cancel = true;
        }

        public override void Tick(ref List<LizardState> resultStates)
        {
            
            Vector3 newDir = head.GetInputRotation(controller,0.5f);
            //Check how long we've been in this state to dertermine the range of the tongue
            float chargePercent = Mathf.Clamp((Time.time - bornTime) * head.rangeChargeRate, 0, 1);
            float range = head.minTongueRange + chargePercent * (head.maxTongueRange - head.minTongueRange);

            Tonguable aimAssistTarget = null;

            if(head.cameraTarget) head.cameraTarget.position = head.transform.position + newDir * range * head.chargeTongueCameraFollow;

            if(head.arrow)
                head.arrow.ChangeLength(range);

            if (head.aimAssist)
            {
                List<Collider> detected = MathUtility.ConeDetection(head.transform.position, controller.getTransformVector(head.transform), range, head.aimAssistHalfAngle, head.targetLayers, head.blockers);

                aimAssistTarget = head.FindBestTarget(detected, controller.getTransformVector(head.transform));

                if(aimAssistTarget) TargettingReticle.target = aimAssistTarget.transform;

                else TargettingReticle.target = null;
            }
           

            if (fire)
            {
                if (head.cameraTarget) head.OnTongueHit += StartCameraReturn;


                //Check for collisions so that the tongue doesn't run into something
                if (aimAssistTarget && head.aimAssist)
                {
                    //Here the target is a transform in case we need to home 
                    Transform target;

                    target = aimAssistTarget.transform;
                    //Set the target reticle

                    aimAssistTarget.OnTargeted();

                    resultStates.Add(new ShootState(controller, target, false));
                }
                else
                {
                    //Here the target is a Vector3 because we can't home if no target is detected
                    Vector3 target;


                    RaycastHit hit;
                    Physics.Raycast(head.transform.position, newDir, out hit, range, controller.ignore);
                    
                    if (hit.collider)
                    {
                        target = hit.point;
                    }
                    else target = head.transform.position + newDir * range;
                    //Reset target reticle if there is no target


                    resultStates.Add(new ShootState(controller, target, false));
                }
                
            }

            //Rotate teh head transform
            //head.RotateHead(newDir);
            head.SetLookPosition(head.transform.position + newDir, true);
            //DebugRay
            Debug.DrawRay(head.transform.position, newDir * range, Color.red);

            if (cancel)
            {
                if (head.cameraTarget) head.cameraTarget.localPosition = Vector3.zero;

                resultStates.Add( new HeadIdle(controller));
                resultStates.Add( new BodyIdle(controller));
            }

            base.Tick(ref resultStates);
        }


        public void StartCameraReturn()
        {
            head.StartCoroutine(ReturnCamera());
            head.OnTongueHit -= StartCameraReturn;
        }

        public IEnumerator ReturnCamera()
        {
            while(Vector3.Distance(head.cameraTarget.localPosition, Vector3.zero) > 0.01f)
            {
                head.cameraTarget.localPosition -= head.cameraTarget.localPosition * Time.deltaTime * head.returnSpeed * head.chargeTongueCameraFollow;

                yield return null;
            }


            yield return null;
        }
    }
}
