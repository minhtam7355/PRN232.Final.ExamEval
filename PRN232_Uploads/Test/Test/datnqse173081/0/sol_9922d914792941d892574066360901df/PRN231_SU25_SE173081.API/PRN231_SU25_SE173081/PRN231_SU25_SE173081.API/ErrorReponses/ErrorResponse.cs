namespace PRN231_SU25_SE173081.API.ErrorReponses
{
    public class ErrorResponse
    {
        public string ErrorCode { get; set; }
        public string Message { get; set; }
        public ErrorResponse(string errorCode, string message) 
        {
         ErrorCode = errorCode;
            Message = message;
        }
    }
}
