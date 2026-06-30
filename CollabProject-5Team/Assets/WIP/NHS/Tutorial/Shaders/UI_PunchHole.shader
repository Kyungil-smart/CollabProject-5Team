Shader "UI/PunchHole"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (0,0,0,0.5) 
        _HoleRect ("Hole Rect (MinX, MinY, MaxX, MaxY) Local Space", Vector) = (0,0,0,0)
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "RenderType"="Transparent" 
            "IgnoreProjector"="True" 
            "PreviewType"="Plane" 
            "CanUseSpriteAtlas"="True" 
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        ZTest [unity_GUIZTestMode]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert 
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t 
            { 
                float4 vertex : POSITION; 
                float4 color : COLOR; 
                float2 texcoord : TEXCOORD0; 
            };
            
            struct v2f 
            { 
                float4 vertex : SV_POSITION; 
                fixed4 color : COLOR; 
                float2 texcoord : TEXCOORD0; 
                float4 localPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _HoleRect;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color; 
                o.localPos = v.vertex;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.texcoord) * i.color;

                if (i.localPos.x >= _HoleRect.x && i.localPos.x <= _HoleRect.z &&
                    i.localPos.y >= _HoleRect.y && i.localPos.y <= _HoleRect.w)
                {
                    discard; 
                }

                return col;
            }
            ENDCG
        }
    }
}