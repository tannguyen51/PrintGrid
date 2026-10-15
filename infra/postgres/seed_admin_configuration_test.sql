-- Re-runnable local test data for Admin commercial configuration and shared catalogues.
-- Login: admin@printgrid.dev / Demo@123

INSERT INTO customer.customers
  ("Id", "Email", "PasswordHash", "FullName", "PhoneNumber", "Roles", "IsActive", "IsEmailVerified", "CreatedAt", "LastLoginAt")
VALUES
  ('30000000-0000-0000-0000-000000000001', 'admin@printgrid.dev',
   '$2a$12$GVi9xSIX0fFjo3DTZOpfX.wAQOddrGrTGWZa.NCxMnCuy6wRfwvPO',
   'Quản trị viên Demo', '0900000099', ARRAY['Admin'], TRUE, TRUE, NOW(), NOW())
ON CONFLICT ("Email") DO UPDATE SET
  "PasswordHash" = EXCLUDED."PasswordHash",
  "FullName" = EXCLUDED."FullName",
  "PhoneNumber" = EXCLUDED."PhoneNumber",
  "Roles" = EXCLUDED."Roles",
  "IsActive" = TRUE,
  "IsEmailVerified" = TRUE;

INSERT INTO admin.commercial_configuration_versions
  ("Id", "VersionNumber", "Name", "ParametersJson", "ChangeSummary", "Status", "CreatedBy", "CreatedAtUtc", "ActivatedAtUtc")
SELECT
  '31000000-0000-0000-0000-000000000002', 2, 'Điều chỉnh giá mùa cao điểm',
  '{"materialRates":{"PLA":470,"PETG":575,"ABS":520,"TPU":680,"RESIN":1550},"materialDensities":{"PLA":1.24,"PETG":1.27,"ABS":1.04,"TPU":1.21,"RESIN":1.10},"pricePerPrintHour":99000,"hubHandlingFee":28000,"platformMarginPercent":13,"prioritySurchargePercent":25,"autoLaneThreshold":350000,"quoteValidityHours":36,"assignmentWeights":{"time":0.35,"load":0.20,"quality":0.20,"cost":0.10,"logistics":0.15},"maxBatchSize":120,"schedulingTimeBudgetSeconds":30,"slaRiskThresholdHours":18}'::jsonb,
  'Dữ liệu test: tăng giá giờ máy, phí hub và ưu tiên thời gian giao.', 'Draft',
  '30000000-0000-0000-0000-000000000001', NOW() - INTERVAL '2 hours', NULL
WHERE NOT EXISTS (SELECT 1 FROM admin.commercial_configuration_versions WHERE "VersionNumber" = 2);

INSERT INTO admin.commercial_configuration_versions
  ("Id", "VersionNumber", "Name", "ParametersJson", "ChangeSummary", "Status", "CreatedBy", "CreatedAtUtc", "ActivatedAtUtc")
SELECT
  '31000000-0000-0000-0000-000000000003', 3, 'Thử nghiệm tối ưu chi phí',
  '{"materialRates":{"PLA":470,"PETG":575,"ABS":520,"TPU":680,"RESIN":1550},"materialDensities":{"PLA":1.24,"PETG":1.27,"ABS":1.04,"TPU":1.21,"RESIN":1.10},"pricePerPrintHour":92000,"hubHandlingFee":22000,"platformMarginPercent":10,"prioritySurchargePercent":18,"autoLaneThreshold":250000,"quoteValidityHours":72,"assignmentWeights":{"time":0.20,"load":0.20,"quality":0.20,"cost":0.25,"logistics":0.15},"maxBatchSize":80,"schedulingTimeBudgetSeconds":45,"slaRiskThresholdHours":30}'::jsonb,
  'Dữ liệu test: ưu tiên chi phí và kéo dài thời hạn báo giá.', 'Draft',
  '30000000-0000-0000-0000-000000000001', NOW() - INTERVAL '1 hour', NULL
WHERE NOT EXISTS (SELECT 1 FROM admin.commercial_configuration_versions WHERE "VersionNumber" = 3);

INSERT INTO admin.shared_catalog_items
  ("Id", "CatalogType", "Code", "Revision", "Name", "Description", "UnitRate", "IsPremium", "PropertiesJson", "IsDeprecated", "IsCurrent", "CreatedBy", "CreatedAtUtc")
VALUES
  ('32000000-0000-0000-0000-000000000001','Color','BLUE',1,'Xanh dương','Màu tiêu chuẩn',NULL,FALSE,'{"hex":"#2563EB"}'::jsonb,FALSE,TRUE,'30000000-0000-0000-0000-000000000001',NOW()),
  ('32000000-0000-0000-0000-000000000002','Color','GREEN',1,'Xanh lá','Màu tiêu chuẩn',NULL,FALSE,'{"hex":"#16A34A"}'::jsonb,FALSE,TRUE,'30000000-0000-0000-0000-000000000001',NOW()),
  ('32000000-0000-0000-0000-000000000003','Color','GOLD',1,'Vàng kim','Màu hiệu ứng cao cấp',NULL,TRUE,'{"hex":"#D4AF37"}'::jsonb,FALSE,TRUE,'30000000-0000-0000-0000-000000000001',NOW()),
  ('32000000-0000-0000-0000-000000000011','Technology','DLP',1,'DLP','In resin bằng máy chiếu kỹ thuật số',NULL,TRUE,'{}'::jsonb,FALSE,TRUE,'30000000-0000-0000-0000-000000000001',NOW()),
  ('32000000-0000-0000-0000-000000000021','QualityGrade','INDUSTRIAL',1,'Công nghiệp','Yêu cầu dung sai và độ bền cao',NULL,TRUE,'{"inspectionLevel":"strict"}'::jsonb,FALSE,TRUE,'30000000-0000-0000-0000-000000000001',NOW()),
  ('32000000-0000-0000-0000-000000000022','QualityGrade','DRAFT',1,'Bản nháp','Ưu tiên tốc độ, dùng cho prototype',NULL,FALSE,'{"inspectionLevel":"basic"}'::jsonb,FALSE,TRUE,'30000000-0000-0000-0000-000000000001',NOW()),
  ('32000000-0000-0000-0000-000000000031','PostProcessing','POLISHING',1,'Đánh bóng','Làm mịn và tăng độ bóng bề mặt',NULL,TRUE,'{}'::jsonb,FALSE,TRUE,'30000000-0000-0000-0000-000000000001',NOW()),
  ('32000000-0000-0000-0000-000000000032','PostProcessing','DYEING',1,'Nhuộm màu','Nhuộm màu cho chi tiết phù hợp',NULL,TRUE,'{}'::jsonb,FALSE,TRUE,'30000000-0000-0000-0000-000000000001',NOW())
ON CONFLICT DO NOTHING;

SELECT
  (SELECT COUNT(*) FROM admin.commercial_configuration_versions) AS configuration_versions,
  (SELECT COUNT(*) FROM admin.shared_catalog_items WHERE "IsCurrent") AS current_catalog_items,
  (SELECT COUNT(*) FROM customer.customers WHERE 'Admin' = ANY("Roles")) AS admin_accounts;
