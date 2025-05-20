using UnityEngine;

namespace Level
{
    [RequireComponent(typeof(Collider2D))]
    public class ExitTile : MonoBehaviour
    {
        [Header("Exit Settings")]
        [SerializeField] private string winMessage = "You win!";
        [SerializeField] private float centerProximityThreshold = 0.2f; // How close the player must be to the center

        private bool _hasTriggered = false;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_hasTriggered || !other.CompareTag("Player")) return;
            TryTriggerExit(other.transform.position);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (_hasTriggered || !other.CompareTag("Player")) return;
            TryTriggerExit(other.transform.position);
        }

        private void TryTriggerExit(Vector2 playerPos)
        {
            Vector2 tileCenter = transform.position;
            float distance = Vector2.Distance(playerPos, tileCenter);
            if (distance <= centerProximityThreshold)
            {
                _hasTriggered = true;
                Debug.Log(winMessage);
                GameEvents.TriggerPlayerReachedExit(); // Assuming this triggers your UI prompt
                // No more scene reload here!
            }
        }

        public void ResetTrigger()
        {
            _hasTriggered = false;
        }
    }
}