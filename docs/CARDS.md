# Card System - STB Client (Unity)

## Overview

The STB (Set-Top Box) client displays the currently drawn card as a large focal point for all players to see. It must support multiple card packs for DLC content.

## Card Image Requirements

### File Format & Location
- **Format**: PNG (Unity standard, supports transparency)
- **Location**: `Assets/Resources/Images/Cards/{packId}/`
- **Naming**: `{id}_{name}.png` (e.g., `01_El_Gallo.png`)

### Image Specifications
- **Aspect Ratio**: 1:1.36 (width:height) or 0.737 (1509:2048 pixels)
- **Source Resolution**: 1509x2048 pixels (full quality)
- **Color Space**: sRGB
- **Import Settings**:
  - Texture Type: Sprite (2D and UI)
  - Sprite Mode: Single
  - Pixels Per Unit: 100
  - Filter Mode: Bilinear
  - Max Size: 2048

## Resources Folder Structure

Unity requires images to be in a `Resources` folder to use `Resources.Load()`:

```
Assets/
  Resources/
    Images/
      Cards/
        base/
          01_El_Gallo.png
          02_El_Diablito.png
          ...
          54_La_Rana.png
        {dlc-pack-id}/
          01_Card_Name.png
          ...
  Images/
    Cards/
      base/         # Optional: Keep originals here for organization
```

**Important**: Only the `Assets/Resources/` path is loaded at runtime. The `Assets/Images/` folder is for organization only.

## Card Display in GameScreen

### UI Layout

The card is displayed in the GameScreen with this hierarchy:

```
GameScreen
└── CardPanel (VerticalLayoutGroup)
    ├── CardImageContainer (LayoutElement: preferredHeight=500, flexibleHeight=0)
    │   └── CardImage (Image component, 368x500px)
    ├── CardNameContainer (LayoutElement: flexibleHeight=1)
    │   └── CardName (TextMeshProUGUI, auto-sizing)
    └── CardVerse (LayoutElement: preferredHeight=100)
```

### Image Component Setup

**CRITICAL: DO NOT HARDCODE SIZES - USE DYNAMIC LAYOUT**

Created in `UIBuilder.cs`:

```csharp
// Card image - expands to fill available space while maintaining aspect ratio
var cardImageContainer = CreateLayoutElement(cardPanel.transform, "CardImageContainer", flexibleHeight: 1);
cardImageContainer.SetActive(false); // Hidden until first card is drawn

var cardImageObj = new GameObject("CardImage");
cardImageObj.transform.SetParent(cardImageContainer.transform, false);
cardImage = cardImageObj.AddComponent<Image>();
cardImage.preserveAspect = true;
StretchToParent(cardImageObj);
```

### Key Settings
- **preserveAspect**: `true` (automatically maintains 1:1.36 ratio from source image)
- **flexibleHeight**: `1` (expands to fill available vertical space)
- **StretchToParent**: Image fills container (anchors to all edges)
- **Layout**: Dynamic - adapts to screen size and available space
- **Hidden by default**: Container starts hidden, shown when first card loads

### Why Dynamic Layout?
- Adapts to different screen resolutions automatically
- Respects the vertical layout system (other elements like text can grow/shrink)
- `preserveAspect` ensures correct aspect ratio without hardcoded dimensions
- Professional, responsive design that scales properly

## Loading Card Images

### CardImageLoader Utility

`Assets/Scripts/UI/CardImageLoader.cs` provides sprite loading:

```csharp
public static Sprite LoadCardSprite(string imagePath)
{
    // imagePath example: "base/01_El_Gallo"
    string fullPath = $"Images/Cards/{imagePath}";
    
    // Load from Resources
    var texture = Resources.Load<Texture2D>(fullPath);
    
    // Create sprite from texture
    var sprite = Sprite.Create(
        texture,
        new Rect(0, 0, texture.width, texture.height),
        new Vector2(0.5f, 0.5f), // Pivot at center
        100f // Pixels per unit
    );
    
    return sprite;
}
```

### Usage in GameScreenController

```csharp
private void UpdateCardDisplay(Card card)
{
    // ... set text fields ...
    
    // Load and display card image
    if (cardImage != null && !string.IsNullOrEmpty(card.image))
    {
        var sprite = CardImageLoader.LoadCardSprite(card.image);
        if (sprite != null)
        {
            cardImage.sprite = sprite;
            cardImage.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"[GameScreenController] Failed to load sprite for card: {card.image}");
            cardImage.gameObject.SetActive(false);
        }
    }
}
```

