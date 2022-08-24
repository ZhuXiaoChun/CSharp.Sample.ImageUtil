using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CSharp.Sample.ImageUtil.Migrations
{
        public partial class 增加了文件处理统计信息 : Migration
        {
                protected override void Up(MigrationBuilder migrationBuilder)
                {
                        migrationBuilder.AddColumn<double>(
                            name: "SecondsToDatabaseOperation",
                            table: "FileInfo",
                            type: "float",
                            nullable: false,
                            defaultValue: 0.0);

                        migrationBuilder.AddColumn<double>(
                            name: "SecondsToFileProcessing",
                            table: "FileInfo",
                            type: "float",
                            nullable: false,
                            defaultValue: 0.0);

                        migrationBuilder.AddColumn<double>(
                            name: "SecondsToFileStorage",
                            table: "FileInfo",
                            type: "float",
                            nullable: false,
                            defaultValue: 0.0);

                        migrationBuilder.AddColumn<double>(
                            name: "SecondsToResponse",
                            table: "FileInfo",
                            type: "float",
                            nullable: false,
                            defaultValue: 0.0);
                }

                protected override void Down(MigrationBuilder migrationBuilder)
                {
                        migrationBuilder.DropColumn(
                            name: "SecondsToDatabaseOperation",
                            table: "FileInfo");

                        migrationBuilder.DropColumn(
                            name: "SecondsToFileProcessing",
                            table: "FileInfo");

                        migrationBuilder.DropColumn(
                            name: "SecondsToFileStorage",
                            table: "FileInfo");

                        migrationBuilder.DropColumn(
                            name: "SecondsToResponse",
                            table: "FileInfo");
                }
        }
}
