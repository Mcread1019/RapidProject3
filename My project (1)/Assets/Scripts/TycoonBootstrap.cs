using UnityEngine;

/// <summary>
/// Entry point for the Tycoon Game.

public class TycoonBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    

    void Start()
    {
        SetupCamera();

        // Create core managers
        var rmGO = new GameObject("ResourceManager");
        var rm   = rmGO.AddComponent<ResourceManager>();

        var bmGO = new GameObject("BuildingManager");
        var bm   = bmGO.AddComponent<BuildingManager>();

        // Create UI and wire it to the managers
        var uiGO = new GameObject("TycoonUI");
        var ui   = uiGO.AddComponent<TycoonUI>();

        // TycoonUI.Init must run after ResourceManager and BuildingManager Awake()
        // so we defer by one frame via a coroutine
        StartCoroutine(InitUINextFrame(ui, rm, bm));
    }

    private System.Collections.IEnumerator InitUINextFrame(
        TycoonUI ui, ResourceManager rm, BuildingManager bm)
    {
        yield return null;   // wait one frame so all Awake() calls complete
        ui.Init(rm, bm);
    }

    private void SetupCamera()
    {
        // Configure the main camera for a clean dark background
        var cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.07f, 1f);
        }
    }
}
