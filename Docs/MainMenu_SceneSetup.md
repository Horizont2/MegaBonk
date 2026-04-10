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

### Scene
1. Open your existing MainMenu scene, OR: `File > New Scene` → save as **"MainMenu"**
2. Add it to Build Settings: `File > Build Settings` → drag MainMenu scene into the list (index 0)
3. Delete any existing UI objects if starting fresh

### Camera
1. Select **Main Camera** in Hierarchy
2. In Inspector:
   - Clear Flags: **Solid Color** (not Skybox!)
   - Background: `#0D0E08` (R:13, G:14, B:8 — almost black with green tint)
   - Projection: **Perspective** (default)
   - Culling Mask: **Everything** (default)

### EventSystem
1. Check if EventSystem exists in Hierarchy
2. If NOT: `GameObject > UI > Event System` (creates it automatically)
3. No settings to change

### MenuCanvas
1. `GameObject > UI > Canvas` → rename to **"MenuCanvas"**
2. **Canvas** component:
   - Render Mode: **Screen Space - Overlay**
   - Sort Order: `0`
3. **Canvas Scaler** component (already added automatically):
   - UI Scale Mode: **Scale With Screen Size**
   - Reference Resolution: X = `1920`, Y = `1080`
   - Screen Match Mode: **Match Width Or Height**
   - Match: `0.5` (balanced between width/height)
4. **Graphic Raycaster** — already added automatically, no changes needed

---

## SPRITES & TEXTURES — WHAT YOU NEED TO CREATE

Before starting, prepare these assets. You can make them in any image editor
(Photoshop, GIMP, Paint.NET, Photopea.com — free online) or find free ones.

### Required sprites list:

| # | Sprite Name | Size | Description | How to make |
|---|------------|------|-------------|-------------|
| 1 | **RoundedRect.png** | 32x32 | White rounded rectangle, 12px corner radius | Draw white rect with rounded corners. Used for ALL buttons (9-slice) |
| 2 | **Vignette.png** | 512x512 | Radial gradient: transparent center → black edges | Fill black, big soft circular eraser in center |
| 3 | **SoftCircle.png** | 64x64 | White blurred circle, transparent edges | Soft brush dot on transparent background |
| 4 | **EmberDot.png** | 8x8 | Small white circle | Tiny dot, or use Unity built-in "Knob" sprite instead |
| 5 | **TreeSilhouette.png** | 64x128 | Dark pine/fir tree shape on transparent bg | Simple triangle-on-triangle tree shape, filled dark green-black |
| 6 | **CrystalIcon.png** | 32x32 | Diamond/pentagon shape, white | Pentagon shape, script tints it cyan |
| 7 | **BG_Gradient.png** (optional) | 1920x1080 | Vertical gradient: `#0D0E08` (top) → `#1A1E10` (middle) → `#0D1008` (bottom) | Or skip this and just use solid color |

### How to import sprites into Unity:
1. Create folder: `Assets/Textures/UI/`
2. Drag all PNGs into this folder
3. For EACH sprite, select it in Project window → Inspector:
   - Texture Type: **Sprite (2D and UI)**
   - Sprite Mode: **Single**
   - Max Size: keep as-is or `256`
   - Filter Mode: **Bilinear**
   - Click **Apply**
4. **For RoundedRect.png specifically** (9-slice):
   - Click **Sprite Editor** button
   - Set green Border lines: Left `12`, Right `12`, Top `12`, Bottom `12`
   - Click **Apply** in Sprite Editor
   - Now this sprite stretches cleanly to any button size

### How to create prefabs:

**EmberPrefab:**
1. In scene: `GameObject > UI > Image` → rename "EmberPrefab"
2. RectTransform: Width `4`, Height `4`
3. Image: Source Image = `EmberDot.png` (or Unity "Knob"), Color = white
4. Drag from Hierarchy into `Assets/Prefabs/UI/` → becomes prefab
5. Delete from scene

**FogPrefab:**
1. In scene: `GameObject > UI > Image` → rename "FogPrefab"
2. RectTransform: Width `300`, Height `300` (script resizes anyway)
3. Image: Source Image = `SoftCircle.png`, Color = white
4. Raycast Target: ❌ OFF
5. Drag into `Assets/Prefabs/UI/` → becomes prefab
6. Delete from scene

---

## 2. Background Layers (children of MenuCanvas)

> **IMPORTANT:** These must be the FIRST children of MenuCanvas (at the top of Hierarchy).
> Order matters: BG first (back), then Vignette, then Embers on top.
> All three use **Stretch-Stretch** anchor to fill the screen.

### 2a. Background Image
1. Right-click **MenuCanvas** → `UI > Image` → rename **"BG_Gradient"**
2. RectTransform:
   - Click the **Anchor Presets** square (top-left of RectTransform)
   - Hold **Alt+Shift** and click the **bottom-right** icon (stretch-stretch)
   - This sets: Left `0`, Right `0`, Top `0`, Bottom `0` — fills canvas
