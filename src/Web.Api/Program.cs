using System.Net;
using System.Threading.RateLimiting;
using Application.Abstractions.Authentication;
using Application.Abstractions.Behaviors;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Payments;
using Application.Users.CreateUser;
using Application.Wallets.CreateWallet;
using Application.Wallets.TransferFunds;
using Domain.Wallets;
using FluentValidation;
using Infrastructure.Authentication;
using Infrastructure.Database;
using Infrastructure.Payments.Stripe;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using SharedKernel;
using Web.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
    });

    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

builder.Services.AddScoped<IApplicationDbContext>(
    sp => sp.GetRequiredService<ApplicationDbContext>());

builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(Application.Wallets.TransferFunds.TransferFundsCommand).Assembly);
    
    cfg.AddOpenBehavior(typeof(IdempotencyBehavior<,>));
});

builder.Services.AddTransient<ICommandHandler<CreateUserCommand, Guid>, CreateUserHandler>();
builder.Services.AddTransient<ICommandHandler<CreateWalletCommand, Result<Guid>>, CreateWalletHandler>();

builder.Services.AddValidatorsFromAssemblyContaining<TransferFundsCommandValidator>();

builder.Services.AddTransient(
    typeof(IPipelineBehavior<,>),
    typeof(ValidationBehavior<,>));

// Rate limiter
builder.Services.AddRateLimiter(options =>
{
    options.AddConcurrencyLimiter("ConcurrencyPolicy", concurrentOptions =>
    {
        concurrentOptions.PermitLimit = 10;
        concurrentOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        concurrentOptions.QueueLimit = 5;
    });

    options.AddPolicy("LoginPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(new { Error = "Too many requests" }, token);
    };
});

var redisEndpoints = new List<RedLockEndPoint> 
{ 
    new() { EndPoint = new DnsEndPoint("localhost", 6379) } 
};

builder.Services.AddSingleton<IDistributedLockFactory>(sp => 
    RedLockFactory.Create(redisEndpoints));

builder.Services.AddSingleton<
    Application.Abstractions.Authentication.IPasswordHasher, 
    Infrastructure.Authentication.PasswordHasher
>();

builder.Services.Configure<StripeOptions>(
    builder.Configuration.GetSection(StripeOptions.SectionName));

builder.Services.AddScoped<IPaymentGateway, StripePaymentGateway>();

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection("Jwt"));

builder.Services.AddScoped<ITokenProvider, JwtTokenProvider>();

var jwtSettings = builder.Configuration
                      .GetSection("Jwt")
                      .Get<JwtOptions>()
                  ?? throw new InvalidOperationException("Jwt settings are missing.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,

            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,

            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.FromSeconds(30),

            ValidateIssuerSigningKey = true,
            RequireSignedTokens = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Convert.FromBase64String(jwtSettings.SigningKey)),

            ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRateLimiter();

// Stripe CLI forwards local HTTP callbacks without following HTTPS redirects.
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseExceptionHandler(handler => handler.Run(async context =>
{
    var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
    var conflict = feature?.Error is DbUpdateConcurrencyException;
    context.Response.StatusCode = conflict ? 409 : 500;
    await context.Response.WriteAsJsonAsync(new
    {
        error = conflict ? "Concurrent wallet update. Retry the operation." : "An unexpected error occurred."
    });
}));

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapWalletEndpoints();

app.MapCreateUserEndpoints();

app.MapCreateWalletEndpoints();

app.MapLoginEndpoints();

app.MapTopUpEndpoints();
app.MapStripeWebhookEndpoints();

app.MapGet("/payments/success", () =>
    Results.Ok("Оплата завершена. Ожидается подтверждение."));

app.MapGet("/payments/cancel", () =>
    Results.Ok("Вы вернулись со страницы оплаты."));

app.Run();
