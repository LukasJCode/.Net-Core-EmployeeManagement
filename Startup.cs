using EmployeeManagement.Models;
using EmployeeManagement.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EmployeeManagement
{
	public class Startup
	{
		private IConfiguration _config;

		public Startup(IConfiguration config)
		{
			_config = config;
		}

		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddDbContextPool<AppDbContext>(options => 
			options.UseSqlServer(_config.GetConnectionString("EmployeeDBConnection")));

			services.AddIdentity<ApplicationUser, IdentityRole>(options =>
			{
				options.Password.RequiredLength = 10;
				options.Password.RequiredUniqueChars = 3;
				options.SignIn.RequireConfirmedEmail = true;

			}).AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();

			services.AddMvc(options => options.EnableEndpointRouting = false).AddXmlDataContractSerializerFormatters();

			services.AddAuthentication().AddGoogle(options =>
			{
				options.ClientId = "174699290088-32hkcukqd193ja61547omsm24nru5uvv.apps.googleusercontent.com";
				options.ClientSecret = "ZIBCZMoGKMvrkytk55EixmHD";
			});

			services.ConfigureApplicationCookie(options =>
			{
				options.AccessDeniedPath = new PathString("/Administration/AccessDenied"); 
			});

			services.AddAuthorization(options =>
			{
				options.AddPolicy("DeleteRolePolicy", policy => policy.RequireClaim("Delete Role"));

				options.AddPolicy("EditRolePolicy", policy =>
				policy.AddRequirements(new ManageAdminRolesAndClaimsRequirement()));
			});

			

			services.AddScoped<IEmployeeRepository, SQLEmployeeRepository>();
			services.AddSingleton<IAuthorizationHandler, CanEditOnlyOtherAdminRolesAndClaimsHandler>();
			services.AddSingleton<IAuthorizationHandler, SuperAdminHandler>();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			else
			{
				app.UseExceptionHandler("/Error");
				app.UseStatusCodePagesWithReExecute("/Error/{0}");
			}

			app.UseStaticFiles();
			//app.UseMvcWithDefaultRoute();

			app.UseAuthentication();
			app.UseAuthorization();
			//Conventional routing
			app.UseMvc(routes =>
			{
				routes.MapRoute("default", "{controller=Home}/{action=Index}/{id?}");
			});
		}

	}
}
