using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCommercialConfigurationAndSharedCatalogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "commercial_configuration_versions",
                schema: "admin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ParametersJson = table.Column<string>(type: "jsonb", nullable: false),
                    ChangeSummary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActivatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commercial_configuration_versions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "shared_catalog_items",
                schema: "admin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CatalogType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    UnitRate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    IsPremium = table.Column<bool>(type: "boolean", nullable: false),
                    PropertiesJson = table.Column<string>(type: "jsonb", nullable: false),
                    IsDeprecated = table.Column<bool>(type: "boolean", nullable: false),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shared_catalog_items", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_commercial_configuration_versions_Status",
                schema: "admin",
                table: "commercial_configuration_versions",
                column: "Status",
                unique: true,
                filter: "\"Status\" = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_commercial_configuration_versions_VersionNumber",
                schema: "admin",
                table: "commercial_configuration_versions",
                column: "VersionNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shared_catalog_items_CatalogType_Code_IsCurrent",
                schema: "admin",
                table: "shared_catalog_items",
                columns: new[] { "CatalogType", "Code", "IsCurrent" },
                unique: true,
                filter: "\"IsCurrent\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_shared_catalog_items_CatalogType_Code_Revision",
                schema: "admin",
                table: "shared_catalog_items",
                columns: new[] { "CatalogType", "Code", "Revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shared_catalog_items_CatalogType_IsCurrent_IsDeprecated",
                schema: "admin",
                table: "shared_catalog_items",
                columns: new[] { "CatalogType", "IsCurrent", "IsDeprecated" });

            migrationBuilder.Sql(
                """
                INSERT INTO admin.commercial_configuration_versions
                    ("Id", "VersionNumber", "Name", "ParametersJson", "ChangeSummary", "Status", "CreatedBy", "CreatedAtUtc", "ActivatedAtUtc")
                VALUES
                    ('10000000-0000-0000-0000-000000000001', 1, 'Cấu hình thương mại ban đầu',
                     '{"materialRates":{"PLA":470,"PETG":575,"ABS":520,"TPU":680,"RESIN":1550},"materialDensities":{"PLA":1.24,"PETG":1.27,"ABS":1.04,"TPU":1.21,"RESIN":1.10},"pricePerPrintHour":94500,"hubHandlingFee":25000,"platformMarginPercent":12,"prioritySurchargePercent":20,"autoLaneThreshold":300000,"quoteValidityHours":48,"assignmentWeights":{"time":0.30,"load":0.20,"quality":0.20,"cost":0.15,"logistics":0.15},"maxBatchSize":100,"schedulingTimeBudgetSeconds":30,"slaRiskThresholdHours":24}'::jsonb,
                     'Khởi tạo từ bộ tham số đang vận hành trước khi có màn quản trị phiên bản.', 'Active',
                     '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-10-13 00:00:00+00', TIMESTAMPTZ '2026-10-13 00:00:00+00');

                INSERT INTO admin.shared_catalog_items
                    ("Id", "CatalogType", "Code", "Revision", "Name", "Description", "UnitRate", "IsPremium", "PropertiesJson", "IsDeprecated", "IsCurrent", "CreatedBy", "CreatedAtUtc")
                VALUES
                    ('20000000-0000-0000-0000-000000000001','Material','PLA',1,'PLA','Dễ in, phù hợp mẫu phổ thông',470,FALSE,'{"density":1.24}'::jsonb,FALSE,TRUE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00'),
                    ('20000000-0000-0000-0000-000000000002','Material','PETG',1,'PETG','Bền và chịu ẩm tốt',575,FALSE,'{"density":1.27}'::jsonb,FALSE,TRUE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00'),
                    ('20000000-0000-0000-0000-000000000003','Material','ABS',1,'ABS','Chịu nhiệt, phù hợp chi tiết kỹ thuật',520,FALSE,'{"density":1.04}'::jsonb,FALSE,TRUE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00'),
                    ('20000000-0000-0000-0000-000000000004','Material','TPU',1,'TPU','Vật liệu dẻo',680,TRUE,'{"density":1.21}'::jsonb,FALSE,TRUE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00'),
                    ('20000000-0000-0000-0000-000000000005','Material','RESIN',1,'Resin','Chi tiết cao cho công nghệ SLA',1550,TRUE,'{"density":1.10}'::jsonb,FALSE,TRUE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00'),
                    ('20000000-0000-0000-0000-000000000011','Color','BLACK',1,'Đen',NULL,NULL,FALSE,'{"hex":"#111111"}'::jsonb,FALSE,TRUE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00'),
                    ('20000000-0000-0000-0000-000000000012','Color','WHITE',1,'Trắng',NULL,NULL,FALSE,'{"hex":"#F5F5F5"}'::jsonb,FALSE,TRUE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00'),
                    ('20000000-0000-0000-0000-000000000013','Color','GRAY',1,'Xám',NULL,NULL,FALSE,'{"hex":"#808080"}'::jsonb,FALSE,TRUE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00'),
                    ('20000000-0000-0000-0000-000000000014','Color','RED',1,'Đỏ',NULL,NULL,TRUE,'{"hex":"#D32F2F"}'::jsonb,FALSE,TRUE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00'),
                    ('20000000-0000-0000-0000-000000000021','Technology','FFF',1,'FFF/FDM','Đùn sợi nhựa nóng chảy',NULL,FALSE,'{}'::jsonb,FALSE,TRUE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00'),
                    ('20000000-0000-0000-0000-000000000022','Technology','SLA',1,'SLA','Quang trùng hợp nhựa resin',NULL,TRUE,'{}'::jsonb,FALSE,TRUE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00'),
                    ('20000000-0000-0000-0000-000000000023','Technology','SLS',1,'SLS','Thiêu kết bột bằng laser',NULL,TRUE,'{}'::jsonb,FALSE,TRUE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00'),
                    ('20000000-0000-0000-0000-000000000031','QualityGrade','STANDARD',1,'Tiêu chuẩn','Chất lượng mặc định',NULL,FALSE,'{}'::jsonb,FALSE,TRUE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00'),
                    ('20000000-0000-0000-0000-000000000032','QualityGrade','FINE',1,'Chi tiết cao','Bề mặt và dung sai tốt hơn',NULL,TRUE,'{}'::jsonb,FALSE,TRUE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00'),
                    ('20000000-0000-0000-0000-000000000041','PostProcessing','SANDING',1,'Chà nhám',NULL,NULL,FALSE,'{}'::jsonb,FALSE,TRUE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00'),
                    ('20000000-0000-0000-0000-000000000042','PostProcessing','PAINTING',1,'Sơn hoàn thiện',NULL,NULL,TRUE,'{}'::jsonb,FALSE,TRUE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "commercial_configuration_versions",
                schema: "admin");

            migrationBuilder.DropTable(
                name: "shared_catalog_items",
                schema: "admin");
        }
    }
}
