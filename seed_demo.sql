-- ============================================================================
-- TÀI KHOẢN DEMO — chạy lại nhiều lần vẫn đúng (upsert theo Email).
-- Mật khẩu chung: Demo@123  (hash dưới đây đã verify bằng BCrypt.Net.Verify)
--
-- ⚠️ Vì sao phải có "Roles" và "IsEmailVerified" trong câu lệnh này:
--   - Cột "Roles" có default ARRAY[]::text[]. INSERT thô mà bỏ cột này sẽ tạo
--     tài khoản KHÔNG role → đăng nhập vẫn 200 nhưng mọi API policy trả 403 và
--     FE ProtectedRoute đá sang /forbidden ("403 không có quyền truy cập").
--   - OrderConfigPage chặn nút đặt hàng khi isEmailVerified = false → tài khoản
--     demo phải được đánh dấu xác thực, nếu không luồng đặt đơn không demo được.
-- ============================================================================
INSERT INTO customer.customers
  ("Id", "Email", "PasswordHash", "FullName", "PhoneNumber", "Roles", "IsActive", "IsEmailVerified", "CreatedAt", "LastLoginAt")
VALUES
  (gen_random_uuid(), 'demo@printgrid.dev', '$2a$12$GVi9xSIX0fFjo3DTZOpfX.wAQOddrGrTGWZa.NCxMnCuy6wRfwvPO', 'Khách hàng Demo',   '0900000000', ARRAY['Customer'],   true, true, NOW(), NOW()),
  (gen_random_uuid(), 'lab@printgrid.dev',  '$2a$12$GVi9xSIX0fFjo3DTZOpfX.wAQOddrGrTGWZa.NCxMnCuy6wRfwvPO', 'Quản lý xưởng in',  '0911000001', ARRAY['LabManager'], true, true, NOW(), NOW()),
  (gen_random_uuid(), 'qc@printgrid.dev',   '$2a$12$GVi9xSIX0fFjo3DTZOpfX.wAQOddrGrTGWZa.NCxMnCuy6wRfwvPO', 'Nhân viên QC Hub',  '0911000002', ARRAY['HubQC'],      true, true, NOW(), NOW()),
  (gen_random_uuid(), 'ops@printgrid.dev',  '$2a$12$GVi9xSIX0fFjo3DTZOpfX.wAQOddrGrTGWZa.NCxMnCuy6wRfwvPO', 'Ops Manager Demo',  '0911000003', ARRAY['OpsManager'], true, true, NOW(), NOW())
ON CONFLICT ("Email") DO UPDATE SET
  "PasswordHash"    = EXCLUDED."PasswordHash",
  "Roles"           = EXCLUDED."Roles",
  "FullName"        = EXCLUDED."FullName",
  "PhoneNumber"     = EXCLUDED."PhoneNumber",
  "IsActive"        = true,
  "IsEmailVerified" = true;

-- Tạo sẵn 1 Job mẫu đang ở trạng thái AwaitingInspection để test ngay màn hình QC Hub
INSERT INTO scheduling.jobs (
  "Id", "OrderItemId", "ModelId", "required_width_mm", "required_depth_mm", "required_height_mm",
  "material_code", "color_code", "layer_height_mm", "tolerance_mm", "technology", "material_grams",
  "Status", "InternalDueDate", "EstimatedPrintMinutes", "ActualPrintMinutes", "AttemptNumber", "CreatedAt"
) VALUES (
  'f47ac10b-58cc-4372-a567-0e02b2c3d479',
  'e2b3c4d5-6a7b-8c9d-0e1f-2a3b4c5d6e7f',
  'd1c2b3a4-5e6f-7a8b-9c0d-1e2f3a4b5c6d',
  120.0, 80.0, 50.0,
  'PLA-PRO', 'MATTE-BLACK', 0.2, 0.1, 0, 150.0,
  4, -- 4 = AwaitingInspection
  CURRENT_DATE + INTERVAL '3 days', 180, 175, 1, NOW()
) ON CONFLICT ("Id") DO NOTHING;

-- Tạo sẵn 1 Job mẫu đang ở trạng thái Assigned để test màn hình Lab Quản lý xưởng (FR-LAB-004 Hạn 2 giờ)
INSERT INTO scheduling.jobs (
  "Id", "OrderItemId", "ModelId", "required_width_mm", "required_depth_mm", "required_height_mm",
  "material_code", "color_code", "layer_height_mm", "tolerance_mm", "technology", "material_grams",
  "Status", "InternalDueDate", "EstimatedPrintMinutes", "ActualPrintMinutes", "AttemptNumber", "CreatedAt", "AssignedAtUtc"
) VALUES (
  'a38bc21c-69dd-4483-b678-1f13c3d4e580',
  'e2b3c4d5-6a7b-8c9d-0e1f-2a3b4c5d6e7f',
  'd1c2b3a4-5e6f-7a8b-9c0d-1e2f3a4b5c6d',
  90.0, 90.0, 45.0,
  'PETG', 'SIGNAL-ORANGE', 0.16, 0.1, 0, 95.0,
  1, -- 1 = Assigned
  CURRENT_DATE + INTERVAL '2 days', 120, 0, 1, NOW(), NOW()
) ON CONFLICT ("Id") DO NOTHING;

