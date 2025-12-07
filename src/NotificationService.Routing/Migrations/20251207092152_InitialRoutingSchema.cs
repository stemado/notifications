using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotificationService.Routing.Migrations
{
    /// <inheritdoc />
    public partial class InitialRoutingSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contacts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    organization = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()"),
                    deactivated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contacts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbound_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    service = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    topic = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    client_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    template_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    body = table.Column<string>(type: "text", nullable: true),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    saga_id = table.Column<Guid>(type: "uuid", nullable: true),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()"),
                    processed_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbound_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "recipient_groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    client_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recipient_groups", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "group_memberships",
                columns: table => new
                {
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: false),
                    added_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()"),
                    added_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_memberships", x => new { x.group_id, x.contact_id });
                    table.ForeignKey(
                        name: "FK_group_memberships_contacts_contact_id",
                        column: x => x.contact_id,
                        principalTable: "contacts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_group_memberships_recipient_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "recipient_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "routing_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    service = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    topic = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    client_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    min_severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    recipient_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_routing_policies", x => x.id);
                    table.ForeignKey(
                        name: "FK_routing_policies_recipient_groups_recipient_group_id",
                        column: x => x.recipient_group_id,
                        principalTable: "recipient_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "outbound_deliveries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    outbound_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    routing_policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    role = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'Pending'"),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()"),
                    sent_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    delivered_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    failed_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    next_retry_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbound_deliveries", x => x.id);
                    table.ForeignKey(
                        name: "FK_outbound_deliveries_contacts_contact_id",
                        column: x => x.contact_id,
                        principalTable: "contacts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_outbound_deliveries_outbound_events_outbound_event_id",
                        column: x => x.outbound_event_id,
                        principalTable: "outbound_events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_outbound_deliveries_routing_policies_routing_policy_id",
                        column: x => x.routing_policy_id,
                        principalTable: "routing_policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_contacts_active",
                table: "contacts",
                column: "is_active",
                filter: "is_active = true");

            migrationBuilder.CreateIndex(
                name: "idx_contacts_email",
                table: "contacts",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "idx_contacts_organization",
                table: "contacts",
                column: "organization",
                filter: "organization IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_group_memberships_contact",
                table: "group_memberships",
                column: "contact_id");

            migrationBuilder.CreateIndex(
                name: "idx_outbound_deliveries_contact",
                table: "outbound_deliveries",
                columns: new[] { "contact_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_outbound_deliveries_event",
                table: "outbound_deliveries",
                column: "outbound_event_id");

            migrationBuilder.CreateIndex(
                name: "idx_outbound_deliveries_pending",
                table: "outbound_deliveries",
                columns: new[] { "status", "created_at" },
                filter: "status IN ('Pending', 'Failed')");

            migrationBuilder.CreateIndex(
                name: "IX_outbound_deliveries_routing_policy_id",
                table: "outbound_deliveries",
                column: "routing_policy_id");

            migrationBuilder.CreateIndex(
                name: "idx_outbound_events_client",
                table: "outbound_events",
                columns: new[] { "client_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_outbound_events_saga",
                table: "outbound_events",
                column: "saga_id",
                filter: "saga_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_outbound_events_unprocessed",
                table: "outbound_events",
                column: "created_at",
                filter: "processed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_recipient_groups_client",
                table: "recipient_groups",
                column: "client_id",
                filter: "client_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_recipient_groups_name_client",
                table: "recipient_groups",
                columns: new[] { "name", "client_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_routing_policies_client_fallback",
                table: "routing_policies",
                columns: new[] { "service", "topic", "is_enabled" },
                filter: "client_id IS NULL AND is_enabled = true");

            migrationBuilder.CreateIndex(
                name: "idx_routing_policies_lookup",
                table: "routing_policies",
                columns: new[] { "service", "topic", "client_id", "is_enabled" },
                filter: "is_enabled = true");

            migrationBuilder.CreateIndex(
                name: "IX_routing_policies_recipient_group_id",
                table: "routing_policies",
                column: "recipient_group_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "group_memberships");

            migrationBuilder.DropTable(
                name: "outbound_deliveries");

            migrationBuilder.DropTable(
                name: "contacts");

            migrationBuilder.DropTable(
                name: "outbound_events");

            migrationBuilder.DropTable(
                name: "routing_policies");

            migrationBuilder.DropTable(
                name: "recipient_groups");
        }
    }
}
