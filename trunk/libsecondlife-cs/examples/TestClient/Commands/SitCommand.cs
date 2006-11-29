using System;
using System.Collections.Generic;
using System.Text;
using libsecondlife;
using libsecondlife.Packets;

namespace libsecondlife.TestClient
{
    public class SitCommand: Command
    {
		public SitCommand()
		{
			Name = "sit";
			Description = "Sit on closest touchable prim.";
		}

		public string Sit(SecondLife Client, LLUUID target)
		{
		    AgentRequestSitPacket sitPacket = new AgentRequestSitPacket();
			
		    sitPacket.AgentData.AgentID = Client.Network.AgentID;
		    sitPacket.AgentData.SessionID = Client.Network.SessionID;

		    sitPacket.TargetObject.TargetID = target;
		    sitPacket.TargetObject.Offset = LLVector3.Zero;

		    Client.Network.SendPacket(sitPacket);

//			SitTime = DateTime.Now;

		    return String.Empty;
		}

        public override string Execute(SecondLife Client, string[] args, LLUUID fromAgentID)
		{
		    PrimObject closest = null;
		    double closestDistance = Double.MaxValue;

		    lock (TestClient.Prims)
		    {
		        foreach (PrimObject p in TestClient.Prims.Values)
		        {
		            if ((p.Flags & ObjectFlags.Touch) > 0)
		            {
		                double distance = QuadranceBetween(Client.Self.Position, p.Position);
		                if (closest == null || distance < closestDistance)
		                {
		                    closest = p;
		                    closestDistance = distance;
		                }
		            }
		        }
		    }

		    if (closest != null)
		    {
		        Sit(Client, closest.ID);
		        return TestClient.Prims.Count + " prims. Sat on " + closest.ID + ". Distance: " + closestDistance;
		    }

		    return String.Empty;
		}

		//string sitTimeCommand(string[] args, LLUUID fromAgentID)
		//{
		//    return "Sitting Since: " + SitTime + " (" + (DateTime.Now - SitTime) + ")";
		//}

		public static double QuadranceBetween(LLVector3 a, LLVector3 b)
		{
			return
				(
					((a.X - b.X) * (a.X - b.X))
					+ ((a.Y - b.Y) * (a.Y - b.Y))
					+ ((a.Z - b.Z) * (a.Z - b.Z))
				);
		}
    }
}
