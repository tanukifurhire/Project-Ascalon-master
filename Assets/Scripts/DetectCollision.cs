using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectCollision : MonoBehaviour
{
    [SerializeField] private float bottomOffset;
    [SerializeField] private float frontOffset;
    [SerializeField] private float collisionRadius;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float WallDistance;

    // Check if there is a floor to stand on, or land on
    public bool CheckGround()
    {
        Vector3 positionOfCheck = transform.position + (-transform.up * bottomOffset);
        Collider[] hitColliders = Physics.OverlapSphere(positionOfCheck, collisionRadius, groundLayer);
        if (hitColliders.Length > 0)
        {
            return true;
        }
        return false;
    }

    public bool CheckWall()
    {
        Vector3 positionOfWallCheck = transform.position + (transform.forward * frontOffset);
        Collider[] hitColliders = Physics.OverlapSphere(positionOfWallCheck, collisionRadius, groundLayer);
        if (hitColliders.Length > 0)
        {
            return true;
        }
        Debug.Log("unit is not grounded!");
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 positionOfCheckVisual = transform.position + (-transform.up * bottomOffset);
        Gizmos.DrawSphere(positionOfCheckVisual, collisionRadius);
        
        Gizmos.color = Color.red;
        Vector3 positionOfWallCheckVisual = transform.position + (transform.forward * frontOffset);
        Gizmos.DrawSphere(positionOfWallCheckVisual, collisionRadius);
    }
}
