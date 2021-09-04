using EmployeeManagement.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
			}).AddEntityFrameworkStores<AppDbContext>();

			services.AddMvc(options => options.EnableEndpointRouting = false).AddXmlDataContractSerializerFormatters();

			services.ConfigureApplicationCookie(options =>
			{
				options.AccessDeniedPath = new PathString("/Administration/AccessDenied"); 
			});

			services.AddAuthorization(options =>
			{
				options.AddPolicy("DeleteRolePolicy", policy => policy.RequireClaim("Delete Role"));
				
				options.AddPolicy("EditRolePolicy", policy => policy.RequireClaim("Edit Role", "true"));
			});

			services.AddScoped<IEmployeeRepository, SQLEmployeeRepository>();
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
