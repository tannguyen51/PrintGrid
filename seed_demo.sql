INSERT INTO customer.customers
  ("Id", "Email", "PasswordHash", "FullName", "PhoneNumber", "Roles", "IsActive", "IsEmailVerified", "CreatedAt", "LastLoginAt")
VALUES
  (gen_random_uuid(), 'demo@printgrid.dev', '$2a$12$GVi9xSIX0fFjo3DTZOpfX.wAQOddrGrTGWZa.NCxMnCuy6wRfwvPO', 'Khách hàng Demo',   '0900000000', ARRAY['Customer'],                 true, true, NOW(), NOW()),
  (gen_random_uuid(), 'lab@printgrid.dev',  '$2a$12$GVi9xSIX0fFjo3DTZOpfX.wAQOddrGrTGWZa.NCxMnCuy6wRfwvPO', 'Quản lý xưởng in',  '0911000001', ARRAY['LabManager'],               true, true, NOW(), NOW()),
  (gen_random_uuid(), 'qc@printgrid.dev',   '$2a$12$GVi9xSIX0fFjo3DTZOpfX.wAQOddrGrTGWZa.NCxMnCuy6wRfwvPO', 'Nhân viên QC Hub',  '0911000002', ARRAY['HubQC', 'HubFulfillment'], true, true, NOW(), NOW()),
  (gen_random_uuid(), 'ops@printgrid.dev',  '$2a$12$GVi9xSIX0fFjo3DTZOpfX.wAQOddrGrTGWZa.NCxMnCuy6wRfwvPO', 'Ops Manager Demo',  '0911000003', ARRAY['OpsManager', 'Admin'],     true, true, NOW(), NOW())
ON CONFLICT ("Email") DO UPDATE SET
  "PasswordHash"    = EXCLUDED."PasswordHash",
  "Roles"           = EXCLUDED."Roles",
  "FullName"        = EXCLUDED."FullName",
  "PhoneNumber"     = EXCLUDED."PhoneNumber",
  "IsActive"        = true,
  "IsEmailVerified" = true;

INSERT INTO scheduling.labs
  ("Id", "Name", "City", "IsActive", "Status", "OperatingHours", "OnTimeDeliveryRate", "FirstPassYield", "TransitDaysToHub", "OwnerUserId", "ContactPhone", "Address", "CreatedAt", "IsInGoodStanding")
VALUES
  ('22222222-2222-2222-2222-222222222201', 'Xưởng In 3D Tân Bình', 'TP. Hồ Chí Minh', true, 'Active', 'T2-T7 08:00-20:00', 0.9500, 0.9200, 1, (SELECT "Id" FROM customer.customers WHERE "Email" = 'lab@printgrid.dev'), '0911000001', '45 Cộng Hòa, Q. Tân Bình, TP.HCM', NOW() - INTERVAL '10 days', true),
  ('22222222-2222-2222-2222-222222222202', 'Xưởng In Quận 7', 'TP. Hồ Chí Minh', false, 'Pending', 'T2-CN 07:30-21:00', 0.7000, 0.7000, 1, NULL, '0909123456', '88 Nguyễn Thị Thập, Quận 7, TP.HCM', NOW() - INTERVAL '1 day', true),
  ('22222222-2222-2222-2222-222222222203', 'Xưởng In Thủ Đức', 'TP. Thủ Đức', false, 'Pending', 'T2-T6 08:00-17:30', 0.7000, 0.7000, 2, NULL, '0909654321', '12 Võ Văn Ngân, TP. Thủ Đức', NOW() - INTERVAL '2 hours', true)
ON CONFLICT ("Id") DO UPDATE SET
  "Name"           = EXCLUDED."Name",
  "City"           = EXCLUDED."City",
  "Address"        = EXCLUDED."Address",
  "ContactPhone"   = EXCLUDED."ContactPhone",
  "OwnerUserId"    = EXCLUDED."OwnerUserId",
  "Status"         = EXCLUDED."Status",
  "IsActive"       = EXCLUDED."IsActive",
  "OperatingHours" = EXCLUDED."OperatingHours";

INSERT INTO scheduling.machines
  ("Id", "LabId", "Name", "Model", "Technology", "build_width_mm", "build_depth_mm", "build_height_mm", "MinLayerHeightMm", "AchievableToleranceMm", "Status", "SpeedFactor", "supported_materials", "MaintenanceFromUtc", "MaintenanceUntilUtc", "MaintenanceReason")
VALUES
  ('33333333-3333-3333-3333-333333333301', '22222222-2222-2222-2222-222222222201', 'Bambu Lab X1C #01', 'Bambu Lab X1 Carbon', 'Fdm', 256, 256, 256, 0.080, 0.100, 'Idle', 1.250, ARRAY['PLA', 'PETG', 'ABS', 'ASA'], NULL, NULL, NULL),
  ('33333333-3333-3333-3333-333333333302', '22222222-2222-2222-2222-222222222201', 'Prusa MK4 #02', 'Original Prusa MK4', 'Fdm', 250, 210, 220, 0.100, 0.120, 'Idle', 1.000, ARRAY['PLA', 'PETG', 'TPU'], NOW() + INTERVAL '6 hours', NOW() + INTERVAL '10 hours', 'Bảo trì định kỳ đầu phun 0.4mm'),
  ('33333333-3333-3333-3333-333333333303', '22222222-2222-2222-2222-222222222202', 'Formlabs Form 3+ #01', 'Formlabs Form 3+', 'Sla', 145, 145, 185, 0.025, 0.050, 'Idle', 1.000, ARRAY['RESIN_STD', 'RESIN_TOUGH'], NULL, NULL, NULL)
