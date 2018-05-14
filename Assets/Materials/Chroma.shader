//TODO: Shader need to be refined
// actual key is pink 
Shader "Custom/transparent_col" {
    Properties {
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_TransparentColor("Transparent Color", Color) = (1,0,1,1)
        _Color ("Color", Color) = (0.8,0.8,0.8,0.8)
        _Threshold ("Threshhold", Float) = 0.5
    }
    SubShader {
	Tags { "Queue"="Overlay" "RenderType"="Overlay" }
    LOD 100
    Lighting Off
         
    CGPROGRAM
    #pragma surface surf Lambert alpha
 
    sampler2D _MainTex;
 
    struct Input {
        float2 uv_MainTex;
    };
 
    fixed4 _Color;
    fixed4 _TransparentColor;
    half _Threshold;
 
    void surf (Input IN, inout SurfaceOutput o) {
        half4 c = tex2D (_MainTex, IN.uv_MainTex);
        // Output colour will be the texture color * the vertex colour
        half4 output_col = c * _Color;
             
        //calculate the difference between the texture color and the transparent color
        //note: we use 'dot' instead of length(transparent_diff) as its faster, and
        //although it'll really give the length squared, its good enough for our purposes!
        half3 transparent_diff = c.xyz - _TransparentColor.xyz;
        half transparent_diff_squared = dot(transparent_diff,transparent_diff);
             
        //if colour is too close to the transparent one, discard it.
        //note: you could do cleverer things like fade out the alpha
        if(transparent_diff_squared < _Threshold)
            discard;
             
        //output albedo and alpha just like a normal shader
        o.Albedo = output_col.rgb;
        o.Alpha  = output_col.a*0.7;
        }
        ENDCG
    } 
    FallBack "Diffuse"
}