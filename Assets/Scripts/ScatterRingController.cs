using UnityEngine;
using UnityEngine.Pool;
using Giometric.UniSonic.Objects;

namespace Giometric.UniSonic
{
    public class ScatterRingController : MonoBehaviour
    {
        public static ScatterRingController Instance;
        
        [Header("Scatter Rings")]
        [SerializeField] private ScatterRing scatterRingPrefab;
        [Tooltip("The maximum number of rings that will be spawned around the character when rings are lost.")]
        [SerializeField] private int scatterRingsCountLimit = 32;
        [SerializeField] private int scatterRingsPerCircle = 16;
        [Tooltip("The initial speed of scattered rings, directed outward from the center.")]
        [SerializeField] private float scatterRingBaseSpeed = 240f;
        
        private IObjectPool<ScatterRing> scatterRingPool;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            scatterRingPool = new ObjectPool<ScatterRing>(
                CreatePooledScatterRing,
                OnScatterRingTakeFromPool,
                OnScatterRingReturnToPool,
                OnScatterRingDestroyPooledObject,
                true,
                scatterRingsCountLimit
            );


            if (scatterRingPrefab != null)
            {
                // Unity's object pool provides no method for pre-creating the pooled items, so we do it ourselves
                ScatterRing[] precreatedPool = new ScatterRing[scatterRingsCountLimit];
                for (int i = 0; i < scatterRingsCountLimit; ++i)
                {
                    precreatedPool[i] = scatterRingPool.Get();
                }
                for (int i = 0; i < scatterRingsCountLimit; ++i)
                {
                    scatterRingPool.Release(precreatedPool[i]);
                }
            }
            else
            {
                Debug.LogWarning("Scatter ring prefab not set!", gameObject);
            }
        }
        
        private void OnScatterRingPostCollectFinished(ScatterRing scatterRing)
        {
            scatterRing.ResetRing();
            scatterRingPool.Release(scatterRing);
        }

        private ScatterRing CreatePooledScatterRing()
        {
            if (scatterRingPrefab == null)
            {
                return null;
            }

            var scatterRing = Instantiate(scatterRingPrefab, transform);
            scatterRing.Pool = scatterRingPool;
            scatterRing.gameObject.SetActive(false);
            return scatterRing;
        }

        private void OnScatterRingTakeFromPool(ScatterRing scatterRing)
        {
            scatterRing.gameObject.SetActive(true);
            scatterRing.ResetRing();
        }

        private void OnScatterRingReturnToPool(ScatterRing scatterRing)
        {
            scatterRing.ResetRing();
            scatterRing.gameObject.SetActive(false);
        }

        private void OnScatterRingDestroyPooledObject(ScatterRing scatterRing)
        {
            Destroy(scatterRing.gameObject);
        }
        
        public void ScatterRings(Vector2 center, float direction, int numRings, LayerMask groundMask)
        {
            if (numRings == 0 || scatterRingsPerCircle == 0 || scatterRingsCountLimit == 0)
            {
                return;
            }

            int numCircles = Mathf.Max(1, Mathf.CeilToInt(numRings / (float)scatterRingsPerCircle));
            int remaining = numRings;
            float scatterSpeed = scatterRingBaseSpeed;
            float angleSpacing = Mathf.PI * 2f / scatterRingsPerCircle;
            float startAngle = (Mathf.PI * 0.5f) + (angleSpacing * 0.5f * direction);
            for (int circle = 0; circle < numCircles; ++circle)
            {
                float currentAngle = 0f;
                bool flip = false;
                for (int i = 0; i < scatterRingsPerCircle && remaining > 0; ++i)
                {
                    float angleRad = startAngle + (direction * currentAngle);
                    Vector2 velocity = new Vector2(Mathf.Cos(angleRad) * scatterSpeed, Mathf.Sin(angleRad) * scatterSpeed);

                    if (flip)
                    {
                        velocity.x = -velocity.x;
                        currentAngle += angleSpacing;
                    }

                    flip = !flip;
                    var scatterRing = scatterRingPool.Get();
                    if (scatterRing != null)
                    {
                        scatterRing.transform.position = center;
                        scatterRing.Velocity = velocity;
                        scatterRing.SetCollisionLayerMask(groundMask);
                    }
                    --remaining;
                }
                scatterSpeed *= 0.5f;
            }
        }
    }
}