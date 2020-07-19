using System.ComponentModel.DataAnnotations;

namespace x42.Features.xServer.Models
{
    public class SubmitPaymentResult
    {
        public bool Success { get; set; }

        public int ErrorCode { get; set; }

        public string ResultMessage { get; set; }
    }
}
