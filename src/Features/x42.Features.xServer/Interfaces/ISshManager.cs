using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using x42.Features.xServer.Models;

namespace x42.Features.xServer.Interfaces
{
    public interface ISshManager
    {
        Task ExecuteCommand(string command);
        Task<bool> TestSshCredentialsAsync(TestSshCredentialRequest request);
    }
}