3. Image component:
   - Color: `#1A1E10` (R:26, G:30, B:16) — dark olive-green
   - Source Image: `None` (solid color is fine), OR if you made a gradient texture, assign it
4. Raycast Target: ❌ OFF (background shouldn't block clicks)

### 2b. Vignette Overlay
1. Right-click **MenuCanvas** → `UI > Image` → rename **"Vignette"**
2. RectTransform: **Stretch-Stretch** (same as above — Alt+Shift bottom-right)
3. Image component:
   - **If you have a vignette texture** (radial gradient: transparent center → black edges):
     - Source Image: assign the texture
     - Color: white `RGBA(255,255,255,255)`
   - **If you DON'T have a texture** (quick alternative):
     - Color: `RGBA(0, 0, 0, 100)` (just a semi-transparent black overlay — less pretty but works)
4. Raycast Target: ❌ OFF
5. **Add CanvasGroup** component (MenuAnimator fades this in)

**How to make a vignette texture:**
- Open any image editor (Photoshop, GIMP, Paint.NET)
- Create 512x512 canvas, fill black
- Use a soft circular eraser in the center (or radial gradient: white center → black edges)
- Save as PNG, import to `Assets/Textures/UI/vignette.png`
- In Unity Import Settings: Texture Type = **Sprite (2D and UI)**

### 2c. Fog Layer
1. Right-click **MenuCanvas** → `Create Empty` → rename **"FogArea"**
2. RectTransform: **Stretch-Stretch** (same as above)
3. Add **MenuFogEffect** script
4. Inspector settings:

| Slot | Value |
|---|---|
| `Fog Prefab` | Drag **FogPrefab** from `Assets/Prefabs/UI/` |
| `Max Particles` | `5` |
| `Spawn Interval` | `5` |
| `Min Speed` | `20` |
| `Max Speed` | `50` |
| `Fog Color` | `RGBA(80, 120, 40, 15)` — green tint, very low alpha (~0.06) |
| `Min Size` | `250` |
| `Max Size` | `500` |
| `Lifetime` | `30` |

> These are big slow-moving green blobs that drift across the screen.
> Very subtle — barely visible. That's intentional, it adds atmosphere.

### 2d. Embers Layer
1. Right-click **MenuCanvas** → `Create Empty` → rename **"EmbersArea"**
2. RectTransform: **Stretch-Stretch** (same as above)
3. Add **MenuEmberParticle** script
4. Inspector settings:

| Slot | Value |
|---|---|
| `Ember Prefab` | Drag **EmberPrefab** from `Assets/Prefabs/UI/` |
| `Max Embers` | `20` |
| `Spawn Interval` | `0.3` |
| `Min Rise Speed` | `30` |
| `Max Rise Speed` | `80` |
| `Horizontal Drift` | `15` |
| `Lifetime` | `4` |
| `Min Size` | `2` |
| `Max Size` | `5` |
| `Ember Colors` | Array size `3`: |
| — Element 0 | `RGBA(255, 180, 50, 179)` — warm orange |
| — Element 1 | `RGBA(255, 140, 30, 153)` — deep orange |
| — Element 2 | `RGBA(255, 220, 100, 128)` — yellow-ish |

> These are tiny bright dots that float upward from the bottom half of the screen,
> like campfire embers. They fade and shrink as they rise.

### 2e. Tree Silhouettes
1. Right-click **MenuCanvas** → `Create Empty` → rename **"TreesArea"**
2. RectTransform:
   - Anchor: **Bottom-Stretch** (stretch left-right, pinned to bottom)
   - Height: `200`
   - Pos Y: `0`, Left: `0`, Right: `0`
3. Add **MenuTreeSilhouettes** script
4. Inspector settings:

| Slot | Value |
|---|---|
| `Tree Sprite` | Drag **TreeSilhouette** sprite from `Assets/Textures/UI/` |
| `Tree Count` | `14` |
| `Min Scale` | `0.5` |
| `Max Scale` | `1.3` |
| `Base Height` | `120` |
| `Tree Color` | `RGBA(15, 18, 8, 255)` — near-black dark green |
| `Min Alpha` | `0.3` |
| `Max Alpha` | `0.7` |
| `Parallax Amount` | `5` (trees shift slightly with mouse) |

> Trees spawn along the bottom of the screen as dark silhouettes.
> Bigger trees have more opacity (appear closer). They shift slightly with mouse movement.

### Background Hierarchy Order (IMPORTANT — back to front):
```
MenuCanvas
  ├── BG_Gradient     ← 1st (furthest back)
  ├── FogArea         ← 2nd
  ├── TreesArea       ← 3rd (trees in front of fog)
  ├── EmbersArea      ← 4th (embers in front of trees)
  ├── Vignette        ← 5th (darkens edges over everything)
  ├── TitleArea       ← 6th (UI content starts here)
  ├── MainArea        ← 7th
  ├── UpgradesArea    ← 8th
  └── BottomBar       ← 9th
```

---

## 3. Content Layout

### 3a. Title Area (child of MenuCanvas)
1. Right-click **MenuCanvas** → `Create Empty` → rename **"TitleArea"**
2. RectTransform:
   - Anchor: **Top-Center** (click anchor presets, pick top-center)
   - Pivot: `(0.5, 1)` (top edge is the origin)
   - Pos X: `0`, Pos Y: `-120` (120px below top edge)
   - Width: `800`, Height: `130`
3. Add **Vertical Layout Group** (to stack title + subtitle):
   - Spacing: `4`
   - Child Alignment: **Upper Center**
   - Control Child Size: Width ✅ ON, Height ❌ OFF

**Title Text:**
1. Right-click **TitleArea** → `UI > Text - TextMeshPro` → rename **"TitleText"**
2. Add **Layout Element**: Preferred Height `90`
3. TextMeshPro settings:
   - Text: `MEGABONK`
   - Font Asset: **Bungee Shade SDF** (or your bold display font)
   - Font Size: `90`
   - Color: `#FFCC33` (R:255, G:204, B:51)
   - Alignment: Center-Center
   - Font Style: (none needed — Bungee Shade is already decorative)
4. **TMP Outline effect** (in Material settings or Extra Settings):
   - Outline → Thickness: `0.15`
   - Outline → Color: `#CC6600` (dark orange)
5. **TMP Underlay** (fake 3D shadow):
   - Underlay → Color: `#883300`
   - Offset X: `3`, Offset Y: `-3`
   - Dilate: `0.1`
6. **Add CanvasGroup** component (for MenuAnimator title fade)

**Subtitle Text:**
1. Right-click **TitleArea** → `UI > Text - TextMeshPro` → rename **"SubtitleText"**
2. Add **Layout Element**: Preferred Height `25`
3. TextMeshPro settings:
   - Text: `BONK OR BE BONKED`
   - Font Asset: **Bungee SDF** (not Shade — simpler)
   - Font Size: `16`
   - Color: `RGBA(255, 200, 100, 115)` (R:255 G:200 B:100 A:115 — gold, 0.45 alpha)
   - Character Spacing: `12` (wide letterspace)
   - Alignment: Center-Center
   - Font Style: **Uppercase**
4. **Add CanvasGroup** component

### 3b. Main Area (child of MenuCanvas) — CENTRAL LAYOUT

This is the core container that holds the character on the LEFT and buttons on the RIGHT.

1. Right-click **MenuCanvas** → `Create Empty` → rename **"MainArea"**
2. RectTransform:
   - Anchor Preset: **Middle-Center** (click the anchor presets square in Inspector)
   - Pivot: `(0.5, 0.5)`
   - Pos X: `0`, Pos Y: `-20` (slightly below center — title takes upper space)
   - Width: `660`, Height: `420`
3. Add **Horizontal Layout Group**:
   - Spacing: `60` (gap between character and buttons)
   - Child Alignment: **Middle Center**
   - Control Child Size: Width ❌ OFF, Height ❌ OFF
   - Force Expand: Width ❌ OFF, Height ❌ OFF
4. Optionally add **Content Size Fitter** → Horizontal Fit: Preferred Size (auto-width from children)

**Visual map of the screen (1920x1080):**
```
┌─────────────────────────────────────────────────┐
│                                                 │
│              M E G A B O N K                    │ ← TitleArea (top-center, Y: -180)
│            bonk or be bonked                    │
│                                                 │
│       ┌─────────┐    ┌───────────────┐          │
│       │         │    │  💎 1,247     │          │
│       │  CHAR   │    │  ══BONK!══   │          │
│       │  ACTER  │60px│  Continue     │          │ ← MainArea (center, Y: -20)
│       │         │gap │  ───────      │          │
│       │   🔨    │    │  Achievements │          │
│       │         │    │  Options      │          │
│       └─────────┘    │  Quit         │          │
│        drag to spin  └───────────────┘          │
│                                                 │
│         ❤️  ⚡  ⚔️  🛡️  🧲                      │ ← UpgradesArea (bottom-center, Y: 70)
│        META UPGRADES                            │
│                                                 │
│  v0.3.0 alpha                                   │ ← BottomBar
└─────────────────────────────────────────────────┘
```

---

## 4. Character Preview (LEFT child of MainArea)

1. Right-click **MainArea** → `Create Empty` → rename **"CharacterArea"**
2. Add **Layout Element**: Preferred Width `240`, Preferred Height `340`
3. RectTransform: Width `240`, Height `340`, Pivot `(0.5, 0.5)`
4. **Add CanvasGroup** component (for MenuAnimator fade-in)

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

## 5. Buttons Panel (child of MainArea) — DETAILED LAYOUT

The buttons sit on the RIGHT side of MainArea next to the character.
The Vertical Layout Group handles positioning — you only set Preferred Heights.

### ButtonsPanel Container
1. Right-click **MainArea** → `UI > Create Empty` → rename **"ButtonsPanel"**
2. RectTransform:
   - Width: `320`  Height: `420` (will be controlled by layout)
   - Pivot: `(0.5, 0.5)`
3. Add **Layout Element**: Preferred Width `320`
4. Add **Vertical Layout Group**:
   - Spacing: `10`
   - Child Alignment: **Upper Center**
   - Control Child Size: Width ✅ ON, Height ❌ OFF
   - Force Expand Width: ✅ ON, Height: ❌ OFF
   - Padding: Left `0`, Right `0`, Top `0`, Bottom `0`

> **HOW IT WORKS:** Every child gets the full 320px width automatically.
> The height of each child is set via **Layout Element → Preferred Height**.
> The spacing between children is 10px. Children stack top-to-bottom.

### Visual guide (what the final column looks like):

```
┌──────────────── 320px ────────────────┐
│  💎 1,247          CRYSTALS           │ ← CrystalBar (h:50)
├───────────────────────────────────────┤
│                                       │ 10px gap
├───────────────────────────────────────┤
│                                       │
│              B O N K !                │ ← BonkButton (h:65) GOLD
│                                       │
├───────────────────────────────────────┤
│                                       │ 10px gap
├───────────────────────────────────────┤
│            CONTINUE                   │ ← ContinueButton (h:52) GREEN
├───────────────────────────────────────┤
│                                       │ 10px gap
├───────────────────────────────────────┤
│                                       │ Spacer (h:4)
├───────────────────────────────────────┤
│                                       │ 10px gap
├───────────────────────────────────────┤
│          ACHIEVEMENTS                 │ ← AchievementsBtn (h:48) BROWN
├───────────────────────────────────────┤
│                                       │ 10px gap
├───────────────────────────────────────┤
│            OPTIONS                    │ ← OptionsButton (h:48) BROWN
├───────────────────────────────────────┤
│                                       │ 10px gap
├───────────────────────────────────────┤
│              QUIT                     │ ← QuitButton (h:42) TRANSPARENT
└───────────────────────────────────────┘
```

Total height: 50 + 65 + 52 + 4 + 48 + 48 + 42 + (6 × 10 gap) = **369px**

---

### 5a. Crystal Bar (1st child of ButtonsPanel)

**Creating:**
1. Right-click **ButtonsPanel** → `UI > Image` → rename **"CrystalBar"**

**RectTransform (handled by layout — only set preferred height):**
- Add **Layout Element** component: Preferred Height = `50`

**Image component:**
- Color: `RGBA(60, 200, 220, 20)` → hex `#3CC8DC` with alpha = `20` (≈0.08)
- Source Image: use a rounded rect 9-slice sprite (12px corners), or leave as default

**Additional components:**
- Add **Outline**: Effect Color `RGBA(60, 200, 220, 38)`, Effect Distance `(2, -2)`
- Add **CanvasGroup** (for MenuAnimator fade reference)
- Add **Horizontal Layout Group**:
  - Spacing: `10`
  - Padding: Left `16`, Right `16`, Top `10`, Bottom `10`
  - Child Alignment: **Middle Left**
  - Control Child Size: Width ❌, Height ✅

**Children of CrystalBar (left to right):**

1. **CrystalGem** — `UI > Image`
   - Layout Element: Preferred Width `22`, Preferred Height `22`
   - Color: `#40E8FF`
   - Source Image: pentagon or diamond sprite
   
2. **CrystalCount** — `UI > TextMeshPro`
   - Layout Element: Preferred Width `100`
   - Font Size: `20`, Color: `#60E8FF`, Style: **Bold**
   - Text: `0` (script updates it)
   - Alignment: Middle-Left

3. **CrystalLabel** — `UI > TextMeshPro`
   - Layout Element: Flexible Width `1` (fills remaining space)
   - Font Size: `10`, Color: `RGBA(60, 220, 255, 89)` (0.35 alpha)
   - Text: `CRYSTALS`
   - Character Spacing: `3`, Alignment: Middle-Right

---

### 5b. BONK! Button (2nd child) — THE BIG GOLD BUTTON

**Creating:**
1. Right-click **ButtonsPanel** → `UI > Button - TextMeshPro` → rename **"BonkButton"**

**Layout Element:** Preferred Height = `65`

**Image component (Button background):**
- Color: `#FFCC33` (bright gold)
- Source Image: rounded rect 9-slice sprite (12px corners)

**Outline component:**
- Effect Color: `#FFD855` (lighter gold border)
- Effect Distance: `(3, -3)` ← gives a thick 3D border feel

**Shadow effect — add a SECOND Image under the button? No — use Unity's Shadow component:**
- Add **Shadow** component: Effect Color `#996600`, Distance `(0, -4)`

**Button component:**
- Transition: **None** (UIButtonEffects handles visuals)
- Navigation: **None**

**Additional components:**
- Add **CanvasGroup** (alpha=1, needed for MenuAnimator pop-in)
- Add **UIButtonEffects** script:
  - Hover Lift: `4`
  - Hover Scale Multiplier: `1.04`
  - Press Scale: `0.95`

**Child: Text (TMP) — already created by the Button:**
- Text: `BONK!`
- Font: **Bungee** (or your display font)
- Font Size: `26`
- Color: `#3A2200` (dark brown — readable on gold)
- Style: **Bold**
- Alignment: Center-Center
- RectTransform: Anchor stretch-stretch, all offsets `0`

**Child: ShineOverlay (new Image inside BonkButton):**
1. Right-click **BonkButton** → `UI > Image` → rename **"ShineOverlay"**
2. RectTransform:
   - Anchor: vertical stretch (top=0, bottom=0)
   - Width: `80`, Height: stretch
   - Pivot: `(0.5, 0.5)`
   - Pos X: `-250` (off-screen left to start — MenuAnimator sweeps it right)
3. Image:
   - Color: `RGBA(255, 255, 255, 64)` (white, 0.25 alpha)
   - Raycast Target: ❌ **OFF** (so it doesn't block button clicks!)
4. Rotation Z: `-20` (slight diagonal skew)

---

### 5c. Continue Button (3rd child) — GREEN

**Creating:**
1. Right-click **ButtonsPanel** → `UI > Button - TextMeshPro` → rename **"ContinueButton"**

**Layout Element:** Preferred Height = `52`

**Image:** Color `#5A8A35` (green), same rounded sprite

**Outline:** Color `#6A9A45`, Distance `(2, -2)`

**Shadow:** Color `#2A4A15`, Distance `(0, -3)`

**Button:** Transition: None, Navigation: None

**Components:**
- **CanvasGroup** (used by MainMenuManager to set alpha=0.3 when disabled)
- **UIButtonEffects**: Hover Lift `3`, Hover Scale `1.03`, Press Scale `0.95`

**Child Text (TMP):**
- Text: `CONTINUE`
- Font: **Bungee**, Size `18`, Color `#D0F0A0`, Bold
- Alignment: Center-Center

---

### 5d. Spacer (4th child) — INVISIBLE SPACING

1. Right-click **ButtonsPanel** → `Create Empty` → rename **"Spacer"**
2. Add **Layout Element**: Preferred Height = `4`
3. No Image, no scripts — just empty space to visually separate primary/secondary buttons

---

### 5e. Achievements Button (5th child) — DARK BROWN

**Creating:**
1. Right-click **ButtonsPanel** → `UI > Button - TextMeshPro` → rename **"AchievementsButton"**

**Layout Element:** Preferred Height = `48`

**Image:** Color `#3A3828` (dark olive-brown), rounded sprite

**Outline:** Color `RGBA(100, 90, 60, 102)` (0.4 alpha), Distance `(2, -2)`

**Shadow:** Color `#1A1810`, Distance `(0, -2)`

**Button:** Transition: None, Navigation: None

**Components:**
- **CanvasGroup**
- **UIButtonEffects**: Hover Lift `2`, Hover Scale `1.02`

**Child Text (TMP):**
- Text: `ACHIEVEMENTS`
- Font: **Bungee**, Size `14`
- Color: `RGBA(220, 200, 160, 179)` (0.7 alpha — slightly faded)
- Alignment: Center-Center

---

### 5f. Options Button (6th child) — SAME STYLE AS ACHIEVEMENTS

1. Right-click **ButtonsPanel** → `UI > Button - TextMeshPro` → rename **"OptionsButton"**
2. **EVERYTHING same as Achievements** — same colors, outline, shadow, components
3. **Layout Element:** Preferred Height = `48`
4. Text: `OPTIONS`, Size `14`
5. Add **CanvasGroup + UIButtonEffects**

---

### 5g. Quit Button (7th child) — ALMOST INVISIBLE

**Creating:**
1. Right-click **ButtonsPanel** → `UI > Button - TextMeshPro` → rename **"QuitButton"**

**Layout Element:** Preferred Height = `42`

**Image:** Color `RGBA(0, 0, 0, 0)` — **fully transparent!** (no visible background)

**Outline:** Color `RGBA(100, 80, 50, 38)` (0.15 alpha — very faint border), Distance `(1, -1)`

**NO Shadow** on this one — it should feel minimal.

**Button:** Transition: None, Navigation: None

**Components:**
- **CanvasGroup**
- **UIButtonEffects**: Hover Lift `1`, Hover Scale `1.01` (subtle)

**Child Text (TMP):**
- Text: `QUIT`
- Font: **Bungee**, Size `13`
- Color: `RGBA(180, 140, 100, 77)` (0.3 alpha — very dim)
- Alignment: Center-Center

---

### Important: Button hierarchy order in Hierarchy

The Vertical Layout Group stacks children **top to bottom in Hierarchy order**.
Make sure the order in the Hierarchy panel is EXACTLY this:

```
ButtonsPanel
  ├── CrystalBar      ← 1st (top)
  ├── BonkButton       ← 2nd
  ├── ContinueButton   ← 3rd
  ├── Spacer           ← 4th
  ├── AchievementsButton ← 5th
  ├── OptionsButton    ← 6th
  └── QuitButton       ← 7th (bottom)
```

To reorder: drag items up/down in the Hierarchy panel.

---

### Button components checklist (quick reference)

| Button | Height | Image Color | Outline | Text Size | Text Color | UIButtonEffects Lift |
|--------|--------|------------|---------|-----------|------------|---------------------|
| CrystalBar | 50 | `RGBA(60,200,220,20)` | `RGBA(60,200,220,38)` | — | — | — |
| **BONK!** | **65** | **`#FFCC33`** | **`#FFD855`** | **26** | **`#3A2200`** | **4** |
| Continue | 52 | `#5A8A35` | `#6A9A45` | 18 | `#D0F0A0` | 3 |
| Achievements | 48 | `#3A3828` | `RGBA(100,90,60,102)` | 14 | `RGBA(220,200,160,179)` | 2 |
| Options | 48 | `#3A3828` | `RGBA(100,90,60,102)` | 14 | `RGBA(220,200,160,179)` | 2 |
| Quit | 42 | `RGBA(0,0,0,0)` | `RGBA(100,80,50,38)` | 13 | `RGBA(180,140,100,77)` | 1 |

---

## 6. Meta Upgrades Area (child of MenuCanvas) — DETAILED

### UpgradesArea Container
1. Right-click **MenuCanvas** → `Create Empty` → rename **"UpgradesArea"**
2. RectTransform:
   - Anchor: **Bottom-Center**
   - Pivot: `(0.5, 0)`
   - Pos X: `0`, Pos Y: `70`
   - Width: `450`, Height: `110`
3. Add **CanvasGroup** (for MenuAnimator fade-in)

### UpgradesLabel
1. Right-click **UpgradesArea** → `UI > TextMeshPro` → rename **"UpgradesLabel"**
2. RectTransform:
   - Anchor: **Top-Stretch** (stretch left to right)
   - Height: `20`
   - Pos Y: `0` (top of parent)
3. TextMeshPro:
   - Text: `META UPGRADES`
   - Font Size: `10`
   - Color: `RGBA(200, 180, 120, 77)` (0.3 alpha)
   - Character Spacing: `4`
   - Alignment: Center-Center
   - Style: **Uppercase**

### UpgradesRow
1. Right-click **UpgradesArea** → `Create Empty` → rename **"UpgradesRow"**
2. RectTransform:
   - Anchor: **Bottom-Stretch**
   - Height: `85`
   - Pos Y: `0`
3. Add **Horizontal Layout Group**:
   - Spacing: `8`
   - Child Alignment: **Middle Center**
   - Control Child Size: Width ❌ OFF, Height ❌ OFF
4. Add **Content Size Fitter**: Horizontal Fit: **Preferred Size** (auto-centers the row)

### Each Upgrade Slot (repeat 5 times)

Create one, then duplicate 4 times. The 5 slots are:

| Name | upgradeID | Icon (use sprite or text) |
|------|-----------|--------------------------|
| Slot_Health | `MetaHealth` | Heart icon / ❤ |
| Slot_Speed | `MetaSpeed` | Lightning / ⚡ |
| Slot_Damage | `MetaDamage` | Sword / ⚔ |
| Slot_Armor | `MetaArmor` | Shield / 🛡 |
| Slot_Magnet | `MetaMagnet` | Magnet / 🧲 |

**Creating Slot_Health (then duplicate for others):**

1. Right-click **UpgradesRow** → `Create Empty` → rename **"Slot_Health"**
2. RectTransform: Width `78`, Height `85`
3. Add **Layout Element**: Preferred Width `78`, Preferred Height `85`

**Background Image:**
4. Add **Image** component to Slot_Health itself:
   - Color: `RGBA(50, 45, 30, 179)` (0.7 alpha)
   - Source Image: rounded rect sprite (10px corners)
5. Add **Outline**: Color `RGBA(100, 90, 50, 51)` (0.2 alpha), Distance `(1, -1)`

**Layout:**
6. Add **Vertical Layout Group**:
   - Spacing: `1`
   - Padding: Left `4`, Right `4`, Top `6`, Bottom `4`
   - Child Alignment: **Upper Center**
   - Control Child Size: Width ✅ ON, Height ❌ OFF

**Children of Slot_Health (top to bottom):**

**a) IconImage** — `UI > Image`
   - Layout Element: Preferred Height `24`
   - Source Image: heart sprite (or use a red circle as placeholder)
   - Color: tinted to match the icon's theme
   - Preserve Aspect: ✅
   - **If you don't have icon sprites yet**, create `UI > TextMeshPro` instead:
     - Text: `♥` or any icon character
     - Font Size: `22`, Color: red, Alignment: Center

**b) NameText** — `UI > TextMeshPro` → rename **"NameText"**
   - Layout Element: Preferred Height `12`
   - Text: `HEALTH`
   - Font Size: `8`
   - Color: `RGBA(200, 180, 130, 102)` (0.4 alpha)
   - Alignment: Center-Center
   - Letter Spacing: `1`

