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
}

float DE(float3 pt)
{
	float Scale = 2;
	float3 a1 = float3(1, 1, 1);
	float3 a2 = float3(-1, -1, 1);
	float3 a3 = float3(1, -1, -1);
	float3 a4 = float3(-1, 1, -1);
	float3 c;
	float dist, d;
	for (int n = 0; n < 10; n++) {
		c = a1;
		dist = length(pt - a1);
		d = length(pt - a2); if (d < dist) { c = a2; dist = d; }
		d = length(pt - a3); if (d < dist) { c = a3; dist = d; }
		d = length(pt - a4); if (d < dist) { c = a4; dist = d; }

		// c is closest a-vertex; d is distance to it
		pt = Scale*pt - c*(Scale - 1.0);
	}

	return length(pt) * pow(Scale, float(-n));
}

static const int cmarch = 48;
float4 ray_marching(float3 pt, float3 vk)
{
	float duPixelRadius = rsViewPlane.x / rsScreen.x;
	float duTotal = 0;
	for (int i = 0; i < cmarch; ++i)
	{
		float du = DE(pt);
		pt += du * vk;
		duTotal += du;
		float duEpsilon = 0.5 * duTotal / duNear * duPixelRadius;
		if (du < duEpsilon)
			return float4(pt, i);
	}
	return float4(pt, -1);
}

float4 main(float4 position : SV_POSITION) : SV_TARGET
{
	float3 ptPlaneCenter = ptCamera + vkCamera * duNear;

	float3 vkDown = vkCameraOrtho;
	float3 vkRight = cross(vkDown, vkCamera);

	float2 vkFromScreenCenter = position.xy - rsScreen / 2;
	float2 vkFromPlaneCenter = float2(vkFromScreenCenter.x * rsViewPlane.x / rsScreen.x, vkFromScreenCenter.y * rsViewPlane.y / rsScreen.y);
	float3 planePoint = ptPlaneCenter + vkRight * vkFromPlaneCenter.x + vkDown * vkFromPlaneCenter.y;

	float3 vkRay = normalize(planePoint - ptCamera.xyz);

	float4 red = float4(1, 0, 0, 1);
	float4 green = float4(0, 1, 0, 1);
	float4 blue = float4(0, 0, 1, 1);
	float4 marched = ray_marching(ptCamera, vkRay);

	if (marched.w == -1)
		return float4(0, 0, 0, 1);

	float4 color = float4(0.2, 0.6, 0.3, 1);

	// ambient occlusion
	color = color * (1 - (marched.w / cmarch));

	return color;
}