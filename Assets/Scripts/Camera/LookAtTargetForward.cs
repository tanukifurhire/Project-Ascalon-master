using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtTargetForward : MonoBehaviour
{
    public static LookAtTargetForward Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }
    private void Update()
    {
        if (Player.LocalInstance != null)
        {
            SetObjectPosition();

            if (Player.LocalInstance.GetPlayerState() != Player.States.Flying)
            {
                if (Player.LocalInstance.GetActivePlayerTarget() == null)
                {
                    Quaternion rotation = Player.LocalInstance.transform.rotation;

                    transform.rotation = rotation;
                }

                if (Player.LocalInstance.GetActivePlayerTarget() != null)
                {

                    Vector3 direction = Player.LocalInstance.GetActivePlayerTarget().transform.position - Player.LocalInstance.transform.position;

                    Quaternion rotation = Quaternion.LookRotation(direction);

                    transform.rotation = rotation;
                }
            }
            else
            {
                if (Player.LocalInstance.GetActivePlayerTarget() == null)
                {
                    transform.rotation = Quaternion.LookRotation(Player.LocalInstance.transform.up, -Player.LocalInstance.transform.forward);
                }

                if (Player.LocalInstance.GetActivePlayerTarget() != null)
                {
                    Vector3 direction = Player.LocalInstance.GetActivePlayerTarget().transform.position - Player.LocalInstance.transform.position;

                    Quaternion rotation = Quaternion.LookRotation(direction);

                    transform.rotation = rotation;
                }
            }
        }
    }

    private void SetObjectPosition()
    {
        Vector3 position = Player.LocalInstance.transform.position;

        transform.position = position;
    }
}
