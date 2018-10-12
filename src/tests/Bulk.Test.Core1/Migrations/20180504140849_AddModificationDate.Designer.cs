﻿using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Bulk.Test;

namespace Bulk.Test.Core1.Migrations
{
    [DbContext(typeof(TestContext))]
    [Migration("20180504140849_AddModificationDate")]
    partial class AddModificationDate
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.1.5")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("Bulk.Test.Model.BaseTphTable", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Name")
                        .HasMaxLength(50);

                    b.Property<byte>("Type");

                    b.HasKey("Id");

                    b.ToTable("BaseTphTable");

                    b.HasDiscriminator<byte>("Type");
                });

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

            modelBuilder.Entity("Bulk.Test.Model.SimpleTableWithShadowProperty", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Description_de")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasDefaultValue("DEFAULT")
                        .HasMaxLength(200);

                    b.Property<string>("Description_en")
                        .HasMaxLength(200);

                    b.Property<DateTime?>("ModificationDate")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValue(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

                    b.Property<string>("Title")
                        .HasMaxLength(100);

                    b.HasKey("Id");

                    b.ToTable("SimpleTableWithShadowProperty");
                });

            modelBuilder.Entity("Bulk.Test.Model.TpHChildTableOne", b =>
                {
                    b.HasBaseType("Bulk.Test.Model.BaseTphTable");

                    b.Property<int>("ChildOneProperty");

                    b.ToTable("TpHChildTableOne");

                    b.HasDiscriminator().HasValue((byte)1);
                });

            modelBuilder.Entity("Bulk.Test.Model.TpHChildTableTwo", b =>
                {
                    b.HasBaseType("Bulk.Test.Model.BaseTphTable");

                    b.Property<int>("ChildTwoProperty");

                    b.ToTable("TpHChildTableTwo");

                    b.HasDiscriminator().HasValue((byte)2);
                });
        }
    }
}