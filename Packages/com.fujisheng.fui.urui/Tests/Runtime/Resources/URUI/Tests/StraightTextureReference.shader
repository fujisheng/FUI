Shader "Hidden/URUI/Tests/StraightTextureReference"
{
    Properties { _MainTex("Source", 2D) = "white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 frag(v2f_img input):SV_Target { return tex2D(_MainTex, input.uv); }
            ENDCG
        }
    }
}