**c) LevelText** — `UI > TextMeshPro` → rename **"LevelText"**
   - Layout Element: Preferred Height `14`
   - Text: `0/10`
   - Font: **Bungee** (bold)
   - Font Size: `10`
   - Color: `#C0A050` (warm gold)
   - Alignment: Center-Center

**d) FillBarBG** — `UI > Image` → rename **"FillBarBG"**
   - Layout Element: Preferred Height `4`
   - Color: `RGBA(255, 255, 255, 15)` (0.06 alpha — very faint)

**e) FillBar** (child of FillBarBG) — `UI > Image` → rename **"FillBar"**
   - RectTransform: Anchor stretch-stretch, all offsets `0` (fills parent completely)
   - Color: `#C09030` (warm gold)
   - **Image Type**: `Filled`
   - **Fill Method**: `Horizontal`
   - **Fill Origin**: `Left`
   - Fill Amount: `0` (script controls this via `fillBar.fillAmount`)

**f) BuyButton** (covers entire slot) — `UI > Button` → rename **"BuyButton"**
   - RectTransform: Anchor stretch-stretch, all offsets `0`
   - Image component: Color `RGBA(0, 0, 0, 0)` (invisible)
   - Button Transition: **None**
   - Remove the default Text child (we don't need it)

**g) CostText** (optional, inside BuyButton or beside it) — `UI > TextMeshPro`
   - Font Size: `8`, Color: `#60E8FF`, Alignment: Center
   - Text: `50` (script will update)

