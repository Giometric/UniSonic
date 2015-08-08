using UnityEngine;
using System.Collections;

public class PlayerCamera : MonoBehaviour
{
    public Movement player;

    // These limit values match the original games
    // They're slightly off-center, so may be good to change them
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
        Vector3 playerPos = player.transform.position;
        if (player.rolling || player.jumped) { playerPos.y += 5f; }

        pos.z = camZ;
        if (playerPos.x > pos.x + rightLimit)
        {
            pos.x += (playerPos.x - (pos.x + rightLimit));
        }
        else if (playerPos.x < pos.x + leftLimit)
        {
            pos.x += (playerPos.x - (pos.x + leftLimit));
        }

        if (playerPos.y > pos.y + topLimit)
        {
            pos.y += (playerPos.y - (pos.y + topLimit));
        }
        else if (playerPos.y < pos.y + bottomLimit)
        {
            pos.y += (playerPos.y - (pos.y + bottomLimit));
        }


        transform.position = pos;
    }
}