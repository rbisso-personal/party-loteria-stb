using UnityEngine;

namespace PartyLoteria.Config
{
    /// <summary>
    /// DEPRECATED: Environment configuration has moved to NetworkManager.
    /// This class is kept for backwards compatibility but does nothing.
    /// Use the Environment dropdown on NetworkManager instead.
    /// </summary>
    public class GameConfig : MonoBehaviour
    {
        private void Awake()
        {
            Debug.LogWarning("[GameConfig] This component is deprecated. Use NetworkManager's Environment dropdown instead.");
        }
    }
}