**Adding MetaUpgradeSlot script:**
7. Select **Slot_Health** in Hierarchy
8. Add Component → **MetaUpgradeSlot**
9. In Inspector, set:
   - `upgradeID`: `MetaHealth`
   - `maxLevel`: `10`
   - `baseCost`: `50`
   - `costMultiplier`: `1.5`
   - `nameText` ← drag **NameText** here
   - `levelText` ← drag **LevelText** here
   - `costText` ← drag **CostText** here (if created)
   - `fillBar` ← drag **FillBar** Image here
   - `buyButton` ← drag **BuyButton** here

**Duplicating for remaining slots:**
10. Select **Slot_Health** → `Ctrl+D` (duplicate) → rename **"Slot_Speed"**
11. Change: icon to lightning, NameText to `SPEED`, upgradeID to `MetaSpeed`
12. Repeat for `Slot_Damage` (MetaDamage), `Slot_Armor` (MetaArmor), `Slot_Magnet` (MetaMagnet)

**Final hierarchy of UpgradesRow:**
```
UpgradesRow (HorizontalLayoutGroup)
  ├── Slot_Health (MetaUpgradeSlot)
  │   ├── IconImage (Image or TMP)
  │   ├── NameText (TMP) — "HEALTH"
  │   ├── LevelText (TMP) — "0/10"
  │   ├── FillBarBG (Image)
  │   │   └── FillBar (Image, Filled)
  │   ├── BuyButton (Button, invisible)
  │   └── CostText (TMP) — "50"
  ├── Slot_Speed (same structure)
  ├── Slot_Damage (same structure)
  ├── Slot_Armor (same structure)
  └── Slot_Magnet (same structure)
```

