using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LizardSpace
{


    public class DeathState : LizardState, HeadState, BodyState, TailState
    {

        HeadController head;
        BodyController body;
        TailController tail;


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

        public DeathState(LizardController c) : base(c)
        {
            head = c.head;
            body = c.body;
            tail = c.tail;
        }
        #endregion

        public override void OnStateEnter()
        {
            base.OnStateEnter();
            body.StartCoroutine(DeathAnim());

            controller.enabled = false;
        }


        IEnumerator DeathAnim()
        {

            Vector3 offsetX = body.transform.right * 0.5f;
            Vector3 offsetY = body.transform.up * 0.3f;


            body.MoveLegHomePosition(BodyController.Leg.BL, -offsetX + offsetY);
            body.MoveLegHomePosition(BodyController.Leg.FL, -offsetX + offsetY);

            body.MoveLegHomePosition(BodyController.Leg.BR, offsetX + offsetY);
            body.MoveLegHomePosition(BodyController.Leg.FR, offsetX + offsetY);

            body.FallToGround();


            SoundManager.instance.PlaySound("CHAM_DEATH", body.transform.position);

            MusicManager.StopMusic();

            yield return new WaitForSeconds(1f);

            SoundManager.instance.PlaySound("STING_FAIL");

            yield return new WaitForSeconds(1f);

            SceneManagement.ReloadLevel();
        }

    }
}
