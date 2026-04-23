# Shop Scene Setup Guide — MegaBonk

Complete step-by-step instructions for building the 3D shop scene in Unity 6 URP.

---

## Overview

The shop is a 3D scene with:
- A row of **pedestals**, each holding a rotating display model
- A **spotlight** above each pedestal (brightens on focus)
- A **camera** that lerps smoothly between pedestals
- A **UI overlay** (Canvas) with item info, stat bars, and buy button
- **ScriptableObjects** as the item database

---

## Required Assets

### Sprites (2D, for UI Canvas)
| Sprite | Size | Notes |
|--------|------|-------|
| `ShopBG` | 1920×1080 | Dark stone/dungeon texture or gradient |
| `PanelBG` | 512×512 | Semi-transparent dark panel, rounded corners |
| `StatBarBG` | 256×32 | Dark bar background |
| `StatBarFill` | 256×32 | Gold/blue gradient fill |
| `CrystalIcon` | 64×64 | Crystal gem icon (same as HUD) |
| `ButtonNormal` | 256×64 | Rounded rect, semi-transparent dark |
| `ArrowLeft` | 64×64 | Left chevron/arrow |
| `ArrowRight` | 64×64 | Right chevron/arrow |

### 3D Models
- **Pedestal mesh** — stone cylinder/pillar, ~0.5m radius × 1m tall
- **Character display models** — each character as a separate prefab
- **Weapon display models** — floating weapon meshes

### Materials (URP)
- `PedestalStone` — Lit, dark grey stone texture
- `SpotlightBeam` (optional) — Additive unlit for volumetric look

---

## Step 1: Create the ScriptableObject Items

### 1.1 Create Item Assets

