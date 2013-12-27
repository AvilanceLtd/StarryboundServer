using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

namespace com.avilance.Starrybound.Extensions
{
    public class WorldCoordinate
    {
        public SystemCoordinate _syscoord;
        public int _planet;
        public int _satellite;

        public WorldCoordinate(string sector, int x, int y, int z, int planet, int satellite)
        {
            _syscoord = new SystemCoordinate(sector, x, y, z);
            _planet = planet;
            _satellite = satellite;
        }

        public override string ToString()
        {
            return _syscoord.ToString() + ":" + _planet + ":" + _satellite;
        }

        public bool Equals(WorldCoordinate test)
        {
            return this.ToString() == test.ToString();
        }
    }
}
