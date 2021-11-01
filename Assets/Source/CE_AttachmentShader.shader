Shader "Custom/CE_AttachmentShader"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}		
        _AttachmentTex("Attachment", 2DArray) = "" {}
        _OutlineTex("Outline", 2DArray) = "" {}					
		_Count("Count", float) = 0						
	}
	SubShader
	{
		Tags 
		{ 
			"IgnoreProjector" = "true" 
			"Queue" = "Transparent-100" 
			"RenderType" = "Transparent"
			"PreviewType" = "Plane"
		}
		ZWrite Off
		Pass
		{
            Blend SrcAlpha OneMinusSrcAlpha
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma require 2darray
			#include "UnityCG.cginc"

			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f {
				float2 uv : TEXCOORD0;                
				float4 vertex : SV_POSITION;                
			};

			v2f vert(appdata v) {
				v2f o;                
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			sampler2D _MainTex;
			sampler2D _MainTex_ST;

			UNITY_DECLARE_TEX2DARRAY(_AttachmentTex);			
			UNITY_DECLARE_TEX2DARRAY(_OutlineTex);			

			uniform float4 _TransformationArray [32]; // x and y are for displacement/offset and z, w are for scale					
			uniform float _Enabled [32];			

            float4 _MtColor;
			float4 _AtTexColor;
            float4 _OtTexColor;
			float _Count;

            float4 tempColor;
                       
			fixed4 frag(v2f i) : SV_Target {
				_MtColor = tex2D(_MainTex, i.uv);										                                                                     
				for(int k = 0; k < _Count; k++)
				{			
					if(_Enabled[k] > 0)
					{											
						// the z value is the index
						float3 suv = float3((i.uv.x + _TransformationArray[k].x) * _TransformationArray[k].z, (i.uv.y + _TransformationArray[k].y) * _TransformationArray[k].w, k);						
					
						_AtTexColor = UNITY_SAMPLE_TEX2DARRAY(_AttachmentTex, suv);
						_OtTexColor = UNITY_SAMPLE_TEX2DARRAY(_OutlineTex, suv);

						tempColor = _MtColor * (1.0 - _OtTexColor.a);
						tempColor += _OtTexColor.a * _AtTexColor + float4(0, 0, 0, _OtTexColor.a);
						tempColor += _MtColor.a * (1 - _AtTexColor.a) * _MtColor * _OtTexColor.a;
						tempColor.a = clamp(tempColor.a, 0.0, 1.0); 
						// update out main color
						_MtColor = tempColor;												
					}		
				}                           
                return _MtColor;
			}
			ENDCG
		}
	}
	Fallback "Custom/ShaderRGB"
}