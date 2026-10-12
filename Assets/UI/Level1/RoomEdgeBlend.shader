Shader "Bounce Club/Room Edge Blend"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _BackgroundColor ("Surrounding Background", Color) = (0.0980392,0.0941176,0.0901961,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="False" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            sampler2D _MainTex;
            fixed4 _BackgroundColor;
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 c=tex2D(_MainTex,i.uv)*i.color;
                float horizontal=smoothstep(0,.045,min(i.uv.x,1-i.uv.x));
                float vertical=smoothstep(0,.06,min(i.uv.y,1-i.uv.y));
                c.rgb=lerp(_BackgroundColor.rgb,c.rgb,horizontal*vertical);
                return c;
            }
            ENDCG
        }
    }
}
