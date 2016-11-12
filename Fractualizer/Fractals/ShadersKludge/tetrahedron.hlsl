#include "parameters.hlsl"

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
float DuDeFractal(float3 pt)
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

#include "rayTracer.hlsl"