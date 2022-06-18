using UnityEngine;
using System.Collections;

namespace Giometric.UniSonic.Objects
{
    public class Ring : ObjectTriggerBase
    {
        [SerializeField]
        [Tooltip("How many rings the character will gain when collecting this ring object.")]
        private int addCount = 1;

        [SerializeField]
        private Animator animator;
        [SerializeField]
        private string collectAnimTrigger = "Collect";
        [SerializeField]
        private float collectDestroyDelay = 1f;

        public bool IsCollected { get; private set; }
        private int collectHash = -1;

        protected override Color32 gizmoColor { get { return new Color32(16, 255, 64, 64); } }

        protected override void Awake()
        {
            base.Awake();
            if (!string.IsNullOrEmpty(collectAnimTrigger))
            {
                collectHash = Animator.StringToHash(collectAnimTrigger);
            }
        }

        protected override void OnPlayerEnterTrigger(Movement player)
        {
            base.OnPlayerEnterTrigger(player);
            if (!player.IsHit && !IsCollected)
            {
                if (collider2d != null)
                {
                    collider2d.enabled = false;
                }
                player.Rings += addCount;
                IsCollected = true;
                if (animator != null && !string.IsNullOrEmpty(collectAnimTrigger))
                {
                    animator.SetTrigger(collectHash);
                }
                Destroy(gameObject, collectDestroyDelay);
            }
        }
    }
}