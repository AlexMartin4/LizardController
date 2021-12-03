using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LizardSpace
{
    public class TailIdle : LizardState, TailState
    {
        TailController tail;
        Coroutine animation;

        bool locked;

        public override void OnStateEnter()
        {
            base.OnStateEnter();
            animation = tail.StartCoroutine(IdleAnim(tail));
        }

        public override void OnStateExit()
        {
            tail.StopCoroutine(animation);

            base.OnStateExit();

        }

        public TailController getTailController()
        {
            return tail;
        }

        public TailIdle(LizardController c) : base(c)
        {
            controller = c;
            tail = c.tail;
        }


        IEnumerator IdleAnim(TailController tail)
        {
            /*Here we do a bunch of animation stuff*/
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
              
                yield return null;
            }

        }

    }
}
