using UnityEngine;
using System.Collections;

public class PlayerCamera : MonoBehaviour
{
    public Movement player;
    public float rightLimit = 0f;
    public float leftLimit = -16f;
    public float topLimit = 48f;
    public float bottomLimit = -16f;

    private float camZ;

    void Awake()
    {
        camZ = transform.position.z;
    }

    void LateUpdate()
    {
        Vector3 pos = transform.position;
        pos.z = camZ;
        if (player.transform.position.x > pos.x + rightLimit)
        {
            pos.x += (player.transform.position.x - (pos.x + rightLimit));
        }
        else if (player.transform.position.x < pos.x + leftLimit)
        {
            pos.x += (player.transform.position.x - (pos.x + leftLimit));
        }

        if (player.transform.position.y > pos.y + topLimit)
        {
            pos.y += (player.transform.position.y - (pos.y + topLimit));
        }
        else if (player.transform.position.y < pos.y + bottomLimit)
        {
            pos.y += (player.transform.position.y - (pos.y + bottomLimit));
        }


        transform.position = pos;
    }
}