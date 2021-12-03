using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace LizardSpace
{
    public class BodyIdle : LizardState, BodyState
    {
        BodyController body;
        Coroutine animation;

        bool locked;
        bool isMoving = false;


        public override void OnStateEnter()
        {
            base.OnStateEnter();
            animation = body.StartCoroutine(IdleAnim(body));
            
            
        }

        public void CheckMovement(Vector2 v)
        {

            if (v != Vector2.zero)
            {
                isMoving = true;
                Debug.Log("Moving!");
            }
            else
            {
                //isMoving = false;
            }
        }

        public override void OnStateExit()
        {
            body.StopCoroutine(animation);

            base.OnStateExit();
        }

        


        public BodyController getBodyController()
        {
            return body;
        }

        public BodyIdle(LizardController c) : base(c)
        {
            controller = c;
            body = c.body;
        }

        //The main point of this state is to exit when the input is pressed
        public override void Tick(ref List<LizardState> resultStates)
        {
            if(InputHandler.instance.currentMoveInput != Vector2.zero)
            {
                resultStates.Add(new BodyMotion(controller));
                return;
            }
        }


        


        IEnumerator IdleAnim(BodyController b)
        {
            //The original plan was to add an idle animation here
            /*Here's an example of an animation coroutine ---
             * 
             * while(head is not in starting position){
             *      get to starting pos
             *  }
             *  
             *  while(true){
             *      do animation
             *  }
             *  */

            while (true)
            {
                //Debug.Log("The body is immobile: " + serialNumber);
                yield return null;
            }

        }

    }
}

