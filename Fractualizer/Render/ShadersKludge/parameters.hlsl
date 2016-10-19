static const int cLightMax = 20;

cbuffer Parameters
{
	float3 ptCamera;
	float3 vkCamera;
	float3 vkCameraOrtho;
	float2 rsScreen;
	float2 rsViewPlane;
	float duNear;
	float param;
	float param2;

	float fogA;
	int cLight;
	float3 rgptLight[cLightMax];
}