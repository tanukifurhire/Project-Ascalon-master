using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetUIManager : MonoBehaviour
{
    [SerializeField] private Transform targetUITemplate;
    private List<Transform> playerTargetList;

    private void Awake()
    {
        targetUITemplate.gameObject.SetActive(false);
        playerTargetList = new List<Transform>();
    }

    private void Start()
    {
        AscalonGameMultiplayer.Instance.OnTargetAdded += AscalonGameMultiplayer_OnTargetAdded;
        AscalonGameMultiplayer.Instance.OnTargetRemoved += AscalonGameMultiplayer_OnTargetRemoved;
    }

    private void AscalonGameMultiplayer_OnTargetRemoved(object sender, AscalonGameMultiplayer.OnTargetChangedEventArgs e)
    {
        if (playerTargetList.Contains(e.target))
        {
            e.target.GetComponent<Player>().OnDestroyed -= Player_OnDestroyed;
            
            playerTargetList.Remove(e.target);

            foreach (Transform child in transform)
            {
                if (child.GetComponent<TargetUI>().GetTarget() == e.target)
                {
                    Destroy(child.gameObject);
                }
            }
        }
    }

    private void Player_OnDestroyed(object sender, EventArgs e)
    {
        Player player = sender as Player;

        if (playerTargetList.Contains(player.transform))
        {
            playerTargetList.Remove(player.transform);
        }

        foreach (Transform child in transform)
        {
            if (child.GetComponent<TargetUI>().GetTarget() == player.transform)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void AscalonGameMultiplayer_OnTargetAdded(object sender, AscalonGameMultiplayer.OnTargetChangedEventArgs e)
    {
        playerTargetList.Add(e.target);

        e.target.GetComponent<Player>().OnDestroyed += Player_OnDestroyed;

        Transform child = Instantiate(targetUITemplate, transform);

        child.gameObject.SetActive(true);

        child.GetComponent<TargetUI>().SetTarget(e.target);
    }
}
