﻿using Fadebook.Application.Interfaces.Repositories;
using Fadebook.Application.Services.Interfaces;
using Fadebook.Application.Services;
using Fadebook.Domain.Entities;
using Fadebook.Infracstructure.Data;
using Fadebook.Infracstructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Diagnostics;
using Fadebook.Domain.Exceptions;
using Fadebook.Application.Interfaces;
using Fadebook.Infracstructure.Uploads;

namespace Fadebook.WebAPI.Extensions
{
    public static class ServiceExtension
    {
        public static void ConfigureCors(this IServiceCollection services) 
        {
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", builder =>

                    builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                );
            });
        }
        public static void ConfigureIdentity(this IServiceCollection services) 
        {
            services.AddIdentity<User, IdentityRole<Guid>>().AddEntityFrameworkStores<ApplicationContext>().AddDefaultTokenProviders();
            services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.User.RequireUniqueEmail = true;

            });
        }  
        public static void ConfigureDependencies(this IServiceCollection services)
        {
            services.AddScoped<IRepositoryManager, RepositoryManager>();
            services.AddScoped<IServiceManager, ServiceManager>();
            services.AddScoped<IUserRepository, UserRepository>();  
            services.AddScoped<IPostRepository, PostRepository>();
            services.AddScoped<ICommentRepository, CommentRepository>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IPostService, PostService>();
            services.AddScoped<ICommentService, CommentService>();
            services.AddSingleton<IUploadService, CloudinaryService>();
        }
        public static void ConfigureJwt(this  IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("JwtSettings");
            var key = Encoding.ASCII.GetBytes(jwtSettings.GetValue<string>("SecretKey"));

            services.AddAuthentication(options =>
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
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.GetValue<string>("ValidIssuer"),
                    ValidAudience = jwtSettings.GetValue<string>("ValidAudience"),
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };
            });
        }
        public static void ConfigureErrorHandler(this IApplicationBuilder app)
        {
            app.UseExceptionHandler(currentApp =>
            {
                currentApp.Run(async context =>
                {
                    context.Response.ContentType = "application/json";
                    var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
                    if (contextFeature != null)
                    {
                        Console.WriteLine($"Something went wrong: ${contextFeature.Error}");
                        CustomError appError;
                        if (contextFeature.Error is BaseException)
                        {
                            Console.WriteLine("hello");
                            var exception = (BaseException)contextFeature.Error;
                            appError = new CustomError { Message = exception.Message, StatusCode = exception.StatusCode };
                        }
                        else appError = new CustomError { Message = "Internal server error", StatusCode = (int)StatusCodes.Status500InternalServerError };
                       
                        context.Response.StatusCode = appError.StatusCode;
                        await context.Response.WriteAsync(appError.ToString());
                    }

                });
            });
        }
    }
}
