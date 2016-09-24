using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;

namespace Render
{
    public class Light
    {
        public Vector3 pt;

        public Light(Vector3 ptInitial)
        {
            pt = ptInitial;
        }
    }
}
