// ParryZone.cs
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ParryZone : MonoBehaviour
{
    private Player player;

    private void Awake()
    {
        player = GetComponentInParent<Player>();
        if (player == null)
            Debug.LogError("ParryZone: no Player found in parents!");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        int missileLayer = LayerMask.NameToLayer("Missile");
        if (other.gameObject.layer != missileLayer || !player.IsParrying)
            return;

        Destroy(other.gameObject);

        // 1) give the extra life
        GameManager.Instance.AddLife(1);

        // 2) tell the GM to ignore the upcoming kill/reset
        GameManager.Instance.NotifyParrySuccess();

        // 3) consume parry in the Player
        player.ConsumeParry();

        Debug.Log("ParryZone: parry SUCCESS – next kill will be skipped.");
    }

}
