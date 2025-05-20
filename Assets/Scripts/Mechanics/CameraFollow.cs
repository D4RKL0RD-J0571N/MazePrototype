using UnityEngine;
using Sirenix.OdinInspector;

namespace Mechanics
{
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        [TitleGroup("Target Settings")]
        [SerializeField]
        [Tooltip("The transform this camera will follow.")]
        private Transform target;

        [TitleGroup("Follow Settings")]
        [SerializeField]
        [Tooltip("The offset from the target's position.")]
        private Vector3 offset = new Vector3(0, 0, -10f);

        [SerializeField]
        [Min(0.01f)]
        [Tooltip("The approximate time in seconds it will take for the camera to reach the target's position. A smaller value means faster damping.")]
        private float dampingTime = 0.3f; // Changed from followSpeed to better describe SmoothDamp's parameter

        [SerializeField]
        [Tooltip("If true, the camera will constantly look at the target's position.")]
        private bool lookAtTarget = false;

        // Private velocity reference for Vector3.SmoothDamp
        private Vector3 currentVelocity;

        public Transform Target
        {
            get => target;
            set => target = value;
        }
        
        private void LateUpdate()
        {
            if (Target == null)
            {
                Debug.LogWarning("CameraFollow: No target assigned to follow.", this);
                return;
            }

            Vector3 desiredPosition = Target.position + offset;
            
            // Use Vector3.SmoothDamp for a smoother, damped camera movement
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, dampingTime);

            // Optional: Make the camera look at the target
            if (lookAtTarget)
            {
                transform.LookAt(Target.position);
            }
        }
    }
}