ON CONFLICT ("Id") DO UPDATE SET
  "Name"               = EXCLUDED."Name",
  "Model"              = EXCLUDED."Model",
  "Technology"         = EXCLUDED."Technology",
  "Status"             = EXCLUDED."Status",
  "MaintenanceReason"  = EXCLUDED."MaintenanceReason",
  "MaintenanceFromUtc" = EXCLUDED."MaintenanceFromUtc",
  "MaintenanceUntilUtc"= EXCLUDED."MaintenanceUntilUtc";

INSERT INTO scheduling.jobs
  ("Id", "OrderItemId", "ModelId", "required_width_mm", "required_depth_mm", "required_height_mm", "material_code", "color_code", "layer_height_mm", "tolerance_mm", "technology", "material_grams", "Status", "InternalDueDate", "EstimatedPrintMinutes", "ActualPrintMinutes", "AttemptNumber", "LabId", "MachineId", "PlannedStartUtc", "PlannedEndUtc", "StartedAtUtc", "CompletedAtUtc", "CreatedAt", "AssignedAtUtc")
VALUES
  ('44444444-4444-4444-4444-444444444401', 'e2b3c4d5-6a7b-8c9d-0e1f-2a3b4c5d6e71', 'd1c2b3a4-5e6f-7a8b-9c0d-1e2f3a4b5c6d', 120, 90, 60, 'PLA', 'BLACK', 0.200, 0.150, 'Fdm', 145.0, 'InProgress', CURRENT_DATE + INTERVAL '2 days', 180, NULL, 1, '22222222-2222-2222-2222-222222222201', '33333333-3333-3333-3333-333333333301', NOW() - INTERVAL '30 minutes', NOW() + INTERVAL '150 minutes', NOW() - INTERVAL '30 minutes', NULL, NOW() - INTERVAL '2 hours', NOW() - INTERVAL '1 hour'),
  ('44444444-4444-4444-4444-444444444402', 'e2b3c4d5-6a7b-8c9d-0e1f-2a3b4c5d6e72', 'd1c2b3a4-5e6f-7a8b-9c0d-1e2f3a4b5c6d', 100, 100, 45, 'PETG', 'ORANGE', 0.160, 0.120, 'Fdm', 110.0, 'Assigned', CURRENT_DATE + INTERVAL '3 days', 150, NULL, 1, '22222222-2222-2222-2222-222222222201', '33333333-3333-3333-3333-333333333302', NOW() + INTERVAL '1 hour', NOW() + INTERVAL '3 hours 30 minutes', NULL, NULL, NOW() - INTERVAL '15 minutes', NOW() - INTERVAL '10 minutes'),
  ('44444444-4444-4444-4444-444444444403', 'e2b3c4d5-6a7b-8c9d-0e1f-2a3b4c5d6e73', 'd1c2b3a4-5e6f-7a8b-9c0d-1e2f3a4b5c6d', 80, 80, 40, 'PLA', 'WHITE', 0.200, 0.150, 'Fdm', 85.0, 'Completed', CURRENT_DATE - INTERVAL '1 day', 120, 115, 1, '22222222-2222-2222-2222-222222222201', '33333333-3333-3333-3333-333333333301', NOW() - INTERVAL '2 days', NOW() - INTERVAL '46 hours', NOW() - INTERVAL '2 days', NOW() - INTERVAL '46 hours', NOW() - INTERVAL '3 days', NOW() - INTERVAL '2 days')
ON CONFLICT ("Id") DO UPDATE SET
  "technology" = EXCLUDED."technology",
  "Status"     = EXCLUDED."Status";

INSERT INTO customer.orders
  ("Id", "CustomerId", "QuoteId", "OrderNumber", "Status", "total_amount", "total_currency", "PromisedDeliveryDate", "delivery_street", "delivery_ward", "delivery_district", "delivery_city", "delivery_postal_code", "delivery_country", "CreatedAt", "ConfirmedAt", "ReadyForPackingAtUtc", "RefundedAmount")
VALUES
  ('55555555-5555-5555-5555-555555555501', (SELECT "Id" FROM customer.customers WHERE "Email" = 'demo@printgrid.dev'), '66666666-6666-6666-6666-666666666601', 'ORD-20261012-101', 'QualityCheck', 450000, 'VND', CURRENT_DATE + INTERVAL '3 days', '123 Nguyễn Huệ', 'Phường Bến Nghé', 'Quận 1', 'TP. Hồ Chí Minh', '700000', 'VN', NOW() - INTERVAL '1 day', NOW() - INTERVAL '20 hours', NOW() - INTERVAL '3 hours', 0),
  ('55555555-5555-5555-5555-555555555502', (SELECT "Id" FROM customer.customers WHERE "Email" = 'demo@printgrid.dev'), '66666666-6666-6666-6666-666666666602', 'ORD-20261012-102', 'QualityCheck', 680000, 'VND', CURRENT_DATE + INTERVAL '3 days', '123 Nguyễn Huệ', 'Phường Bến Nghé', 'Quận 1', 'TP. Hồ Chí Minh', '700000', 'VN', NOW() - INTERVAL '18 hours', NOW() - INTERVAL '16 hours', NOW() - INTERVAL '1 hour', 0)
ON CONFLICT ("Id") DO NOTHING;
