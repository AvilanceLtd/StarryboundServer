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
using System.Text;

namespace com.avilance.Starrybound.Commands
{
    class VersionC : CommandBase
    {
        public VersionC(Client client)
        {
            this.name = "version";
            this.HelpText = "";

            this.client = client;
            this.player = client.playerData;
        }

        public override bool doProcess(string[] args)
        {
            this.client.sendCommandMessage("This server is running Starrybound Server version " + StarryboundServer.VersionNum.ToString() + ".");
            this.client.sendCommandMessage("Running Starbound Server version " + StarryboundServer.starboundVersion.Name + " (" + StarryboundServer.ProtocolVersion + ").");
            this.client.sendCommandMessage("Copyright 2013, Avilance Ltd. Licensed under the GNU GPL v3.");
            return true;
        }
    }
}
