using System;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PaymentGateway.Web.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialMultiProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,")
                .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,");

            migrationBuilder.CreateTable(
                name: "audit_log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    app_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    old_values = table.Column<string>(type: "jsonb", nullable: true),
                    new_values = table.Column<string>(type: "jsonb", nullable: true),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_log", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "company",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    comp_code = table.Column<string>(type: "citext", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    contact_email = table.Column<string>(type: "citext", nullable: true),
                    contact_phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    default_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "DataProtectionKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FriendlyName = table.Column<string>(type: "text", nullable: true),
                    Xml = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_message",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    event_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    signature = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    max_attempts = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_attempted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_response_code = table.Column<int>(type: "integer", nullable: true),
                    last_error = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    delivered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_message", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "webhook_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    source = table.Column<short>(type: "smallint", nullable: false),
                    provider_code = table.Column<short>(type: "smallint", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payment_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    provider_transaction_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    headers = table.Column<string>(type: "jsonb", nullable: false),
                    body = table.Column<string>(type: "jsonb", nullable: false),
                    hmac_received = table.Column<string>(type: "text", nullable: true),
                    hmac_computed = table.Column<string>(type: "text", nullable: true),
                    hmac_valid = table.Column<bool>(type: "boolean", nullable: false),
                    processing_status = table.Column<short>(type: "smallint", nullable: false),
                    processing_error = table.Column<string>(type: "text", nullable: true),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    remote_ip = table.Column<IPAddress>(type: "inet", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_webhook_event", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "app_user",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: true),
                    email = table.Column<string>(type: "citext", nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<short>(type: "smallint", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    failed_login_count = table.Column<int>(type: "integer", nullable: false),
                    locked_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    must_change_password = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_user", x => x.id);
                    table.CheckConstraint("ck_app_user_role_company", "(role = 1 AND company_id IS NULL) OR (role IN (2,3) AND company_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_app_user_company_company_id",
                        column: x => x.company_id,
                        principalTable: "company",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "company_application",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    app_code = table.Column<string>(type: "citext", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    success_return_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    failure_return_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    pending_return_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    webhook_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    webhook_secret_encrypted = table.Column<byte[]>(type: "bytea", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_application", x => x.id);
                    table.ForeignKey(
                        name: "FK_company_application_company_company_id",
                        column: x => x.company_id,
                        principalTable: "company",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "company_integration_method",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_code = table.Column<short>(type: "smallint", nullable: false),
                    method_type = table.Column<short>(type: "smallint", nullable: false),
                    provider_integration_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_integration_method", x => x.id);
                    table.ForeignKey(
                        name: "FK_company_integration_method_company_company_id",
                        column: x => x.company_id,
                        principalTable: "company",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "company_provider_config",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_code = table.Column<short>(type: "smallint", nullable: false),
                    environment = table.Column<short>(type: "smallint", nullable: false),
                    api_key_encrypted = table.Column<byte[]>(type: "bytea", nullable: false),
                    public_key_encrypted = table.Column<byte[]>(type: "bytea", nullable: true),
                    secret_key_encrypted = table.Column<byte[]>(type: "bytea", nullable: true),
                    hmac_secret_encrypted = table.Column<byte[]>(type: "bytea", nullable: false),
                    extra_config_json = table.Column<string>(type: "jsonb", nullable: true),
                    base_url = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    display_label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_provider_config", x => x.id);
                    table.ForeignKey(
                        name: "FK_company_provider_config_company_company_id",
                        column: x => x.company_id,
                        principalTable: "company",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "customer",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_code = table.Column<string>(type: "citext", nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    mobile_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    email = table.Column<string>(type: "citext", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer", x => x.id);
                    table.ForeignKey(
                        name: "FK_customer_company_company_id",
                        column: x => x.company_id,
                        principalTable: "company",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "password_reset_token",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    app_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_password_reset_token", x => x.id);
                    table.ForeignKey(
                        name: "FK_password_reset_token_app_user_app_user_id",
                        column: x => x.app_user_id,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "application_api_key",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    company_application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key_prefix = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    key_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_application_api_key", x => x.id);
                    table.ForeignKey(
                        name: "FK_application_api_key_company_application_company_application~",
                        column: x => x.company_application_id,
                        principalTable: "company_application",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payment_order",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    order_reference = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    selected_provider_code = table.Column<short>(type: "smallint", nullable: true),
                    selected_method_id = table.Column<Guid>(type: "uuid", nullable: true),
                    provider_order_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    payment_token = table.Column<string>(type: "text", nullable: true),
                    payment_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    success_return_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    failure_return_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_order", x => x.id);
                    table.ForeignKey(
                        name: "FK_payment_order_company_application_company_application_id",
                        column: x => x.company_application_id,
                        principalTable: "company_application",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payment_order_company_company_id",
                        column: x => x.company_id,
                        principalTable: "company",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payment_order_company_integration_method_selected_method_id",
                        column: x => x.selected_method_id,
                        principalTable: "company_integration_method",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_payment_order_customer_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_transaction",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    payment_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_code = table.Column<short>(type: "smallint", nullable: false),
                    provider_transaction_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_success = table.Column<bool>(type: "boolean", nullable: false),
                    is_pending = table.Column<bool>(type: "boolean", nullable: false),
                    is_refund = table.Column<bool>(type: "boolean", nullable: false),
                    is_void = table.Column<bool>(type: "boolean", nullable: false),
                    is_3d_secure = table.Column<bool>(type: "boolean", nullable: false),
                    amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    provider_integration_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    source_data = table.Column<string>(type: "jsonb", nullable: true),
                    error_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    error_message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    hmac_valid = table.Column<bool>(type: "boolean", nullable: false),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_transaction", x => x.id);
                    table.ForeignKey(
                        name: "FK_payment_transaction_payment_order_payment_order_id",
                        column: x => x.payment_order_id,
                        principalTable: "payment_order",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_app_user_company_id",
                table: "app_user",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_app_user_email",
                table: "app_user",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_application_api_key_company_application_id",
                table: "application_api_key",
                column: "company_application_id");

            migrationBuilder.CreateIndex(
                name: "IX_application_api_key_key_prefix",
                table: "application_api_key",
                column: "key_prefix");

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_company_id_created_at",
                table: "audit_log",
                columns: new[] { "company_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_entity_type_entity_id",
                table: "audit_log",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "IX_company_comp_code",
                table: "company",
                column: "comp_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_company_application_company_id_app_code",
                table: "company_application",
                columns: new[] { "company_id", "app_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_company_integration_method_company_id_provider_code",
                table: "company_integration_method",
                columns: new[] { "company_id", "provider_code" });

            migrationBuilder.CreateIndex(
                name: "IX_company_integration_method_company_id_provider_code_method_~",
                table: "company_integration_method",
                columns: new[] { "company_id", "provider_code", "method_type", "provider_integration_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_company_provider_config_company_id_provider_code",
                table: "company_provider_config",
                columns: new[] { "company_id", "provider_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_company_id_customer_code",
                table: "customer",
                columns: new[] { "company_id", "customer_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_company_id_mobile_number",
                table: "customer",
                columns: new[] { "company_id", "mobile_number" });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_message_status_next_attempt_at",
                table: "outbox_message",
                columns: new[] { "status", "next_attempt_at" });

            migrationBuilder.CreateIndex(
                name: "IX_password_reset_token_app_user_id",
                table: "password_reset_token",
                column: "app_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_order_company_application_id_created_at",
                table: "payment_order",
                columns: new[] { "company_application_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_payment_order_company_id_created_at",
                table: "payment_order",
                columns: new[] { "company_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_payment_order_company_id_external_reference",
                table: "payment_order",
                columns: new[] { "company_id", "external_reference" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_order_company_id_status",
                table: "payment_order",
                columns: new[] { "company_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_order_customer_id",
                table: "payment_order",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_order_order_reference",
                table: "payment_order",
                column: "order_reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_order_provider_order_id",
                table: "payment_order",
                column: "provider_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_order_selected_method_id",
                table: "payment_order",
                column: "selected_method_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_transaction_company_id_is_success_received_at",
                table: "payment_transaction",
                columns: new[] { "company_id", "is_success", "received_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_payment_transaction_company_id_received_at",
                table: "payment_transaction",
                columns: new[] { "company_id", "received_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_payment_transaction_payment_order_id",
                table: "payment_transaction",
                column: "payment_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_transaction_provider_code_provider_transaction_id",
                table: "payment_transaction",
                columns: new[] { "provider_code", "provider_transaction_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_webhook_event_processing_status_received_at",
                table: "webhook_event",
                columns: new[] { "processing_status", "received_at" });

            migrationBuilder.CreateIndex(
                name: "IX_webhook_event_provider_code_provider_transaction_id",
                table: "webhook_event",
                columns: new[] { "provider_code", "provider_transaction_id" });

            migrationBuilder.CreateIndex(
                name: "IX_webhook_event_received_at",
                table: "webhook_event",
                column: "received_at",
                descending: new bool[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "application_api_key");

            migrationBuilder.DropTable(
                name: "audit_log");

            migrationBuilder.DropTable(
                name: "company_provider_config");

            migrationBuilder.DropTable(
                name: "DataProtectionKeys");

            migrationBuilder.DropTable(
                name: "outbox_message");

            migrationBuilder.DropTable(
                name: "password_reset_token");

            migrationBuilder.DropTable(
                name: "payment_transaction");

            migrationBuilder.DropTable(
                name: "webhook_event");

            migrationBuilder.DropTable(
                name: "app_user");

            migrationBuilder.DropTable(
                name: "payment_order");

            migrationBuilder.DropTable(
                name: "company_application");

            migrationBuilder.DropTable(
                name: "company_integration_method");

            migrationBuilder.DropTable(
                name: "customer");

            migrationBuilder.DropTable(
                name: "company");
        }
    }
}
