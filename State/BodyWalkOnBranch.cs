using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace LizardSpace
{

    //A state for walking on designated "tightrope" areas
    //This state could involve a blanace mechanic or simply lock the player to a certain direction
    //***UNIMPLEMENTED***
    public class BodyWalkOnBranch : LizardState, BodyState
    {
        BodyController body;
        Vector3 logForwardDirection;



        public BodyController getBodyController()
        {
            return body;
        }

        public BodyWalkOnBranch(LizardController c) : base(c)
        {
            body = c.body;
        }


        //This state can't be exited unless it does so itself
        public override bool CanExit()
        {
            return false;
        }

        public override void OnStateEnter()
        {
            base.OnStateEnter();
        }

        public override void OnStateExit()
        {
            base.OnStateExit();
        }


        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
