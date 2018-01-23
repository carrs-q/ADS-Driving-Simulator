//TODO: Shader need to be refined
// actual key is pink 
Shader "Custom/YCrCb_Dist" {
     
    Properties {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _thresh ("Threshold", Range (0, 255)) = 100
        _slope ("Slope", Range (0, 1)) = 1
        _keyingColor ("Key Color", Color) = (0.7,0.1,0.7,1) // pink
    }
       
    SubShader {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        LOD 100
        Lighting ON
        ZWrite Off
        AlphaTest Off
        Blend SrcAlpha OneMinusSrcAlpha
           
        Pass {
            CGPROGRAM
                #pragma vertex vert_img
                #pragma fragment frag
                #pragma fragmentoption ARB_precision_hint_fastest
     
                sampler2D _MainTex;
                float3 _keyingColor;
                float _thresh;
                float _slope;
                float2 _keyingCrCb;
                   
     
                    #include "UnityCG.cginc"
                float4 frag(v2f_img i) : COLOR {
                    float3 ic = tex2D(_MainTex, i.uv).rgb;
                       
                    //transform the keying color to YCrCb (just the CrCb part)
                    _keyingCrCb = float2(128-37.945*_keyingColor.x - 74.494*_keyingColor.y + 112.439*_keyingColor.z, 128+112.439*_keyingColor.x-94.154*_keyingColor.y-18.285*_keyingColor.z);
                    //transform the current pixel's color to YCrCb (just the CrCb part)
                    float2 crcb = float2(128-37.945*ic.x - 74.494*ic.y + 112.439*ic.z, 128+112.439*ic.x-94.154*ic.y-18.285*ic.z);
                     
                    //euclidian cutoff
                    float d = abs(length(abs(_keyingCrCb - crcb)));
           
                    //little slope to be more forgiving
                    float edge0 = _thresh * (1.0 - _slope);
                    float alpha = smoothstep(edge0, _thresh, d);
                       
                    //slap the alpha value onto the original color
                    return float4(ic, alpha);
                }
            ENDCG
        }
     
    }
       
    FallBack "Unlit"
}

/*
Old Shader, works faster, but just with white

 Shader "Custom/transparent_col" {
     Properties {
         _Color ("Color", Color) = (1,1,1,1)
         _TransparentColor ("Transparent Color", Color) = (1,1,1,1)
		 _Transparency("Transparency", Range(0.0,0.5)) =0.25
         _Threshold ("Threshhold", Float) = 0.1
         _MainTex ("Albedo (RGB)", 2D) = "white" {}
     }
     SubShader {
         Tags { "Queue"="Transparent" "RenderType"="Transparent" }
         LOD 200
         
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
             // Read color from the texture
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
             o.Alpha = output_col.a;
         }
         ENDCG
     } 
     FallBack "Diffuse"
 }
 */