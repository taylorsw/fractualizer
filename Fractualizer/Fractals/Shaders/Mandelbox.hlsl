#include "parameters.hlsl"
void sphereFold(inout float3 z, inout float dz)
{
	float minRadius2 = 0.25;
	float fixedRadius2 = 1;
	float r2 = dot(z, z);
	if (r2<minRadius2)
	{
		float temp = (fixedRadius2/minRadius2);
		z*=temp;
		dz*=temp;
	}
	else


	if (r2<fixedRadius2)
	{
		float temp = fixedRadius2/r2;
		z*=temp;
		dz*=temp;
	}
}


void boxFold(inout float3 z, inout float dz)
{
	float foldingLimit = 1;
	z=clamp(z, -foldingLimit, foldingLimit)*2.0-z;
}


float DuDeFractal(float3 pos)
{
	float sf = 2;
	float sfNormalizing = 3*(sf+1)/(sf-1);
	pos=pos*sfNormalizing;
	int Iterations = 20;
	float3 offset = pos;
	float dr = 1.0;
	for (int n = 0; n<Iterations; n++)
	{
		boxFold(pos, dr);
		sphereFold(pos, dr);
		pos=sf*pos+offset;
		dr=dr*abs(sf)+1.0;
	}
	float r = length(pos);
	return r/abs(dr)/sfNormalizing;
}
#include "rayTracer.hlsl"
