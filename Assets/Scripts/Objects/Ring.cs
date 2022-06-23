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
        [SerializeField]
        private string collectAnimTrigger = "Collect";
        [SerializeField]
        private float collectDestroyDelay = 1f;

        private bool isCollected = false;
        public bool IsCollected
        {
            get { return isCollected; }
            protected set
            {
                isCollected = value;
                if (animator != null && collectHash != -1)
                {
                    animator.SetBool(collectHash, value);
                }
                if (collider2d != null)
                {
                    collider2d.enabled = !value;
                }
            }
        }

        private int collectHash = -1;
        private Coroutine collectCoroutine;

        protected virtual bool CanBeCollected { get { return !IsCollected; } }

        protected override Color32 gizmoColor { get { return new Color32(16, 255, 64, 64); } }

        protected override void Awake()
        {
            base.Awake();
            if (collectHash == -1 && !string.IsNullOrEmpty(collectAnimTrigger))
            {
                collectHash = Animator.StringToHash(collectAnimTrigger);
            }
        }

        protected override void OnPlayerEnterTrigger(Movement player)
        {
            base.OnPlayerEnterTrigger(player);
            if (CanBeCollected && !player.IsHit)
            {
                player.Rings += addCount;
                OnCollected();
            }
        }

        protected virtual void OnCollected()
        {
            IsCollected = true;
            // TODO: Spawn the collect FX separately so we don't need to do this
            collectCoroutine = StartCoroutine(PostCollectSequence(collectDestroyDelay));
        }

        private IEnumerator PostCollectSequence(float delay)
        {
            yield return new WaitForSeconds(delay);
            PostCollectSequenceFinished();
        }

        protected virtual void PostCollectSequenceFinished()
        {
            Destroy(gameObject);
        }
    }
}