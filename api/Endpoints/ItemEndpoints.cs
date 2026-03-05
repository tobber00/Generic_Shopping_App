using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.IO;

public static class ItemEndpoints
{
    public static async Task MapItemEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/item");

        //Get items based on store or tag (Minimal information, ment for list views)
        group.MapGet("/getList", async (int? storeId, int? tagId, AppDbContext db, HttpContext httpContext) =>
        {
            var query = db.Items.AsQueryable();

            //Checks if a store- or tag list is requested
            if (storeId.HasValue)
                query = query.Where(i => i.StoreId == storeId.Value);

            else if (tagId.HasValue)
                query = query.Where(i => i.Tags != null && i.Tags.Any(t => t.Id == tagId));

            var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";

            //Makes the result list
            var result = await query
                .Select(i => new
                {
                    i.Id,
                    i.Name,
                    i.Price,
                    ImageUrl = i.Images
                        .OrderBy(img => img.Order)
                        .Select(img => $"{baseUrl}/item_images/{img.FileName}")
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Results.Ok(result);
        });

        //Create a new item (images has to be added with it's own endpoint)
        group.MapPost("/create", async (CreateItemRequest request, AppDbContext db, ClaimsPrincipal user) =>
        {
            //Checks
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null) return Results.Unauthorized();

            var store = await db.Stores.FirstOrDefaultAsync(s => s.Id == request.storeId);

            if (store == null) return Results.NotFound("This store does not exist.");
            if (store.OwnerId != int.Parse(userId)) return Results.Forbid();
            
            //Makes a new item with given information
            var newItem = new Item
            {
                StoreId = request.storeId,
                Name = request.name,
                Price = request.price,
                Description = request.description,
                SearchTerms = request.searchTerms ?? Array.Empty<string>(),
                Stock = request.stock,
                Tags = new List<Tag>()
            };

            //Links the tags if any are given
            if (request.tagIds != null && request.tagIds.Any())
            {
                var selectedTags = await db.Tags
                    .Where(t => request.tagIds.Contains(t.Id))
                    .ToListAsync();

                foreach (var tag in selectedTags)
                {
                    newItem.Tags.Add(tag);
                }
            }

            //Saves the item
            try 
            {
                db.Items.Add(newItem);
                await db.SaveChangesAsync();
                return Results.Created($"/items/{newItem.Id}", newItem);
            }
            catch (DbUpdateException)
            {
                return Results.Conflict("An item with this name already exists in this store.");
            }
        }).RequireAuthorization();

        //Add a new image to an item
        group.MapPost("/{itemId}/images", async (int itemId, IFormFile file, AppDbContext db, ClaimsPrincipal user) =>
        {
            //Checks
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null) return Results.Unauthorized();

            var item = await db.Items.Include(i => i.Store).FirstOrDefaultAsync(i => i.Id == itemId);

            if (item == null) return Results.NotFound("Item not found.");
            if (item.Store.OwnerId != int.Parse(userId)) return Results.Forbid();

            if (file == null || file.Length == 0) return Results.BadRequest("No file was uploaded.");

            //Looks up what the highest order is then adds it to the nextOrder +1
            int nextOrder = await db.ItemImages
                .Where(img => img.ItemId == itemId)
                .Select(img => (int?)img.Order)
                .MaxAsync() ?? 0;
            nextOrder++;

            //Checks file extension
            string[] allowedExtensions = [".jpg", ".jpeg", ".png"];
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(extension)) return Results.BadRequest("Invalid file type.");

            //Generates a GUID for filename
            var uniqueId = Guid.NewGuid().ToString("n").Substring(0, 8);
            var fileName = $"item_{itemId}_{uniqueId}{extension}";

            //Combines path, filename, and extension
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/item_images");
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
            var filePath = Path.Combine(folderPath, fileName);

            //Saves the file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            //Create image object
            var itemImage = new ItemImage
            {
                ItemId = itemId,
                FileName = fileName,
                Order = nextOrder
            };

            //Saves image object in database
            db.ItemImages.Add(itemImage);
            await db.SaveChangesAsync();

            return Results.Created($"/item_images/{fileName}", itemImage);

        }).RequireAuthorization().DisableAntiforgery();

        //Get a specific item (Includes everything)
        group.MapGet("/{id}", async (int id, AppDbContext db, HttpContext httpContext) =>
        {
            var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";

            //Fetches item based on item ID
            var item = await db.Items
                .AsNoTracking()
                .Where(i => i.Id == id)
                .Select(i => new
            {
                //Item details
                i.Id,
                i.Name,
                i.Description,
                i.Price,
                i.Stock,
                i.SearchTerms,

                //Adds needed store details
                Store = new
                {
                    i.Store.Id,
                    i.Store.Name
                },
                
                //Adds tags
                Tags = (i.Tags ?? new List<Tag>()).Select(t => new
                {
                    t.Id,
                    t.Name
                }).ToList(),

                //Adds images 
                Images = i.Images
                    .OrderBy(img => img.Order)
                    .Select(img => new
                    {
                        img.Id,
                        img.Order,
                        Url = $"{baseUrl}/item_images/{img.FileName}"
                    })
            })
            .FirstOrDefaultAsync();

            return item != null ? Results.Ok(item) : Results.NotFound();
        });

        //Updates an item
        group.MapPut("/{id}", async (int id, UpdateItemRequest request, AppDbContext db, ClaimsPrincipal user, IWebHostEnvironment env) =>
        {
            //TODO maybe take the checks out in seperate methods?
            //Checks
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null) return Results.Unauthorized();

            var item = await db.Items
                .Include(i => i.Tags)
                .Include(i => i.Images)
                .Include(i => i.Store)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (item == null) return Results.NotFound("Item not found.");
            if (item.Store.OwnerId != int.Parse(userId)) return Results.Forbid();

            //Update item details
            item.Name = request.name ?? item.Name;
            item.Price = request.price ?? item.Price;
            item.Description = request.description ?? item.Description;
            item.Stock = request.stock ?? item.Stock;
            item.SearchTerms = request.searchTerms ?? item.SearchTerms;

            //Update tags
            if (request.tagIds != null)
            {
                if (item.Tags != null)
                {
                    //clears current tags
                    item.Tags.Clear();
                }
                else
                {
                    item.Tags = new List<Tag>();
                }
                //Links new tags
                var newTags = await db.Tags.Where(t => request.tagIds.Contains(t.Id)).ToListAsync();
                foreach (var tag in newTags) item.Tags.Add(tag);
                
            }

            //Update image order
            if (request.imageOrder != null)
            {
                foreach (var image in item.Images)
                {
                    //Gets the new position of current image based on index
                    int newPosition = request.imageOrder.IndexOf(image.Id);

                    //Checks -1 in case image ID wasn't in the list
                    if (newPosition != -1)
                    {
                        image.Order = newPosition + 1;
                    } 
                    //If it's not in the list, it is deleted
                    else
                    {
                        //Gets the path to the image
                        var filePath = Path.Combine(env.WebRootPath, "item_images", image.FileName);
                        //If the image exists, it deletes it
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                        }
                        //Removes image from database
                        db.ItemImages.Remove(image);
                    }
                }
            }

            //Saves changes in database
            await db.SaveChangesAsync();
            //Maybe make it so it sends the new item if needed
            return Results.NoContent();
        }).RequireAuthorization();
    }
}