using UnityEngine;

public class SnapToGround : MonoBehaviour
{
    private void Start()
    {
        // Чекаємо 1 кадр, щоб WorldGenerator точно закінчив їх крутити і скейлити
        Invoke(nameof(AlignWithGround), 0.05f);
    }

    private void AlignWithGround()
    {
        Collider col = GetComponentInChildren<Collider>();
        if (col != null)
        {
            // Рахуємо, наскільки глибоко під землю зайшла найнижча точка колайдера
            float bottomY = col.bounds.min.y;
            float difference = transform.position.y - bottomY;

            // Виштовхуємо об'єкт вгору рівно на цю різницю
            transform.position += Vector3.up * difference;
        }
    }
}