using System.Threading.Tasks;
using Renci.SshNet;
using x42.Features.xServer.Interfaces;
using x42.Features.xServer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Blockcore.Features.NodeHost.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace x42.Features.xServer
{
    public class SshManager : ISshManager, IDisposable
    {

        private  SshClient client;
        private readonly NodeHub nodeHub;
        private string IpAddress;
        private string SshPassword;
        private string SshUser;


        public SshManager()
        {

        }


        public SshManager(string ipAddress, string sshUser, string sshPassword, NodeHub nodeHub)
        {
            this.client = new SshClient(ipAddress, sshUser, sshPassword);
            this.IpAddress = ipAddress;
            this.SshUser = sshUser;
            this.SshPassword = sshPassword;

            this.nodeHub = nodeHub;
        }

        public async Task<bool> TestSshCredentialsAsync(TestSshCredentialRequest request)
        {
            using (var client = new SshClient(request.IpAddress, request.SshUser, request.SsHPassword))
            {
                client.Connect();

                if (client.IsConnected)
                {
                    return true;
                }
            }

            return await Task.FromResult(false);
          
           }

        public async Task ExecuteCommand(string command) {
           

                if (!this.client.IsConnected)
                {
                    this.client.Connect();
                }

                if (this.client.IsConnected)
                {
                    await ExecuteSshCommandAsync(this.client, command);
                }
      
            

        }

        private async Task ExecuteSshCommandAsync(SshClient client, string command)
        {
   
            using (var cmd = client.CreateCommand(command))
            {
                var asyncExecute = cmd.BeginExecute();
                cmd.OutputStream.CopyTo(Console.OpenStandardOutput());
                using (var reader = new StreamReader(
                                      cmd.OutputStream, Encoding.UTF8, true, 1024, true))
                {
                    while (!asyncExecute.IsCompleted || !reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        if (line != null)
                        {
                            await this.nodeHub.Echo(line);
                        }
                    }
                }

                cmd.EndExecute(asyncExecute);

            }

            await Task.CompletedTask;
        }

        public void Dispose()
        {
            this.Dispose();
        }
    }
}
