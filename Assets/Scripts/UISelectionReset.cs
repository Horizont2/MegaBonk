using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

// Stops clicked buttons getting stuck looking un-hovered.
//
// ==== WHAT IS ACTUALLY WRONG ====
//
// Unity's Selectable has four visual states and picks between them in a fixed
// priority: Pressed, then Selected, then Highlighted, then Normal. Clicking a
// button leaves it SELECTED in the EventSystem, and Selected outranks
// Highlighted — so from that moment the button renders its Selected tint no
// matter what the mouse does. Hovering it again changes nothing, moving away
// changes nothing, and because most projects leave Selected looking identical
// to Normal, the button simply looks dead until the player clicks somewhere
// else and the EventSystem finally drops the selection.
//
// That is exactly the reported symptom: press a button, want to press it again,
// and it sits in its default state until you tap somewhere on the screen.
//
// ==== WHY A GLOBAL FIX AND NOT A FIELD ON EVERY BUTTON ====
//
// The per-button fix is to call EventSystem.SetSelectedGameObject(null) in each
// onClick, and it is wrong twice over: there are well over a hundred buttons
// across the camp, the shop, the barracks, the map and the pause menu, and any
// button added later starts out broken again. This is one behaviour of the UI
// system, so it belongs in one place.
//
// ==== WHY IT WATCHES THE POINTER ====
//
// Clearing the selection unconditionally would break keyboard and gamepad
// navigation, where the selection IS the cursor — arrow-keying through a menu
// would deselect on every move and leave the player with nothing focused. So
// the reset only fires on a genuine pointer release, and anything that needs to
// keep focus to work at all (a text field being typed into, an open dropdown)
// is left alone.
[DisallowMultipleComponent]
public class UISelectionReset : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<UISelectionReset>() != null) return;
        var go = new GameObject("[UISelectionReset]");
        DontDestroyOnLoad(go);
        go.AddComponent<UISelectionReset>();
    }

    private void Update()
    {
        if (!Input.GetMouseButtonUp(0) && !Input.GetMouseButtonUp(1)) return;

        var es = EventSystem.current;
        if (es == null) return;

        GameObject selected = es.currentSelectedGameObject;
        if (selected == null) return;
        if (KeepsFocus(selected)) return;

        // Dropping the selection sends the Selectable back through its own state
        // machine, so it lands on Highlighted if the pointer is still over it and
        // Normal if it is not — which is what the player expects to see.
        es.SetSelectedGameObject(null);
    }

    // Controls that stop working the moment they lose focus.
    private static bool KeepsFocus(GameObject go)
    {
        if (go.GetComponent<TMP_InputField>() != null) return true;
        if (go.GetComponent<InputField>() != null) return true;
        if (go.GetComponent<TMP_Dropdown>() != null) return true;
        if (go.GetComponent<Dropdown>() != null) return true;
        // A dropdown's open list is a separate object parented under a blocker;
        // deselecting it closes the list out from under the click that was about
        // to choose an item.
        if (go.GetComponentInParent<TMP_Dropdown>() != null) return true;
        if (go.GetComponentInParent<Dropdown>() != null) return true;
        return false;
    }
}
