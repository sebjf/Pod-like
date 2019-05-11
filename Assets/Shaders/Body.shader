Shader "POD/Body"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness("Glossiness", 2D) = "black"{}
		_GlossinessScalar("Glossiness", Range(0,1)) = 1.0
        _Metallic ("Metallic", Range(0,1)) = 0.0
		_Illumination("Illumination", 2D) = "black" {}
		_Damage("Damage", 2D) = "black" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
		sampler2D _Illumination;
		sampler2D _Damage;
		sampler2D _DamageNormals;
		sampler2D _Glossiness;

        struct Input
        {
            float2 uv_MainTex  : TEXCOORD0;
			float4 color : COLOR;
        };

		float _Metallic;
		float _GlossinessScalar;
		float4 _Color;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
			float deformation = 1 - IN.color.r; // deformation is inverted so this works before the colours are set.

            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;

		//	o.Albedo = float3(1, 1, 1);

			float4 d = tex2D(_Damage, IN.uv_MainTex);
			float4 n = tex2D(_DamageNormals, IN.uv_MainTex);

			//o.Albedo = lerp(o.Albedo, float3(1, 0, 0), deformation);

			o.Albedo = lerp(o.Albedo, d.rgb, deformation * d.a);
			
			float glossiness = tex2D(_Glossiness, IN.uv_MainTex) * _GlossinessScalar;
			glossiness = max(glossiness, deformation * d.a * 1.8);

            o.Metallic = _Metallic;
			o.Smoothness = glossiness;
            o.Alpha = c.a;
			o.Emission = (1-deformation) * tex2D(_Illumination, IN.uv_MainTex) * (c + 0.1);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
