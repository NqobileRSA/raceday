using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RaceDay.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    email = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    first_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    role = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    phone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    gender = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true),
                    province = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.user_id);
                    table.CheckConstraint("CK_Users_Gender", "[gender] IN ('Male', 'Female', 'Other') OR [gender] IS NULL");
                    table.CheckConstraint("CK_Users_Role", "[role] IN ('Organiser', 'Participant')");
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    event_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    organiser_id = table.Column<int>(type: "int", nullable: false),
                    title = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    event_date = table.Column<DateOnly>(type: "date", nullable: false),
                    location = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    province = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    event_type = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "Draft"),
                    contact_email = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    image_url = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.event_id);
                    table.CheckConstraint("CK_Events_EventType", "[event_type] IN ('Running', 'Walking', 'Cycling')");
                    table.CheckConstraint("CK_Events_Status", "[status] IN ('Draft', 'Published', 'Completed', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_Events_Organiser",
                        column: x => x.organiser_id,
                        principalTable: "Users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    category_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    event_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    distance_km = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    entry_fee = table.Column<decimal>(type: "decimal(10,2)", nullable: false, defaultValue: 0.00m),
                    max_participants = table.Column<int>(type: "int", nullable: true),
                    min_age = table.Column<int>(type: "int", nullable: true),
                    max_age = table.Column<int>(type: "int", nullable: true),
                    gender_restriction = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.category_id);
                    table.CheckConstraint("CK_Categories_GenderRestriction", "[gender_restriction] IN ('Male', 'Female') OR [gender_restriction] IS NULL");
                    table.ForeignKey(
                        name: "FK_Categories_Event",
                        column: x => x.event_id,
                        principalTable: "Events",
                        principalColumn: "event_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventAnnouncements",
                columns: table => new
                {
                    announcement_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    event_id = table.Column<int>(type: "int", nullable: false),
                    author_id = table.Column<int>(type: "int", nullable: false),
                    title = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    priority = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "Normal"),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventAnnouncements", x => x.announcement_id);
                    table.CheckConstraint("CK_EventAnnouncements_Priority", "[priority] IN ('Low', 'Normal', 'High', 'Urgent')");
                    table.ForeignKey(
                        name: "FK_Announcements_Author",
                        column: x => x.author_id,
                        principalTable: "Users",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "FK_Announcements_Event",
                        column: x => x.event_id,
                        principalTable: "Events",
                        principalColumn: "event_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Routes",
                columns: table => new
                {
                    route_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    event_id = table.Column<int>(type: "int", nullable: false),
                    start_latitude = table.Column<decimal>(type: "decimal(9,6)", nullable: true),
                    start_longitude = table.Column<decimal>(type: "decimal(9,6)", nullable: true),
                    end_latitude = table.Column<decimal>(type: "decimal(9,6)", nullable: true),
                    end_longitude = table.Column<decimal>(type: "decimal(9,6)", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    elevation_gain_m = table.Column<int>(type: "int", nullable: true),
                    terrain_type = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Routes", x => x.route_id);
                    table.ForeignKey(
                        name: "FK_Routes_Event",
                        column: x => x.event_id,
                        principalTable: "Events",
                        principalColumn: "event_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Enrolments",
                columns: table => new
                {
                    enrolment_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    participant_id = table.Column<int>(type: "int", nullable: false),
                    category_id = table.Column<int>(type: "int", nullable: false),
                    enrolment_date = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETDATE()"),
                    bib_number = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    emergency_contact_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    emergency_contact_phone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrolments", x => x.enrolment_id);
                    table.CheckConstraint("CK_Enrolments_Status", "[status] IN ('Pending', 'Confirmed', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_Enrolments_Category",
                        column: x => x.category_id,
                        principalTable: "Categories",
                        principalColumn: "category_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Enrolments_Participant",
                        column: x => x.participant_id,
                        principalTable: "Users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "Results",
                columns: table => new
                {
                    result_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    enrolment_id = table.Column<int>(type: "int", nullable: false),
                    finish_time = table.Column<TimeSpan>(type: "time", nullable: true),
                    position = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    recorded_by = table.Column<int>(type: "int", nullable: true),
                    recorded_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Results", x => x.result_id);
                    table.CheckConstraint("CK_Results_Status", "[status] IN ('Finished', 'DNF', 'DNS', 'DQ')");
                    table.ForeignKey(
                        name: "FK_Results_Enrolment",
                        column: x => x.enrolment_id,
                        principalTable: "Enrolments",
                        principalColumn: "enrolment_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Results_RecordedBy",
                        column: x => x.recorded_by,
                        principalTable: "Users",
                        principalColumn: "user_id");
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "user_id", "created_at", "date_of_birth", "email", "first_name", "gender", "last_name", "password_hash", "phone", "province", "role", "updated_at" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 7, 1, 8, 0, 0, 0, DateTimeKind.Unspecified), new DateOnly(1985, 3, 15), "thabo.mokoena@raceday.co.za", "Thabo", "Male", "Mokoena", "AQAAAAIAAYagAAAAEM3yVBdri1JnLOwPQYTjOeQevMHqGYNhUYdxMp9joXYol06nEC2EY4Oj8Ot0Fl7yOQ==", "082 555 0101", "Gauteng", "Organiser", new DateTime(2026, 7, 1, 8, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 2, new DateTime(2026, 7, 1, 8, 0, 0, 0, DateTimeKind.Unspecified), new DateOnly(1990, 7, 22), "lerato.dlamini@raceday.co.za", "Lerato", "Female", "Dlamini", "AQAAAAIAAYagAAAAEM3yVBdri1JnLOwPQYTjOeQevMHqGYNhUYdxMp9joXYol06nEC2EY4Oj8Ot0Fl7yOQ==", "083 555 0202", "Western Cape", "Organiser", new DateTime(2026, 7, 1, 8, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 3, new DateTime(2026, 7, 1, 8, 0, 0, 0, DateTimeKind.Unspecified), new DateOnly(1995, 11, 8), "sipho.ndlovu@gmail.com", "Sipho", "Male", "Ndlovu", "AQAAAAIAAYagAAAAEM3yVBdri1JnLOwPQYTjOeQevMHqGYNhUYdxMp9joXYol06nEC2EY4Oj8Ot0Fl7yOQ==", "071 555 0303", "KwaZulu-Natal", "Participant", new DateTime(2026, 7, 1, 8, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 4, new DateTime(2026, 7, 1, 8, 0, 0, 0, DateTimeKind.Unspecified), new DateOnly(1998, 2, 14), "naledi.khumalo@gmail.com", "Naledi", "Female", "Khumalo", "AQAAAAIAAYagAAAAEM3yVBdri1JnLOwPQYTjOeQevMHqGYNhUYdxMp9joXYol06nEC2EY4Oj8Ot0Fl7yOQ==", "072 555 0404", "Gauteng", "Participant", new DateTime(2026, 7, 1, 8, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 5, new DateTime(2026, 7, 1, 8, 0, 0, 0, DateTimeKind.Unspecified), new DateOnly(1988, 9, 30), "james.venter@outlook.com", "James", "Male", "Venter", "AQAAAAIAAYagAAAAEM3yVBdri1JnLOwPQYTjOeQevMHqGYNhUYdxMp9joXYol06nEC2EY4Oj8Ot0Fl7yOQ==", "082 555 0505", "Western Cape", "Participant", new DateTime(2026, 7, 1, 8, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 6, new DateTime(2026, 7, 1, 8, 0, 0, 0, DateTimeKind.Unspecified), new DateOnly(2000, 6, 18), "amahle.zulu@yahoo.com", "Amahle", "Female", "Zulu", "AQAAAAIAAYagAAAAEM3yVBdri1JnLOwPQYTjOeQevMHqGYNhUYdxMp9joXYol06nEC2EY4Oj8Ot0Fl7yOQ==", "061 555 0606", "Eastern Cape", "Participant", new DateTime(2026, 7, 1, 8, 0, 0, 0, DateTimeKind.Unspecified) }
                });

            migrationBuilder.InsertData(
                table: "Events",
                columns: new[] { "event_id", "contact_email", "created_at", "description", "event_date", "event_type", "image_url", "location", "organiser_id", "province", "status", "title", "updated_at" },
                values: new object[,]
                {
                    { 1, "info@joburgmarathon.co.za", new DateTime(2026, 7, 1, 8, 0, 0, 0, DateTimeKind.Unspecified), "A flagship road running event through the heart of Johannesburg, starting at the FNB Stadium and finishing at Sandton City.", new DateOnly(2026, 9, 20), "Running", null, "FNB Stadium, Johannesburg", 1, "Gauteng", "Published", "Joburg City Marathon 2026", new DateTime(2026, 7, 1, 8, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 2, "events@capecoastalcycle.co.za", new DateTime(2026, 7, 1, 8, 0, 0, 0, DateTimeKind.Unspecified), "A scenic cycling event along the Atlantic Seaboard from the V&A Waterfront to Hout Bay and back.", new DateOnly(2026, 10, 15), "Cycling", null, "V&A Waterfront, Cape Town", 2, "Western Cape", "Published", "Cape Town Coastal Cycle", new DateTime(2026, 7, 1, 8, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 3, "walk@sowetoheritage.org.za", new DateTime(2026, 7, 1, 8, 0, 0, 0, DateTimeKind.Unspecified), "A community walking event celebrating Soweto heritage, passing through Vilakazi Street and Freedom Square.", new DateOnly(2026, 11, 1), "Walking", null, "Vilakazi Street, Soweto", 1, "Gauteng", "Published", "Soweto Heritage Walk", new DateTime(2026, 7, 1, 8, 0, 0, 0, DateTimeKind.Unspecified) }
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "category_id", "distance_km", "entry_fee", "event_id", "gender_restriction", "max_age", "max_participants", "min_age", "name" },
                values: new object[,]
                {
                    { 1, 42.20m, 350.00m, 1, null, null, 5000, 18, "42.2km Full Marathon" },
                    { 2, 21.10m, 250.00m, 1, null, null, 8000, 16, "21.1km Half Marathon" },
                    { 3, 10.00m, 120.00m, 1, null, null, 10000, 12, "10km Fun Run" },
                    { 4, 80.00m, 450.00m, 2, null, null, 3000, 18, "80km Full Route" },
                    { 5, 40.00m, 300.00m, 2, null, null, 5000, 16, "40km Half Route" },
                    { 6, 10.00m, 50.00m, 3, null, null, 2000, null, "10km Heritage Trail" }
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "category_id", "distance_km", "event_id", "gender_restriction", "max_age", "max_participants", "min_age", "name" },
                values: new object[] { 7, 5.00m, 3, null, null, 5000, null, "5km Family Walk" });

            migrationBuilder.InsertData(
                table: "EventAnnouncements",
                columns: new[] { "announcement_id", "author_id", "created_at", "event_id", "message", "priority", "title" },
                values: new object[,]
                {
                    { 1, 1, new DateTime(2026, 9, 10, 9, 0, 0, 0, DateTimeKind.Unspecified), 1, "Due to road works on Jan Smuts Avenue, the 42.2km route will be diverted via Oxford Road between km 18 and km 22. Updated maps are available on the event page.", "High", "Route Change Notice" },
                    { 2, 1, new DateTime(2026, 9, 12, 14, 0, 0, 0, DateTimeKind.Unspecified), 1, "Race packs can be collected from FNB Stadium on 18-19 September between 09:00 and 17:00. Please bring your ID and confirmation email.", "Normal", "Race Pack Collection" },
                    { 3, 2, new DateTime(2026, 9, 28, 10, 0, 0, 0, DateTimeKind.Unspecified), 2, "Online registration for the Cape Town Coastal Cycle closes on 10 October. Late entries will be accepted at the venue for an additional R100.", "Normal", "Registration Closing Soon" },
                    { 4, 1, new DateTime(2026, 10, 1, 8, 0, 0, 0, DateTimeKind.Unspecified), 3, "The Soweto Heritage Walk is confirmed for 1 November 2026. Water stations will be placed every 2km along the route.", "Low", "Event Confirmed" }
                });

            migrationBuilder.InsertData(
                table: "Routes",
                columns: new[] { "route_id", "description", "elevation_gain_m", "end_latitude", "end_longitude", "event_id", "start_latitude", "start_longitude", "terrain_type" },
                values: new object[,]
                {
                    { 1, "Start at FNB Stadium, head north through Braamfontein, past the Johannesburg CBD, through Rosebank, and finish at Sandton City. Mostly tar roads with gentle elevation changes.", 185, -26.107300m, 28.056700m, 1, -26.235900m, 27.982500m, "Road" },
                    { 2, "Start at V&A Waterfront, ride along the Atlantic Seaboard via Sea Point, Camps Bay, and Llandudno to Hout Bay. Return via Constantia Nek. Rolling coastal terrain.", 640, -34.043900m, 18.353800m, 2, -33.903200m, 18.420700m, "Road" },
                    { 3, "Start at Vilakazi Street, loop through Orlando West, past the Hector Pieterson Museum, through Freedom Square, and return. Flat urban terrain.", 25, -26.341500m, 27.905400m, 3, -26.336700m, 27.896200m, "Road" }
                });

            migrationBuilder.InsertData(
                table: "Enrolments",
                columns: new[] { "enrolment_id", "bib_number", "category_id", "emergency_contact_name", "emergency_contact_phone", "enrolment_date", "participant_id", "status" },
                values: new object[,]
                {
                    { 1, "JBM-2145", 2, "Nomsa Ndlovu", "073 111 2233", new DateTime(2026, 7, 1, 10, 30, 0, 0, DateTimeKind.Unspecified), 3, "Confirmed" },
                    { 2, "JBM-5032", 3, "David Khumalo", "082 444 5566", new DateTime(2026, 7, 5, 14, 15, 0, 0, DateTimeKind.Unspecified), 4, "Confirmed" },
                    { 3, "CCC-0789", 4, "Sarah Venter", "083 777 8899", new DateTime(2026, 8, 10, 9, 0, 0, 0, DateTimeKind.Unspecified), 5, "Confirmed" },
                    { 4, "JBM-2301", 2, "Bongani Zulu", "071 222 3344", new DateTime(2026, 7, 12, 16, 45, 0, 0, DateTimeKind.Unspecified), 6, "Confirmed" },
                    { 5, null, 6, "Nomsa Ndlovu", "073 111 2233", new DateTime(2026, 8, 20, 11, 0, 0, 0, DateTimeKind.Unspecified), 3, "Pending" },
                    { 6, "CCC-1456", 5, "David Khumalo", "082 444 5566", new DateTime(2026, 8, 15, 8, 30, 0, 0, DateTimeKind.Unspecified), 4, "Confirmed" },
                    { 7, null, 7, "Sarah Venter", "083 777 8899", new DateTime(2026, 8, 25, 12, 0, 0, 0, DateTimeKind.Unspecified), 5, "Pending" }
                });

            migrationBuilder.InsertData(
                table: "Results",
                columns: new[] { "result_id", "enrolment_id", "finish_time", "position", "recorded_at", "recorded_by", "status" },
                values: new object[,]
                {
                    { 1, 1, new TimeSpan(0, 1, 42, 35, 0), 1, new DateTime(2026, 9, 20, 12, 30, 0, 0, DateTimeKind.Unspecified), 1, "Finished" },
                    { 2, 4, new TimeSpan(0, 2, 5, 18, 0), 2, new DateTime(2026, 9, 20, 12, 45, 0, 0, DateTimeKind.Unspecified), 1, "Finished" },
                    { 3, 2, new TimeSpan(0, 0, 52, 10, 0), 1, new DateTime(2026, 9, 20, 11, 15, 0, 0, DateTimeKind.Unspecified), 1, "Finished" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_event_id",
                table: "Categories",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "IX_Enrolments_category_id",
                table: "Enrolments",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "UQ_Enrolment_Unique",
                table: "Enrolments",
                columns: new[] { "participant_id", "category_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventAnnouncements_author_id",
                table: "EventAnnouncements",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_EventAnnouncements_event_id",
                table: "EventAnnouncements",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "IX_Events_organiser_id",
                table: "Events",
                column: "organiser_id");

            migrationBuilder.CreateIndex(
                name: "IX_Results_recorded_by",
                table: "Results",
                column: "recorded_by");

            migrationBuilder.CreateIndex(
                name: "UQ_Results_Enrolment",
                table: "Results",
                column: "enrolment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Routes_Event",
                table: "Routes",
                column: "event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Users_Email",
                table: "Users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventAnnouncements");

            migrationBuilder.DropTable(
                name: "Results");

            migrationBuilder.DropTable(
                name: "Routes");

            migrationBuilder.DropTable(
                name: "Enrolments");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
