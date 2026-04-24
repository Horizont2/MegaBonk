using UnityEngine;

public class AnimationEventReceiver : MonoBehaviour
{
    private PlayerController player;

    private void Start()
    {
        // Шукаємо головний скрипт на батьківському об'єкті
        player = GetComponentInParent<PlayerController>();
    }

    // Ці функції побачить Аніматор
    public void ExecuteAttack()
    {
        if (player != null) player.ExecuteAttack();
    }

    public void ExecuteThrow()
    {
        if (player != null) player.ExecuteThrow();
    }
}