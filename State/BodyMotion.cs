using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LizardSpace
{
    public class BodyMotion : LizardState, BodyState
    {
        BodyController body;

        Coroutine groundCheck;
        Coroutine turnSpeedDecay;

        bool locked;

        float currentTurnSpeed;

        float nextTurnSpeed;

        public override void OnStateEnter()
        {
            base.OnStateEnter();
            groundCheck = body.StartCoroutine(body.GroundChecking());
            turnSpeedDecay = body.StartCoroutine(TurnSpeedDecay());
        }

        public override void OnStateExit()
        {
            base.OnStateExit();

            body.StopCoroutine(groundCheck);
            if (turnSpeedDecay != null) body.StopCoroutine(turnSpeedDecay);

            body.currentStartOffTurnSpeed = nextTurnSpeed;
            //Debug.Log("NexTurnSpeed: " + nextTurnSpeed);
        }

        public BodyMotion(LizardController c) : base(c)
        {
            controller = c;
            body = controller.body;
        }


        //Either exit the motion state or apply the move function
        public override void Tick(ref List<LizardState> resultStates)
        {
            if(InputHandler.instance.currentMoveInput == Vector2.zero)
            {
                resultStates.Add(new BodyIdle(controller));
                return;
            }
            else 
            {
                body.MoveFunction(InputHandler.instance.currentMoveInput, currentTurnSpeed); 
            }


            //Check for floor collision
            //--> if true enter a falling joint state
        }

        public BodyController getBodyController()
        {
            return body;
        }

        

        /// <summary>
        /// Rapidly slows turns speed upon entering the motion state
        /// Decreases the turn circle when the player moves off from idle
        /// </summary>
        /// <returns></returns>
        IEnumerator TurnSpeedDecay()
        {
            currentTurnSpeed = body.currentStartOffTurnSpeed;
            while (currentTurnSpeed >= body.maxTurningSpeed)
            {
                currentTurnSpeed -= body.startTurnDecayRate * Time.deltaTime;
                nextTurnSpeed = currentTurnSpeed;
                yield return null;
            }
           

            while(nextTurnSpeed < body.startOffTurnSpeed)
            {
                nextTurnSpeed += body.startTurnDecayRate*2 * Time.deltaTime;
                yield return null;
            }
            Debug.Log("Recharged");

            yield return null;
        }






    }
}
