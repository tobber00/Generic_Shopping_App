using Microsoft.EntityFrameworkCore;

public static class UserEndpoints
{

    //TODO maybe make it so username isn't casesensitive
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/user");

        //Make a new user
        group.MapPost("/register", async (RegisterRequest request, AppDbContext db) =>
        {
            //Hash the password
            string hash = BCrypt.Net.BCrypt.HashPassword(request.password);

            //Makes a new user object
            var newUser = new User 
            { 
                Username = request.username,
                Email = request.email,
                //Links to credential table and saves the hashed password
                Credential = new UserCredential 
                { 
                    PasswordHash = hash 
                }
            };

            //Saves the user
            try 
            {
                db.Users.Add(newUser);
                
                await db.SaveChangesAsync();

                return Results.Ok("User created successfully!");
            }
            catch (Exception) 
            {
                return Results.BadRequest("Username taken or database error.");
            }
        });

        //Logs a user in and returns a JWT token
        group.MapPost("/login", async (LoginRequest request, AppDbContext db, TokenService tokenService) =>
        {
            //Tries to fetch user from database
            var user = await db.Users
                .Include(u => u.Credential)
                .FirstOrDefaultAsync(u => u.Username == request.username);

            //Checks if the user exists and passwords matches
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.password, user.Credential!.PasswordHash))
            {
                return Results.Unauthorized();
            }

            //Generates a token using token service
            var token = tokenService.GenerateToken(user);
            
            return Results.Ok(new { token });
        });
    }
}