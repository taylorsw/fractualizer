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

float3x3 MxRotateX(float agr)
{
	float c = cos(agr);
	float s = sin(agr);
	float3x3 mx =
	{
		1, 0, 0,
		0, c, -s,
		0, s, c
	};
	return mx;
}

float3x3 MxRotateY(float agr)
{
	float c = cos(agr);
	float s = sin(agr);
	float3x3 mx =
	{
		c, 0, s,
		0, 1, 0,
		-s, 0, c
	};
	return mx;
}

float3x3 MxRotateZ(float agr)
{
	float c = cos(agr);
	float s = sin(agr);
	float3x3 mx =
	{
		c, -s, 0,
		s, c, 0,
		0, 0, 1
	};
	return mx;
}

static const float PI = 3.14159265f;
float DE(float3 pt)
{
	float sf = 1.25;
	float r = dot(pt, pt);
	float3 ptTop = { 2, 4.8, 0 };
	int n;
	float bailout = 1000;
	float r1 = -0.0;
	float3x3 mxRotate1 = MxRotateX(r1);
	float3x3 mxRotate2 = MxRotateY(-22 * PI / 180);
	//float3x3 mxRotate2 = MxRotateY(param);
	for (n = 0; n < 20 && r < bailout; n++)
	{
		pt = mul(mxRotate1, pt);
		float x = abs(pt.x);
		float y = abs(pt.y);
		float z = abs(pt.z);
		float temp;

		if (x - y < 0)
		{
			temp = x;
			x = y;
			y = temp;
		}

		if (x - z < 0)
		{
			temp = x;
			x = z;
			z = temp;
		}

		if (y - z < 0)
		{
			temp = y;
			y = z;
			z = temp;
		}

		z -= 0.5 * ptTop.z * (sf - 1) / sf;
		z = -abs(-z);
		z += 0.5 * ptTop.z * (sf - 1) / sf;
		pt = float3(x, y, z);

		pt = mul(mxRotate2, pt);

		pt.x = sf * pt.x - ptTop.x * (sf - 1);
		pt.y = sf * pt.y - ptTop.y * (sf - 1);
		pt.z = sf * pt.z;
		//if (pt.z > 0.5 * ptTop.z * (sf - 1))
		//	z -= ptTop.z * (sf - 1);

		//pt = sf * pt - ptTop * (sf - 1);
		r = dot(pt, pt);
	}

	return (length(pt) - 0.5) * pow(sf, float(-n));
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
		float duEpsilon = 1 * duTotal / duNear * duPixelRadius;
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