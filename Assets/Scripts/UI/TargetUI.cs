using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using System;

public class TargetUI : MonoBehaviour
{
    private Transform target;

    [SerializeField] private Image targetSprite;

    [SerializeField] private Color normalColor;
    [SerializeField] private Color targetColor;
    [SerializeField] private Color targetedColor;

    private bool isTargetPressed = false;

    private void Start()
    {
        GameInput.Instance.OnTargetPressed += GameInput_OnTargetPressed;
        GameInput.Instance.OnTargetReleased += GameInput_OnTargetReleased;
        target.GetComponent<Player>().OnDestroyed += Target_OnDestroyed;
    }

    private void Target_OnDestroyed(object sender, EventArgs e)
    {
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        GameInput.Instance.OnTargetPressed -= GameInput_OnTargetPressed;
        GameInput.Instance.OnTargetReleased -= GameInput_OnTargetReleased;
    }

    private void GameInput_OnTargetReleased(object sender, EventArgs e)
    {
        isTargetPressed = false;

        if (target != null)
        {
            if (target == Player.LocalInstance.GetPlayerTarget())
            {
                targetSprite.color = targetColor;
            }
            else
            {
                targetSprite.color = normalColor;
            }
        }
    }

    private void GameInput_OnTargetPressed(object sender, EventArgs e)
    {
        isTargetPressed = true;

        if (target != null)
        {
            if (target == Player.LocalInstance.GetPlayerTarget())
            {
                targetSprite.color = targetedColor;
            }
        }
    }

    private void Update()
    {
        if (target != null)
        {
            if (target == Player.LocalInstance.GetPlayerTarget() && !isTargetPressed)
            {
                targetSprite.color = targetColor;
            }
            if (target != Player.LocalInstance.GetPlayerTarget() && !isTargetPressed)
            {
                targetSprite.color = normalColor;
            }

            transform.position = Camera.main.WorldToScreenPoint(target.position);
        }
    }

    public Transform GetTarget()
    {
        return target;
    }

    public void SetTarget(Transform target)
    {
        this.target = target;
    }
}
