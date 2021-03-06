﻿// <auto-generated />
using System;
using DygBot.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DygBot.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20200726150515_WarnsTable")]
    partial class WarnsTable
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.6")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("DygBot.Models.Ban", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTime>("BanEnd")
                        .HasColumnType("datetime(6)");

                    b.Property<bool>("Finished")
                        .HasColumnType("tinyint(1)");

                    b.Property<ulong>("GuildId")
                        .HasColumnType("bigint unsigned");

                    b.Property<string>("Reason")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<ulong>("UserId")
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong>("WhoBanned")
                        .HasColumnType("bigint unsigned");

                    b.HasKey("Id");

                    b.ToTable("Bans");
                });

            modelBuilder.Entity("DygBot.Models.DetailStat", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("Bans")
                        .HasColumnType("int");

                    b.Property<DateTime>("DateTime")
                        .HasColumnType("datetime(6)");

                    b.Property<ulong>("GuildId")
                        .HasColumnType("bigint unsigned");

                    b.Property<int>("Members")
                        .HasColumnType("int");

                    b.Property<int>("Online")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("DetailStat");
                });

            modelBuilder.Entity("DygBot.Models.GeneralStat", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTime>("DateTime")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("UniqueSenders")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("GeneralStats");
                });

            modelBuilder.Entity("DygBot.Models.Warn", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<bool>("Expired")
                        .HasColumnType("tinyint(1)");

                    b.Property<ulong>("GuildId")
                        .HasColumnType("bigint unsigned");

                    b.Property<string>("Reason")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<ulong>("UserId")
                        .HasColumnType("bigint unsigned");

                    b.Property<DateTime>("WarnExpiration")
                        .HasColumnType("datetime(6)");

                    b.Property<ulong>("WhoWarned")
                        .HasColumnType("bigint unsigned");

                    b.HasKey("Id");

                    b.ToTable("Warns");
                });
#pragma warning restore 612, 618
        }
    }
}
