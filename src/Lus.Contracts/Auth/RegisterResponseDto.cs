namespace Lus.Contracts.Auth
{
    /// <summary>
    /// Result of a self-service registration attempt. <see cref="ExceptionId"/>
    /// reuses the numeric error codes the frontend already handles
    /// (17 = e-mail already registered, 41 = recaptcha failed, 10 = invalid input).
    /// </summary>
    public class RegisterResponseDto
    {
        public bool IsSuccess { get; set; }

        public int? ExceptionId { get; set; }

        public string ErrorMessage { get; set; }

        /// <summary>
        /// True when the account was created but still needs e-mail confirmation
        /// before the user can sign in.
        /// </summary>
        public bool RequiresEmailConfirmation { get; set; }

        public static RegisterResponseDto Success() =>
            new() { IsSuccess = true, RequiresEmailConfirmation = true };

        public static RegisterResponseDto Failure(int exceptionId, string message = null) =>
            new() { IsSuccess = false, ExceptionId = exceptionId, ErrorMessage = message };
    }
}