---

## 7. Bottom Bar (child of MenuCanvas)

1. Right-click **MenuCanvas** → `UI > Image` → rename **"BottomBar"**
2. RectTransform:
   - Anchor: **Bottom-Stretch** (hold Alt+Shift, click bottom-stretch preset)
   - Left: `0`, Right: `0`, Pos Y: `0`
   - Height: `40`
3. Image Color: `RGBA(0, 0, 0, 102)` (0.4 alpha)
4. Raycast Target: ❌ OFF

**Version Text:**
5. Right-click **BottomBar** → `UI > TextMeshPro` → rename **"VersionText"**
6. RectTransform: Anchor bottom-left, Pos X: `25`, Pos Y: `12`
7. TextMeshPro:
   - Text: `v0.3.0 alpha`
   - Font Size: `10`
   - Color: `RGBA(255, 255, 255, 26)` (0.1 alpha — barely visible)
   - Alignment: Left-Center

---

## 8. Script Wiring — COMPLETE GUIDE

This is where you connect all the objects to the scripts. Do this LAST after all UI is built.

### Step 1: Create MenuManager GameObject

1. Right-click in Hierarchy (root level, not inside Canvas) → `Create Empty` → rename **"MenuManager"**
2. Position doesn't matter (it's not visible)

### Step 2: Add MainMenuManager script

