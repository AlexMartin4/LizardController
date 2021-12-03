using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LizardSpace
{
    public class LizardState :  StateInterface
    {
        public const int LIZARD_HEAD = 0;
        public const int LIZARD_BODY = 1;
        public const int LIZARD_TAIL = 2;
        public const int LIZARD_MAX = 3;

        [SerializeField]
        protected int priority;

        public int serialNumber;

        protected LizardController controller;

        public bool deleted = false;

        public enum BodyPart { Head, Body, Tail }
        public List<BodyPart> bodyParts;

        public int GetPriority()
        {
            return priority;
        }

        public virtual void Tick(ref List<LizardState> resultStates)
        {
            
        }


        public virtual void OnStateEnter()
        {
            controller.getReturnStates += Tick;
        }

        //Returns false if the state does not want to be exited
        public virtual void OnStateExit()
        {
            controller.getReturnStates -= Tick;
        }

        public virtual bool IsStateLocked()
        {
            return false;
        }


        //Potential alternative exit
        public virtual bool CanExit()
        {
            return true;
        }

        public LizardState(LizardController c)
        {
            controller = c;
            serialNumber = SerialNumberGenerator.serial;
            FillBodyPartList();
        }

        //THIS ISNT WORKING FIX ASAP
        void FillBodyPartList()
        {
            bodyParts = new List<BodyPart>();
            if (this is HeadState) bodyParts.Add(BodyPart.Head);
            if (this is BodyState) bodyParts.Add(BodyPart.Body);
            if (this is TailState) bodyParts.Add(BodyPart.Tail);
        }
    }

 

    public interface BodyPartController
    {
        LizardState getDefaultState(LizardController c);
    }

    public interface StateInterface {
        int GetPriority();
    }


    public interface HeadState : StateInterface
    {
        HeadController getHeadController();

    }
    public interface BodyState : StateInterface
    {
        BodyController getBodyController();
    }
    public interface TailState : StateInterface
    {
        TailController getTailController();
    }




}


