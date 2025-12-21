using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CleanStart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    Code = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "EventCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectName = table.Column<string>(type: "text", nullable: false),
                    IsAutoCreate = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    GroupCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventCategories_Groups_GroupCode",
                        column: x => x.GroupCode,
                        principalTable: "Groups",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FormationTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletionTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    NotificationTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsNotified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsFormed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    GroupCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Events_EventCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "EventCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Events_Groups_GroupCode",
                        column: x => x.GroupCode,
                        principalTable: "Groups",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    TelegramId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false),
                    GroupCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    GroupCodes = table.Column<string>(type: "text", nullable: false),
                    IsAdmin = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AveragePosition = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    ParticipationCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    EventCategoryId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.TelegramId);
                    table.ForeignKey(
                        name: "FK_Users_EventCategories_EventCategoryId",
                        column: x => x.EventCategoryId,
                        principalTable: "EventCategories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Users_Groups_GroupCode",
                        column: x => x.GroupCode,
                        principalTable: "Groups",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "EventParticipants",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantsTelegramId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventParticipants", x => new { x.EventId, x.ParticipantsTelegramId });
                    table.ForeignKey(
                        name: "FK_EventParticipants_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventParticipants_Users_ParticipantsTelegramId",
                        column: x => x.ParticipantsTelegramId,
                        principalTable: "Users",
                        principalColumn: "TelegramId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventCategories_GroupCode",
                table: "EventCategories",
                column: "GroupCode");

            migrationBuilder.CreateIndex(
                name: "IX_EventParticipants_ParticipantsTelegramId",
                table: "EventParticipants",
                column: "ParticipantsTelegramId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_CategoryId",
                table: "Events",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_GroupCode",
                table: "Events",
                column: "GroupCode");

            migrationBuilder.CreateIndex(
                name: "IX_Users_EventCategoryId",
                table: "Users",
                column: "EventCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_GroupCode",
                table: "Users",
                column: "GroupCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventParticipants");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "EventCategories");

            migrationBuilder.DropTable(
                name: "Groups");
        }
    }
}
