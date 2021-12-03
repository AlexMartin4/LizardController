using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LizardSpace
{
    public class ShootState : LizardState, HeadState, BodyState
    {
        BodyController body;
        HeadController head;

        Coroutine tongueAnimation;

        Vector3 targetTonguePos;
        Transform targetTongueTransform;
        
        bool exit = false;
        bool tongueReturn = false;

        bool isHoming;

        LizardState returnState;

        public BodyController getBodyController()
        {
            return body;
        }

        public HeadController getHeadController()
        {
            return head;
        }
        #region constructors
        //Takes a transform because we have a target to home on
        public ShootState(LizardController c, Transform target, bool home) : base(c)
        {
            controller = c;
            body = c.body;
            head = c.head;
            targetTonguePos = target.position;
            priority = 100;
            isHoming = home;
            targetTongueTransform = target;
        }
        //Takes a target because we have no target to home on
        public ShootState(LizardController c, Vector3 target, bool home) : base(c)
        {
            controller = c;
            body = c.body;
            head = c.head;
            targetTonguePos = target;
            priority = 100;
            isHoming = home;
        }
        #endregion  

        public void HandleTongueCollisionDetected(Tonguable t)
        {
            if(t != null)
            {
                returnState = t.getTonguedState(controller);
            }
            tongueReturn = true;
        }


        public override void OnStateEnter()
        {
            base.OnStateEnter();
            tongueAnimation = head.StartCoroutine(ShootTongue());
            //Shoot();
            head.tongue.gameObject.SetActive(true);
            head.tongue.returnTrigger += HandleTongueCollisionDetected;

            TargettingReticle.target = null;

            SoundManager.instance.PlaySound("CHAM_TONGUE_OUT", head.transform.position);

            head.tongue.tongueRend.ResetRandomAtributes(Vector3.Distance(head.transform.position, targetTonguePos));


        }


        public override void OnStateExit()
        {
            base.OnStateExit();

            if (tongueAnimation != null) head.StopCoroutine(tongueAnimation);

            head.tongue.returnTrigger -= HandleTongueCollisionDetected;
            head.tongue.gameObject.SetActive(false);

            
        }

  

        public override void Tick(ref List<LizardState> resultStates)
        {
            //Send out the tongue
            base.Tick(ref resultStates);

            if (isHoming && targetTongueTransform) targetTonguePos = targetTongueTransform.position;

            if (returnState != null)
            {
                resultStates.Add(returnState);

                Debug.Log("Change state to grapple");
                return;
            }


            if (exit)
            {
                if (InputHandler.instance.isAiming == true)
                {
                    resultStates.Add(new TongueChargeState(controller));
                }
                else resultStates.Add(new BodyIdle(controller));

               resultStates.Add(new CancelledTongueState(controller, head.tongueCooldown));
            }
        }



        public IEnumerator ShootTongue()
        {

            exit = false;

            #region Send tongue

            Vector3 originalPos = head.tongue.tip.position;

            float currentDis = Vector3.Distance(head.tongue.tip.position, targetTonguePos);

            while (currentDis > 0.1f)
            {

                if (tongueReturn) break;

                currentDis = Vector3.Distance(head.tongue.tip.position, targetTonguePos);

                float mvt = Mathf.Clamp(head.tongueSpeed * Time.deltaTime, 0, currentDis);

                head.tongue.tip.position += mvt * (targetTonguePos - head.tongue.tip.position).normalized;

                yield return null;
            }

            #endregion

            head.tongue.tongueRend.Normalize();

            #region Return tongue


            
            currentDis = Vector3.Distance(head.tongue.tip.position, originalPos);

            if (head.OnTongueHit != null) head.OnTongueHit();

            bool once = true;

            while(currentDis > 0.01f)
            {
                currentDis = Vector3.Distance(head.tongue.tip.position, originalPos);

                float mvt = Mathf.Clamp(head.returnSpeed * Time.deltaTime, 0, currentDis);

                head.tongue.tip.position += mvt * (originalPos - head.tongue.tip.position).normalized;

                if (head.tongue.attachedTarget)
                {
                    head.tongue.attachedTarget.SetToShrink(currentDis / head.returnSpeed);
                }

                if(once && currentDis/head.returnSpeed < 0.3f)
                {
                    SoundManager.instance.PlaySound("CHAM_TONGUE_RETURN");
                    once = false;
                }
                

                yield return null;
            }

            #endregion




            head.tongue.DestroyTarget(controller);
            if (BodyController.dettachFromGrapple != null) BodyController.dettachFromGrapple();

            exit = true;
            yield return null;
        }

    }
}

