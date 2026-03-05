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
            //checks
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null) return Results.Unauthorized();

            //Making the store element
            var newStore = new Store
            {
                OwnerId = int.Parse(userId),
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
        }).RequireAuthorization();
        

        //Add a logo to a store
        group.MapPost("/{storeId}/logo", async (int storeId, IFormFile file, AppDbContext db, ClaimsPrincipal user) =>
        {

            //Checks
            if (file == null || file.Length == 0) return Results.BadRequest("No file uploaded");

            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null) return Results.Unauthorized();

            var store = await db.Stores.FirstOrDefaultAsync(s => s.Id == storeId);

            if (store == null) return Results.BadRequest("Store does not exists.");

            if (store.OwnerId != int.Parse(userId))
            {
                return Results.Forbid();
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

            //Update store Bool
            store.HasLogo = true;
            await db.SaveChangesAsync();

            return Results.Ok(new { Message = "Logo uploaded successfully" });
        }).RequireAuthorization().DisableAntiforgery();
    }
}