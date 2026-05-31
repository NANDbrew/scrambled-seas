#if DEBUG
using SailwindConsole;
using SailwindConsole.Commands;
using System.Collections.Generic;
using UnityEngine;
using static ONSPPropagationMaterial;
using static UnityEngine.GraphicsBuffer;

namespace ScrambledSeas
{
    internal class TestMissionDistCommand : Command
    {
        public override string Name => "TestMissionDist";

        public override string[] Aliases => new string[]{ "TMD" };

        public override string Usage => "TestMissionDist [port]";

        public override string Description => "";

        public override int MinArgs => 0;

        public override void OnRun(List<string> args)
        {
            Port targetPort = GameState.lastVisitedPort;
            PortRegion region = PortRegion.none;
            if (args.Count > 0)
            {
                if (int.TryParse(args[0], out int a) && a == 1 || args[0].ToLower() == "emerald")
                {
                    region = PortRegion.emerald;
                }
                else if (int.TryParse(args[0], out int b) && b == 0 || args[0].ToLower() == "alankh" || args[0].ToLower() == "al'ankh")
                {
                    region = PortRegion.alankh;
                }
                else if (int.TryParse(args[0], out int c) && c == 2 || args[0].ToLower() == "aestrin" || args[0].ToLower() == "medi")
                {
                    region = PortRegion.medi;
                }
                else
                {
                    string text = string.Join(" ", args);
                    bool flag = false;
                    Port[] ports = Port.ports;
                    foreach (Port port in ports)
                    {
                        if ((bool)port && port.GetPortName().ToLower() == text.ToLower())
                        {
                            targetPort = port;
                            flag = true;
                            break;
                        }
                    }
                    if (!flag)
                    {
                        ModConsoleLog.Error(Main.instance.Info, "Invalid port name");
                        return;
                    }
                }
                //if (targetPort == null)
                //{
                //    ModConsoleLog.Error(Main.instance.Info, "Invalid port");
                //    return;
                //}
            }
            if (region != PortRegion.none)
            {
                ModConsoleLog.Log(Main.instance.Info, $"Testing all ports in {region}");
                for (int i = 0; i < Port.ports.Length; i++)
                {
                    if (Port.ports[i] == null) continue;
                    if (Port.ports[i].region == region)
                    {
                        ModConsoleLog.Log(Main.instance.Info, TestPort(Port.ports[i]));
                    }
                }
            }
            else
            {
                ModConsoleLog.Log(Main.instance.Info, TestPort(targetPort));
            }
            //ModConsoleLog.Log(Main.instance.Info, outText);

        }

        private string TestPort(Port originPort)
        {
            string outText = $"Destination counts for {originPort.GetPortName()}: \t";
            for (int l = 0; l < 11; l++)
            {
                int currentRep = PlayerReputation.GetRep(originPort.region);
                PlayerReputation.ChangeReputation(PlayerReputation.GetRequiredRep(l) - currentRep, originPort.region);
                float dist = PlayerReputation.GetMaxDistance(originPort.region);
                int count = 0;
                for (int i = 0; i < Port.ports.Length; i++)
                {
                    if (i == originPort.portIndex || Port.ports[i] == null) continue;
                    //if (Vector3.Distance(targetPort.transform.position, Port.ports[i].transform.position) <= dist)
                    if (Mission.GetDistance(originPort, Port.ports[i]) <= dist)
                    { count++; }
                }
                outText += $" {count}, ";
            }
            return outText.TrimEnd(new char[]{ ' ', ',' });
        }
    }
}
#endif