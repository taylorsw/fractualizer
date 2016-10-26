using System;
using Fractals;
using SharpDX;
namespace Fractals
{
	public class Mandelbulb : Fractal3d
	{
		public override double DuEstimate(Vector3d pos)
		{
			double Power = 8;
			int iterations = 10;
			double Bailout = 5;
			Vector3d z = pos;
			double dr = 1.0;
			double r = 0.0;
			for (int i = 0; i<iterations; i++)
			{
				r=Vector3d.Length(z);
				if (r>Bailout)
					break;
				double theta = Math.Acos(z.z/r);
				double phi = Util.Atan(z.y/z.x);
				dr=Math.Pow(r, Power-1.0)*Power*dr+1.0;
				double zr = Math.Pow(r, Power);
				theta=theta*Power;
				phi=phi*Power;
				z=zr*new Vector3d(Math.Sin(theta)*Math.Cos(phi), Math.Sin(phi)*Math.Sin(theta), Math.Cos(theta));
				z+=pos;
			}
			return 0.5*Math.Log(r)*r/dr;
		}
	}
}
