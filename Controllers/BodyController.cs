using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LizardSpace
{
    //Handles operations relating to the body of the chameleon
    //States call on functions in this script
    public class BodyController : MonoBehaviour, BodyPartController
    {
        [Header("Movement Variables")]
        public float maxTurningSpeed;
        [Range(0, 1)]
        public float turnSpeedModifier;
        [Range(0, 2)]
        public float modifierSmoothing;

        public float startOffTurnSpeed;
        public float startTurnDecayRate;

        [HideInInspector]
        public float currentStartOffTurnSpeed;



        public float minSpeed;
        public float maxSpeed;
        //public float acceleration;

        public float damping = 0.01f;



        [Header("Walking on Branch Settings")]
        public float minBranchSpeed;
        public float maxBranchSpeed;


        [Header("Grappling Varaibles")]
        public float grappleSpeed;
        public float grappleDistance;


        [Header("Skeleton Refs")]
        public GameObject bodyRoot;

        public enum Leg{ FR, FL, BR, BL}

        [SerializeField]
        GameObject homeFrontLeftLeg;
        [SerializeField]
        GameObject homeFrontRightLeg;
        [SerializeField]
        GameObject homeBackLeftLeg;
        [SerializeField]
        GameObject homeBackRightLeg;

        [HideInInspector]
        public LizardController controller;

        [Header("GroundChecking")]

        public int groundedCutoff = 3;
        public int xCount = 3;
        public int yCount = 3;
        public float detectionLength = 1.5f;
        public LayerMask groundLayers;

        public bool showRays = false;

        public List<Collider> floorTouchingColliders;

        [Header("Other")]
        public GameObject deathParticles;

        Coroutine toppleCheck;


        SkeletonSubTree _neckBone;

        public delegate void SendSimple();
        public static SendSimple dettachFromGrapple;

        public SkeletonSubTree neckBone
        {
            get
            {
                return _neckBone;
            }
        }

        private void Awake()
        {
            _neckBone = LizardSkeleton.makeSubtree(bodyRoot);
            currentStartOffTurnSpeed = startOffTurnSpeed;

            ToggleToppleChecks(true);
            
        }


        public LizardState getDefaultState(LizardController c)
        {
            return new BodyIdle(c);
        }


        private void OnDestroy()
        {
            dettachFromGrapple = null;
        }




        //Function for moving the lizard character, take the input direction
        public void MoveFunction(Vector2 input, float turnSpeed)
        {
            if (input == Vector2.zero) return;

            //Get the direction of the neck joint
            Vector3 lookDirection = controller.GetNeckOrientation();

            //Change the analog "square" input into a "circle" input for homogenised response
            Vector2 smooth = MathUtility.UnitSquareToUnitCircle(input);
            Vector3 newDirection = new Vector3(smooth.x, 0, smooth.y);

            #region DebugRays
            //Draw the direction of the input and current alignment for debug purpoises
            //Debug.DrawLine(transform.position, transform.position + lookDirection * 10f, Color.blue);
            //Debug.DrawLine(transform.position, transform.position + newDirection * 10f, Color.green);
            #endregion

            float directionDot = Vector3.Dot(newDirection.normalized, lookDirection.normalized);
            float TotalDiffAngle = Vector3.SignedAngle(lookDirection, newDirection, Vector3.up);

            //If the angle difference is > 90, apply a turn modifier to make the turn circle smaller
            float modifier = Mathf.Pow(Mathf.Clamp(-directionDot, 0, 1), modifierSmoothing) *turnSpeedModifier;

            float finalTurnSpeed = turnSpeed* (modifier + 1);

            finalTurnSpeed = Mathf.Clamp(finalTurnSpeed, 0, startOffTurnSpeed);

            //Rotate to match the max angular speed of the controller
            float ActualRotation = Mathf.Clamp(Time.fixedDeltaTime * finalTurnSpeed, 0, Mathf.Abs(TotalDiffAngle)) * Mathf.Sign(TotalDiffAngle);

            //Now rotate the first joint aka neck joint
            RotateJoint(_neckBone, ActualRotation);

            //Align other joints with a recursive function
            AlignJoints(_neckBone, (1 - damping));


            //TODO: Make the movement speed acceleration based as well as rotation based
            
            //The chameleon has slower forward velocity if it is turning significantly.
            float speed = minSpeed + (directionDot + 1)  /2 * (maxSpeed - minSpeed);

            bodyRoot.transform.position += speed * (lookDirection * smooth.magnitude) * Time.fixedDeltaTime;


        }

        //Funtion for moving along a restricted axis
        // ***UNUSED***
        public void MoveOnBranch(Vector2 input, Vector3 branchForwardDirection)
        {
            if (input == Vector2.zero) return;

            Vector3 fwdDirection = Vector3.ProjectOnPlane(branchForwardDirection, Vector3.up);

            Vector3 lookDirection = controller.GetNeckOrientation();

            //Change the analog "square" input into a "circle" input for improved response
            Vector2 smooth = MathUtility.UnitSquareToUnitCircle(input);
            Vector3 newDirection = new Vector3(smooth.x, 0, smooth.y);


            //Walking in the branch is very similar to normal motion
            if (Vector3.Dot(newDirection, fwdDirection) > 0)
            {
                //Get the difference between the above directions
                float TotalDiffAngle = Vector3.SignedAngle(lookDirection, newDirection, Vector3.up);

                //Rotate to match the max angular speed of the controller
                float ActualRotation = Mathf.Clamp(Time.fixedDeltaTime * maxTurningSpeed, 0, Mathf.Abs(TotalDiffAngle)) * Mathf.Sign(TotalDiffAngle);

                //Now rotate the first joint aka neck joint
                RotateJoint(_neckBone, ActualRotation);

                //Align other joints with a recursive function
                AlignJoints(_neckBone, (1 - damping));


                //TODO: Make the movement speed acceleration based as well as rotation based
                float speed = minBranchSpeed + Vector3.Dot(newDirection.normalized, lookDirection.normalized) * (maxBranchSpeed - minBranchSpeed);

                bodyRoot.transform.position += speed * (lookDirection * smooth.magnitude) * Time.fixedDeltaTime;
            }
            else if(Vector3.Dot(newDirection, fwdDirection.normalized) < 0)
            {
                //implement walking backwards
            }


            //newDirection = fwdDirection.normalized * Vector3.Dot(newDirection, fwdDirection.normalized);
        }

        //A function that rotates a joint relatively to its children
        void RotateJoint(SkeletonSubTree target, float angle)
        {
            SkeletonSubTree child = target.subTrees[0];

            Vector3 temp = controller.getTransformVector(target.joint.transform);
            temp = controller.getTransformVector(child.joint.transform);

            //Rotate the target joint
            target.joint.transform.RotateAround(Vector3.up, angle * Mathf.Deg2Rad);


            temp = controller.getTransformVector(target.joint.transform);
            //Rotate the child back into place
            child.joint.transform.RotateAround(Vector3.up, -angle * Mathf.Deg2Rad);


            temp = controller.getTransformVector(child.joint.transform);

        }

        //The recursive variant of rotate joint with a damping factor
        public void AlignJoints(SkeletonSubTree root, float damping)
        {

            if (root.subTrees.Count == 0) return;
            SkeletonSubTree child = root.subTrees[0];



            float angle = Vector3.SignedAngle(controller.getTransformVector(child.joint.transform), controller.getTransformVector(root.joint.transform), Vector3.up);


            float actualAngle = angle * damping * Mathf.Deg2Rad;
            child.joint.transform.RotateAround(Vector3.up, actualAngle);

            SkeletonSubTree gChild = null;


            if (child.subTrees.Count > 0) gChild = child.subTrees[0];


            if (gChild != null)
                gChild.joint.transform.RotateAround(Vector3.up, -actualAngle);

            AlignJoints(child, damping);


        }

        //Recursive function for untoppling individual bones
        public void UntoppleBone(SkeletonSubTree root)
        {
            root.joint.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(root.joint.transform.forward, Vector3.up), Vector3.up);

            if(root.subTrees.Count > 0)
            {
                foreach(SkeletonSubTree child in root.subTrees)
                {
                    UntoppleBone(child);
                }
            }
        }

        //Only use this every so often to make sure no bone is toppled
        public bool FullToppleCheck(SkeletonSubTree root)
        {
            float angle = Vector3.SignedAngle(root.joint.transform.up, Vector3.up, root.joint.transform.forward);


            bool result = false;


            if (Mathf.Abs(angle) > 20f)
            {
                return true;
            }

            
            else
            {
                if (root.subTrees.Count > 0)
                {
                    foreach (SkeletonSubTree child in root.subTrees)
                    {
                        result = result || FullToppleCheck(child);
                    }

                    return result;
                }
                else return false;
            }
        }

        //The lightweight check that only looks at the neck bone
        public IEnumerator ToppleCheck()
        {
            float Timer = 1;

            while (true)
            {
                Timer -= Time.deltaTime;
                bool fullCheckResult = false;

                if(Timer < 0)
                {
                    Timer = 1;
                    fullCheckResult = FullToppleCheck(neckBone);
                    Debug.Log("Full Check executed with result: " + fullCheckResult);
                }

                float angle = Vector3.SignedAngle(transform.up, Vector3.up, transform.forward);

                if (Mathf.Abs(angle) > 5f || fullCheckResult)
                {
                    if (GroundCheck())
                    {
                        UntoppleBone(neckBone);
                        //transform.rotation = Quaternion.LookRotation(transform.forward, Vector3.up);
                    }
                }

                yield return null;
            }

        }

        //Check if the lizard is currently touching the ground
        public IEnumerator GroundChecking()
        {
            while (true)
            {
               
                Rigidbody rb = GetComponent<Rigidbody>();


                if (GroundCheck())
                {
                    rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                }
                else
                {
                    rb.constraints = RigidbodyConstraints.None;
                }
                

                yield return null;
            }
        }

        //Returns true if the lizard is touching the ground
        public bool GroundCheck()
        {
            BoxCollider mainCollider = GetComponent<BoxCollider>();

            Vector3 Center = transform.position +  MathUtility.ElementProduct(mainCollider.center , transform.lossyScale);

            //Size when the scale of the bone is taken into acount
            Vector3 trueSize = MathUtility.ElementProduct(transform.lossyScale, mainCollider.size);

            //Position of the Bottom Left of the collider
            Vector3 BotLeft = Center - trueSize / 2;

            Debug.DrawLine(Vector3.zero, BotLeft, Color.blue);
            Debug.DrawLine(Vector3.zero, Center, Color.cyan);



            int numberOftrues = 0;

            for(int i = 0; i < xCount; i++)
            {

                //Itterate through offsets in the X
                float offsetX = 0;
                if (xCount > 1) offsetX = trueSize.x / (xCount - 1) * i;

                for (int j = 0; j < yCount; j++)
                {
                    RaycastHit hit;

                   //Itterate through offsets in the Y (but really Z)
                    float offsetY = 0;
                    if (yCount > 1) offsetY = trueSize.z / (yCount - 1) * j;

                    //Place the start of the rays a little higher because when the boi is twisted 
                    //it some times starts checkeking lower tan the floor
                    Vector3 start = BotLeft + new Vector3(offsetX, 0, offsetY) - Vector3.down * detectionLength;
                    Vector3 end = start + Vector3.down * detectionLength;

                    Ray ray = new Ray(start, end - start);

                    //Double the length to account for the above ground length of the ray
                    Physics.Raycast(ray, out hit, detectionLength*2, groundLayers);

                    if (hit.collider)
                    {
                        numberOftrues++;

                        if (showRays) Debug.DrawLine(start, end, Color.green);
                    }
                    else if (showRays) Debug.DrawLine(start, end, Color.red);
                    
                }
            }

            return numberOftrues >= groundedCutoff;
        }

        public void ToggleToppleChecks(bool toggle)
        {
            if (toppleCheck != null)
            {
                StopCoroutine(toppleCheck);
                toppleCheck = null;
            }
            if (toggle)
            {
                toppleCheck = StartCoroutine(ToppleCheck());
            }
        }

        //Leg IK movement
        public void MoveLegHomePosition(Leg leg, Vector3 offset)
        {
            GameObject target = null;

            switch (leg)
            {
                case Leg.BL:

                    target = homeBackLeftLeg;

                    break;

                case Leg.BR:

                    target = homeBackRightLeg;

                    break;

                case Leg.FL:

                    target = homeFrontLeftLeg;

                    break;

                case Leg.FR:

                    target = homeFrontRightLeg;

                    break;
            }

            if (target == null) return;

            target.SetActive(false);
            target.transform.position += offset;

        }

        public void ResetLegHomePos(Leg leg)
        {
            GameObject target = null;

            switch (leg)
            {
                case Leg.BL:

                    target = homeBackLeftLeg;

                    break;

                case Leg.BR:

                    target = homeBackRightLeg;

                    break;

                case Leg.FL:

                    target = homeFrontLeftLeg;

                    break;

                case Leg.FR:

                    target = homeFrontRightLeg;

                    break;
            }

            if (target == null) return;

            target.transform.localPosition = Vector3.zero;
            target.SetActive(true);
        }

        public void FallToGround()
        {
            foreach(Collider c in floorTouchingColliders)
            {
                c.enabled = false;
            }
            if (deathParticles) Instantiate(deathParticles, transform);
        }

    }
}
