﻿// <auto-generated />
using Bulk.Test;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using System;

namespace Bulk.Test.Migrations
{
    [DbContext(typeof(TestContext))]
    [Migration("20180119135904_create")]
    partial class create
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.0.1-rtm-125")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("Bulk.Test.Model.SimpleTableWithIdentity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("CreateTime");

                    b.Property<DateTime>("ModifyTime");

                    b.Property<string>("Title");

                    b.Property<string>("Whatever");

                    b.HasKey("Id");

                    b.ToTable("SimpleTableWithIdentity");
                });
#pragma warning restore 612, 618
        }
    }
}
