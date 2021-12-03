using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace LizardSpace {

   
    //REMINDER depending on how the skelton is set up I might have to change which way is "forward" for the bones! (Ctrl+ F "transform.righttransform.right")
    public class LizardController : MonoBehaviour
    {
        public static LizardController instance;

        public enum forwardDirection { X,Z};
        public forwardDirection forwardDir;

        //This is the current state of each body part of the lizard
        //Note: A state can appear twice or more if it is shared by multiple body parts (i.e. is a joint state)
        LizardState[] currentStates;


        //This is the list that is populated by the states' Tick function
        //Note: If a change does not want to change it won't add anything to this list
        List<LizardState> returnStates;

        List<LizardState> overrideStates;

        public delegate void ReturnStates(ref List<LizardState> statesList);
        public ReturnStates getReturnStates;

        [Header("Body Part Controllers")]
        public HeadController head;
        public BodyController body;
        public TailController tail;


        public LayerMask ignore;

        [Header("Debug")]
        public bool verbose;

        Rigidbody[] rigidbodies;

        LizardHealth _healthManager;
        public LizardHealth healthManager
        {
            get
            {
                if(_healthManager == null)
                {
                    _healthManager = GetComponent<LizardHealth>();
                    if(_healthManager == null)
                    {
                      _healthManager = gameObject.AddComponent<LizardHealth>();
                        _healthManager.maxHealth = 100;

                    }
                }
                return _healthManager;
            }
            private set
            {
                _healthManager = value;
            }
        }


        private void Awake()
        {
            currentStates = new LizardState[LizardState.LIZARD_MAX];
            returnStates = new List<LizardState>();
            healthManager = GetComponent<LizardHealth>();


            healthManager.OnDeath += Death;
            rigidbodies = GetComponentsInChildren<Rigidbody>();

            instance = this;

            head.controller = this;
            body.controller = this;
            tail.controller = this;

            Cinematics.ToggleControl += ToggleControl;
        }

        private void OnDestroy()
        {
            Cinematics.ToggleControl -= ToggleControl;
            instance = null;
        }


        private void FixedUpdate()
        {
            //Clear the return states
            returnStates = new List<LizardState>();
            //Override states are applied outside of the controller (e.g. cutscenes) 
            if(overrideStates != null && overrideStates.Count > 0)
            {
                returnStates = overrideStates;
                overrideStates = null;
            }
            //This is a delegate which collects return states from each state machine
            else if(getReturnStates != null) getReturnStates(ref returnStates);
            //Sort the return states by priority to handle conflicts
            if (returnStates.Count > 0) SortAndAssign();
            //Handle states blocked by other state machines
            HandleAbandonedStates();
        }

        void SortAndAssign()
        {
            //A new array of state pointers that way we can know if a 
            LizardState[] newStates = new LizardState[LizardState.LIZARD_MAX];

            

            //Order the states reurned by the update methods by order of priority
            returnStates = (returnStates.OrderByDescending(s => s.GetPriority())).ToList<LizardState>();


            foreach (LizardState returnedState in returnStates)
            {
                if(EvaluateState(returnedState, newStates))
                {
                    foreach(LizardState.BodyPart b in returnedState.bodyParts)
                    {
                        newStates[(int)b] = returnedState;
                    }
                }
            }

            //A list of the states to activate
            //Note: this is to avoid activating the same state twice (specifically in the case of joint states)
            HashSet<LizardState> toActivate = new HashSet<LizardState>(); 

            for(int i = 0; i < newStates.Length; i++)
            {
                if(newStates[i] != null)
                {
                    //Trigger the exit routine of the current state
                    //Note: If the exit function fails (for example if the state is locked) the state will remain unchanged
                    //Further Note: The first condition prevents joint states from being exited twice
                    if (!currentStates[i].IsStateLocked())
                    {
                        if (!currentStates[i].deleted)
                        {
                            //Exit the current state
                            currentStates[i].OnStateExit();

                            //Mark the current state for replacement
                            //Note: This is useful for when joint states are replaced       (specifically to reset newly abondoned body parts)
                            //Further Note: This isn't particularly elegant but hey
                            currentStates[i].deleted = true;

                        }
                        //Replace the targeted state for each relevant body part
                        currentStates[i] = newStates[i];

                        //Keep a list of states to activate so they only get activated once
                        toActivate.Add(currentStates[i]);
                    }

                    
                }
            }
            //Trigger the enter routine ONCE for every new state (because toActivate is a hash)
            foreach(LizardState s in toActivate)
            {
                s.OnStateEnter();
            }


            if(verbose)Debug.Log("Current States: " +
                "Head: " + currentStates[0] + "# " + currentStates[0].serialNumber + "\n" +
                "Body: " + currentStates[1] + "# " + currentStates[1].serialNumber + "\n"+
                "Tail: " + currentStates[2] + "# " + currentStates[2].serialNumber + "\n");
        }

        public void HandleAbandonedStates()
        {
            //Set defaults for abandoned states
            for (int i = 0; i < currentStates.Length; i++)
            {
                //Note: If the current state is deleted but not replaced that must mean that it was a previously a joint state --> reset to default
                if (currentStates[i] == null || currentStates[i].deleted)
                {
                    currentStates[i] = getDefault(i);
                    //Defaults are never joint states
                    currentStates[i].OnStateEnter();
                }
            }
        }


        //Check if the new state is taking the the place of a more important state
        public bool EvaluateState(LizardState s, LizardState[] newStates)
        {
            foreach (LizardState.BodyPart b in s.bodyParts)
            {
                if (newStates[(int)b] != null)
                {
                    return false;
                }
            }

            return true;
        }


        public void Death()
        {
            AddOverrideState(new DeathState(this));
            

            //Change to a death anim
            
        }

        void ResetLevel()
        {
            SceneManagement.ReloadLevel();
        }

        public void SetGravity(bool dir)
        {
            foreach(Rigidbody rb in rigidbodies)
            {
                rb.useGravity = dir;
            }
        }


        public LizardState getDefault(int index)
        {
            switch (index)
            {
                case LizardState.LIZARD_HEAD:

                    return head.getDefaultState(this);


                case LizardState.LIZARD_BODY:

                    return body.getDefaultState(this);


                case LizardState.LIZARD_TAIL:

                    return tail.getDefaultState(this);

            }

            return null;
        }

        //Changing the apparent forward vector makes this adaptable to different model constructions
        public Vector3 getTransformVector(Transform t)
        {
            if(forwardDir == forwardDirection.X)
            {
                return t.right;
            }

            if(forwardDir  == forwardDirection.Z)
            {
                return t.forward;
            }

            return Vector3.zero;
        }

        public Vector3 GetNeckOrientation()
        {
            return getTransformVector(body.bodyRoot.transform) ;
        }

        void ToggleControl(bool value)
        {
            this.enabled = value;
            healthManager.enabled = value;
        }

        void AddOverrideState(LizardState state)
        {
            if(overrideStates == null)
            {
                overrideStates = new List<LizardState>();
            }
            overrideStates.Add(state);
        }

    }
}