## Card Data Structure

Cards are defined in `Assets/Scripts/Data/Card.cs`:

```csharp
[Serializable]
public class Card
{
    public int id;
    public string name_es;
    public string name_en;
    public string verse_es;
    public string verse_en;
    public string image;      // e.g., "base/01_El_Gallo" (no extension)
    public string vo_es;
    public string vo_en;
}
```

**Note**: The `image` field includes the pack path (e.g., `"base/01_El_Gallo"`), but NOT the file extension.

## Adding New Card Packs (DLC)

### Step 1: Import PNG Images

1. Create pack directory in Resources:
   ```
   Assets/Resources/Images/Cards/{packId}/
   ```

2. Copy PNG files (1509x2048px) into the directory

3. Select all images in Unity, set import settings:
   - Texture Type: Sprite (2D and UI)
   - Sprite Mode: Single
   - Pixels Per Unit: 100
   - Max Size: 2048
   - Click "Apply"

### Step 2: Update Card Data

The card data comes from the server. Ensure the `image` field includes the pack path:

```json
{
  "id": 55,
  "name_es": "La Bruja",
  "image": "halloween/55_La_Bruja"
}
```

### Step 3: Test Loading

The `CardImageLoader` will automatically load from the correct pack:

```csharp
// Loads from: Resources/Images/Cards/halloween/55_La_Bruja.png
var sprite = CardImageLoader.LoadCardSprite("halloween/55_La_Bruja");
```

## Performance Considerations

### Resources vs Addressables

**Current System**: Uses `Resources.Load()`
- ✅ Simple to implement
- ✅ Works for small/medium card libraries (50-200 cards)
- ⚠️ All Resources are included in build (increases size)
- ⚠️ Cannot download packs post-release

**Future DLC System**: Consider Unity Addressables
- ✅ Download packs on-demand
- ✅ Reduce base game size
- ✅ Patch/update packs independently
- ⚠️ More complex setup

### Memory Management

- **Sprite Creation**: Creates new sprites on load (not cached)
- **Memory**: Each 1509x2048 PNG ≈ 12MB uncompressed in memory
- **Optimization**: Only one card displayed at a time (minimal memory impact)

### Future Enhancement: Sprite Caching

```csharp
private static Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

public static Sprite LoadCardSprite(string imagePath)
{
    if (spriteCache.TryGetValue(imagePath, out Sprite cached))
    {
        return cached;
    }
    
    // Load and cache...
    spriteCache[imagePath] = sprite;
    return sprite;
}
```

## Testing New Card Packs

1. **Import Images**: Place PNGs in `Assets/Resources/Images/Cards/{packId}/`
2. **Configure Import Settings**: Sprite (2D and UI), Max Size 2048
3. **Test in Play Mode**: Start game, draw cards from the pack
4. **Check Console**: Look for warnings about missing sprites
5. **Verify Aspect Ratio**: Card should appear tall (not stretched)

## Troubleshooting

### Image Not Loading
- ✅ Check image is in `Assets/Resources/` folder (not just `Assets/`)
- ✅ Verify path in card data matches folder structure
- ✅ Check file extension is `.png` (not `.PNG` or other)
- ✅ Look for warnings in Unity Console

### Image Stretched/Distorted
- ✅ Ensure `preserveAspect = true` on Image component
- ✅ Verify import settings: Texture Type = Sprite (2D and UI)
- ✅ Check source image is 1509x2048 (correct aspect ratio)

### Image Too Small/Large
- ✅ Check `preferredHeight` in UIBuilder (default: 500)
- ✅ Adjust `sizeDelta` to maintain aspect ratio: `(height * 0.737, height)`

## File Structure Reference

```
Assets/
  Resources/
    Images/
      Cards/
        base/
          01_El_Gallo.png
          ...
          54_La_Rana.png
        {dlc-pack-id}/
          01_Card_Name.png
          ...
  Scripts/
    Data/
      Card.cs               # Card data structure
    UI/
      UIBuilder.cs          # Creates card image UI
      GameScreenController.cs  # Displays cards
      CardImageLoader.cs    # Loads sprites from Resources
```

## Quick Reference

| Property | Value |
|----------|-------|
| Aspect Ratio | 1:1.36 (0.737) |
| Format | PNG |
| Source Resolution | 1509x2048 pixels |
| Display Size | 368x500 pixels |
| Location | `Assets/Resources/Images/Cards/{packId}/` |
| Naming | `{id}_{name}.png` |
| Texture Type | Sprite (2D and UI) |
| Pixels Per Unit | 100 |
