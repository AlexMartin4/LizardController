using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LizardSpace
{
    public class TailController : MonoBehaviour, BodyPartController
    {
        GameObject TailRoot;
        SkeletonSubTree Tail;

        [HideInInspector]
        public LizardController controller;

        private void Start()
        {
            if (TailRoot) Tail = LizardSkeleton.makeSubtree(TailRoot);
        }

        public LizardState getDefaultState(LizardController c)
        {
            return new TailIdle(c);
        }

    }
}

