Shader "Custom/InstancedColor"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1) // Defaultn� farba
    }
        SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing // Povolenie GPU instancingu
            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID 
            };

            struct v2f {
                float4 pos : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            UNITY_INSTANCING_BUFFER_START(Props) 
                UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color) 
            UNITY_INSTANCING_BUFFER_END(Props) 

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v); 
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i); 
                return UNITY_ACCESS_INSTANCED_PROP(Props, _Color); 
            }
            ENDCG
        }
    }
}
