namespace x42.Features.xServer.Models
{
    public class PriceLockResult
    {
        public bool Success { get; set; }

        public string ResultMessage { get; set; }

        public string PriceLockId { get; set; }

        public int Status { get; set; }

        public decimal RequestAmount { get; set; }

        public int RequestAmountPair { get; set; }

        public decimal FeeAmount { get; set; }

        public string FeeAddress { get; set; }

        public decimal DestinationAmount { get; set; }

        public string DestinationAddress { get; set; }

        public string TransacrionId { get; set; }

        public string SignAddress { get; set; }

        public string PriceLockSignature { get; set; }

        public string PayeeSignature { get; set; }

        public long ExpireBlock { get; set; }
    }
}
