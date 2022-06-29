using UnityEngine;
using System.Collections;

namespace Giometric.UniSonic.Objects
{
    public class DamageTrigger : ObjectTriggerBase
    {
        [SerializeField]
        [Tooltip("If true, player loses rings when hit by this damage trigger. If false, player will only be knocked away.")]
        private bool doesDamage = true;

        protected override Color32 gizmoColor { get { return new Color32(255, 16, 16, 64); } }

        protected override void OnPlayerEnterTrigger(Movement player)
        {
            base.OnPlayerEnterTrigger(player);
            if (!player.IsInvulnerable)
            {
                player.SetHitState(transform.position, doesDamage);
            }
        }
    }
}