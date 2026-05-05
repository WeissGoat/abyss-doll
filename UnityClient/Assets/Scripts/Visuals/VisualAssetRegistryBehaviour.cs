using UnityEngine;

public class VisualAssetRegistryBehaviour : MonoBehaviour {
    public VisualAssetRegistry Registry;

    private void Awake() {
        if (Registry != null) {
            VisualAssetService.SetRegistry(Registry);
        }
    }
}
