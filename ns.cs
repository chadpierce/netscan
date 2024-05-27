// netscan
// simple subnet port scanner

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Usage: ns.exe <subnet in CIDR format>");
            return;
        }

        string subnet = args[0];
        string[] parts = subnet.Split('/');
        if (parts.Length != 2)
        {
            Console.WriteLine("Invalid subnet format. Please use CIDR format (e.g., 192.168.1.0/24).");
            return;
        }

        string ipBase = parts[0];
        int prefixLength = int.Parse(parts[1]);

        // get number of ips in subnet
        int ipCount = (int)Math.Pow(2, 32 - prefixLength);

        // calculate base ip as integer
        string[] ipParts = ipBase.Split('.');
        int ipBaseInt = (int.Parse(ipParts[0]) << 24) + (int.Parse(ipParts[1]) << 16) + (int.Parse(ipParts[2]) << 8) + int.Parse(ipParts[3]);

        List<Task> tasks = new List<Task>();

        for (int i = 0; i < ipCount; i++)
        {
            // get current ip addr
            int ipCurrentInt = ipBaseInt + i;
            string ipCurrent = ((ipCurrentInt >> 24) & 255) + "." + ((ipCurrentInt >> 16) & 255) + "." + ((ipCurrentInt >> 8) & 255) + "." + (ipCurrentInt & 255);

            tasks.Add(Task.Run(() =>
            {
                if (prefixLength == 32 || PingHost(ipCurrent))  // skip ping test if /32 subnet
                {
                    Console.WriteLine(ipCurrent + " is active. Scanning ports...");
                    ScanPorts(ipCurrent);
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());
        
        Console.WriteLine("Finished.");
    }

static bool PingHost(string nameOrAddress)
{
    bool pingable = false;
    Ping pinger = new Ping();
    try
    {
        PingReply reply = pinger.Send(nameOrAddress);
        pingable = reply.Status == IPStatus.Success;
        if (!pingable)
        {
            // Console.WriteLine("Ping to " + nameOrAddress + " failed with status: " + reply.Status);
            if (reply.Status == IPStatus.TimedOut)
            {
                Console.WriteLine("Retrying ping to " + nameOrAddress);
                reply = pinger.Send(nameOrAddress);
                pingable = reply.Status == IPStatus.Success;
                // if (!pingable)
                // {
                //     Console.WriteLine("Retry ping to " + nameOrAddress + " failed with status: " + reply.Status);
                // }
            }
        }
    }
    catch (PingException) // ex)
    {
        // catch errors, move on
        // Console.WriteLine("Ping to " + nameOrAddress + " failed with error: " + ex.Message);
    }
    return pingable;
}

    static void ScanPorts(string ipAddress)
    {
        int[] ports = new int[] { 21,22,23,25,26,53,80,81,88,110,111,113,135,137,138,139,143,179,199,389,443,445,464,465,514,515,548,554,587,636,646,993,995,1025,1026,1027,1433,1720,1723,2000,2001,3268,3269,3306,3389,5060,5666,5900,6001,8000,8008,8080,8443,8888,9389,10000,32768,49152,49154,49443 };
        string openPorts = "";

        foreach (int port in ports)
        {
            if (IsPortOpen(ipAddress, port))
            {
                openPorts += port + ",";
            }
        }

        if (!string.IsNullOrEmpty(openPorts))
        {
            openPorts = openPorts.TrimEnd(',');
            File.AppendAllText("results.txt", ipAddress + "," + openPorts + Environment.NewLine);
        }
    }

    static bool IsPortOpen(string host, int port)
    {
        try
        {
            using (var client = new TcpClient())
            {
                var result = client.BeginConnect(host, port, null, null);
                var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1));
                if (!success)
                {
                    return false;
                }

                client.EndConnect(result);
            }
        }
        catch
        {
            return false;
        }
        return true;
    }
}