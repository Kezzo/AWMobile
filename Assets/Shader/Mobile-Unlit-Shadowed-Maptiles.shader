// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Shader created with Shader Forge v1.36 
// Shader Forge (c) Neat Corporation / Joachim Holmer - http://www.acegikmo.com/shaderforge/
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:1.36;sub:START;pass:START;ps:flbk:,iptp:0,cusa:False,bamd:0,cgin:,lico:0,lgpr:1,limd:0,spmd:1,trmd:0,grmd:0,uamb:True,mssp:True,bkdf:False,hqlp:False,rprd:False,enco:False,rmgx:True,imps:True,rpth:0,vtps:0,hqsc:False,nrmq:1,nrsp:0,vomd:0,spxs:False,tesm:0,olmd:2,culm:0,bsrc:0,bdst:1,dpts:2,wrdp:True,dith:0,atcv:False,rfrpo:False,rfrpn:Refraction,coma:15,ufog:False,aust:True,igpj:False,qofs:0,qpre:1,rntp:1,fgom:False,fgoc:False,fgod:False,fgor:False,fgmd:0,fgcr:0.5,fgcg:0.5,fgcb:0.5,fgca:1,fgde:0.01,fgrn:0,fgrf:300,stcl:False,stva:128,stmr:255,stmw:255,stcp:6,stps:0,stfa:0,stfz:0,ofsf:0,ofsu:0,f2p0:True,fnsp:False,fnfb:False,fsmp:False;n:type:ShaderForge.SFN_Final,id:3138,x:32719,y:32712,varname:node_3138,prsc:2|custl-1085-OUT;n:type:ShaderForge.SFN_Tex2d,id:876,x:32268,y:33096,ptovrint:False,ptlb:Texture,ptin:_Texture,varname:_Texture,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,tex:5615dd843e958774baed5613c5b7ec00,ntxv:0,isnm:False;n:type:ShaderForge.SFN_LightVector,id:7479,x:31451,y:33159,varname:node_7479,prsc:2;n:type:ShaderForge.SFN_NormalVector,id:4125,x:31451,y:33010,prsc:2,pt:False;n:type:ShaderForge.SFN_Dot,id:120,x:31633,y:33036,varname:node_120,prsc:2,dt:1|A-4125-OUT,B-7479-OUT;n:type:ShaderForge.SFN_Multiply,id:1085,x:32519,y:32951,varname:node_1085,prsc:2|A-2849-OUT,B-876-RGB;n:type:ShaderForge.SFN_Clamp,id:7216,x:31848,y:33036,varname:node_7216,prsc:2|IN-120-OUT,MIN-3188-OUT,MAX-4770-OUT;n:type:ShaderForge.SFN_Vector1,id:3188,x:31621,y:33250,varname:node_3188,prsc:2,v1:0.65;n:type:ShaderForge.SFN_Vector1,id:4770,x:31621,y:33321,varname:node_4770,prsc:2,v1:1;n:type:ShaderForge.SFN_Multiply,id:8749,x:32045,y:33026,varname:node_8749,prsc:2|A-1590-OUT,B-7216-OUT;n:type:ShaderForge.SFN_Vector1,id:1590,x:31855,y:32956,varname:node_1590,prsc:2,v1:1.3;n:type:ShaderForge.SFN_LightAttenuation,id:5287,x:32045,y:32816,varname:node_5287,prsc:2;n:type:ShaderForge.SFN_Multiply,id:2849,x:32232,y:32863,varname:node_2849,prsc:2|A-5287-OUT,B-8749-OUT;proporder:876;pass:END;sub:END;*/

Shader "Custom/Mobile-Unlit-Shadowed-Maptiles" {
    Properties {
        _Texture ("Texture", 2D) = "white" {}
    }
    SubShader {
        Tags {
            "RenderType"="Opaque"
        }
        Pass {
            Name "FORWARD"
            Tags {
                "LightMode"="ForwardBase"
            }
            
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_FORWARDBASE
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "Lighting.cginc"
            #pragma multi_compile_fwdbase_fullshadows
            #pragma only_renderers d3d9 d3d11 glcore gles gles3 
            #pragma target 2.0
            uniform sampler2D _Texture; uniform float4 _Texture_ST;
            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 texcoord0 : TEXCOORD0;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float4 posWorld : TEXCOORD1;
                float3 normalDir : TEXCOORD2;
                LIGHTING_COORDS(3,4)
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.normalDir = UnityObjectToWorldNormal(v.normal);
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                o.pos = UnityObjectToClipPos(v.vertex );
                TRANSFER_VERTEX_TO_FRAGMENT(o)
                return o;
            }
            float4 frag(VertexOutput i) : COLOR {
                i.normalDir = normalize(i.normalDir);
                float3 normalDirection = i.normalDir;
                float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
////// Lighting:
                float attenuation = LIGHT_ATTENUATION(i);
                float4 _Texture_var = tex2D(_Texture,TRANSFORM_TEX(i.uv0, _Texture));
                float3 finalColor = ((attenuation*(1.3*clamp(max(0,dot(i.normalDir,lightDirection)),0.65,1.0)))*_Texture_var.rgb);
                return fixed4(finalColor,1);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
    CustomEditor "ShaderForgeMaterialInspector"
}
