namespace x42.Features.xServer.Models
{
    public class xServerProvisioningRequest
    {
        public string Profile { get; set; }
        public string IpAddress { get; set; }
        public string SshUser { get; set; }
        public string SsHPassword { get; set; }
        public string EmailAddress { get; set; }
        public string CertificatePassword { get; set; }
        public string DatabasePassword { get; set; }

    }
}
