using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LizardSpace
{

    public class LizardSkeleton : MonoBehaviour
    {

        //There's actually no need for this to even be a mono behaviour 
        //this is just for testing purposes

        public SkeletonSubTree Head;
        public SkeletonSubTree Tail;
        public SkeletonSubTree Body;

        public SkeletonSubTree FrontRightLeg;
        public SkeletonSubTree FrontLeftLeg;
        public SkeletonSubTree AftRightLeg;
        public SkeletonSubTree AftLeftLeg;


        // Start is called before the first frame update
        void Start()
        {
            Head = makeSubtree(Head.joint);
            Tail = makeSubtree(Tail.joint);
            Body = makeSubtree(Body.joint);
            FrontLeftLeg = makeSubtree(FrontLeftLeg.joint);
            FrontRightLeg = makeSubtree(FrontRightLeg.joint);
            AftLeftLeg = makeSubtree(AftLeftLeg.joint);
            AftRightLeg = makeSubtree(AftRightLeg.joint);
        }

        // Update is called once per frame
        void Update()
        {

        }

        static public SkeletonSubTree makeSubtree(GameObject newJoint)
        {
            return new SkeletonSubTree(newJoint, newJoint.name);
        }
    }

    public class SkeletonSubTree
    {
        private string prefix;
        //**** Prefix convention ****//
        //Prefixes are corrolated with the name of the joints in the skeleton//
        //Example: "LizardHead" -- All Gameobjects with names starting with "LizardHead" (in a subtree!) will be added in a link list startign at subTrees


        //Nested prefixes, this allows for subtrees of a different name nested in themain subtree
        //Example: "LizardBody/LizardLeg" -- Gameobjects starting with "LizardLeg", childed to "LizardBody"s will also be added as subtrees


        public List<SkeletonSubTree> subTrees;
        public GameObject joint;





        public SkeletonSubTree(GameObject newJoint, string newPrefix)
        {
            joint = newJoint;
            prefix = newPrefix;
            subTrees = new List<SkeletonSubTree>();


            //Get all the individual nested prefixes
            //List<string> prefixes = new List<string>(prefix.Split(separator));


            for (int i = 0; i < joint.transform.childCount; i++)
            {
                GameObject child = joint.transform.GetChild(i).gameObject;

                if (child.name.Contains(prefix))
                {
                    subTrees.Add(new SkeletonSubTree(child, prefix));
                }

                //This was used for nested prefix but A: it doesn't work and B: not necessary
                //List through nested prefixes
                /*for(int j =0; j < prefixes.Count; j++)
                {

                    //Check if the gameObject's name contains the prefix
                    if (child.name.Contains(prefixes[j]))
                    {
                        //Create current remain prefixes list
                        string temp = "";
                        for(int ii = j; ii < prefixes.Count; ii++)
                        {
                            if (ii != j) temp += separator;
                            temp += prefixes[ii];

                        }


                        //Add new Subtree to the list
                        subTrees.Add(new SkeletonSubTree(child, temp, separator));
                    }

                }*/
            }
        }

    }
}
