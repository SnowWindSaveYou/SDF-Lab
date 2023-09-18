Shader "Unlit/PlaneSDFDisplayShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Resolution("Resolution",float) = 512.0
        _HighLight("HighLight",Range(-512,512)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Resolution;
            float _HighLight;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                
                
                fixed4 col =0;
//#define TestSqDist = 1
#ifdef TestSqDist
                
                int2 uvidx = i.uv*_Resolution;

                fixed dist = tex2D(_MainTex, float2(uvidx)/_Resolution.0).r;
                fixed stage = abs( dist)%10<0.3;
                fixed t =  abs(dist%10)/10;
                col = lerp(fixed4(0,1,0,1),fixed4(1,1,0,1),t)-stage;
                col.b = dist>0;
                if(abs(dist)<0.5){
                    col = 1;
                }

                //fixed dist_b = tex2D(_MainTex, i.uv+float2(1.0/512.0,0)).r;
                fixed dist_b = tex2D(_MainTex, float2(uvidx+int2(1,0))/_Resolution).r;
                fixed dist_c = tex2D(_MainTex, float2(uvidx+int2(-1,0))/_Resolution).r;
                fixed dist_d = tex2D(_MainTex, float2(uvidx+int2(0,1))/_Resolution).r;
                fixed dist_e = tex2D(_MainTex, float2(uvidx+int2(0,-1))/_Resolution).r;

                fixed distab = abs(dist-dist_b);

                fixed distac = abs(dist-dist_c);

                fixed distad = abs(dist-dist_d);

                fixed distae = abs(dist-dist_e);

                const float L = 0.1;
                if(abs(distab)<L
                ||abs(distac)<L
                ||abs(distad)<L
                ||abs(distae)<L

                ){
                    col = fixed4(1,0,0,1);
                }



                          
#else
                fixed dist = tex2D(_MainTex, i.uv).r;
                fixed stage = abs( dist)%10<0.3;
                fixed t =  abs(dist%10)/10;
                col = lerp(fixed4(0,1,0,1),fixed4(1,1,0,1),t)-stage;
                col.b = dist>0;
                if(abs(dist)<0.5){
                    col = 1;
                }
#endif

                if(abs(dist-_HighLight)<0.5)
                    col = fixed4(1,0,0,1);
                
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
