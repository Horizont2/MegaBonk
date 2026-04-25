using UnityEngine;
using Unity.Cinemachine;

public class CameraTransitionManager : MonoBehaviour
{
    [Header("Камери")]
    public CinemachineCamera heroCamera;
    public CinemachineCamera arsenalCamera;
    public CinemachineCamera inspectCamera; // Додана камера огляду

    public void GoToHeroes()
    {
        heroCamera.Priority = 20;
        arsenalCamera.Priority = 10;
        if (inspectCamera) inspectCamera.Priority = 10;
    }

    public void GoToArsenal()
    {
        arsenalCamera.Priority = 20;
        heroCamera.Priority = 10;
        if (inspectCamera) inspectCamera.Priority = 10;
    }

    public void GoToInspect()
    {
        if (inspectCamera) inspectCamera.Priority = 20;
        arsenalCamera.Priority = 10;
        heroCamera.Priority = 10;
    }
}