using UnityEngine;
using UnityEngine.Pool;
using System.Collections;

namespace Giometric.UniSonic
{
    public class RingCollectFXSpawner : MonoBehaviour
    {
        private static RingCollectFXSpawner instance;

        [SerializeField]
        private ParticleSystem particles;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        public static void SpawnCollectFX(Vector3 position)
        {
            if (instance != null && instance.particles != null)
            {
                ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
                emitParams.position = position;
                instance.particles.Emit(emitParams, 1);
            }
        }
    }
}