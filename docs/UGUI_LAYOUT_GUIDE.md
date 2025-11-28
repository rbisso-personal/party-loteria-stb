# Unity UGUI Layout System Guide

A comprehensive reference for Unity's automatic UI layout system.

## Core Concepts

### RectTransform vs Transform
- Standard `Transform` positions objects in 3D space without size
- `RectTransform` includes width and height for 2D UI elements
- UI elements have **minimum**, **preferred**, and **flexible** sizes

### ILayoutElement Interface
Every layout-aware element implements `ILayoutElement` with 6 key properties:
- `minWidth`, `minHeight` - Absolute minimum size
- `preferredWidth`, `preferredHeight` - Ideal size when space allows
- `flexibleWidth`, `flexibleHeight` - Weight ratios for distributing extra space

**Important**: A value of `-1` means "don't specify this value" (let other components decide).

---

## Size Allocation Algorithm

The layout system allocates space in this **strict order**:

1. **First**: Allocate minimum sizes to all children
2. **Second**: If space remains, allocate up to preferred sizes
3. **Third**: Distribute remaining space based on flexible size **ratios**

### Flexible Sizes Are Ratios, Not Units!
```
Example: Container has 400px, Header preferred=100px, two items with flexible 2:1

1. Allocate header's preferred: 400 - 100 = 300px remaining
2. Distribute 300px by ratio 2:1: Item1 gets 200px, Item2 gets 100px
```

---

## Layout Groups

### HorizontalLayoutGroup / VerticalLayoutGroup

#### Key Properties

| Property | What It Does |
|----------|--------------|
| `padding` | Space inside the edges of the layout group |
| `spacing` | Space between child elements |
| `childAlignment` | Where to place children if they don't fill all space |
| `childControlWidth` | Layout group controls child widths using LayoutElement values |
| `childControlHeight` | Layout group controls child heights using LayoutElement values |
| `childForceExpandWidth` | Force all children to have flexible width >= 1 |
| `childForceExpandHeight` | Force all children to have flexible height >= 1 |

### CRITICAL: childControlWidth/Height Behavior

