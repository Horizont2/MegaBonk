// Flowing map fog for the region map.
//
// The Shader Graph it replaces did one thing: scrolled a single noise texture in
// one direction and multiplied it by a mask. Both of its texture slots even
// pointed at the SAME image -- FogOfWar.png, which is a soft sprite blob, not
// tiling noise. That is why it read as a sliding picture rather than as fog: one
// layer always gives away its repeat, and uniform alpha makes the whole sheet
// fade together instead of wisps forming and dissolving.
//
// What actually makes fog look expensive, in the order it matters:
//   1. DOMAIN WARP -- a second noise offsets the first layer's UVs instead of
//      merely multiplying it. This is the difference between a texture moving
//      and a mass churning, and it is by far the largest gain here.
//   2. FBM LAYERS at incommensurate speeds and scales, drifting in different
//      directions, so the pattern never visibly repeats.
//   3. EROSION -- a smoothstep over the density with a drifting threshold, so
//      strands form and dissolve rather than the sheet dimming as one.
//   4. COLOUR BY DENSITY -- thick fog cooler and darker, thin fog brighter. Fog
//      painted one flat colour always reads as a decal.
//   5. PARALLAX -- the layers offset slightly against each other so a flat quad
//      reads as depth.
//   6. EDGE FADE, so it does not end on the texture's rectangle.
//
// The noise is generated in the shader rather than sampled. There is no tiling
// noise texture in the project, and a procedural fbm is both seamless at any
// scale and free of the repeat a 2048 sprite would show once it is tiled a few
// times across the map. Cost is one fullscreen-ish UI quad on a menu screen.
//
// Written as ShaderLab rather than edited into the .shadergraph: hand-editing
// that JSON graph is far more fragile than a plain unlit UI shader, and the old
// graph is left untouched so the material can be pointed back at it.
Shader "Hollow/UI_VolumetricFog"
{
    Properties
    {
        [PerRendererData] _MainTex ("Mask (sprite)", 2D) = "white" {}

        // These TINT the vertex colour rather than replace it. RegionUI drives
        // the storm's colour and pulse through Image.color per region, and that
        // authored look has to survive.
        //
        // Both brightnesses stay at or below 1, so density only ever DARKENS.
        // RegionUI only recolours forest regions; every other biome keeps
        // whatever rgb the scene authored, which is white -- so any multiplier
        // above 1 blows those regions out to a hard black-and-white pattern
        // instead of fog.
        _ThinColor  ("Thin Tint", Color) = (1.00, 1.00, 1.00, 1)
        _ThickColor ("Thick Tint", Color) = (0.70, 0.74, 0.88, 1)
        _ThinBoost  ("Thin Brightness", Range(0, 1)) = 1.0
        _ThickBoost ("Thick Brightness", Range(0, 1)) = 0.55

        _Scale1 ("Layer 1 Scale", Float) = 3.0
        _Scale2 ("Layer 2 Scale", Float) = 6.5
        _Scale3 ("Warp Scale", Float) = 1.6

        _Speed1 ("Layer 1 Speed", Vector) = (0.020, 0.012, 0, 0)
        _Speed2 ("Layer 2 Speed", Vector) = (-0.031, 0.024, 0, 0)
        _Speed3 ("Warp Speed", Vector) = (0.008, -0.015, 0, 0)

        _WarpStrength ("Domain Warp", Range(0, 1.5)) = 0.45
        _Density ("Density", Range(0, 3)) = 1.0
        // Erosion above the density's mean and a wide softness together keep the
        // fog a wash rather than a stencil. This map is a hand-drawn parchment
        // illustration -- hard-edged photographic smoke fights the art, and
        // burying the linework defeats the point of drawing it.
        _Erode ("Erosion", Range(0, 1)) = 0.52
        _Softness ("Edge Softness", Range(0.01, 1)) = 0.70
        _Parallax ("Parallax", Range(0, 1)) = 0.12
        _EdgeFade ("Border Fade", Range(0, 1)) = 0.12
        _Opacity ("Opacity", Range(0, 2)) = 0.85

        // Canvas plumbing, so this stays a drop-in replacement for the graph.
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos    : SV_POSITION;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
                float4 screen : TEXCOORD1;
            };

            sampler2D _MainTex;

            float4 _ThinColor, _ThickColor;
            float _Scale1, _Scale2, _Scale3;
            float4 _Speed1, _Speed2, _Speed3;
            float _ThinBoost, _ThickBoost;
            float _WarpStrength, _Density, _Erode, _Softness, _Parallax, _EdgeFade, _Opacity;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                o.screen = ComputeScreenPos(o.pos);
                return o;
            }

            float Hash (float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise (float2 p)
            {
                float2 c = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);       // smoothstep interpolation
                float a = Hash(c);
                float b = Hash(c + float2(1, 0));
                float d = Hash(c + float2(0, 1));
                float e = Hash(c + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(d, e, f.x), f.y);
            }

            // Rotating each octave keeps the lattice from lining up, which is what
            // otherwise shows as a faint square grid in the finished fog.
            //
            // Normalised by the accumulated amplitude so every octave count comes
            // back centred on 0.5. Without that the 2-octave warp field would sit
            // at 0.375, and subtracting 0.5 from it would bias the displacement in
            // one fixed direction instead of pushing evenly both ways.
            float FBM (float2 p, int octaves)
            {
                float v = 0.0;
                float norm = 0.0;
                float amp = 0.5;
                float2x2 rot = float2x2(0.80, 0.60, -0.60, 0.80);
                for (int k = 0; k < octaves; k++)
                {
                    v += ValueNoise(p) * amp;
                    norm += amp;
                    p = mul(rot, p) * 2.02 + 17.3;
                    amp *= 0.5;
                }
                return v / max(norm, 0.0001);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float t = _Time.y;

                // Offsetting one layer by screen position separates the layers as
                // the map pans, which is what sells depth on a flat quad.
                float2 par = (i.screen.xy / max(i.screen.w, 0.0001) - 0.5) * _Parallax;

                // The WARP field is evaluated first and used to push the other
                // layers' UVs around. Multiplying it in instead -- what the old
                // graph did -- only darkens; displacing is what makes fog roll.
                float2 uvW = i.uv * _Scale3 + _Speed3.xy * t;
                float2 warp = (float2(FBM(uvW, 2), FBM(uvW + 37.7, 2)) - 0.5) * _WarpStrength;

                float2 uv1 = i.uv * _Scale1 + _Speed1.xy * t + warp;
                float2 uv2 = i.uv * _Scale2 + _Speed2.xy * t - warp * 1.6 + par;

                // Incommensurate scales and opposed directions: the pair never
                // lines up, so there is no visible period. Weights chosen so the
                // sum sits near 0.5 before saturate -- push them higher and most
                // of the quad clamps at 1, which flattens the fog back out.
                float n1 = FBM(uv1, 4);
                float n2 = FBM(uv2, 3);
                float density = saturate(n1 * 0.70 + n2 * 0.45) * _Density;

                // EROSION. A drifting threshold makes strands appear and dissolve
                // in place; a plain alpha fades the whole sheet at once.
                float edge = _Erode + (FBM(i.uv * 1.1 - _Speed1.xy * t * 0.5, 2) - 0.5) * 0.30;
                float a = smoothstep(edge, edge + _Softness, density);

                // Fade before the quad's border, so the fog does not end on a
                // rectangle.
                float2 d = abs(i.uv - 0.5) * 2.0;
                a *= 1.0 - smoothstep(1.0 - _EdgeFade, 1.0, max(d.x, d.y));

                // The sprite's own mask still gates everything, so fog-of-war
                // reveals keep working exactly as before.
                a *= tex2D(_MainTex, i.uv).a;

                // Colour by density rather than one flat tint -- this is most of
                // the perceived "volume". Applied as a multiplier on the vertex
                // colour so RegionUI's per-region storm colour still reads.
                float3 tint = lerp(_ThinColor.rgb * _ThinBoost,
                                   _ThickColor.rgb * _ThickBoost,
                                   saturate(density));

                a *= i.color.a * _Opacity;
                return fixed4(i.color.rgb * tint, saturate(a));
            }
            ENDCG
        }
    }

    Fallback "UI/Default"
}
