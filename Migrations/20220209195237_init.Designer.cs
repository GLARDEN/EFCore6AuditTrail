﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TestAuditTrail;

#nullable disable

namespace TestAuditTrail.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20220209195237_init")]
    partial class init
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("TestAuditTrail.AuditEntry", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"), 1L, 1);

                    b.Property<string>("ActionType")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("EntityId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("EntityName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PropertyChanges")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("TimeStamp")
                        .HasColumnType("datetime2");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("AuditTrail", (string)null);
                });

            modelBuilder.Entity("TestAuditTrail.Test", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("CreatedBy")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("CreatedWhen")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("Date")
                        .HasColumnType("datetime2");

                    b.Property<string>("ModifiedBy")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("ModifiedWhen")
                        .HasColumnType("datetime2");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<TimeSpan>("Time")
                        .HasColumnType("time");

                    b.HasKey("Id");

                    b.ToTable("Tests", (string)null);
                });

            modelBuilder.Entity("TestAuditTrail.Test", b =>
                {
                    b.OwnsOne("TestAuditTrail.DateRange", "DateRange", b1 =>
                        {
                            b1.Property<Guid>("TestId")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<DateTime>("End")
                                .HasColumnType("datetime2")
                                .HasColumnName("End");

                            b1.Property<DateTime>("Start")
                                .HasColumnType("datetime2")
                                .HasColumnName("Start");

                            b1.HasKey("TestId");

                            b1.ToTable("Tests");

                            b1.WithOwner()
                                .HasForeignKey("TestId");
                        });

                    b.OwnsOne("TestAuditTrail.Address", "MainAddress", b1 =>
                        {
                            b1.Property<Guid>("TestId")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<string>("Address1")
                                .IsRequired()
                                .HasColumnType("nvarchar(max)")
                                .HasColumnName("MainAddress_Address1");

                            b1.Property<string>("Address2")
                                .IsRequired()
                                .HasColumnType("nvarchar(max)")
                                .HasColumnName("MainAddress_Address2");

                            b1.HasKey("TestId");

                            b1.ToTable("Tests");

                            b1.WithOwner()
                                .HasForeignKey("TestId");
                        });

                    b.OwnsOne("TestAuditTrail.Address", "SecondaryAddress", b1 =>
                        {
                            b1.Property<Guid>("TestId")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<string>("Address1")
                                .IsRequired()
                                .HasColumnType("nvarchar(max)")
                                .HasColumnName("SecondaryAddress_Address1");

                            b1.Property<string>("Address2")
                                .IsRequired()
                                .HasColumnType("nvarchar(max)")
                                .HasColumnName("SecondaryAddress_Address2");

                            b1.HasKey("TestId");

                            b1.ToTable("Tests");

                            b1.WithOwner()
                                .HasForeignKey("TestId");
                        });

                    b.Navigation("DateRange")
                        .IsRequired();

                    b.Navigation("MainAddress")
                        .IsRequired();

                    b.Navigation("SecondaryAddress")
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
