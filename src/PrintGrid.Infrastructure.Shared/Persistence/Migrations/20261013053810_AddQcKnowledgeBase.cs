using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQcKnowledgeBase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "InspectedAtUtc",
                schema: "scheduling",
                table: "jobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InspectionChecklistResultsJson",
                schema: "scheduling",
                table: "jobs",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InspectionChecklistVersionId",
                schema: "scheduling",
                table: "jobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InspectionFaultAttribution",
                schema: "scheduling",
                table: "jobs",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InspectionNote",
                schema: "scheduling",
                table: "jobs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "inspection_defect_code",
                schema: "scheduling",
                table: "jobs",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "defect_taxonomy_items",
                schema: "admin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DefaultFaultAttribution = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    IsDeprecated = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_defect_taxonomy_items", x => x.Id);
                    table.UniqueConstraint("AK_defect_taxonomy_items_Code", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "inspection_checklist_versions",
                schema: "admin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QualityGradeCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    ItemsJson = table.Column<string>(type: "jsonb", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inspection_checklist_versions", x => x.Id);
                });

            migrationBuilder.Sql("""
                INSERT INTO admin.inspection_checklist_versions
                    ("Id", "QualityGradeCode", "VersionNumber", "ItemsJson", "IsActive", "CreatedBy", "CreatedAtUtc")
                VALUES
                    ('30000000-0000-0000-0000-000000000001', 'STANDARD', 1,
                     '[{"code":"DIMENSION","name":"Kích thước hình học và dung sai","passCriteria":"Sai lệch không vượt quá ±0,5 mm","isMandatory":true,"displayOrder":1},{"code":"SURFACE","name":"Bề mặt và hoàn thiện","passCriteria":"Không nứt, rỗ hoặc ba via ảnh hưởng sử dụng","isMandatory":true,"displayOrder":2},{"code":"MATERIAL_COLOR","name":"Đúng vật liệu và màu sắc","passCriteria":"Khớp cấu hình trên lệnh sản xuất","isMandatory":true,"displayOrder":3},{"code":"LAYER_ADHESION","name":"Độ bám dính lớp in","passCriteria":"Không tách lớp khi kiểm tra trực quan","isMandatory":true,"displayOrder":4}]'::jsonb,
                     TRUE, '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-10-13 00:00:00+00'),
                    ('30000000-0000-0000-0000-000000000002', 'FINE', 1,
                     '[{"code":"DIMENSION","name":"Kích thước hình học và dung sai","passCriteria":"Sai lệch không vượt quá ±0,2 mm","isMandatory":true,"displayOrder":1},{"code":"SURFACE","name":"Bề mặt chi tiết cao","passCriteria":"Bề mặt đồng đều, không có khuyết tật nhìn thấy ở khoảng cách 30 cm","isMandatory":true,"displayOrder":2},{"code":"MATERIAL_COLOR","name":"Đúng vật liệu và màu sắc","passCriteria":"Khớp cấu hình trên lệnh sản xuất","isMandatory":true,"displayOrder":3},{"code":"LAYER_ADHESION","name":"Độ bám dính lớp in","passCriteria":"Không tách lớp hoặc đường lớp bất thường","isMandatory":true,"displayOrder":4},{"code":"FINE_DETAIL","name":"Chi tiết nhỏ","passCriteria":"Các chi tiết thiết kế từ 0,5 mm trở lên thể hiện rõ","isMandatory":true,"displayOrder":5}]'::jsonb,
                     TRUE, '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-10-13 00:00:00+00');

                INSERT INTO admin.defect_taxonomy_items
                    ("Id", "Code", "Name", "Description", "DefaultFaultAttribution", "IsDeprecated", "CreatedBy", "CreatedAtUtc", "UpdatedBy", "UpdatedAtUtc")
                VALUES
                    ('31000000-0000-0000-0000-000000000001','DIMENSIONAL','Sai kích thước','Kích thước hoặc dung sai vượt tiêu chuẩn của cấp chất lượng.','Lab',FALSE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00',NULL,NULL),
                    ('31000000-0000-0000-0000-000000000002','SURFACE_DEFECT','Lỗi bề mặt','Rỗ, nứt, xước hoặc bề mặt hoàn thiện không đạt.','Lab',FALSE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00',NULL,NULL),
                    ('31000000-0000-0000-0000-000000000003','WRONG_MATERIAL_COLOR','Sai vật liệu hoặc màu','Sản phẩm không khớp vật liệu hoặc màu đã đặt.','Lab',FALSE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00',NULL,NULL),
                    ('31000000-0000-0000-0000-000000000004','LAYER_FAILURE','Tách lớp','Các lớp in không liên kết hoặc xuất hiện layer shift.','Lab',FALSE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00',NULL,NULL),
                    ('31000000-0000-0000-0000-000000000005','WARPING','Cong vênh','Đế hoặc thân sản phẩm bị cong vênh ngoài dung sai.','Lab',FALSE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00',NULL,NULL),
                    ('31000000-0000-0000-0000-000000000006','TRANSIT_DAMAGE','Hư hỏng tại Hub','Sản phẩm bị va đập, trầy xước hoặc hư hỏng trong xử lý tại Hub.','Hub',FALSE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00',NULL,NULL),
                    ('31000000-0000-0000-0000-000000000007','SOURCE_MODEL','Lỗi mô hình nguồn','Hình học hoặc thiết kế file khách hàng khiến sản phẩm không thể đạt.','Customer',FALSE,'00000000-0000-0000-0000-000000000000',TIMESTAMPTZ '2026-10-13 00:00:00+00',NULL,NULL);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_jobs_inspection_defect_code",
                schema: "scheduling",
                table: "jobs",
                column: "inspection_defect_code");

            migrationBuilder.CreateIndex(
                name: "IX_jobs_InspectionChecklistVersionId",
                schema: "scheduling",
                table: "jobs",
                column: "InspectionChecklistVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_defect_taxonomy_items_IsDeprecated_Name",
                schema: "admin",
                table: "defect_taxonomy_items",
                columns: new[] { "IsDeprecated", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_inspection_checklist_versions_QualityGradeCode_IsActive",
                schema: "admin",
                table: "inspection_checklist_versions",
                columns: new[] { "QualityGradeCode", "IsActive" },
                unique: true,
                filter: "\"IsActive\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_inspection_checklist_versions_QualityGradeCode_VersionNumber",
                schema: "admin",
                table: "inspection_checklist_versions",
                columns: new[] { "QualityGradeCode", "VersionNumber" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_jobs_defect_taxonomy_items_inspection_defect_code",
                schema: "scheduling",
                table: "jobs",
                column: "inspection_defect_code",
                principalSchema: "admin",
                principalTable: "defect_taxonomy_items",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_jobs_inspection_checklist_versions_InspectionChecklistVersi~",
                schema: "scheduling",
                table: "jobs",
                column: "InspectionChecklistVersionId",
                principalSchema: "admin",
                principalTable: "inspection_checklist_versions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_jobs_defect_taxonomy_items_inspection_defect_code",
                schema: "scheduling",
                table: "jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_jobs_inspection_checklist_versions_InspectionChecklistVersi~",
                schema: "scheduling",
                table: "jobs");

            migrationBuilder.DropTable(
                name: "defect_taxonomy_items",
                schema: "admin");

            migrationBuilder.DropTable(
                name: "inspection_checklist_versions",
                schema: "admin");

            migrationBuilder.DropIndex(
                name: "IX_jobs_inspection_defect_code",
                schema: "scheduling",
                table: "jobs");

            migrationBuilder.DropIndex(
                name: "IX_jobs_InspectionChecklistVersionId",
                schema: "scheduling",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "InspectedAtUtc",
                schema: "scheduling",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "InspectionChecklistResultsJson",
                schema: "scheduling",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "InspectionChecklistVersionId",
                schema: "scheduling",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "InspectionFaultAttribution",
                schema: "scheduling",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "InspectionNote",
                schema: "scheduling",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "inspection_defect_code",
                schema: "scheduling",
                table: "jobs");
        }
    }
}
