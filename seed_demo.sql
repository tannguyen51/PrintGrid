INSERT INTO customer.customers ("Id", "Email", "PasswordHash", "FullName", "PhoneNumber", "CreatedAt", "LastLoginAt")
VALUES 
  ('a1b2c3d4-e5f6-4a1b-8c2d-3e4f5a6b7c8d', 'qc@printgrid.dev', '$2a$12$8kSA3bIVghipwSsqsQdiWOo0W3C2Y61sC/lUwdl31s.B5yd793yIC', 'Nhân viên QC Hub', '0901234567', NOW(), NOW()),
  ('b2c3d4e5-f6a1-4b2c-9d3e-4f5a6b7c8d9e', 'lab@printgrid.dev', '$2a$12$8kSA3bIVghipwSsqsQdiWOo0W3C2Y61sC/lUwdl31s.B5yd793yIC', 'Quản lý xưởng in', '0907654321', NOW(), NOW())
ON CONFLICT ("Email") DO NOTHING;

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