1. Select **MenuManager** in Hierarchy
2. Inspector → Add Component → search **"MainMenuManager"** → add it
3. **Now drag-and-drop references from Hierarchy into the Inspector slots:**

| Inspector Slot | Drag This Object | What Component |
|---|---|---|
| `Crystal Count Text` | **CrystalCount** (inside CrystalBar) | TextMeshProUGUI |
| `Bonk Button` | **BonkButton** | Button |
| `Continue Button` | **ContinueButton** | Button |
| `Achievements Button` | **AchievementsButton** | Button |
| `Options Button` | **OptionsButton** | Button |
| `Quit Button` | **QuitButton** | Button |
| `Continue Canvas Group` | **ContinueButton** | CanvasGroup |
| `Game Scene Name` | Type: `GameScene` | (string field) |

> **TIP:** To drag a specific component, click the object in Hierarchy, then drag
> from the Hierarchy into the Inspector slot. Unity auto-picks the right component.

### Step 3: Add MenuAnimator script

1. Still on **MenuManager** (or create separate empty GO)
2. Add Component → **MenuAnimator**
3. **Drag references:**

| Inspector Slot | Drag This Object | Notes |
|---|---|---|
| `Title Rect` | **TitleText** | The RectTransform of the title TMP |
| `Title Group` | **TitleText** | The CanvasGroup on TitleText |
| `Subtitle Group` | **SubtitleText** | CanvasGroup on SubtitleText |
| `Character Group` | **CharacterArea** | CanvasGroup on CharacterArea |
| `Character Rect` | **CharacterArea** | RectTransform |
| `Button Rects` | (array — see below) | Size: 5 |
| `Crystal Bar Group` | **CrystalBar** | CanvasGroup |
| `Crystal Bar Rect` | **CrystalBar** | RectTransform |
| `Upgrades Group` | **UpgradesArea** | CanvasGroup |
| `Upgrades Rect` | **UpgradesArea** | RectTransform |
| `Shine Rect` | **ShineOverlay** (inside BonkButton) | RectTransform |
| `Vignette Group` | **Vignette** | CanvasGroup |

