/* 
 * Starrybound Server
 * Copyright 2013, Avilance Ltd
 * Created by Zidonuke (zidonuke@gmail.com) and Crashdoom (crashdoom@avilance.com)
 * 
 * This file is a part of Starrybound Server.
 * Starrybound Server is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Starrybound Server is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 * You should have received a copy of the GNU General Public License along with Starrybound Server. If not, see http://www.gnu.org/licenses/.
*/

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

        public WorldCoordinate()
        {
            _syscoord = new SystemCoordinate("", 0, 0, 0);
            _planet = 0;
            _satellite = 0;
        }

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
