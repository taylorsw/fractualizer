cbuffer Parameters
{
	float4 ptCamera;
	float4 vkCamera;
	float4 vkCameraOrtho;
	float2 rsScreen;
	float2 rsViewPlane;
	float duNear;
}

static const int cmarch = 48;

float DE_Box(float3 p)
{
	const float scale = 9;
	const float3 boxfold = float3(1, 1, 1);
	const float spherefold = 0.2;

	float4 c0 = float4(p, 1);
	float4 c = c0;
	for (int i = 0; i < 4; ++i)
	{
		c.xyz = clamp(c.xyz, -boxfold, boxfold) * 2 - c.xyz;
		float rr = dot(c.xyz, c.xyz);
		c *= saturate(max(spherefold / rr, spherefold));
		c = c * scale + c0;
	}
	return ((length(c.xyz) - (scale - 1)) / c.w - pow(scale, -3));
}

float4 ray_marching(float3 pt, float3 vk)
{
	float duPixelRadius = rsViewPlane.x / rsScreen.x;
	float duTotal = 0;
	for (int i = 0; i < cmarch; ++i)
	{
		float du = DE_Box(pt);
		pt += du * vk;
		duTotal += du;
		float duEpsilon = 1 * duTotal / duNear * duPixelRadius;
		if (du < duEpsilon)
			return float4(pt, i);
	}
	return float4(pt, -1);
}

float3 cross(float3 a, float3 b)
{
	return float3(a.y * b.z - a.z * b.x, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
}

float length(float3 v)
{
	return sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
}

float3 normalized(float3 v)
{
	return v / length(v);
}

float4 main(float4 position : SV_POSITION) : SV_TARGET
{
	float3 ptPlaneCenter = ptCamera.xyz + vkCamera.xyz * duNear;

	float3 vkDown = vkCameraOrtho.xyz;
	float3 vkRight = cross(vkDown, vkCamera);

	float2 vkFromScreenCenter = position.xy - rsScreen / 2;
	float2 vkFromPlaneCenter = float2(vkFromScreenCenter.x * rsViewPlane.x / rsScreen.x, vkFromScreenCenter.y * rsViewPlane.y / rsScreen.y);
	float3 planePoint = ptPlaneCenter + vkRight * vkFromPlaneCenter.x + vkDown * vkFromPlaneCenter.y;

	float3 vkRay = normalized(planePoint - ptCamera.xyz);

	float4 red = float4(1, 0, 0, 1);
	float4 green = float4(0, 1, 0, 1);
	float4 blue = float4(0, 0, 1, 1);
	float4 marched = ray_marching(ptCamera.xyz, vkRay);

	if (marched.w == -1)
		return float4(0, 0, 0, 1);

	float4 color = float4(0.2, 0.6, 0.3, 1);

	// ambient occlusion
	color = color * (1 - (marched.w / cmarch));

	return color;
}