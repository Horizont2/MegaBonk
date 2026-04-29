using UnityEngine;
using System;

public class ActiveBuildManager : MonoBehaviour
{
    public static ActiveBuildManager Instance;

    public GameObject buildWidgetPrefab; // Префаб твого віджета з ActiveBuildWidget
    public Transform widgetContainer;    // Контейнер (Vertical Layout Group) справа на екрані

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AddBuildTask(string bName, Sprite bIcon, DateTime targetTime, float duration)
    {
        if (buildWidgetPrefab == null || widgetContainer == null) return;

        GameObject newWidget = Instantiate(buildWidgetPrefab, widgetContainer);
        ActiveBuildWidget tracker = newWidget.GetComponent<ActiveBuildWidget>();

        if (tracker != null)
        {
            tracker.Setup(bName, bIcon, targetTime, duration);
        }
    }
}