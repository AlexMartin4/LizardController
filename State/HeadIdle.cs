using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LizardSpace
{
    public class HeadIdle : LizardState, HeadState
    {
        protected HeadController head;

        bool aiming = false;
        bool firing = false;


        Tonguable currentTarget;

        Coroutine leftEyeIdle;
        Coroutine rightEyeIdle;
        public override void OnStateEnter()
        {
            base.OnStateEnter();
            InputHandler.instance.AimTongueInput += HandleAimInput;
            InputHandler.instance.ShootTongueInput += HandleTongueInput;

            leftEyeIdle = head.StartCoroutine(EyeIdle(head.EyeL.transform, true));
            rightEyeIdle = head.StartCoroutine(EyeIdle(head.EyeR.transform, false));
        }

        public override void OnStateExit()
        {
            base.OnStateExit();
            InputHandler.instance.AimTongueInput -= HandleAimInput;
            InputHandler.instance.ShootTongueInput -= HandleTongueInput;

            head.StopCoroutine(leftEyeIdle);
            head.StopCoroutine(rightEyeIdle);
        }

        public virtual void HandleAimInput()
        {
            aiming = true;
        }

        public virtual void HandleTongueInput()
        {
            firing = true;
        }

        public override void Tick(ref List<LizardState> resultStates)
        {
            base.Tick(ref resultStates);


            currentTarget = head.AutoTargeting(controller);




            if (currentTarget != null)
            {
                head.SetLookPosition(currentTarget.transform.position, true);

                TargettingReticle.target = currentTarget.transform;
            }
            else
            {
                head.SetLookPosition(head.transform.position + controller.GetNeckOrientation(), false);

                TargettingReticle.target = null;
            }
            if (firing)
            {
                if (currentTarget)
                {
                    resultStates.Add(new ShootState(controller, currentTarget.transform.position, true));
                    return;
                }

                

                RaycastHit hit;
                Physics.Raycast(head.transform.position, head.transform.forward, out hit, head.defaultTongueRange, head.targetLayers);
                if (hit.collider)
                {
                    resultStates.Add(new ShootState(controller, hit.point, false));
                    return;
                }

                resultStates.Add(new ShootState(controller, head.transform.position + head.transform.forward*head.defaultTongueRange, false));
                return;
            }
            
            

            firing = false;

            if (aiming)
            {
                //Go into charging state
                resultStates.Add(new TongueChargeState(controller, InputHandler.instance.currentTongueInput));
            }

            
        }

        public IEnumerator EyeIdle(Transform eye, bool isLeftEye)
        {
            while (true)
            {
                float delay = Random.Range((1-head.eyeDelayError) * head.eyeRepositionDelay, (1+ head.eyeDelayError)*head.eyeRepositionDelay);

                Vector3 random = Random.insideUnitSphere.normalized;
                //Debug.DrawRay(eye.position, random, Color.green, delay);

                yield return new WaitForSeconds(delay);

                while(currentTarget != null)
                {
                    yield return null;
                }

                

                head.SetEyeTarget(isLeftEye, eye.position + random);

                

                yield return null;
            }
        }        

        public HeadController getHeadController()
        {
            return head;
        }

        public HeadIdle(LizardController c) : base(c)
        {
            controller = c;
            head = c.head;
        }


    }

    //This state exists after cancelling a fire command or after shooting 
    //The purpose of this is to force the player to reset the stick before firing again
    public class CancelledTongueState : HeadIdle
    {

        bool exit = false;
        Coroutine cooldown;
        float cooldownAmt;

        public override void OnStateEnter()
        {
            base.OnStateEnter();
            cooldown = head.StartCoroutine(TongueCooldown());
        }

        public CancelledTongueState(LizardController c, float amt) : base(c)
        {
            cooldownAmt = amt;
        }

        public override void Tick(ref List<LizardState> resultStates)
        {

            base.Tick(ref resultStates);

            if (exit)
            {
                resultStates.Add(new HeadIdle(controller));
            }
        }

        public override void HandleTongueInput()
        {
            //empty 
        }

        public override void HandleAimInput()
        {
            //empty
        }

        IEnumerator TongueCooldown()
        {
            yield return new WaitForSeconds(cooldownAmt);
            exit = true;
        }
    }
}
