using UnityEngine;

public class UIBillboard : MonoBehaviour
{
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
    }

    void LateUpdate()
    {
        if (mainCam != null)
        {
            // «мушуЇ Canvas дивитис€ точно в камеру, не перевертаючись
            transform.LookAt(transform.position + mainCam.transform.rotation * Vector3.forward,
                             mainCam.transform.rotation * Vector3.up);
        }
    }
}