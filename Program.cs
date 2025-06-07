using System.Text;
using BukuTamuAPI.Models;
using BukuTamuAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "BukuTamu", Version = "v1" });
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

//dbcontext
builder.Services.AddDbContext<DbtamuContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 30))));

//jwt auth
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// auth policy
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("GuruOnly", policy => policy.RequireRole("Guru"));
    options.AddPolicy("PenerimaTamuOnly", policy => policy.RequireRole("PenerimaTamu"));
});

//di register servic
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITamuService, TamuService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddLogging();
builder.Services.AddSingleton<WebSocketHandler>();

var app = builder.Build();


//seed database
//using (var scope = app.Services.CreateScope())
//{
//    var context = scope.ServiceProvider.GetRequiredService<DbtamuContext>();
//    context.Database.Migrate();

//    // Seed Pengguna
//    if (!context.Penggunas.Any())
//    {
//        context.Penggunas.AddRange(
//            new Pengguna
//            {
//                Nama = "Admin Utama",
//                Email = "admin@bukutama.com",
                
//                Password = BCrypt.Net.BCrypt.HashPassword("admin123"),
//                Role = "Admin"
//            },
//            new Pengguna
//            {
//                Nama = "Operator",
//                Email = "operator@bukutama.com",
//                Password = BCrypt.Net.BCrypt.HashPassword("operator123"),
//                Role = "PenerimaTamu"
//            },
//            new Pengguna
//            {
//                Nama = "Guru",
//                Email = "guru@bukutama.com",
//                Password = BCrypt.Net.BCrypt.HashPassword("guru123"),
//                Role = "Guru"
//            }
//        );

//        context.ChangeTracker.Entries<Pengguna>().ToList().ForEach(entry =>
//        {
//            if (entry.Entity.Email == "admin@bukutama.com")
//                entry.Property("username").CurrentValue = "admin_utama";
//            else if (entry.Entity.Email == "operator@bukutama.com")
//                entry.Property("username").CurrentValue = "operator";
//            else if (entry.Entity.Email == "guru@bukutama.com")
//                entry.Property("username").CurrentValue = "guru";
//        });

//        context.SaveChanges();
//    }

//    //// Seed Tamu
//    //if (!context.Tamus.Any())
//    //{
//    //    context.Tamus.Add(
//    //        new Tamu
//    //        {
//    //            Nama = "havid",
//    //            Telepon = "085233445459",
                
//    //        }
//    //    );
//    //    context.SaveChanges();
//    //}

//    //// Seed JanjiTemu
//    //if (!context.JanjiTemus.Any())
//    //{
//    //    var janjiTemu = new JanjiTemu
//    //    {
//    //        IdTamu = 1,
//    //        IdGuru = 3,
//    //        Tanggal = new DateOnly(2025, 6, 2),
//    //        Waktu = new TimeOnly(14, 0, 0),
//    //        Keperluan = "epepan",
//    //        Status = "Menunggu",
//    //        KodeQr = ""
//    //    };
//    //    context.JanjiTemus.Add(janjiTemu);
//    //    janjiTemu.SetTanggalWaktu(context); // Set shadow property
//    //    context.ChangeTracker.Entries<JanjiTemu>().ToList().ForEach(entry =>
//    //    {
//    //        if (entry.Entity.IdTamu == 1 && entry.Entity.IdGuru == 3)
//    //        {
//    //            entry.Property("created_at").CurrentValue = new DateTime(2025, 6, 2, 13, 14, 7, 999, DateTimeKind.Unspecified);
//    //            entry.Property("updated_at").CurrentValue = new DateTime(2025, 6, 2, 13, 14, 7, 999, DateTimeKind.Unspecified);
//    //        }
//    //    });
//    //    context.SaveChanges();
//    //}
//}
app.Urls.Add("http://0.0.0.0:5000");
// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2)
});
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();