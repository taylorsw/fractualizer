#include "parameters.hlsl"

//--------------------------------------------------------------------------
float DBFold(float3 p, float fo, float g, float w) {
	if (p.z>p.y) p.yz = p.zy;
	float vx = p.x - 2.*fo;
	float vy = p.y - 4.*fo;
	float v = max(abs(vx + fo) - fo, vy);
	float v1 = max(vx - g, p.y - w);
	v = min(v, v1);
	v1 = max(v1, -abs(p.x));
	return min(v, p.x);
}

//the coordinates are pushed/pulled in parallel
float3 DBFoldParallel(float3 p, float3 fo, float3 g, float3 w) {
	float3 p1 = p;
	p.x = DBFold(p1, fo.x, g.x, w.x);
	p.y = DBFold(p1.yzx, fo.y, g.y, w.y);
	p.z = DBFold(p1.zxy, fo.z, g.z, w.z);
	return p;
}

//serial version
float3 DBFoldSerial(float3 p, float3 fo, float3 g, float3 w) {
	p.x = DBFold(p, fo.x, g.x, w.x);
	p.y = DBFold(p.yzx, fo.y, g.y, w.y);
	p.z = DBFold(p.zxy, fo.z, g.z, w.z);
	return p;
}

static const float3 fo = float3(0.7, .9528, .9);
static const float3 gh = float3(.8, .7, 0.5638);
static const float3 gw = float3(.3, 0.5, .2);
static const float4 X = float4(.1, 0.5, 0.1, .3);
static const float4 Y = float4(.1, 0.8, .1, .1);
static const float4 Z = float4(.2, 0.2, .2, .45902);
static const float4 R = float4(0.19, .1, .1, .2);
static const float sr = 20.0;
static const float Scale = 4.0;
static const float MinRad2 = 0.25;
float DuDeFractal(float3 p)
{
	float4 JC = float4(p, 1);
	float r2 = dot(p, p);
	float dd = 1;
	for (int i = 0; i < 6; i++)
	{
		p = p - clamp(p.xyz, -1.0, 1.0) * 2.0;  // mandelbox's box fold

		//Apply pull transformation
		float3 signs = sign(p);//Save the original signs
		p = abs(p);
		p = DBFoldParallel(p, fo, gh, gw);

		p *= signs;//resore signs: this way the mandelbrot set won't extend in negative directions

		//Sphere fold
		r2 = dot(p, p);
		float  t = clamp(1.0 / r2, 1, 1.0 / MinRad2);
		p *= t;
		dd *= t;

		//Scale and shift
		p = p*Scale + JC.xyz; dd = dd*Scale + JC.w;
		p = float3(1.0, 1.0, .92)*p;

		r2 = dot(p, p);
	}
	dd = abs(dd);	
	float val = (sqrt(r2) - sr) / dd;
	return val;//bounding volume is a sphere
}

#include "rayTracer.hlsl"