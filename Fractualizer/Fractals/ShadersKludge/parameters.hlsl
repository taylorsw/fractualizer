static const int cLightMax = 20;

cbuffer Parameters : register(b0)
{
	float3 ptCamera;
	float duNear;
	float3 vkCamera;
	int cLight;
	float3 vkCameraOrtho;
	float fogA;
	float2 rsScreen;
	float2 rsViewPlane;
	float3 rgptLight[cLightMax];
}