**For the `Button Rects` array:**
1. In Inspector, set Size to `5`
2. Drag into each element slot:
   - Element 0: **BonkButton**
   - Element 1: **ContinueButton**
   - Element 2: **AchievementsButton**
   - Element 3: **OptionsButton**
   - Element 4: **QuitButton**

### Step 4: MenuEmberParticle (on EmbersArea)

1. Select **EmbersArea** in Hierarchy (inside MenuCanvas)
2. It should already have **MenuEmberParticle** script (if not, add it)
3. Inspector slots:

| Inspector Slot | Value / Drag |
|---|---|
| `Ember Prefab` | Drag your **EmberPrefab** from `Assets/Prefabs/UI/` |
| `Max Embers` | `20` |
| `Spawn Interval` | `0.3` |
| `Min Rise Speed` | `30` |
| `Max Rise Speed` | `80` |
| `Horizontal Drift` | `15` |
| `Lifetime` | `4` |
| `Min Size` | `2` |
| `Max Size` | `5` |

**Creating the EmberPrefab:**
1. In scene: Right-click → `UI > Image` (anywhere temporary)
2. Rename to **"EmberPrefab"**
3. RectTransform: Width `4`, Height `4`
4. Image: Source Image = Unity built-in **"Knob"** sprite (circle), Color: white
5. Drag from Hierarchy into `Assets/Prefabs/UI/` folder → it becomes a prefab
6. Delete the instance from scene
7. Drag prefab into the `Ember Prefab` slot on MenuEmberParticle

