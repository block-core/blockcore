using System.ComponentModel.DataAnnotations;

namespace x42.Features.xServer.Models
{
    public class SubmitPaymentRequest
    {
        /// <summary>
        ///     The payment id to be submitted.
        /// </summary>
        [Required(ErrorMessage = "PaymentId is missing")]
        public string PriceLockId { get; set; }

        /// <summary>
        ///     The transaction details.
        /// </summary>
        public string TransactionHex { get; set; }

        /// <summary>
        ///     The transaction ID.
        /// </summary>
        [Required(ErrorMessage = "TransactionId is missing")]
        public string TransactionId { get; set; }

        /// <summary>
        ///     Proof of payment signature by payee.
        /// </summary>
        [Required(ErrorMessage = "PayeeSignature is missing")]
        public string PayeeSignature { get; set; }
    }
}
