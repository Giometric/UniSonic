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
        protected Animator animator;

        private bool isCollected = false;
        public bool IsCollected
        {
            get { return isCollected; }
            protected set
            {
                if (value && !isCollected)
                {
                    RingCollectFXSpawner.SpawnCollectFX(transform.position);
                }
                isCollected = value;
                if (collider2d != null)
                {
                    collider2d.enabled = !value;
                }
            }
        }

        protected virtual bool CanBeCollected { get { return !IsCollected; } }
        protected override Color32 gizmoColor { get { return new Color32(16, 255, 64, 64); } }

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnPlayerEnterTrigger(Movement player)
        {
            base.OnPlayerEnterTrigger(player);
            if (CanBeCollected && !player.IsHit)
            {
                player.Rings += addCount;
                IsCollected = true;
                OnCollected();
            }
        }

        protected virtual void OnCollected()
        {
            Destroy(gameObject);
        }
    }
}