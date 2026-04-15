using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

public static class ClaimsExtensions
{
    //Checks and returns the user ID
    public static int GetUserId(this ClaimsPrincipal user)
    {
        //gets the user id from token
        var idClaim = user.FindFirst(JwtRegisteredClaimNames.Sub);
        
        //checks if it is null
        if (idClaim == null)
        {
            throw new UnauthorizedAccessException("User ID is missing from token.");
        }

        //returns user ID
        return int.Parse(idClaim.Value);
    }
}