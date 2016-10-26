using System;
using Fractals;
using SharpDX;
namespace Fractals
{
	public class Mandelbox : Fractal3d
	{
		void sphereFold(ref Vector3d z, ref double dz)
		{
			double minRadius2 = 0.25;
			double fixedRadius2 = 1;
			double r2 = Vector3d.Dot(z, z);
			if (r2<minRadius2)
			{
				double temp = (fixedRadius2/minRadius2);
				z*=temp;
				dz*=temp;
			}
			else if (r2<fixedRadius2)
			{
				double temp = fixedRadius2/r2;
				z*=temp;
				dz*=temp;
			}
		}


		void boxFold(ref Vector3d z, ref double dz)
		{
			double foldingLimit = 1;
			z=Util.Clamp(z, -foldingLimit, foldingLimit)*2.0-z;
		}


		public override double DuEstimate(Vector3d pos)
		{
			double sf = 2;
			double sfNormalizing = 3*(sf+1)/(sf-1);
			pos=pos*sfNormalizing;
			int Iterations = 20;
			Vector3d offset = pos;
			double dr = 1.0;
			for (int n = 0; n<Iterations; n++)
			{
				boxFold(ref pos, ref dr);
				sphereFold(ref pos, ref dr);
				pos=sf*pos+offset;
				dr=dr*Util.Abs(sf)+1.0;
			}
			double r = Vector3d.Length(pos);
			return r/Util.Abs(dr)/sfNormalizing;
		}
	}
}
