using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Extras.DynamicProxy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.DotNet.PlatformAbstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using SupplierWebApi.Framework;
using SupplierWebApi.Models.DataContext;

namespace SupplierWebApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

      

        /// <summary>
        /// Autofac依赖注入
        /// </summary>
        /// <param name="builder"></param>
        public void ConfigureContainer(ContainerBuilder builder)
        {


            #region Autofac
            var path = Microsoft.DotNet.PlatformAbstractions.ApplicationEnvironment.ApplicationBasePath;//获取项目路径
            var servicesDllFile = Path.Combine(path, "SupplierWebApi.Services.dll");//获取注入项目绝对路径
            var assemblysServices = Assembly.LoadFile(servicesDllFile);//直接采用加载文件的方法

            var assemblysRepository = Assembly.LoadFile(Path.Combine(path, "SupplierWebApi.Repositories.dll"));//模式是 Load(解决方案名)
            builder.RegisterAssemblyTypes(assemblysServices).AsImplementedInterfaces().PropertiesAutowired()
                      //.InstancePerLifetimeScope()
                      .EnableInterfaceInterceptors();//引用Autofac.Extras.DynamicProxy;
                                                     //.InterceptedBy(typeof(UserLogAop));//可以直接替换拦截器

            builder.RegisterAssemblyTypes(assemblysRepository).AsImplementedInterfaces().PropertiesAutowired();
            #endregion

        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {



            #region Mysql
            services.AddDbContextPool<SupplierdbContext>(options =>
            {

                options.UseMySql(Configuration.GetConnectionString("SupBack"));
                options.EnableSensitiveDataLogging(true);
            });
            #endregion

            #region Swagger
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v0.1.0",
                    Title = "SupplierWebApi API",
                    Description = "框架说明文档",   

                });
                var basePath = Microsoft.DotNet.PlatformAbstractions.ApplicationEnvironment.ApplicationBasePath;
                var xmlPath = Path.Combine(basePath, "SupplierWebApi.xml");//这个就是刚刚配置的xml文件名
                c.IncludeXmlComments(xmlPath, true);//默认的第二个参数是false，这个是controller的注释，记得修改
                var xmlModelPath = Path.Combine(basePath, "SupplierWebApi.Models.xml");//这个就是Model层的xml文件名

                c.IncludeXmlComments(xmlModelPath);

             

               
                //添加header验证信息
                var security = new OpenApiSecurityRequirement{ 
                    
                };
                c.AddSecurityRequirement(security);//添加一个必须的全局安全信息，和AddSecurityDefinition方法指定的方案名称要一致，这里是Bearer。
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT授权(数据将在请求头中进行传输) 参数结构: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",//jwt默认的参数名称
                    In = ParameterLocation.Header,//jwt默认存放Authorization信息的位置(请求头中)
                    Type = SecuritySchemeType.ApiKey
                }); ;

            });

            #endregion

        
            services.AddControllers();

           
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            #region Swagger
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "ApiHelp V1");
                c.RoutePrefix = "";

            });
            #endregion

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }

    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SupplierdbContext>
    {

        public SupplierdbContext CreateDbContext(string[] args)
        {
            var environment=Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT ");
         
            IConfigurationRoot configuration = new ConfigurationBuilder()
           .SetBasePath(Directory.GetCurrentDirectory())
           .AddJsonFile($"appsettings.Development.json")//应该修改为环境变量的appsettings.json
           .Build();
            var builder = new DbContextOptionsBuilder<SupplierdbContext>();
            builder.UseMySql(configuration.GetConnectionString("SupBack"));
            return new SupplierdbContext(builder.Options);
        }
    }

}
