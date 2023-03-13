using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace LoginAndRegisterASPMVC5.Models
{
    public class DB_Entities : DbContext
    {
        public DB_Entities() : base("dbconnection") { }
        public DbSet<User> Users { get; set; }
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            base.OnModelCreating(modelBuilder);


        }

        //public DB_Entities() : base("ServiceDB") { }
        //public DbSet<ServiceTB> ServiceColumn { get; set; }
        //protected override void OnModelCreating(DbModelBuilder modelBuilder)
        //{
        //    //Database.SetInitializer<demoEntities>(null);
        //    modelBuilder.Entity<User>().ToTable("ServiceTB");
        //    modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

        //    base.OnModelCreating(modelBuilder);


        //}
        //public DbSet<ServiceTB> ServiceColumn { get; set; }
        //protected override void OnModelCreating(DbModelBuilder modelBuilder)
        //{
        //    //Database.SetInitializer<demoEntities>(null);
        //    modelBuilder.Entity<User>().ToTable("ServiceTB");
        //    modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

        //    base.OnModelCreating(modelBuilder);


        //}
    }


}