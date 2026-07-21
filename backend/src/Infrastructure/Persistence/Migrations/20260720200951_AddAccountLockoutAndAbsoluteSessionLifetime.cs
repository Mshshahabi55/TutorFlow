using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountLockoutAndAbsoluteSessionLifetime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailedLoginAttemptCount",
                table: "Tutors",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedUntilUtc",
                table: "Tutors",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FailedLoginAttemptCount",
                table: "Students",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedUntilUtc",
                table: "Students",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FailedLoginAttemptCount",
                table: "ParentGuardians",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedUntilUtc",
                table: "ParentGuardians",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AbsoluteExpiresAtUtc",
                table: "AuthTokens",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "FailedLoginAttemptCount",
                table: "AdminStaffs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedUntilUtc",
                table: "AdminStaffs",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailedLoginAttemptCount",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "LockedUntilUtc",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "FailedLoginAttemptCount",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "LockedUntilUtc",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "FailedLoginAttemptCount",
                table: "ParentGuardians");

            migrationBuilder.DropColumn(
                name: "LockedUntilUtc",
                table: "ParentGuardians");

            migrationBuilder.DropColumn(
                name: "AbsoluteExpiresAtUtc",
                table: "AuthTokens");

            migrationBuilder.DropColumn(
                name: "FailedLoginAttemptCount",
                table: "AdminStaffs");

            migrationBuilder.DropColumn(
                name: "LockedUntilUtc",
                table: "AdminStaffs");
        }
    }
}
