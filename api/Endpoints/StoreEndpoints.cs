using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

public static class StoreEndpoints
{
    public static void MapStoreEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/store");

        //Create a store
        group.MapPost("/create", async (CreateStoreRequest request, AppDbContext db, ClaimsPrincipal user) =>
        {
            try
            {
                //Checks the user id from the token
                int userId = user.GetUserId();

                //Making the store element
                var newStore = new Store
                {
                    OwnerId = userId,
                    Name = request.name,
                    Description = request.description
                };

                //Adding to database
                try 
                {
                    db.Stores.Add(newStore);
                    await db.SaveChangesAsync();
                    return Results.Created($"/Stores/{newStore.Id}", newStore);
                }
                catch (DbUpdateException)
                {
                    return Results.Conflict("A store with that name already exists.");
                }
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
        }).RequireAuthorization();
        

        //Add a logo to a store
        group.MapPost("/{storeId}/logo", async (int storeId, IFormFile file, AppDbContext db, ClaimsPrincipal user) =>
        {

            //Checks
            if (file == null || file.Length == 0) return Results.BadRequest("No file uploaded");

            int userId = user.GetUserId();

            var store = await db.Stores.FirstOrDefaultAsync(s => s.Id == storeId);

            if (store == null) return Results.BadRequest("Store does not exists.");

            if (store.OwnerId != userId)
            {
                return Results.Forbid();
            }

            //Delete old logo if one already exists
            if (!string.IsNullOrEmpty(store.Logo))
            {
                var existingFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/logos", store.Logo);
                if (File.Exists(existingFilePath))
                {
                    File.Delete(existingFilePath);
                }
            }

            //Check file extension
            string[] allowedExtensions = [".jpg", ".jpeg", ".png"];
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(extension)) return Results.BadRequest("Invalid file type.");

            //Check foulder
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/logos");
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            //Renaming the file, and combines path + file
            var fileName = $"logo_{storeId}{extension}";
            var filePath = Path.Combine(folderPath, fileName);

            //Save the file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            //Update store logo filename
            store.Logo = fileName;
            await db.SaveChangesAsync();

            return Results.Ok(new { Message = "Logo uploaded successfully" });
        }).RequireAuthorization().DisableAntiforgery();

        //TODO add endpoint to edit store


        //Get a specific store
        group.MapGet("/{id}", async (int id, AppDbContext db, FileUrlProvider urlProvider) =>
        {

            //Fetches store based on ID
            var store = await db.Stores
                .AsNoTracking()
                .Where(s => s.Id == id)
                .Select(s => new
            {
                //Store details
                s.Id,
                s.Name,
                s.Description,
                LogoUrl = urlProvider.GetLogoUrl(s.Logo),

                //Owner information
                Owner = new
                {
                    s.Owner.Username
                }
            }).FirstOrDefaultAsync();

            return store != null ? Results.Ok(store) : Results.NotFound();
        });

        //Search for stores (Look into GIN Index later if optimization is needed)
        group.MapGet("/search", async (string query, AppDbContext db, FileUrlProvider urlProvider) =>
        {
            //Checks if the query is empty
            if (string.IsNullOrWhiteSpace(query))
            {
                return Results.BadRequest("Search quert can't be empty");
            }

            //Makes a list of stores
            var stores = await db.Stores
                .AsNoTracking()
                //Looks for stores where the query is in the name or description (not case-sensitive)
                .Where(s => EF.Functions.ILike(s.Name, $"%{query}%") ||
                            EF.Functions.ILike(s.Description ?? "", $"%{query}%"))
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    LogoUrl = urlProvider.GetLogoUrl(s.Logo)
                })
                //The stores show up in alphabetical order (based on name), this can be changed later to something more relevant
                .OrderBy(s => s.Name)
                .ToListAsync();

            return Results.Ok(stores);
        });
    }
}