Right-click in **Project → Assets/ScriptableObjects/Shop/** → `Create → MegaBonk → Shop Item`

**Create one asset per item.** Example items:

| Asset Name | itemID | displayName | itemType | price | isDefaultUnlocked |
|---|---|---|---|---|---|
| `Item_Char_Default` | `char_default` | WARRIOR | Character | 0 | ✅ true |
| `Item_Char_Rogue` | `char_rogue` | ROGUE | Character | 300 | ❌ false |
| `Item_Char_Mage` | `char_mage` | MAGE | Character | 500 | ❌ false |
| `Item_Weapon_Default` | `weapon_default` | BONK HAMMER | Weapon | 0 | ✅ true |
| `Item_Weapon_Axe` | `weapon_axe` | BATTLE AXE | Weapon | 250 | ❌ false |
| `Item_Grenade_Default` | `grenade_default` | FRAG GRENADE | Grenade | 0 | ✅ true |

### 1.2 Fill Character Stats (example — Warrior)
```
characterStats:
  maxHP: 100
  moveSpeed: 8
  pickupRadius: 4
  damageReduction: 0
  damageMultiplier: 1.0
passiveDescription: "Balanced stats. Good for beginners."
```

### 1.3 Fill Weapon Stats (example — Bonk Hammer)
```
weaponStats:
  damage: 25
  attackSpeed: 1.0
  range: 3.0
  knockback: 2.0
passiveDescription: "Reliable melee weapon."
```

---

## Step 2: Scene Hierarchy

Create a new scene: **File → New Scene → Basic (URP)**.  
Name it `Shop` and add to Build Settings.

```
Shop (scene)
├── Directional Light          (dim ambient, intensity 0.3, color #8088AA)
├── ShopManager                (empty GO — ShopManager.cs + ShopEffects.cs)
├── ShopCamera                 (Main Camera)
├── PedestalRow                (empty GO at origin)
│   ├── Pedestal_0             (ShopPedestal.cs)
│   │   ├── PedestalMesh       (MeshRenderer — stone pillar)
│   │   ├── ModelSpawnPoint    (empty Transform, Y = top of pillar ~1.0)
│   │   ├── Spotlight          (Point or Spot Light)
│   │   └── PedestalParticles  (Particle System — optional rune glow)
│   ├── Pedestal_1             (same structure)
│   ├── Pedestal_2             (same structure)
│   └── ... (one per item)
├── ShopCanvas                 (Canvas — Screen Space Overlay)
│   ├── Background             (Image — dark overlay)
│   ├── TopBar                 (crystal balance)
│   ├── InfoPanel              (CanvasGroup — left panel)
│   │   ├── ItemName           (TMP)
│   │   ├── ItemDescription    (TMP)
│   │   ├── PassiveText        (TMP)
│   │   └── StatBars           (vertical group of 4 rows)
│   │       ├── StatRow_HP
│   │       ├── StatRow_Speed
│   │       ├── StatRow_Radius
│   │       └── StatRow_Damage
│   ├── BottomBar              (buy button + page indicator)
│   │   ├── LeftArrowButton
│   │   ├── PageIndicator      (TMP)
│   │   ├── BuyButton
│   │   └── RightArrowButton
│   └── BackButton
└── ShopCharacterLoader        (empty GO — attach in gameplay scene, not here)
```

---

## Step 3: Pedestal Setup

### 3.1 Position Pedestals

Place pedestals in a row along the **X axis**, spaced **4 units** apart:

| Pedestal | Position |
|---|---|
| Pedestal_0 | X: -8, Y: 0, Z: 0 |
| Pedestal_1 | X: -4, Y: 0, Z: 0 |
| Pedestal_2 | X:  0, Y: 0, Z: 0 |
| Pedestal_3 | X:  4, Y: 0, Z: 0 |
| Pedestal_4 | X:  8, Y: 0, Z: 0 |

### 3.2 ShopPedestal Inspector Settings

Attach `ShopPedestal.cs` to each Pedestal_N GO.

| Field | Value |
|---|---|
| **itemData** | Drag the matching ShopItemData ScriptableObject |
| **spotlight** | Drag the child Spotlight light |
| **modelSpawnPoint** | Drag the child ModelSpawnPoint transform |
| **pedestalParticles** | Drag child ParticleSystem (or leave empty) |
| **normalIntensity** | 1 |
| **focusedIntensity** | 3 |
| **normalColor** | #99AABB (soft blue-grey) |
| **focusedColor** | #CCDDFF (bright cool white) |
| **lightTransitionSpeed** | 4 |
| **idleRotationSpeed** | 20 |
| **focusBobAmplitude** | 0.05 |
| **focusBobFrequency** | 1.5 |

### 3.3 Spotlight Settings

Select the Spotlight child of each pedestal:

| Setting | Value |
|---|---|
| Type | Spot |
| Range | 6 |
| Spot Angle | 40 |
| Intensity | 1 (ShopPedestal will control this at runtime) |
| Color | #99AABB |
| Shadows | Soft Shadows |
| Position | Y: 4 (above pedestal), pointing straight down |
| Rotation | X: 90 (pointing down) |

### 3.4 PedestalParticles Settings (optional rune glow)

| Setting | Value |
|---|---|
| Shape | Circle, Radius 0.4 |
| Start Lifetime | 2 |
| Start Speed | 0.5 |
| Start Size | 0.05 |
| Start Color | #4488FF with alpha 150 |
| Emission Rate | 8 |
| Play On Awake | OFF (ShopPedestal controls Play/Stop) |
| Looping | ON |

---

## Step 4: ShopManager Setup

### 4.1 Create ShopManager GO

Create empty GO, name it `ShopManager`. Add components:
- `ShopManager.cs`
- `ShopEffects.cs`

### 4.2 ShopManager Inspector

| Field | Value |
|---|---|
| **pedestals** (array, size = number of items) | Drag each Pedestal_N GO |
| **shopCamera** | Drag the Main Camera transform |
| **cameraDistance** | 4 |
| **cameraHeight** | 1.5 |
| **cameraLerpSpeed** | 4 |
| **lookAtOffsetY** | 1 |
| **uiManager** | Drag ShopCanvas GO (which has ShopUIManager.cs) |
| **effects** | Drag self (ShopManager GO has ShopEffects.cs) |
| **prevKey** | LeftArrow |
| **nextKey** | RightArrow |
| **buyKey** | Return |
| **backKey** | Escape |
| **mainMenuSceneName** | `MainMenu` |

### 4.3 ShopEffects Inspector (on same GO)

| Field | Value |
|---|---|
| **purchaseParticlePrefab** | A gold/confetti particle prefab (or leave empty) |
| **purchaseShakeDuration** | 0.3 |
| **purchaseShakeMagnitude** | 0.15 |
| **errorShakeDuration** | 0.2 |
| **errorShakeMagnitude** | 0.05 |
| **shopCamera** | Drag the Main Camera **transform** |

---

## Step 5: Camera Setup

### 5.1 Initial Position

Set the Main Camera initial position so it faces Pedestal_0 or whichever pedestal is default-equipped:
- Position: X: -8, Y: 1.5, Z: -4  
- Rotation: X: 10, Y: 0, Z: 0

ShopManager will take over camera positioning at runtime.

### 5.2 Camera Settings

| Setting | Value |
|---|---|
| Clear Flags | Skybox (or Solid Color #0D1210 for dark dungeon) |
| Field of View | 60 |
| Near Clip | 0.1 |
| Far Clip | 100 |

---

## Step 6: UI Canvas Setup

### 6.1 Canvas Root

Create UI → Canvas. Name it `ShopCanvas`.

| Setting | Value |
|---|---|
| Render Mode | Screen Space — Overlay |
| UI Scale Mode | Scale With Screen Size |
| Reference Resolution | 1920 × 1080 |
| Match | 0.5 |

Add `ShopUIManager.cs` to ShopCanvas GO.

### 6.2 Background

Create UI → Image. Name `Background`. Child of ShopCanvas.

| Field | Value |
|---|---|
| Anchor | Stretch-Stretch (all corners) |
| Left/Right/Top/Bottom | 0 |
| Source Image | ShopBG sprite (or None for solid color) |
| Color | #0D1210 at alpha 180 (semi-transparent dark) |

### 6.3 TopBar (Crystal Balance)

Create UI → Empty, name `TopBar`. Child of ShopCanvas.

| RectTransform | Value |
|---|---|
| Anchor | Top-Right |
| Pivot | (1, 1) |
| Pos X | -20 |
| Pos Y | -20 |
| Width | 220 |
| Height | 50 |

Inside TopBar, create:

**CrystalIcon** — UI → Image
- Width/Height: 36×36, anchored Left-Center
- Source Image: CrystalIcon sprite, Color: #88DDFF

**CrystalBalanceText** — UI → TextMeshPro
- Anchor: Right-Center stretch
- Font: Bold, Size: 28, Color: #FFCC33
- Alignment: Right-Center
- Text: "0" (runtime)

### 6.4 InfoPanel (Left Side)

Create UI → Empty, name `InfoPanel`. Child of ShopCanvas.  
Add `CanvasGroup` component.

| RectTransform | Value |
|---|---|
| Anchor | Left-Middle |
| Pivot | (0, 0.5) |
| Pos X | 30 |
| Pos Y | 0 |
| Width | 380 |
| Height | 580 |

Inside InfoPanel, add UI → Image:
- Stretch-Stretch, Source Image: PanelBG, Color: #1A2010 at alpha 200

#### ItemName Text
| Field | Value |
|---|---|
| Anchor | Top-Stretch |
| Pivot | (0.5, 1) |
| Pos Y | -20 |
| Height | 60 |
| Font | ExtraBold |
| Size | 36 |
| Color | #FFCC33 |
| Alignment | Center-Top |
| Text | "WARRIOR" (runtime) |

#### ItemDescription Text
| Field | Value |
|---|---|
| Anchor | Top-Stretch |
| Pos Y | -90 |
| Height | 80 |
| Font | Regular |
| Size | 18 |
| Color | #BBCCAA |
| Word Wrap | ON |
| Text | "Description..." (runtime) |

#### PassiveText
| Field | Value |
|---|---|
| Anchor | Top-Stretch |
| Pos Y | -178 |
| Height | 50 |
| Font | Italic |
| Size | 16 |
| Color | #99AAFF |
| Text | "Passive: ..." (runtime) |

### 6.5 Stat Bars

Create empty GO `StatBars` inside InfoPanel.

| RectTransform | Value |
|---|---|
| Anchor | Bottom-Stretch |
| Height | 240 |
| Pos Y | 10 |

Add `Vertical Layout Group`:
- Spacing: 10
- Child Force Expand Height: OFF
- Child Control Height: ON

Create 4 child stat rows, each named `StatRow_HP`, `StatRow_Speed`, `StatRow_Radius`, `StatRow_Damage`.

#### Each StatRow Structure

Add `Layout Element` → Preferred Height: **46**

Inside each StatRow:

```
StatRow_HP
├── LabelText      (TMP — "HP", Left, Bold, Size 16, #AABB88, Width 80)
├── BarContainer   (RectTransform, Horizontal Layout Group, spacing 4)
│   ├── BarBG      (Image — #1C2415, Height 12, Flexible Width)
│   │   └── BarFill (Image — #55CC33 or #FFAA22, Image Type: Filled, Fill Method: Horizontal)
│   └── PercentText (TMP — "75%", Width 45, Right, Size 14, #88AA66)
└── ValueText      (TMP — "750/1000", Right, Size 14, #CCDDBB)
```

**BarFill Image settings:**
- Image Type: **Filled**
- Fill Method: **Horizontal**
- Fill Origin: Left
- Fill Amount: 0.75 (set at runtime by ShopUIManager)

### 6.6 BottomBar (Navigation + Buy)

Create empty GO `BottomBar`. Child of ShopCanvas.

| RectTransform | Value |
|---|---|
| Anchor | Bottom-Center |
| Pivot | (0.5, 0) |
| Pos Y | 30 |
| Width | 700 |
| Height | 80 |

Add `Horizontal Layout Group`:
- Spacing: 20
- Child Alignment: Middle Center
- Child Force Expand Width: OFF

Inside BottomBar:

**LeftArrowButton** — UI → Button
- Layout Element → Preferred Width: 60, Height: 60
- Source Image: ArrowLeft sprite, Color: #AABBCC

**PageIndicator** — UI → TextMeshPro
- Layout Element → Preferred Width: 120, Height: 40
- Font: Bold, Size: 22, Color: #AABBCC, Alignment: Center

**BuyButton** — UI → Button
- Layout Element → Preferred Width: 340, Height: 65
- Source Image: ButtonNormal, Color: #3366CC (buyColor — runtime controlled)
- Child TMP text: "BUY - 150 💎", Bold, Size: 26, Color: White

**RightArrowButton** — UI → Button
- Layout Element → Preferred Width: 60, Height: 60  
- Source Image: ArrowRight sprite, Color: #AABBCC

### 6.7 BackButton

Create UI → Button. Name `BackButton`. Child of ShopCanvas (top-left corner).

| RectTransform | Value |
|---|---|
| Anchor | Top-Left |
| Pivot | (0, 1) |
| Pos X | 20 |
| Pos Y | -20 |
| Width | 120 |
| Height | 48 |

- Source Image: ButtonNormal, Color: #1A2010 at alpha 200
- Text: "← BACK", Bold, Size: 20, Color: #AABB88

---

## Step 7: ShopUIManager Inspector Drag-and-Drop

Select the ShopCanvas GO (which has ShopUIManager.cs).

| Field | Drag From |
|---|---|
| **crystalBalanceText** | TopBar → CrystalBalanceText |
| **crystalIcon** | TopBar → CrystalIcon |
| **infoPanelGroup** | InfoPanel (CanvasGroup) |
| **itemNameText** | InfoPanel → ItemName |
| **itemDescriptionText** | InfoPanel → ItemDescription |
| **passiveText** | InfoPanel → PassiveText |
| **hpBar.barRoot** | StatBars → StatRow_HP (GameObject) |
| **hpBar.labelText** | StatRow_HP → LabelText |
| **hpBar.valueText** | StatRow_HP → ValueText |
| **hpBar.fillImage** | StatRow_HP → BarContainer → BarBG → BarFill |
| **hpBar.percentText** | StatRow_HP → BarContainer → PercentText |
| *(repeat for speedBar, radiusBar, damageBar)* | StatRow_Speed/Radius/Damage |
| **buyButton** | BottomBar → BuyButton (Button component) |
| **buyButtonImage** | BottomBar → BuyButton (Image component) |
| **buyButtonText** | BottomBar → BuyButton → Text (TMP) |
| **buyButtonRect** | BottomBar → BuyButton (RectTransform) |
| **buyColor** | `#3366CC` (default blue — set in Inspector) |
| **selectColor** | `#336622` (green) |
| **equippedColor** | `#444444` (grey) |
| **cantAffordColor** | `#882222` (red) |
| **leftArrowButton** | BottomBar → LeftArrowButton |
| **rightArrowButton** | BottomBar → RightArrowButton |
| **pageIndicator** | BottomBar → PageIndicator |
| **backButton** | BackButton |
| **panelFadeSpeed** | 8 |
| **statBarAnimSpeed** | 4 |

---

## Step 8: ShopCharacterLoader (Gameplay Scene)

This script goes in the **gameplay scene** (not the shop scene). It reads PlayerPrefs set by the shop and applies stats when a run starts.

1. Select the same GO that has `PlayerController`
2. Add `ShopCharacterLoader.cs`
3. Fill the Inspector:

| Field | Value |
|---|---|
| **allItems** (array) | Drag ALL ShopItemData ScriptableObjects here (every item) |
| **playerController** | Drag the PlayerController component (or leave empty — auto-finds) |
| **modelParent** | The Transform where the character model sits (optional — for visual swaps) |

---

## Step 9: Navigation from Main Menu

In `MainMenuManager.cs`, the Shop button should load the shop scene:

```csharp
public void OpenShop()
{
    SceneManager.LoadScene("Shop");
}
```

Wire the Shop button's `onClick` → `MainMenuManager.OpenShop()`.

In Build Settings (**File → Build Settings**), add both scenes:
- Index 0: `MainMenu`
- Index 1: `Game`  
- Index 2: `Shop`

---

## Step 10: Quick Test Checklist

- [ ] All pedestals visible, each with correct ShopItemData
- [ ] Camera starts facing the equipped (or first) pedestal
- [ ] Left/Right arrows navigate smoothly, camera lerps
- [ ] Page indicator shows "1 / 5" etc.
- [ ] Item name, description, stats update on navigation
- [ ] Default items show "EQUIPPED", locked items show "BUY - X"
- [ ] Buying spends crystals (check SaveManager.GetTotalCrystals() before/after)
- [ ] Owned items show "SELECT", clicking equips them
- [ ] Error shake when not enough crystals
- [ ] Back button returns to MainMenu
- [ ] Reloading gameplay scene applies equipped character stats

---

## Color Reference

| Purpose | Hex |
|---|---|
| Background | #0D1210 |
| Panel BG | #1A2010 |
| Item Name | #FFCC33 |
| Description | #BBCCAA |
| Passive | #99AAFF |
| Buy button | #3366CC |
| Select button | #336622 |
| Equipped button | #444444 |
| Can't afford | #882222 |
| HP bar fill | #CC3333 |
| Speed bar fill | #33AACC |
| Radius bar fill | #33CC77 |
| Damage bar fill | #CC8833 |
| Crystal text | #FFCC33 |
| Arrow/nav | #AABBCC |
