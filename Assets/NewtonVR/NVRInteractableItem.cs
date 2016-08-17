using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;

namespace NewtonVR
{
    public class NVRInteractableItem : NVRInteractable
    {
        [Tooltip("If you have a specific point you'd like the object held at, create a transform there and set it to this variable")]
        public Transform InteractionPoint;

        [Tooltip("In what hand reference space is this attached (only applies if there's an interaction point)")]
        public NVRHandReferenceSpace ReferenceSpace = NVRHandReferenceSpace.HandGrip;

        public float maxVelocityChange = 1000f;
        public float maxAngularVelocityChange = 1000f;

        [Tooltip("Use swept movement when not colliding")]
        public bool UseSweepMovement = true;

        [Tooltip("Force rotation if rotation frozen ")]
        public bool ForceRotationIfFrozen = true;

        [Tooltip("Freeze rotation when held")]
        public bool FreezeRotationOnAttach = false;

        [Tooltip("Freeze rotation when held")]
        public bool UnfreezeRotationOnDetatch = true;

        protected Transform PickupTransform;
        private Vector3 AngularTarget;
        private Vector3 VelocityTarget;
        private Vector3 OldCenterOfMass;

        private bool collidedThisFrame = false;
        private bool sweptThisFrame = false;

        protected override void Awake()
        {
            base.Awake();
            this.Rigidbody.maxAngularVelocity = 100f;
        }  

        public override void BeginInteraction(NVRHand hand)
        {
            base.BeginInteraction(hand);

            if (FreezeRotationOnAttach)
            {
                Rigidbody.freezeRotation = true;
            }

            PickupTransform = new GameObject(string.Format("[{0}] NVRPickupTransform", this.gameObject.name)).transform;
            PickupTransform.parent = hand.transform;
            PickupTransform.position = this.transform.position;
            PickupTransform.rotation = this.transform.rotation;          
            Rigidbody.useGravity = false;
        }

        public override void EndInteraction()
        {
            base.EndInteraction();

            if (UnfreezeRotationOnDetatch)
            {
                Rigidbody.freezeRotation = false;
            }

            if (sweptThisFrame)
            {
                this.Rigidbody.velocity = VelocityTarget;
                this.Rigidbody.angularVelocity = AngularTarget;
            }

            if (PickupTransform != null)
                Destroy(PickupTransform.gameObject);
        }

        void OnCollisionEnter(Collision col)
        {
            collidedThisFrame = true;
        }

        void OnCollisionStay(Collision col)
        {
            collidedThisFrame = true;
        }

        void FixedUpdate()
        {
            sweptThisFrame = false;
            if (IsAttached == true)
            {
                Vector3 PositionDelta;
                Quaternion RotationDelta;

                if (InteractionPoint != null)
                {
                    RotationDelta = AttachedHand.transform.rotation*AttachedHand.GetReferenceRotation(ReferenceSpace)*
                                    Quaternion.Inverse(InteractionPoint.rotation);
                    PositionDelta = (AttachedHand.transform.position - InteractionPoint.position);
                }
                else
                {
                    RotationDelta = PickupTransform.rotation*Quaternion.Inverse(this.transform.rotation);
                    PositionDelta = (PickupTransform.position - this.transform.position);
                }

                var newPos = transform.position + PositionDelta;
                var newRot = RotationDelta*transform.rotation;

                float angle;
                Vector3 axis;
                RotationDelta.ToAngleAxis(out angle, out axis);

                if (angle > 180)
                    angle -= 360;

                AngularTarget = angle*Mathf.Deg2Rad*axis/Time.fixedDeltaTime;                
                VelocityTarget = PositionDelta/Time.fixedDeltaTime;
                this.Rigidbody.velocity = Vector3.MoveTowards(this.Rigidbody.velocity, VelocityTarget, maxVelocityChange);
                this.Rigidbody.angularVelocity = Vector3.MoveTowards(this.Rigidbody.angularVelocity, AngularTarget, maxAngularVelocityChange);

                if (ForceRotationIfFrozen && Rigidbody.freezeRotation)
                {
                    this.Rigidbody.MoveRotation(newRot);
                }

                if (UseSweepMovement == true && collidedThisFrame == false)
                {
                    var prevPos = transform.position;
                    var prevRot = transform.rotation;

                    var diff = newPos - prevPos;
                    var dist = diff.magnitude;
                    var dir = diff/dist;
                    var hit = new RaycastHit();

                    transform.rotation = newRot;
                    bool sweepHit = Rigidbody.SweepTest(dir, out hit, dist);
                    transform.rotation = prevRot;

                    if (!sweepHit)
                    {
                        this.Rigidbody.velocity = Vector3.zero;
                        this.Rigidbody.angularVelocity = Vector3.zero;
                        Rigidbody.MoveRotation(newRot);
                        Rigidbody.MovePosition(newPos);
                        sweptThisFrame = true;
                    }
                }
            }
            collidedThisFrame = false;
        }
    }
}