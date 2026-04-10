# Main Menu Scene Setup Guide

This guide walks you through building the main menu in Unity to match the mockup design.
All scripts use Inspector references — no UI is created in code.

**Color reference (from mockup):**
- Background: `#0D0E08` to `#1A1E10` (very dark green-brown)
- Title gold: `#FFCC33`
- Title shadow: `#CC6600`, `#883300`
- Subtitle: `RGBA(255, 200, 100, 115)` (0.45 alpha)
- BONK button bg: `#FFCC33` to `#CC8800` (gold gradient)
- BONK text: `#3A2200` (dark brown)
- BONK border: `#FFD855`
- BONK shadow: `#996600`
- Continue bg: `#5A8A35` to `#3A6020` (green)
- Continue text: `#D0F0A0`
- Continue border: `#6A9A45`
- Secondary btn bg: `#3A3828` to `#28261A` (dark brown)
- Secondary text: `RGBA(220, 200, 160, 179)` (0.7 alpha)
- Secondary border: `RGBA(100, 90, 60, 102)` (0.4 alpha)
- Quit text: `RGBA(180, 140, 100, 77)` (0.3 alpha)
- Crystal cyan: `#40E8FF` to `#20A0C0`
- Crystal count text: `#60E8FF`
- Upgrade slot bg: `RGBA(50, 45, 30, 179)` (0.7 alpha)
- Upgrade level text: `#C0A050`
- Vignette: black, 0.6 alpha at edges

---

## 1. Scene & Canvas Setup

1. Create a new scene called `MainMenu` (or use the existing one)
2. Delete the default camera's Skybox — set Camera **Clear Flags = Solid Color**, **Background = `#0D0E08`**
3. Add **EventSystem** (if not present): `GameObject > UI > Event System`

### Main Canvas
1. `GameObject > UI > Canvas` → rename to **"MenuCanvas"**
2. Canvas settings:
   - Render Mode: **Screen Space - Overlay**
   - Sort Order: `0`
3. **Canvas Scaler** component:
   - UI Scale Mode: **Scale With Screen Size**
   - Reference Resolution: `1920 x 1080`
   - Match: `0.5`
4. Make sure it has **Graphic Raycaster**

---

## 2. Background Layers (children of MenuCanvas)

### 2a. Background Image
1. Create `UI > Image` → rename **"BG_Gradient"**
2. Anchor: **Stretch-Stretch** (fill entire canvas)
3. Color: `#1A1E10` (dark olive)
4. If you have a gradient texture: assign it as Source Image

### 2b. Vignette Overlay
1. Create `UI > Image` → rename **"Vignette"**
2. Anchor: **Stretch-Stretch**
3. Use a radial gradient texture (transparent center → black edges)
4. Or: use a solid black image and create a circular mask
5. Color: White (full), the texture handles the alpha
6. **Add CanvasGroup** component (for MenuAnimator reference)

