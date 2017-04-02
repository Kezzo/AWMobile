// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Shader created with Shader Forge v1.36 
// Shader Forge (c) Neat Corporation / Joachim Holmer - http://www.acegikmo.com/shaderforge/
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:1.36;sub:START;pass:START;ps:flbk:,iptp:0,cusa:False,bamd:0,cgin:,lico:0,lgpr:1,limd:0,spmd:1,trmd:0,grmd:0,uamb:True,mssp:True,bkdf:False,hqlp:False,rprd:False,enco:False,rmgx:True,imps:True,rpth:0,vtps:0,hqsc:False,nrmq:1,nrsp:0,vomd:0,spxs:False,tesm:0,olmd:2,culm:0,bsrc:0,bdst:1,dpts:2,wrdp:True,dith:0,atcv:False,rfrpo:False,rfrpn:Refraction,coma:15,ufog:False,aust:True,igpj:False,qofs:0,qpre:1,rntp:1,fgom:False,fgoc:False,fgod:False,fgor:False,fgmd:0,fgcr:0.5,fgcg:0.5,fgcb:0.5,fgca:1,fgde:0.01,fgrn:0,fgrf:300,stcl:False,stva:128,stmr:255,stmw:255,stcp:6,stps:0,stfa:0,stfz:0,ofsf:0,ofsu:0,f2p0:True,fnsp:False,fnfb:False,fsmp:False;n:type:ShaderForge.SFN_Final,id:3138,x:32719,y:32712,varname:node_3138,prsc:2|custl-243-OUT,olwid-396-OUT,olcol-1202-RGB;n:type:ShaderForge.SFN_Tex2d,id:876,x:31865,y:32957,ptovrint:False,ptlb:Texture,ptin:_Texture,varname:_Texture,prsc:0,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,tex:c9c6d26a953293f43b89b3816caf2bb2,ntxv:2,isnm:False;n:type:ShaderForge.SFN_Color,id:1202,x:32244,y:33271,ptovrint:False,ptlb:Outline Color,ptin:_OutlineColor,varname:_OutlineColor,prsc:0,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:0,c2:0,c3:0,c4:1;n:type:ShaderForge.SFN_Divide,id:396,x:32244,y:33076,varname:node_396,prsc:2|A-2319-OUT,B-3368-OUT;n:type:ShaderForge.SFN_ValueProperty,id:3368,x:32043,y:33166,ptovrint:False,ptlb:Outline Width Division Normalizer,ptin:_OutlineWidthDivisionNormalizer,varname:_OutlineWidthDivisionNormalizer,prsc:0,glob:False,taghide:True,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:1000;n:type:ShaderForge.SFN_Color,id:5911,x:32274,y:32873,ptovrint:False,ptlb:Color,ptin:_Color,varname:_Color,prsc:0,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:1,c2:1,c3:1,c4:1;n:type:ShaderForge.SFN_Multiply,id:243,x:32515,y:32777,varname:node_243,prsc:2|A-1085-OUT,B-5911-RGB;n:type:ShaderForge.SFN_LightVector,id:7479,x:31181,y:32920,varname:node_7479,prsc:2;n:type:ShaderForge.SFN_NormalVector,id:4125,x:31181,y:32771,prsc:2,pt:False;n:type:ShaderForge.SFN_Dot,id:120,x:31363,y:32797,varname:node_120,prsc:2,dt:1|A-4125-OUT,B-7479-OUT;n:type:ShaderForge.SFN_Multiply,id:1085,x:32090,y:32750,varname:node_1085,prsc:2|A-4195-OUT,B-876-RGB,C-8749-OUT;n:type:ShaderForge.SFN_Clamp,id:7216,x:31578,y:32797,varname:node_7216,prsc:2|IN-120-OUT,MIN-3188-OUT,MAX-4770-OUT;n:type:ShaderForge.SFN_Vector1,id:3188,x:31351,y:33011,varname:node_3188,prsc:2,v1:0.7;n:type:ShaderForge.SFN_Vector1,id:4770,x:31351,y:33082,varname:node_4770,prsc:2,v1:1;n:type:ShaderForge.SFN_Multiply,id:8749,x:31746,y:32730,varname:node_8749,prsc:2|A-1590-OUT,B-7216-OUT;n:type:ShaderForge.SFN_Vector1,id:1590,x:31565,y:32632,varname:node_1590,prsc:2,v1:1.4;n:type:ShaderForge.SFN_Vector1,id:2319,x:32037,y:33017,varname:node_2319,prsc:2,v1:0;n:type:ShaderForge.SFN_LightAttenuation,id:4195,x:31726,y:32475,varname:node_4195,prsc:2;proporder:876-5911-1202-3368;pass:END;sub:END;*/

Shader "Shader Forge/Mobile-Unlit-Outline-Shadowed" {
    Properties {
        _Texture ("Texture", 2D) = "black" {}
        _Color ("Color", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        [HideInInspector]_OutlineWidthDivisionNormalizer ("Outline Width Division Normalizer", Float ) = 1000
    }
    SubShader {
        Tags {
            "RenderType"="Opaque"
        }
        Pass {
            Name "Outline"
            Tags {
            }
            Cull Front
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #pragma fragmentoption ARB_precision_hint_fastest
            #pragma multi_compile_shadowcaster
            #pragma only_renderers d3d9 d3d11 glcore gles gles3 
            #pragma target 2.0
            uniform fixed4 _OutlineColor;
            uniform fixed _OutlineWidthDivisionNormalizer;
            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 vertexColor : COLOR;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float4 vertexColor : COLOR;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.pos = UnityObjectToClipPos(float4(v.vertex.xyz + v.vertexColor*(0.0/_OutlineWidthDivisionNormalizer),1) );
                return o;
            }
            float4 frag(VertexOutput i) : COLOR {
                return fixed4(_OutlineColor.rgb,0);
            }
            ENDCG
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
            uniform fixed4 _Color;
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
                fixed4 _Texture_var = tex2D(_Texture,TRANSFORM_TEX(i.uv0, _Texture));
                float3 finalColor = ((attenuation*_Texture_var.rgb*(1.4*clamp(max(0,dot(i.normalDir,lightDirection)),0.7,1.0)))*_Color.rgb);
                return fixed4(finalColor,1);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
    CustomEditor "ShaderForgeMaterialInspector"
}
