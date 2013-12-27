using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.avilance.Starrybound.Extensions
{
    public class SystemCoordinate
    {
        public string _sector;
        public int _x;
        public int _y;
        public int _z;

        public SystemCoordinate(string sector, int x, int y, int z)
        {
            _sector = sector;
            _x = x;
            _y = y;
            _z = z;
        }

        public override string ToString()
        {
            return _sector + ":" + _x + ":" + _y + ":" + _z;
        }

        public bool Equals(WorldCoordinate test)
        {
            return this.ToString() == test.ToString();
        }
    }
}
