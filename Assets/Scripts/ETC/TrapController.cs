using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrapController : MonoBehaviour
{
    [SerializeField] private Material originalMaterial;
    [SerializeField] private Material dieMaterial;
    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
    
    private PlayerController playerController;
    
    private void OnEnable()
    {
        GameResetEvent.OnPlayerReset += ResetMaterial;
    }

    private void OnDisable()
    {
        GameResetEvent.OnPlayerReset -= ResetMaterial;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            playerController = other.GetComponent<PlayerController>();
            playerController.stateMachine.ChangeState(playerController.deathState);

            skinnedMeshRenderer.material = dieMaterial;
        }
    }

    private void ResetMaterial()
    {
        skinnedMeshRenderer.material = originalMaterial;
    }
}
