// Espejo estereoscópico: cada ojo ve el reflejo calculado desde su propia posición.
// _LeftTex / _RightTex los rellena PlanarMirror (en modo mono ambas son la misma textura).
Shader "VRCar/StereoMirror"
{
    Properties
    {
        _LeftTex ("Left Eye", 2D) = "white" {}
        _RightTex ("Right Eye", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            sampler2D _LeftTex;
            sampler2D _RightTex;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = float2(1.0 - v.uv.x, v.uv.y); // un espejo invierte izquierda/derecha
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                return unity_StereoEyeIndex == 0 ? tex2D(_LeftTex, i.uv) : tex2D(_RightTex, i.uv);
            }
            ENDCG
        }
    }
}
