using MediatR;

namespace Lus.Application.Users.Commands.GoogleSignIn
{
    /// <summary>
    /// Ensures a local user exists for a Google-authenticated identity.
    /// The token has already been verified by the controller, so this command
    /// only finds-or-creates the matching user and returns the e-mail used to
    /// issue the auth cookie.
    /// </summary>
    public record GoogleSignInCommand(
        string Email,
        string FirstName,
        string LastName) : IRequest<string>;
}
