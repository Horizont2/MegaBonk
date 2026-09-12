using UnityEngine;

public class POISettings : MonoBehaviour
{
    [Header("Terraforming Passport")]
    [Tooltip("����� ���� ������� �������, ���� ���� �������� ��������.")]
    public float flattenRadius = 30f;

    [Tooltip("�������� ������� �������� ������ � �������� ����� (�������� ������� ��������, ��� ��������� ���������).")]
    public float yOffset = -0.5f;

    [Tooltip("����������� ���������� ������� ����� �� ��� ����� �� ����������� (������ �� ��). ��� ������� �� ���� 5-7, ��� ������ ������ 10.")]
    public float maxAllowedSlope = 6f;

    // ==== RARITY ====
    //
    // The generator used to draw POI prefabs uniformly, which is fine while every
    // location is ordinary furniture and completely wrong the moment one of them
    // is an EVENT. With five prefabs in the list and eighty placements, a
    // reliquary appeared about sixteen times per region — so the thing that was
    // supposed to be the find you change your route for became the most common
    // sight on the map, and the armour economy went with it.
    //
    // The odds belong here, on the prefab, rather than in a table inside the
    // generator: whoever builds a location is the person who knows how rare it
    // ought to be, and they can set it in the same inspector where they set its
    // footprint. Adding a prefab to the generator's list stays a one-step job.
    [Header("Rarity")]
    [Tooltip("Weight in the ordinary draw, relative to the other eligible locations. 1 = a normal location. Lower makes it rarer WHEN it is eligible; 0 means it is never drawn.")]
    public float spawnWeight = 1f;

    [Tooltip("Chance this location is eligible AT ALL in a given region, rolled once before anything is placed. This is the knob that makes a location an event: below 1 means some regions simply do not have one, and a region with none is what makes the next one's silhouette worth walking to.")]
    [Range(0f, 1f)] public float regionAppearChance = 1f;

    [Tooltip("Hard cap per region. 0 = no cap. With a high maxPOIs the cap, not the weight, is what actually enforces scarcity — weights only decide the order things get picked in.")]
    public int maxPerRegion = 0;

    // ��� ����� ����� ����� ���� � �������� Unity! 
    // �� ������ �������� �������, ������ ���� ����� ������� � �� �� ������� �����.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.9f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position + Vector3.up * yOffset, flattenRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 1f); // ����� (Pivot)
    }
}