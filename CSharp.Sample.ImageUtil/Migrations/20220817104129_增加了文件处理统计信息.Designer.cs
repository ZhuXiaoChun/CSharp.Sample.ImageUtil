// <auto-generated />
using System;
using CSharp.Sample.ImageUtil.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace CSharp.Sample.ImageUtil.Migrations
{
    [DbContext(typeof(FileInfoDbContext))]
    [Migration("20220817104129_增加了文件处理统计信息")]
    partial class 增加了文件处理统计信息
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.8")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("CSharp.Sample.ImageUtil.Models.FileInfo", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<DateTime>("CreateTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("RelativeFilePath")
                        .HasColumnType("nvarchar(450)");

                    b.Property<double>("SecondsToDatabaseOperation")
                        .HasColumnType("float");

                    b.Property<double>("SecondsToFileProcessing")
                        .HasColumnType("float");

                    b.Property<double>("SecondsToFileStorage")
                        .HasColumnType("float");

                    b.Property<double>("SecondsToResponse")
                        .HasColumnType("float");

                    b.Property<int>("State")
                        .HasColumnType("int");

                    b.Property<DateTime>("StorageTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("TagNamesOrderByTagNameAES")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("UpdateTime")
                        .HasColumnType("datetime2");

                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("RelativeFilePath");

                    b.ToTable("FileInfo");
                });
#pragma warning restore 612, 618
        }
    }
}
