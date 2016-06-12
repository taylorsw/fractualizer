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

static const int cmarch = 48;

float DE_sphere(float3 pt)
{
	return length(pt) - 1;
}

float smod(float a, float b)
{
	return sign(a) * fmod(a, b);
}

float fold(float u, float du)
{
	return abs(smod(u + du, 4 * du) - 2 * du) - du;
}

float3 fold(float3 pt, float du)
{
	float3 ptFolded;
	for (int i = 0; i < 3; i++)
		ptFolded[i] = fold(pt[i], du);

	return ptFolded;
}

float3 sphereFold(float3 pt, float duRadius)
{
	float du = length(pt);
	if (du < duRadius)
		return pt * du * du / duRadius / duRadius;
	return pt;
}

float duFromIv(float u, float2 iv)
{
	return max(max(u - iv.y, iv.x - u), 0);
}

float2 ivFromUCenterDu(float uCenter, float du)
{
	return float2(uCenter - du / 2, uCenter + du / 2);
}

float DE_box(float3 pt, float3 ptCenter, float3 rs)
{
	float3 vk;
	for (int i = 0; i < 3; i++)
		vk[i] = duFromIv(pt[i], ivFromUCenterDu(ptCenter[i], rs[i]));

	return length(vk);
}

float DE_Box(float3 pt)
{
	return max(
		DE_sphere(pt),
		DE_box(fold(sphereFold(pt, 0.9), 0.1), float3(0, 0, 0), float3(0.5, 0.02, 0.02)));
}

//float DE_Box(float3 p)
//{
//	const float scale = 12;
//	const float3 boxfold = float3(1, 1, 1);
//	const float spherefold = 0.2;
//
//	float4 c0 = float4(p, 1);
//	float4 c = c0;
//	for (int i = 0; i < 4; ++i)
//	{
//		c.xyz = clamp(c.xyz, -boxfold, boxfold) * 2 - c.xyz;
//		float rr = dot(c.xyz, c.xyz);
//		c *= saturate(max(spherefold / rr, spherefold));
//		c = c * scale + c0;
//	}
//	return ((length(c.xyz) - (scale - 1)) / c.w - pow(scale, -3));
//}

float4 ray_marching(float3 pt, float3 vk)
{
	float duPixelRadius = rsViewPlane.x / rsScreen.x;
	float duTotal = 0;
	for (int i = 0; i < cmarch; ++i)
	{
		float du = DE_Box(pt);
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