using CreekRiver.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// allows passing datetimes without time zone data 
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// allows our api endpoints to access the database through Entity Framework Core
builder.Services.AddNpgsql<CreekRiverDbContext>(builder.Configuration["CreekRiverDbConnectionString"]);

// Set the JSON serializer options
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// GET ALL CAMPSITES
app.MapGet("/api/campsites", (CreekRiverDbContext db) =>
{
    return db.Campsites.ToList();
});

// GET CAMPSITE BY ID
app.MapGet("/api/campsites/{id}", (CreekRiverDbContext db, int id) =>
{
    try
    {
        var selectedCampsite = db.Campsites.Include(c => c.CampsiteType).Single(c => c.Id == id);

        return Results.Ok(selectedCampsite);
    }
   
    catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
});

// CREATE CAMPSITE
app.MapPost("/api/campsites", (CreekRiverDbContext db, Campsite campsite) =>
{
    db.Campsites.Add(campsite);
    db.SaveChanges();
    return Results.Created($"/api/campsites/{campsite.Id}", campsite);
});

// DELETE CAMPSITE
app.MapDelete("/api/campsites/{id}", (CreekRiverDbContext db, int id) =>
{
    Campsite campsite = db.Campsites.SingleOrDefault(campsite => campsite.Id == id);
    if (campsite == null)
    {
        return Results.NotFound();
    }
    db.Campsites.Remove(campsite);
    db.SaveChanges();
    return Results.NoContent();
});

// UPDATE CAMPSITE
app.MapPut("/api/campsites/{id}", (CreekRiverDbContext db, int id, Campsite campsite) =>
{
    Campsite campsiteToUpdate = db.Campsites.SingleOrDefault(campsite => campsite.Id == id);
    if (campsiteToUpdate == null)
    {
        return Results.NotFound();
    }
    campsiteToUpdate.Nickname = campsite.Nickname;
    campsiteToUpdate.CampsiteTypeId = campsite.CampsiteTypeId;
    campsiteToUpdate.ImageUrl = campsite.ImageUrl;

    db.SaveChanges();
    return Results.NoContent();
});

// GET ALL RESERVATIONS
app.MapGet("/api/reservations", (CreekRiverDbContext db) =>
{
    return db.Reservations
        // JOIN UserProfiles table
        .Include(r => r.UserProfile)
        // JOIN Campsites table
        .Include(r => r.Campsite)
        // adds CampsiteType data to Campsites data
        .ThenInclude(c => c.CampsiteType)
        // corresponds to the ORDER BY keywords in SQL
        .OrderBy(res => res.CheckinDate)
        .ToList();
});

// CREATE RESERVATION
app.MapPost("/api/reservations", (CreekRiverDbContext db, Reservation newRes) =>
{
    db.Reservations.Add(newRes);
    db.SaveChanges();
    return Results.Created($"/api/reservations/{newRes.Id}", newRes);
});

app.Run();