### Step 5: UIButtonEffects (on each button)

Each button (BonkButton, ContinueButton, etc.) should already have UIButtonEffects.
If not, select the button → Add Component → **UIButtonEffects**.

Default values are fine, but for best match:

| Button | hoverLift | hoverScaleMultiplier | pressScale |
|---|---|---|---|
| BonkButton | `4` | `1.04` | `0.95` |
| ContinueButton | `3` | `1.03` | `0.95` |
| AchievementsButton | `2` | `1.02` | `0.95` |
| OptionsButton | `2` | `1.02` | `0.95` |
| QuitButton | `1` | `1.01` | `0.97` |

### Step 6: MetaUpgradeSlot (on each upgrade slot)

Already covered in Section 6, but summary:

| Slot Object | upgradeID | maxLevel | baseCost |
|---|---|---|---|
| Slot_Health | `MetaHealth` | `10` | `50` |
| Slot_Speed | `MetaSpeed` | `10` | `50` |
| Slot_Damage | `MetaDamage` | `10` | `75` |
| Slot_Armor | `MetaArmor` | `10` | `75` |
| Slot_Magnet | `MetaMagnet` | `10` | `50` |

For each slot, drag its children into the script's Inspector slots:
- `nameText` ← NameText (TMP child)
- `levelText` ← LevelText (TMP child)
- `costText` ← CostText (TMP child)
- `fillBar` ← FillBar (Image child, the Filled one)
- `buyButton` ← BuyButton (Button child)

### Step 7: Camera

1. Select **Main Camera**
2. Set Clear Flags: **Solid Color**
3. Background: `#0D0E08`
4. Optionally add **MenuCameraParallax** script:
   - `Max Offset`: `0.5`
   - `Smooth Speed`: `5`

### Step 8: Character model (optional 3D preview)

If using 3D character preview:
1. Place your player model in the scene near an offset position (e.g. `(10, 0, 0)`)
2. Add **MenuCharacterSpin** script to it
   - `Spin Speed`: `500`
3. Add a **Box Collider** (required for OnMouseDrag)
4. Create a second camera **CharPreviewCam** pointing at the model
5. Create Render Texture, assign to camera
6. In **CharacterArea** create `UI > Raw Image` → assign Render Texture

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
