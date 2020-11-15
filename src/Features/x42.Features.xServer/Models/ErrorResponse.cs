using System.Collections.Generic;

namespace x42.Features.xServer.Models
{
    public class ErrorResponse
    {
        public List<Error> errors { get; set; }
    }

    public class Error
    {
        public int status { get; set; }
        public string message { get; set; }
        public string description { get; set; }
    }
}
