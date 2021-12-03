using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace LizardSpace
{
    


    public class HeadController : MonoBehaviour, BodyPartController
    {
        GameObject HeadRoot;
        SkeletonSubTree Head;

        Coroutine changeHeadDirection;




        [Header("Test Varaibles")]
        public LizardController.forwardDirection forwardDir;


        [Header("Animation Variables")]
        public float headRotateSpeed;
        public float headTrackingSpeed;

        public Transform cameraTarget;

        public float chargeTongueCameraFollow = 0.5f;

        [Header("Tongue variables")]
        public Tongue tongue;

        Vector3 tonguePos;
        public float tongueSpeed;
        public float returnSpeed;

        public bool homingTongue;
        public float defaultTongueRange = 3f;


        [Header("Charge Shot Variables")]
        public float tongueCooldown;
        public float minTongueRange;
        public float maxTongueRange;
        public float rangeChargeRate;
        public float headRotationHalfAngle;

        
        public bool aimAssist;

        public float aimAssistHalfAngle;

        public bool aimUsingMoveStick;
        public bool invertTongueAiming;

        [Header("Targeting variable")]

        public float detectionRange;
        public float forwardTargettingBias;
        [Range(0,90)]
        public float detectionHalfAngle;

        public LayerMask blockers;
        public LayerMask targetLayers;

        public TargettingArrow arrow;

        [Header("Eyes Variables")]

        public GameObject EyeL;
        public GameObject EyeR;

        public float eyeRepositionDelay;
        [Range(0, 1)]
        public float eyeDelayError;


        public delegate void SendSimple();
        public SendSimple OnTongueHit;


        [HideInInspector]
        public LizardController controller;



        private void Start()
        {
            if(HeadRoot) Head = LizardSkeleton.makeSubtree(HeadRoot);
            tonguePos = tongue.transform.localPosition;
        }

        public void ResetTongue()
        {
            tongue.transform.localPosition = tonguePos;
            tongue.transform.rotation = Quaternion.identity;
        }

        public LizardState getDefaultState(LizardController c)
        {


            return new HeadIdle(c);
        }

        Vector3 currentTarget;
        Vector3 leftEyeTarget;
        Vector3 rightEyeTarget;

        public void SetEyeTarget(bool leftEye, Vector3 newTarget)
        {
            if (leftEye)
            {
                leftEyeTarget = newTarget;
                //Debug.DrawLine(EyeL.transform.position, leftEyeTarget, Color.blue, 1f);
            }

            else
            {
                rightEyeTarget = newTarget; 
                //Debug.DrawLine(EyeR.transform.position, rightEyeTarget, Color.blue, 1f);
            }
        }

        public void SetLookPosition(Vector3 newLookTo, bool eyeFollow)
        {
            currentTarget = newLookTo;
            if (eyeFollow)
            {
                leftEyeTarget = newLookTo;
                rightEyeTarget = newLookTo;
            }


            if (changeHeadDirection == null) changeHeadDirection = StartCoroutine(FollowTarget());
        }

        IEnumerator FollowTarget()
        {

            while (true)
            {
                float headDot = Vector3.Dot((currentTarget - transform.position).normalized, controller.getTransformVector(transform));


                if (headDot < 0.99f)
                {
                    LookAt(currentTarget);

                }

               float eyeLDot = Vector3.Dot((leftEyeTarget - EyeL.transform.position).normalized, EyeL.transform.forward);

                if(eyeLDot < 0.99f)
                {
                    EyeLookAt(EyeL.transform, leftEyeTarget);
                }


                float eyeRDot = Vector3.Dot((rightEyeTarget - EyeR.transform.position).normalized, EyeR.transform.forward);

                if (eyeRDot < 0.99f)
                {
                    EyeLookAt(EyeR.transform, rightEyeTarget);
                }

                yield return null;
            }
            
            
        }

        public Vector3 GetInputRotation(LizardController c, float cutoff)
        {
            Vector2 input = InputHandler.instance.currentTongueInput;

            if (input.magnitude < cutoff)
            {
                return controller.getTransformVector(transform); ;
            }

            else return GetInputRotation(c);
        }

        public Vector3 GetInputRotation(LizardController c)
        {
            Vector3 reference = c.GetNeckOrientation();

            //Get the current tongue input and normalize it
            //Note: There's no point in using the square-to-circle operation because there is no analog control
            Vector2 input = InputHandler.instance.currentTongueInput;
            if (aimUsingMoveStick) input = InputHandler.instance.currentMoveInput;

            Vector3 newDir = (new Vector3(input.x, 0, input.y)).normalized;

            if (invertTongueAiming) newDir = -newDir;

            Debug.DrawRay(transform.position, Quaternion.Euler(0, headRotationHalfAngle, 0) * reference * maxTongueRange, Color.yellow);
            Debug.DrawRay(transform.position, Quaternion.Euler(0, -headRotationHalfAngle, 0) * reference * maxTongueRange, Color.yellow);

            if (input == Vector2.zero)
            {
                return controller.getTransformVector(transform); ;

                
            }

            float angle = Vector3.SignedAngle(reference, newDir, Vector3.up);
            if (Mathf.Abs(angle) > headRotationHalfAngle)
            {
                if (angle > 0)
                {
                    newDir = Quaternion.Euler(0, headRotationHalfAngle, 0) * reference;
                }
                else newDir = Quaternion.Euler(0, -headRotationHalfAngle, 0) * reference;
            }

            

            return newDir;
        }

        public void LookAt(Vector3 worldSpaceTarget)
        {
            // Store the current head rotation since we will be resetting it
            Quaternion currentLocalRotation = transform.localRotation;
            // Reset the head rotation so our world to local space transformation will use the head's zero rotation. 
            // Note: Quaternion.Identity is the quaternion equivalent of "zero"
            transform.localRotation = Quaternion.identity;

            Vector3 targetWorldLookDir = worldSpaceTarget - transform.position;
            Vector3 targetLocalLookDir = transform.InverseTransformDirection(targetWorldLookDir);

            // Apply angle limit
            // Note we multiply by Mathf.Deg2Rad here to convert degrees to radians
            targetLocalLookDir = Vector3.RotateTowards(
                Vector3.forward,
                targetLocalLookDir,
                Mathf.Deg2Rad * headRotationHalfAngle, 
                0 // We don't care about the length here, so we leave it at zero
                );

            


            // Get the local rotation by using LookRotation on a local directional vector
            Quaternion targetLocalRotation = Quaternion.LookRotation(targetLocalLookDir, Vector3.up);

            // Apply smoothing
            transform.localRotation = Quaternion.Slerp(
              currentLocalRotation,
              targetLocalRotation,
              1 - Mathf.Exp(-headTrackingSpeed * Time.deltaTime)
            );

            
            
        }

        public void EyeLookAt(Transform eye, Vector3 worldTarget)
        {



            Quaternion currentLocalRotation = eye.localRotation;

            Vector3 targetWorldLookDir = worldTarget - eye.position;

            Vector3 targetLocalLookDir = eye.InverseTransformDirection(targetWorldLookDir);




            // Get the local rotation by using LookRotation on a local directional vector
            Quaternion targetLocalRotation = Quaternion.LookRotation(targetLocalLookDir, Vector3.up);

            eye.localRotation = Quaternion.Slerp(
             currentLocalRotation,
             targetLocalRotation,
             1 - Mathf.Exp(-headTrackingSpeed * Time.deltaTime)
           );
        }


        public Tonguable AutoTargeting(LizardController controller)
        {
            Vector3 reference = controller.GetNeckOrientation();

            List<Collider> preTonguable = MathUtility.ConeDetection(transform.position, reference, detectionRange, detectionHalfAngle, targetLayers, blockers);


            return FindBestTarget(preTonguable, reference);
        }


        public Tonguable FindBestTarget(List<Collider> inputs, Vector3 reference)
        {
            List<Tonguable> tonguables = new List<Tonguable>();

            foreach (Collider c in inputs)
            {
                Tonguable t = c.GetComponent<Tonguable>();
                if (t != null)
                {
                    tonguables.Add(t);
                    t.lastReccordedDistance = MathUtility.TargetingMetric(transform.position, t.transform.position, reference, forwardTargettingBias);
                }
            }

            if (tonguables.Count == 0) return null;

            tonguables = (tonguables.OrderBy(t => t.lastReccordedDistance)).ToList<Tonguable>(); ;


            bool first = true;
            foreach (Tonguable t in tonguables)
            {
                Color color = Color.Lerp(Color.green, Color.red, t.lastReccordedDistance / detectionRange);

                Debug.DrawLine(transform.position, t.transform.position, color);

                t.DrawTargeting(first);


                if (first) first = false;
            }


            return tonguables[0];
        }


        public Vector3 getTransformVector(Transform t)
        {
            if (forwardDir == LizardController.forwardDirection.X)
            {
                return t.right;
            }

            if (forwardDir == LizardController.forwardDirection.Z)
            {
                return t.forward;
            }

            return Vector3.zero;
        }

        public void OnDrawGizmos()
        {
            Debug.DrawLine(EyeL.transform.position, EyeL.transform.position + EyeL.transform.forward * 20f, Color.red);

            Debug.DrawLine(EyeR.transform.position, EyeR.transform.position + EyeR.transform.forward * 20f, Color.red);

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, maxTongueRange);
        }
    }
}
