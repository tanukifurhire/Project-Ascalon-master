using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectCollision : MonoBehaviour
{
    [SerializeField] private float bottomOffset;
    [SerializeField] private float frontOffset;
    [SerializeField] private float collisionRadius;
    [SerializeField] private float frontCollisionRadius;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float WallDistance;
    private List<Player> playerCollisionList;

    private void Awake()
    {
        playerCollisionList = new List<Player>();
    }

    // Check if there is a floor to stand on, or land on
    public bool CheckGround()
    {
        Vector3 positionOfCheck = transform.position + (Vector3.down * bottomOffset);
        Collider[] hitColliders = Physics.OverlapSphere(positionOfCheck, collisionRadius, groundLayer);
        if (hitColliders.Length > 0)
        {
            return true;
        }
        return false;
    }

    public bool CheckWall()
    {
        Vector3 positionOfWallCheck = transform.position + (transform.up * frontOffset);
        Collider[] hitColliders = Physics.OverlapSphere(positionOfWallCheck, collisionRadius, groundLayer);
        if (hitColliders.Length > 0)
        {
            return true;
        }

        return false;
    }

    public bool CheckCollisionWithPlayer()
    {
        Vector3 positionOfWallCheck = transform.position + (transform.up * frontOffset);
        Collider[] hitColliders = Physics.OverlapSphere(positionOfWallCheck, frontCollisionRadius);

        bool isCollidingWithPlayer = false;
        for (int i = 0; i < hitColliders.Length; i++)
        {
            if (hitColliders[i].GetComponent<Player>() != null && hitColliders[i].transform != Player.LocalInstance.transform)
            {
                isCollidingWithPlayer = true;

                break;
            }
        }
        return isCollidingWithPlayer;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 positionOfCheckVisual = transform.position + (-transform.up * bottomOffset);
        Gizmos.DrawSphere(positionOfCheckVisual, collisionRadius);
        
        Gizmos.color = Color.red;
        Vector3 positionOfWallCheckVisual = transform.position + (transform.up * frontOffset);
        Gizmos.DrawSphere(positionOfWallCheckVisual, frontCollisionRadius);
    }
}
