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
    [Migration("20200619224947_Initial")]
    partial class Initial
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("DygBot.Models.DailyStat", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTime>("DateTime")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("UniqueSenders")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("DailyStats");
                });

            modelBuilder.Entity("DygBot.Models.HourlyStat", b =>
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

                    b.HasKey("Id");

                    b.ToTable("HourlyStats");
                });
#pragma warning restore 612, 618
        }
    }
}
