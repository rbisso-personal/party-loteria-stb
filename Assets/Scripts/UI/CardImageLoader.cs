using UnityEngine;
using UnityEngine.UI;
using System.IO;

namespace PartyLoteria.UI
{
    /// <summary>
    /// Utility class for loading card images from Resources at runtime.
    /// </summary>
    public static class CardImageLoader
    {
        private const string CARDS_PATH = "Images/Cards";

        /// <summary>
        /// Load a card sprite from Resources based on the card's image path.
        /// Example: "base/01_El_Gallo" -> Resources/Images/Cards/base/01_El_Gallo.png
        /// </summary>
        /// <param name="imagePath">The image path from the card data (e.g., "base/01_El_Gallo")</param>
        /// <returns>The loaded sprite, or null if not found</returns>
        public static Sprite LoadCardSprite(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
            {
                Debug.LogWarning("[CardImageLoader] Image path is null or empty");
                return null;
            }

            // Remove .png extension if present (Resources.Load doesn't need it)
            string resourcePath = imagePath.Replace(".png", "");
            
            // If path doesn't include pack, prepend "base/"
            if (!resourcePath.Contains("/"))
            {
                resourcePath = $"base/{resourcePath}";
            }
            
            // Full path: Images/Cards/{pack}/{filename}
            string fullPath = $"{CARDS_PATH}/{resourcePath}";
            
            // Load from Resources
            var texture = Resources.Load<Texture2D>(fullPath);
            
            if (texture == null)
            {
                Debug.LogWarning($"[CardImageLoader] Failed to load texture at: Resources/{fullPath}");
                return null;
            }

            // Create sprite from texture
            var sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), // Pivot at center
                100f // Pixels per unit
            );

            if (sprite == null)
            {
                Debug.LogWarning($"[CardImageLoader] Failed to create sprite from texture: {fullPath}");
                return null;
            }

            Debug.Log($"[CardImageLoader] Successfully loaded sprite: {fullPath}");
            return sprite;
        }

        /// <summary>
        /// Get the aspect ratio of card images (width:height).
        /// Traditional Lotería cards are approximately 1509:2048 (≈0.74:1 or 1:1.36)
        /// </summary>
        public static float GetCardAspectRatio()
        {
            return 1509f / 2048f; // ≈0.737
        }
    }
}
