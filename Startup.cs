using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using TopLearn.Core.Convertors;
using TopLearn.Core.Services;
using TopLearn.Core.Services.Interfaces;
using TopLearn.DataLayer.Context;

namespace TopLearn.Web
{
    public class Startup
    {
        public IConfiguration Configuration { get; set; }   /*این متغیر و کانتستراکتور باید نوشته شود */
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();  /*برای استفاده از mvc*/

            //services.Configure<FormOptions>(options =>
            //options.MultipartBodyLengthLimit = 52428800); برداشتن محدودیت سایز آپلود
            #region DataBase Context  
            //برای شناختن کانتکس
            services.AddDbContext<TopLearnContext>(options =>
            options.UseSqlServer(Configuration.GetConnectionString("TopLearnConnection")));
            #endregion

            #region UseAuthentication
            services.AddAuthentication((options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

            }
            )).AddCookie(options =>
            {
                options.LoginPath = "/Login";
                options.LogoutPath = "/Logout";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(43200);
            }
            ); ; 



            #endregion

            #region IoC
            services.AddTransient<IUserService, UserService>();
            services.AddTransient<IViewRenderService, RenderViewToString>();
            services.AddTransient<IPermissionService, PermissionService>();
            services.AddTransient<ICourseService,CourseService>();
            services.AddTransient<IOrderService, OrderService>();

            #endregion

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {

            app.Use(async (context, next) =>
           {
            if (context.Request.Path.Value.ToString().ToLower().StartsWith("/corsefilesonline"))
               {
                   var callingUrl = context.Request.Headers["Referer"].ToString();
                   if(callingUrl !="" && callingUrl.StartsWith("Localhost:4439"))
                   {
await next.Invoke();
                   }
                   else
                   {
                       context.Response.Redirect("/Login");
                   }
               }

               else
               {
                   await next.Invoke();

               }

           });
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }     
            app.UseStaticFiles(); /*برای استفاده از wwroot*/
            app.UseAuthentication();
            //app.UseMvcWithDefaultRoute();/*برای استفاده از مسیر پیشفرض*/

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                  name: "areas",
                  template: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
                );
                routes.MapRoute(
                 name: "Default",
                 template: "{controller=Home}/{action=Index}/{id?}"
               );


            });


            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World!");
            });
        }
    }
}
