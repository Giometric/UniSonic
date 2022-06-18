using UnityEngine;
using System.Collections;

namespace Giometric.UniSonic.Objects
{
    public class CollisionLayerSwitchTrigger : ObjectTriggerBase
    {
        private enum TriggerAction
        {
            None = -1,
            ToLayerA = 0,
            ToLayerB = 1,
        }

        private enum TriggerDirectionMode
        {
            Horizontal = 0,
            Vertical = 1,
        }

        [SerializeField]
        private bool mustBeGrounded;
        [SerializeField]
        private TriggerDirectionMode triggerDirection = TriggerDirectionMode.Horizontal;
        [Header("Horizontal")]
        [SerializeField]
        private TriggerAction fromLeft = TriggerAction.None;
        [SerializeField]
        private TriggerAction fromRight = TriggerAction.None;
        [Header("Vertical")]
        [SerializeField]
        private TriggerAction fromAbove = TriggerAction.None;
        [SerializeField]
        private TriggerAction fromBelow = TriggerAction.None;

        protected override Color32 gizmoColor { get { return new Color32(255, 255, 0, 64); } }

        protected override void OnPlayerEnterTrigger(Movement player)
        {
            base.OnPlayerEnterTrigger(player);

            if (mustBeGrounded && !player.Grounded)
            {
                return;
            }

            var center = collider2d != null ? collider2d.bounds.center : transform.position;
            var dif = player.transform.position - collider2d.bounds.center;

            if (triggerDirection == TriggerDirectionMode.Horizontal)
            {
                if (dif.x >= 0f && fromRight != TriggerAction.None)
                {
                    player.SetCollisionLayer((int)fromRight);
                }
                else if (fromLeft != TriggerAction.None)
                {
                    player.SetCollisionLayer((int)fromLeft);
                }
            }
            else
            {
                if (dif.y >= 0f && fromAbove != TriggerAction.None)
                {
                    player.SetCollisionLayer((int)fromAbove);
                }
                else if (fromBelow != TriggerAction.None)
                {
                    player.SetCollisionLayer((int)fromBelow);
                }
            }
        }
    }
}