#### When `childControlWidth/Height = TRUE`:
- Layout group **reads** LayoutElement's min/preferred/flexible values
- Layout group **sets** the child's width/height based on those values
- You **cannot** manually set width/height via RectTransform (it's driven)
- **LayoutElement values ARE respected**

#### When `childControlWidth/Height = FALSE`:
- Layout group **only positions** children (doesn't touch their size)
- **LayoutElement values ARE IGNORED completely**
- You **must** set sizes via RectTransform directly
- Useful when you want manual control over sizes

### childForceExpand Behavior

#### When `childForceExpand = TRUE`:
- All children act as if they have `flexibleWidth/Height >= 1`
- Children will expand to fill available space even if their LayoutElement says `flexible = 0`

#### When `childForceExpand = FALSE`:
- Children respect their own LayoutElement flexible values
- `flexibleWidth = 0` means "don't expand beyond preferred size"

---

## LayoutElement Component

Add to any GameObject to override its layout properties.

### Properties

| Property | Purpose |
|----------|---------|
| `ignoreLayout` | Layout group skips this element entirely |
| `minWidth/Height` | Minimum size this element needs |
| `preferredWidth/Height` | Ideal size before flexible allocation |
| `flexibleWidth/Height` | Weight for extra space distribution (0 = don't expand) |
| `layoutPriority` | Higher priority overrides lower priority components |

### Common Patterns

#### Fixed-Size Element (doesn't expand):
```csharp
var le = obj.AddComponent<LayoutElement>();
le.preferredWidth = 300;
le.preferredHeight = 300;
le.flexibleWidth = 0;   // Won't expand horizontally
le.flexibleHeight = 0;  // Won't expand vertically
```
**Parent must have**: `childForceExpand = false` for this to work!

#### Expanding Element (fills remaining space):
```csharp
var le = obj.AddComponent<LayoutElement>();
le.flexibleWidth = 1;   // Will expand to fill available width
le.flexibleHeight = 1;  // Will expand to fill available height
```

#### Proportional Split (e.g., 70/30):
```csharp
// Element 1: 70% of extra space
var le1 = obj1.AddComponent<LayoutElement>();
le1.flexibleWidth = 7;

// Element 2: 30% of extra space
var le2 = obj2.AddComponent<LayoutElement>();
le2.flexibleWidth = 3;
```

---

## Content Size Fitter

Makes an element size itself based on its content.

### Fit Modes:
- **Unconstrained**: Don't adjust size
- **Min Size**: Size to minimum
- **Preferred Size**: Size to preferred (most common)

### WARNING: ContentSizeFitter + LayoutGroup Conflicts
Using ContentSizeFitter inside a LayoutGroup can cause **instability** due to order-of-operations issues. The layout may flicker or calculate incorrectly.

**Solution**: If you need self-sizing inside a layout group, consider implementing a custom component that implements both `ILayoutElement` and handles sizing internally.

---

## Aspect Ratio Fitter

Maintains aspect ratio of an element.

### Modes:
- **Width Controls Height**: Height adjusts based on width
- **Height Controls Width**: Width adjusts based on height
- **Fit In Parent**: Scales to fit inside parent while maintaining ratio
- **Envelope Parent**: Scales to cover parent while maintaining ratio

### WARNING: AspectRatioFitter Does NOT Work in Layout Groups!
AspectRatioFitter doesn't communicate with the layout system. It will fight with layout groups and cause broken layouts.

**Solution for square elements in layout groups**:
```csharp
// Set both preferred dimensions equal, flexible to 0
var le = obj.AddComponent<LayoutElement>();
le.preferredWidth = 350;
le.preferredHeight = 350;
le.flexibleWidth = 0;
le.flexibleHeight = 0;

// Parent layout group MUST have:
// childControlWidth = true
// childControlHeight = true
// childForceExpandWidth = false
// childForceExpandHeight = false
```

---

## Layout Calculation Order

Unity processes layouts in this order:

1. `CalculateLayoutInputHorizontal` - Bottom-up (children before parents)
2. `SetLayoutHorizontal` - Top-down (parents before children)
3. `CalculateLayoutInputVertical` - Bottom-up (children before parents)
4. `SetLayoutVertical` - Top-down (parents before children)

**Important**: Widths are calculated first, then heights. Heights can depend on widths, but widths can NEVER depend on heights.

---

## Forcing Layout Rebuilds

When modifying layout at runtime, Unity doesn't always update immediately.

```csharp
// Mark for rebuild (happens end of frame)
LayoutRebuilder.MarkLayoutForRebuild(rectTransform);

// Force immediate rebuild
LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
```

---

## Common Mistakes and Solutions

### Mistake 1: LayoutElement values ignored
**Symptom**: preferredWidth/Height have no effect
**Cause**: Parent's `childControlWidth/Height = false`
**Fix**: Set `childControlWidth/Height = true` on the parent layout group

### Mistake 2: Elements expanding when they shouldn't
**Symptom**: Element grows beyond preferred size
**Cause**: Parent's `childForceExpand = true` OR element's `flexibleWidth/Height > 0`
**Fix**: Set `childForceExpand = false` on parent AND `flexible = 0` on element

### Mistake 3: AspectRatioFitter breaking layout
**Symptom**: Elements overlap or go off-screen
**Cause**: AspectRatioFitter doesn't work with layout groups
**Fix**: Remove AspectRatioFitter, use LayoutElement with equal preferred width/height

### Mistake 4: ContentSizeFitter causing flickering
**Symptom**: Layout jumps around or calculates wrong sizes
**Cause**: Order-of-operations conflict between ContentSizeFitter and LayoutGroup
**Fix**: Use LayoutElement instead, or implement custom ILayoutElement

### Mistake 5: Nested layout groups not sizing correctly
**Symptom**: Inner layout group collapses or doesn't size properly
**Cause**: Inner layout group needs LayoutElement to report its size to parent
**Fix**: Add LayoutElement to the inner layout group's GameObject

---

## Quick Reference: Common Layout Configurations

### Two columns, left fixed, right expanding:
```csharp
// Parent: HorizontalLayoutGroup
hlg.childControlWidth = true;
hlg.childControlHeight = true;
hlg.childForceExpandWidth = false;
hlg.childForceExpandHeight = true;

// Left column (fixed 300px)
leftLE.preferredWidth = 300;
leftLE.flexibleWidth = 0;

// Right column (expands)
rightLE.flexibleWidth = 1;
```

### Vertical list with fixed header, scrolling content:
```csharp
// Parent: VerticalLayoutGroup
vlg.childControlWidth = true;
vlg.childControlHeight = true;
vlg.childForceExpandWidth = true;
vlg.childForceExpandHeight = false;

// Header (fixed 100px)
headerLE.preferredHeight = 100;
headerLE.flexibleHeight = 0;

// Content (fills remaining)
contentLE.flexibleHeight = 1;
```

### Square element inside layout:
```csharp
// Parent layout group
layout.childControlWidth = true;
layout.childControlHeight = true;
layout.childForceExpandWidth = false;
layout.childForceExpandHeight = false;

// Square element
squareLE.preferredWidth = 350;
squareLE.preferredHeight = 350;
squareLE.flexibleWidth = 0;
squareLE.flexibleHeight = 0;
```

---

## References

- [Unity Auto Layout Documentation](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/UIAutoLayout.html)
- [Unity Basic Layout Documentation](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/UIBasicLayout.html)
- [Layout Element Documentation](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-LayoutElement.html)
- [Hallgrim Games - Layout Groups Explained](https://www.hallgrimgames.com/blog/2018/10/16/unity-layout-groups-explained)
