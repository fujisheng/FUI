Shader "Hidden/URUI/Tests/Painting"
{
 Properties {
  _UruiPaintOpacity("Opacity",Float)=1
  _UruiClipEnabled("Clip",Float)=0
  _UruiClipRect("Clip Rect",Vector)=(0,0,0,0)
 }
 SubShader {
  Cull Off ZWrite Off ZTest Always Blend SrcAlpha OneMinusSrcAlpha
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   #include "Packages/com.fujisheng.urui/Runtime/Resources/URUI/ElementPaint.cginc"
   struct v2f { float4 position:SV_POSITION; float2 pixel:TEXCOORD0; };
   v2f vert(float4 vertex:POSITION) { v2f o; o.position=UnityObjectToClipPos(vertex); o.pixel=mul(unity_ObjectToWorld,vertex).xy; return o; }
   fixed4 frag(v2f i):SV_Target { UruiClipPaint(i.pixel); return fixed4(0,1,0,_UruiPaintOpacity); }
   ENDCG
  }
 }
}
