using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlightTargetHandler : MonoBehaviour
{
    public static FlightTargetHandler Instance { get; private set; }

    private Vector2 smoothedInput;
    private Vector2 smoothVectorVelocity;

    private void Awake()
    {
        Instance = this;
    }

    public void SetFlightPosition(Vector3 position)
    {
        transform.position = position;
    }
    public void SetFlightRotation ()
    {
        Vector2 lookInput = GameInput.Instance.LookInputNormalized().normalized;

        smoothedInput = Vector2.SmoothDamp(smoothedInput, lookInput, ref smoothVectorVelocity, 0.3f);

        Vector3 facingDirection = new Vector3(smoothedInput.x * 2f, 0f, .25f);

        Vector3 movementDirection = new Vector3(1f, smoothedInput.y, 0f);

        float directionAngle = GetDirectionAngle(facingDirection);

        directionAngle += LookAtTargetForward.Instance.transform.eulerAngles.y;

        if (directionAngle >= 360f)
        {
            directionAngle -= 360f;
        }

        float directionXAngle = Mathf.Atan2(movementDirection.y, movementDirection.x) * Mathf.Rad2Deg;

        float additionalAngle = 35f * smoothedInput.x;

        transform.rotation = Quaternion.Euler(-directionXAngle, directionAngle + additionalAngle, 0f);
    }
    private float GetDirectionAngle(Vector3 direction)
    {
        float directionAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

        if (directionAngle < 0f)
        {
            directionAngle += 360f;
        }

        return directionAngle;
    }
    public Vector3 GetFlightDirection()
    {
        return transform.forward;
    }
}
