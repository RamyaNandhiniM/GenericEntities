using Azure;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

using PropertyBuilder = System.Reflection.Emit.PropertyBuilder;

namespace GenericEntities
{
    internal class Program
    {
        static void Main(string[] args)
        {
          
            Console.WriteLine("Hello, World!");

            IServiceProvider serviceProvider = new ServiceCollection()               
                .BuildServiceProvider(true);

            Console.Write("What is the database name ? ");
            string dbName = Console.ReadLine();

            DbContextOptionsBuilder optionsBuilder = new DbContextOptionsBuilder();
            optionsBuilder.UseApplicationServiceProvider(serviceProvider);
            optionsBuilder.UseSqlServer($"Server=DESKTOP-N1A0AT6;Database={dbName};Trusted_Connection=True;TrustServerCertificate=True;");

            //optionsBuilder.UseModel()

            ModelBuilder modelBuilder = new ModelBuilder();
            Console.Write("What is the table name ?");
            string tableName = Console.ReadLine();

            AssemblyName aName = new AssemblyName("DynamicAssembly");
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(aName, AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("NCLCModule");
            TypeBuilder typeBuilder = moduleBuilder.DefineType(tableName, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AutoLayout, null);

            modelBuilder.Entity(tableName, (b) =>
            {
                b.HasNoKey();
                for (int i = 0; i < 3; i++)
                {
                    Console.WriteLine($"Enter Column{i} : ");
                    string colName = Console.ReadLine();
                    typeBuilder.DefineProperty(colName, PropertyAttributes.HasDefault, typeof(string), Type.EmptyTypes);

                    b.Property(typeof(String), colName);
                }
            });

            //Type entity = typeBuilder.CreateType();

            IModel model = modelBuilder.FinalizeModel();

          optionsBuilder.UseModel(model);

            DynamicContext _context = ActivatorUtilities.CreateInstance<DynamicContext>(serviceProvider, optionsBuilder.Options);
            _context.Database.Migrate();

            IModel cModel = _context.GetService<IDesignTimeModel>().Model;
            _context.GetService<IModelRuntimeInitializer>().Initialize(model);

            var diff= _context.GetService<IMigrationsModelDiffer>();
            var operations = diff.GetDifferences(cModel.GetRelationalModel(), model.GetRelationalModel());
            var sqlGenerator = _context.GetService<IMigrationsSqlGenerator>();

            var commands = sqlGenerator.Generate(operations, model);
            var executor = _context.GetService<IMigrationCommandExecutor>();
            executor.ExecuteNonQuery(commands, _context.GetService<IRelationalConnection>());

            

            DynamicDbContext dynamicDbContext = new DynamicDbContext() { 
                Identifier = dbName,
                Context = _context
            };
            DynamicContextFactory.Instance.ContextPool.Add(dynamicDbContext);
            Console.WriteLine("Database Created");



            //var dbSetMethodInfo = _context.GetType().GetMethods().First(f => f.Name == "Set");
            //dynamic dbSet = dbSetMethodInfo.MakeGenericMethod(entity).Invoke(_context, null);

            //var migrations = dbContext.Database.GetMigrations();            
        }
    } 

    public class DynamicContext : DbContext
    {
        public DynamicContext(DbContextOptions options):base(options)
        {
            this.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            this.ChangeTracker.AcceptAllChanges();
        }
    }


    public class DynamicDbContext 
    {
        public string Identifier { get; set; } = string.Empty;
        public DbContext Context { get; set; }
    }

    // Singleton Class
    public sealed class DynamicContextFactory 
    {
        private static readonly Lazy<DynamicContextFactory> lazy = new Lazy<DynamicContextFactory>(() => new DynamicContextFactory());

        public static DynamicContextFactory Instance { get { return lazy.Value; } }

        private DynamicContextFactory()
        {
        }

        public ConcurrentBag<DynamicDbContext> ContextPool { get; set; } = new ConcurrentBag<DynamicDbContext>();
        public DbContext GetPooledDbContextByIdentifier(string identifier) => ContextPool.Single(s=>s.Identifier == identifier).Context;
    }
}