### 2c. Embers Layer
1. Create empty `UI > Empty` → rename **"EmbersArea"**
2. Add **RectTransform**: Anchor stretch-stretch, fill entire canvas
3. Add **MenuEmberParticle** script
4. Create ember prefab:
   - `UI > Image` (4x4 px), use a small circle sprite (or Unity's default "Knob")
   - Color: white (script will tint it)
   - Save as prefab in `Assets/Prefabs/UI/EmberPrefab`
5. Assign **emberPrefab** in the Inspector

---

## 3. Content Layout

### 3a. Title Area (child of MenuCanvas)
1. Create empty → rename **"TitleArea"**
2. Anchor: **Top-Center**
3. Position Y: about `-180` (below top edge)
4. Width: `800`, Height: `120`

**Title Text:**
1. Inside TitleArea, `UI > TextMeshPro` → rename **"TitleText"**
2. Text: `MEGABONK`
3. Font: Use a chunky/display font (import Bungee Shade from Google Fonts, or any bold display font)
4. Size: `90`
5. Color: `#FFCC33` (gold)
6. Alignment: Center-Center
7. Add **Material Preset** or **Outline**: Color `#CC6600`, Thickness `0.15`
8. Optionally add **Underlay**: Color `#883300`, Offset X: `3`, Y: `-3`
9. **Add CanvasGroup** component

**Subtitle Text:**
1. Inside TitleArea, `UI > TextMeshPro` → rename **"SubtitleText"**
2. Text: `BONK OR BE BONKED`
3. Size: `16`
4. Color: `RGBA(255, 200, 100, 115)` — gold with 0.45 alpha
5. Character Spacing: `12`
6. Alignment: Center
7. **Add CanvasGroup** component

### 3b. Main Area (child of MenuCanvas)
1. Create empty → rename **"MainArea"**
2. Anchor: **Middle-Center**
3. Position: `(0, -20)`
4. Size: `900 x 400`
5. Add **Horizontal Layout Group**:
   - Spacing: `60`
   - Child Alignment: Middle Center
   - Control Child Size: Width OFF, Height OFF

---

## 4. Character Preview (child of MainArea)

1. Create empty → rename **"CharacterArea"**
2. Size: `240 x 340`
3. **Add CanvasGroup** component (for MenuAnimator)

**Option A: 3D Character Preview (recommended)**
1. Create a second camera → rename **"CharPreviewCam"**
   - Clear Flags: Solid Color, Background: transparent (alpha = 0)
   - Culling Mask: only "UI" layer or a custom "MenuPreview" layer
2. Create a **Render Texture** (`Assets > Create > Render Texture`), 512x512
3. Assign it to CharPreviewCam's Target Texture
4. Place your player model in front of CharPreviewCam (on the preview layer)
5. Add **MenuCharacterSpin** script to the model
6. In CharacterArea, create `UI > Raw Image` → assign the Render Texture
7. Add **MenuCameraParallax** to CharPreviewCam

**Option B: 2D Sprite (simpler)**
1. Inside CharacterArea, `UI > Image` → assign a character sprite
2. Below it, `UI > Image` → small dark ellipse for shadow, rename **"CharShadow"**

**Character Label:**
1. `UI > TextMeshPro` below character → "DRAG TO SPIN"
2. Size: `11`, Color: `RGBA(255,255,255,51)` (0.2 alpha)
3. Letter Spacing: `3`

---

## 5. Buttons Panel (child of MainArea)

1. Create empty → rename **"ButtonsPanel"**
2. Size: Width `320`, flexible height
3. Add **Vertical Layout Group**:
   - Spacing: `10`
   - Child Alignment: Upper Center
   - Control Child Size: Width ON, Height OFF
   - Force Expand Width: ON

### 5a. Crystal Bar (first child of ButtonsPanel)
1. Create `UI > Image` → rename **"CrystalBar"**
2. Preferred Height (Layout Element): `50`
3. Color: `RGBA(60, 200, 220, 20)` (0.08 alpha)
4. Add **Outline** component: Color `RGBA(60, 200, 220, 38)`, Distance `(2, -2)`
5. Add **CanvasGroup** component
6. Add **Horizontal Layout Group**: Spacing `10`, Padding `10/16/10/16`

**Inside CrystalBar:**
- `UI > Image` → rename **"CrystalGem"**: Size `22x22`, Color `#40E8FF`, use a diamond/pentagon sprite
- `UI > TextMeshPro` → rename **"CrystalCount"**: Size `20`, Color `#60E8FF`, bold
- `UI > TextMeshPro` → rename **"CrystalLabel"**: Text `CRYSTALS`, Size `10`, Color `RGBA(60, 220, 255, 89)` (0.35 alpha), Letter Spacing `3`

### 5b. BONK! Button
1. `UI > Button - TextMeshPro` → rename **"BonkButton"**
2. Layout Element: Preferred Height `65`
3. **Image** color: `#FFCC33`
4. **If using a 9-slice sprite:** create one with rounded corners (12px radius), assign it
5. **Add Outline** component: Color `#FFD855`, Distance `(3, -3)`
6. Child TextMeshPro: Text `BONK!`, Size `26`, Color `#3A2200`, Bold
7. **Add CanvasGroup** component (for MenuAnimator button pop-in)
8. **Add UIButtonEffects** script

**Shine Overlay (child of BonkButton):**
1. `UI > Image` → rename **"ShineOverlay"**
2. Anchor: Stretch vertically, width `80px`
3. Color: `RGBA(255, 255, 255, 64)` (0.25 alpha)
4. Rotation Z: `-20` degrees (skew effect)
5. Raycast Target: **OFF**
6. Position off-screen left initially (MenuAnimator will sweep it)

### 5c. Continue Button
1. `UI > Button - TextMeshPro` → rename **"ContinueButton"**
2. Layout Element: Preferred Height `52`
3. Image color: `#5A8A35` (green)
4. Add Outline: Color `#6A9A45`
5. Child text: `CONTINUE`, Size `18`, Color `#D0F0A0`
6. **Add CanvasGroup** (for disabled alpha + MenuAnimator)
7. **Add UIButtonEffects**

### 5d. Spacer
1. Create empty → rename **"Spacer"**
2. Layout Element: Preferred Height `4`

### 5e. Achievements Button
1. `UI > Button - TextMeshPro` → rename **"AchievementsButton"**
2. Layout Element: Preferred Height `48`
3. Image color: `#3A3828` (dark brown)
4. Add Outline: Color `RGBA(100, 90, 60, 102)`
5. Child text: `ACHIEVEMENTS`, Size `14`, Color `RGBA(220, 200, 160, 179)`
6. **Add CanvasGroup + UIButtonEffects**

### 5f. Options Button
1. Same as Achievements but text: `OPTIONS`
2. rename **"OptionsButton"**
3. **Add CanvasGroup + UIButtonEffects**

### 5g. Quit Button
1. `UI > Button - TextMeshPro` → rename **"QuitButton"**
2. Layout Element: Preferred Height `42`
3. Image color: `RGBA(0, 0, 0, 0)` (transparent)
4. Add Outline: Color `RGBA(100, 80, 50, 38)` (0.15 alpha)
5. Child text: `QUIT`, Size `13`, Color `RGBA(180, 140, 100, 77)`
6. **Add CanvasGroup + UIButtonEffects**

---

## 6. Meta Upgrades Area (child of MenuCanvas)

1. Create empty → rename **"UpgradesArea"**
2. Anchor: **Bottom-Center**
3. Position Y: `70` (from bottom)
4. Width: `500`, Height: `120`
5. **Add CanvasGroup** component

**Label:**
1. `UI > TextMeshPro` → "META UPGRADES"
2. Size: `10`, Color: `RGBA(200, 180, 120, 77)`, Letter Spacing: `4`, alignment center-top

**Upgrades Row:**
1. Create empty → rename **"UpgradesRow"**
2. Add **Horizontal Layout Group**: Spacing `8`, Child Alignment: Middle Center

**For each upgrade (Health, Speed, Damage, Armor, Magnet):**
1. Create empty → rename e.g. **"Slot_Health"**
2. Size: `70 x 85`
3. Background: `UI > Image`, Color `RGBA(50, 45, 30, 179)`
4. Round corners: use a 9-slice rounded rect sprite
5. Add Outline: Color `RGBA(100, 90, 50, 51)`
6. Add **Vertical Layout Group**: Spacing `2`, Padding `8`, Child Alignment Upper Center

**Inside each slot:**
- `UI > TextMeshPro` → **icon/emoji** text (or use an Image with icon sprite): Size `22`
- `UI > TextMeshPro` → **name**: "HEALTH", Size `8`, Color `RGBA(200, 180, 130, 102)`
- `UI > TextMeshPro` → **level**: "0/10", Size `10`, Color `#C0A050`, Font: bold
- `UI > Image` → **fill bar background**: Height `3`, Color `RGBA(255, 255, 255, 15)`
  - Inside it: `UI > Image` → **fill**: Color gradient `#C09030` to `#E0B040`, **Image Type: Filled**, Fill Method: Horizontal
- `UI > Button (invisible)` → covers the entire slot, for buying
7. Add **MetaUpgradeSlot** script to each slot
8. Set upgradeID: `MetaHealth`, `MetaSpeed`, `MetaDamage`, `MetaArmor`, `MetaMagnet`

---

## 7. Bottom Bar (child of MenuCanvas)

1. Create `UI > Image` → rename **"BottomBar"**
2. Anchor: **Bottom-Stretch**
3. Height: `40`
4. Color: `RGBA(0, 0, 0, 102)` (0.4 alpha)
5. Inside: `UI > TextMeshPro` → "v0.3.0 alpha"
6. Size: `10`, Color: `RGBA(255, 255, 255, 26)` (0.1 alpha)

---

## 8. Script Wiring

### On an empty GameObject "MenuManager":
1. Add **MainMenuManager** script
2. Assign:
   - `crystalCountText` → CrystalCount TextMeshPro
   - `bonkButton` → BonkButton
   - `continueButton` → ContinueButton
   - `achievementsButton` → AchievementsButton
   - `optionsButton` → OptionsButton
   - `quitButton` → QuitButton
   - `continueCanvasGroup` → the CanvasGroup on ContinueButton
   - `gameSceneName` → your game scene name

### On the same (or another) GameObject "MenuAnimator":
1. Add **MenuAnimator** script
2. Assign:
   - `titleRect` → TitleText RectTransform
   - `titleGroup` → TitleText CanvasGroup
   - `subtitleGroup` → SubtitleText CanvasGroup
   - `characterGroup` → CharacterArea CanvasGroup
   - `characterRect` → CharacterArea RectTransform
   - `buttonRects` → array of 5 elements: BonkButton, ContinueButton, AchievementsButton, OptionsButton, QuitButton (RectTransforms)
   - `crystalBarGroup` → CrystalBar CanvasGroup
   - `crystalBarRect` → CrystalBar RectTransform
   - `upgradesGroup` → UpgradesArea CanvasGroup
   - `upgradesRect` → UpgradesArea RectTransform
   - `shineRect` → ShineOverlay RectTransform
   - `vignetteGroup` → Vignette CanvasGroup

### On the camera:
1. Add **MenuCameraParallax** (optional, for 3D preview)

### On the character model (if using 3D preview):
1. Add **MenuCharacterSpin**
2. Add a **Box Collider** (required by the script)

---

## 9. Hierarchy Summary

```
MainMenu (Scene)
├── Main Camera (Background: #0D0E08)
│   └── MenuCameraParallax (optional)
├── EventSystem
├── MenuManager (empty GO)
│   ├── MainMenuManager (script)
│   └── MenuAnimator (script)
├── MenuCanvas
│   ├── BG_Gradient (Image, stretch)
│   ├── Vignette (Image + CanvasGroup, stretch)
│   ├── EmbersArea (empty + MenuEmberParticle, stretch)
│   ├── TitleArea
│   │   ├── TitleText (TMP + CanvasGroup) — "MEGABONK"
│   │   └── SubtitleText (TMP + CanvasGroup) — "BONK OR BE BONKED"
│   ├── MainArea (HorizontalLayoutGroup)
│   │   ├── CharacterArea (CanvasGroup)
│   │   │   ├── CharPreview (RawImage or Image)
│   │   │   ├── CharShadow (Image)
│   │   │   └── CharLabel (TMP) — "DRAG TO SPIN"
│   │   └── ButtonsPanel (VerticalLayoutGroup)
│   │       ├── CrystalBar (Image + CanvasGroup + HorizontalLayoutGroup)
│   │       │   ├── CrystalGem (Image)
│   │       │   ├── CrystalCount (TMP)
│   │       │   └── CrystalLabel (TMP)
│   │       ├── BonkButton (Button + CanvasGroup + UIButtonEffects)
│   │       │   ├── Text (TMP) — "BONK!"
│   │       │   └── ShineOverlay (Image)
│   │       ├── ContinueButton (Button + CanvasGroup + UIButtonEffects)
│   │       │   └── Text (TMP) — "CONTINUE"
│   │       ├── Spacer (LayoutElement)
│   │       ├── AchievementsButton (Button + CanvasGroup + UIButtonEffects)
│   │       │   └── Text (TMP) — "ACHIEVEMENTS"
│   │       ├── OptionsButton (Button + CanvasGroup + UIButtonEffects)
│   │       │   └── Text (TMP) — "OPTIONS"
│   │       └── QuitButton (Button + CanvasGroup + UIButtonEffects)
│   │           └── Text (TMP) — "QUIT"
│   ├── UpgradesArea (CanvasGroup)
│   │   ├── UpgradesLabel (TMP) — "META UPGRADES"
│   │   └── UpgradesRow (HorizontalLayoutGroup)
│   │       ├── Slot_Health (MetaUpgradeSlot)
│   │       ├── Slot_Speed (MetaUpgradeSlot)
│   │       ├── Slot_Damage (MetaUpgradeSlot)
│   │       ├── Slot_Armor (MetaUpgradeSlot)
│   │       └── Slot_Magnet (MetaUpgradeSlot)
│   └── BottomBar (Image)
│       └── VersionText (TMP) — "v0.3.0 alpha"
├── CharPreviewCam (optional, for 3D character)
└── PlayerModel (optional, for 3D character preview)
```

---

## 10. Fonts to Import

1. Download **Bungee** and **Bungee Shade** from Google Fonts
2. Import `.ttf` files into `Assets/Fonts/`
3. Create TMP Font Assets: `Window > TextMeshPro > Font Asset Creator`
   - Source Font: Bungee Shade (for title)
   - Source Font: Bungee (for buttons, labels)
4. Assign to the corresponding TextMeshPro objects

---

## 11. Tips

- For rounded button backgrounds: create a simple 32x32 white rounded rect in any image editor, import to Unity, set **Sprite Mode: Single**, **Mesh Type: Tight**, set Border in Sprite Editor for 9-slice
- For the vignette: use a 512x512 radial gradient texture (white center → black edges), set image Color to black
- All button Transition modes can be set to **None** since UIButtonEffects handles the visual feedback
- Set button **Navigation** to **None** to avoid arrow key navigation issues
- For the crystal gem icon: use a pentagon-shaped sprite tinted cyan, or a simple diamond shape
