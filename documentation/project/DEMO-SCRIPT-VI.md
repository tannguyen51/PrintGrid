# Kịch bản demo PrintGrid (12–15 phút)

> Mục tiêu: kể một câu chuyện xuyên suốt từ nhu cầu in 3D của khách hàng đến điều phối, sản xuất, kiểm định và quản trị hệ thống. Không cố trình diễn mọi màn hình; tập trung vào giá trị khác biệt của PrintGrid: **một mạng lưới xưởng hoạt động như một nhà máy ảo**.

## 1. Chuẩn bị trước buổi demo

### Môi trường

- Mở sẵn frontend và kiểm tra API hoạt động.
- Nếu chạy local: frontend `http://localhost:5173`, API `http://localhost:5000`.
- Kiểm tra `/health` trả về trạng thái healthy.
- Chạy migration và `seed_demo.sql` trước buổi demo.
- Chuẩn bị một file `.stl` nhỏ, hợp lệ, tên dễ đọc; không dùng file quá lớn.
- Mở sẵn 5 cửa sổ trình duyệt hoặc 5 profile/ẩn danh để không phải đăng xuất nhiều lần.

### Tài khoản demo

Tất cả dùng mật khẩu `Demo@123`.

| Vai trò | Tài khoản | Màn hình chính |
|---|---|---|
| Khách hàng | `demo@printgrid.dev` | Model, tạo đơn, theo dõi đơn |
| Quản lý xưởng | `lab@printgrid.dev` | Hàng đợi, máy, tồn kho, lô giao |
| Hub QC | `qc@printgrid.dev` | Kiểm định và hoàn tất đơn |
| Điều phối vận hành | `ops.manager@printgrid.dev` | Scheduling, review, escalation, SLA |
| Quản trị hệ thống | `admin@printgrid.dev` | Dashboard, cấu hình, user, audit |

### Dữ liệu cần kiểm tra

- Customer có model hoặc có thể upload model mới.
- Scheduling có job đã gán máy.
- Lab có ít nhất một job `Assigned` hoặc `InProgress`.
- Hub có đơn/item chờ QC.
- Admin có dữ liệu dashboard, cấu hình và audit log.

Nếu dữ liệu xuyên suốt không đủ để chuyển trạng thái trực tiếp trong buổi demo, dùng dữ liệu seed ở từng vai trò và nói rõ: “Ở đây em chuyển sang bản ghi đã được chuẩn bị ở trạng thái kế tiếp để tiết kiệm thời gian chờ sản xuất thực tế.”

## 2. Thông điệp mở đầu — 45 giây

### Thao tác

Mở trang chủ, cuộn chậm qua hero, phần B2C/B2B, vật liệu và mạng lưới.

### Lời nói

“PrintGrid là nền tảng điều phối in 3D phân tán. Thay vì khách hàng phải tự tìm từng xưởng, hỏi giá và đoán ngày giao, PrintGrid gom nhiều xưởng độc lập thành một nhà máy ảo: một trải nghiệm đặt hàng, một cách tính giá, một cam kết giao hàng và một tiêu chuẩn chất lượng tại hub.

Trong phần demo này, em sẽ đi theo một đơn hàng từ lúc khách chọn model, hệ thống xác định cấu hình và báo giá, đến khi job được gán cho máy phù hợp, xưởng sản xuất, hub kiểm định và quản trị viên theo dõi toàn bộ quyết định.”

**Câu chốt:** “Điểm khác biệt không chỉ là nhận file để in, mà là điều phối được năng lực thật của cả mạng lưới.”

## 3. Khách hàng tạo yêu cầu in — 3 phút

### Thao tác

1. Đăng nhập bằng `demo@printgrid.dev`.
2. Mở **Thư viện model**.
3. Chọn một model có sẵn; nếu dữ liệu phù hợp, trình diễn upload file `.stl`.
4. Mở preview 3D để xoay và quan sát model.
5. Chọn **Đặt in/Cấu hình đơn**.
6. Chọn công nghệ, vật liệu, màu, độ dày lớp, dung sai, số lượng và ngày mong muốn.
7. Nếu màn hình có bộ lọc/chọn máy, chỉ ra rằng danh sách máy đã được lọc theo kích thước build volume, công nghệ, vật liệu và ràng buộc kỹ thuật.
8. Tạo/xem báo giá và chỉ vào giá, thời gian in dự kiến, ngày giao dự kiến.

### Lời nói

“Khách hàng có thể dùng model đã lưu hoặc tải file thiết kế mới lên. File không chỉ được lưu trữ; hệ thống phân tích hình học để phục vụ kiểm tra khả năng in và preview 3D.

Ở bước cấu hình, khách chọn kết quả mong muốn như vật liệu, màu, độ chính xác và số lượng. PrintGrid chuyển các lựa chọn này thành ràng buộc kỹ thuật. Một máy chỉ được xem là ứng viên nếu chứa được kích thước model, hỗ trợ đúng công nghệ, đúng vật liệu, layer height và tolerance yêu cầu.

Sau đó CuraEngine slice model bằng profile máy và vật liệu đã được lab xác thực. Kết quả như thời gian máy, lượng vật liệu và các hệ số thương mại được dùng để lập báo giá. Ngày giao không phải một con số nhập tay; scheduler dựa trên năng lực và timeline máy để tìm phương án khả thi.”

**Câu chốt:** “Giá và ngày giao đều có căn cứ kỹ thuật, thay vì phụ thuộc vào việc một xưởng trả lời tin nhắn nhanh hay chậm.”

### Nếu bước slice/báo giá chưa sẵn sàng

Mở một báo giá hoặc đơn đã seed, rồi nói: “Để không phụ thuộc thời gian slice trong buổi demo, em dùng báo giá đã tính sẵn; dữ liệu này vẫn lưu đầy đủ snapshot cấu hình để có thể tái lập quyết định.”

## 4. Điều phối vận hành và scheduler — 2 phút 30 giây

### Thao tác

1. Chuyển sang cửa sổ `ops.manager@printgrid.dev`.
2. Mở **Scheduling Board**.
3. Chỉ vào job, lab, máy, thời gian bắt đầu/kết thúc và deadline.
4. Mở **Decision Trace** hoặc chi tiết quyết định nếu có dữ liệu.
5. Lướt qua **Escalation Queue** để cho thấy luồng xử lý ngoại lệ.

### Lời nói

“Sau khi đơn được xác nhận, scheduler biến từng item thành job và xếp lên timeline máy. Trước khi chấm điểm, hệ thống loại các ứng viên vi phạm ràng buộc cứng. Trong các máy còn lại, engine cân bằng nhiều mục tiêu như khả năng giao đúng hạn, chất lượng, chi phí và rủi ro vận chuyển.

Điểm quan trọng là mỗi quyết định đều có dấu vết: tập ứng viên, máy được chọn, điểm số và phiên bản cấu hình. Vì vậy khi có câu hỏi ‘tại sao job này được giao cho xưởng này’, hệ thống trả lời bằng dữ liệu thay vì phỏng đoán.

Nếu xưởng từ chối, máy hỏng hoặc có lỗi QC, sự kiện sẽ đưa công việc vào luồng sửa lịch/escalation thay vì buộc nhân viên điều phối lại hoàn toàn bằng tay.”

**Câu chốt:** “Thuật toán tự động hóa quyết định, nhưng quyết định vẫn giải thích và kiểm toán được.”

## 5. Xưởng nhận và sản xuất — 2 phút

### Thao tác

1. Chuyển sang `lab@printgrid.dev`.
2. Mở **Hàng đợi công việc**.
3. Chỉ một job `Assigned`, sau đó mở chi tiết nếu có.
4. Trình diễn chuyển sang bắt đầu/in xong nếu dữ liệu và nút thao tác cho phép.
5. Mở **Máy in** và chỉ trạng thái máy, vật liệu hỗ trợ, lịch bảo trì.
6. Lướt qua tồn kho hoặc kế hoạch gom lô giao về hub.

### Lời nói

“Xưởng chỉ thấy những công việc được giao cho mình cùng thông tin đủ để sản xuất. Quản lý xưởng có thể theo dõi hàng đợi, trạng thái máy, tồn kho và lịch bảo trì.

Ở đây có hai máy với năng lực khác nhau; một máy còn có khoảng bảo trì. Scheduler phải tôn trọng cả năng lực vật lý lẫn trạng thái vận hành này. Khi operator bắt đầu hoặc hoàn tất job, trạng thái được cập nhật để toàn mạng lưới có timeline mới nhất.

Sau khi in xong, các item được gom thành lô chuyển về hub. Cách này giúp PrintGrid giữ một chuẩn kiểm định chung dù sản phẩm được in ở nhiều xưởng khác nhau.”

**Câu chốt:** “Xưởng tập trung vào sản xuất; PrintGrid chịu trách nhiệm điều phối và cam kết cấp mạng lưới.”

## 6. Hub kiểm định và hoàn tất đơn — 2 phút

### Thao tác

1. Chuyển sang `qc@printgrid.dev`.
2. Mở **Hub QC**.
3. Chọn item chờ kiểm định.
4. Chỉ checklist, grade, bằng chứng QC hoặc phân loại lỗi.
5. Nếu an toàn với dữ liệu demo, cho item pass; nếu không, chỉ giải thích hai nhánh pass/fail.
6. Mở **Shipments/Complete Orders** và chỉ điều kiện chỉ đóng gói khi mọi item đạt QC.

### Lời nói

“Mọi sản phẩm đi qua hub để được kiểm theo cùng một checklist. Nhân viên QC ghi nhận kết quả, grade và bằng chứng. Nếu đạt, item được đưa sang hoàn tất đơn. Nếu không đạt, lỗi được phân loại để xác định trách nhiệm và kích hoạt reprint hoặc xử lý ngoại lệ.

Một đơn có thể được chia cho nhiều xưởng, nhưng chỉ được đóng gói khi tất cả item đã qua QC. Khách hàng vì thế nhận một trải nghiệm và một chuẩn chất lượng thống nhất, không cần biết từng chi tiết được in ở đâu.”

**Câu chốt:** “Hub là điểm biến nhiều xưởng độc lập thành một thương hiệu có chất lượng nhất quán.”

## 7. Quản trị và khả năng kiểm soát — 1 phút 30 giây

### Thao tác

1. Chuyển sang `admin@printgrid.dev`.
2. Mở **Admin Dashboard**.
3. Lướt qua **Commercial Configurations**, **Scoring Configurations** hoặc **System Parameters**.
4. Mở **Cura Profiles** để chỉ thư viện máy/profile.
5. Kết bằng **Audit Logs**.

### Lời nói

“Cuối cùng là lớp quản trị. Admin theo dõi tình trạng nền tảng, quản lý người dùng, phê duyệt lab, cấu hình tham số thương mại, trọng số scheduler và thư viện Cura.

Các cấu hình quan trọng có phiên bản. Báo giá cũ giữ snapshot cấu hình đã dùng, còn thay đổi quản trị được ghi audit log. Điều này giúp hệ thống vừa điều chỉnh được theo vận hành thực tế, vừa không làm mất khả năng giải thích các quyết định trước đây.”

**Câu chốt:** “PrintGrid không chỉ tự động hóa luồng chính mà còn kiểm soát được ai đã thay đổi điều gì và quyết định nào chịu tác động.”

## 8. Kết thúc — 30 giây

### Lời nói

“Qua một hành trình ngắn, PrintGrid đã nối năm vai trò: khách hàng, điều phối, xưởng in, hub QC và quản trị hệ thống. Giá được tính từ dữ liệu slice; ngày giao dựa trên năng lực máy; việc gán job có dấu vết quyết định; chất lượng được chuẩn hóa tại hub; và toàn bộ cấu hình có phiên bản cùng audit.

Giá trị cốt lõi của PrintGrid là biến công suất phân tán thành một dịch vụ in 3D có thể dự đoán, giải thích và mở rộng. Em xin kết thúc phần demo và sẵn sàng nhận câu hỏi.”

## 9. Phương án demo rút gọn 5 phút

Nếu thời gian bị cắt, chỉ trình diễn bốn điểm:

1. **Trang chủ (30 giây):** vấn đề và “nhà máy ảo”.
2. **Customer (2 phút):** model → cấu hình → báo giá/ngày giao.
3. **Ops (1 phút 30 giây):** scheduling board → decision trace.
4. **Hub + Admin (1 phút):** QC thống nhất → cấu hình có version/audit.

Không trình diễn đăng ký, quên mật khẩu, CRUD user hoặc mọi menu. Đây là tính năng hỗ trợ, không phải câu chuyện giá trị chính.

## 10. Câu hỏi thường gặp

### Vì sao cần hub, sao không để xưởng giao thẳng?

Hub tạo một tiêu chuẩn QC chung, hỗ trợ gom nhiều item từ nhiều xưởng vào một đơn và giữ trải nghiệm thương hiệu thống nhất.

### Nếu máy hỏng hoặc xưởng từ chối thì sao?

Sự kiện vận hành kích hoạt sửa lịch cho phần công việc chưa chạy. Nếu không thể xử lý tự động trong giới hạn, hệ thống tạo escalation để Ops can thiệp có lý do và audit.

### Vì sao ngày giao đáng tin hơn cách báo giá truyền thống?

Ngày giao được tính từ timeline máy, thời gian sản xuất và vận chuyển thay vì dùng một khoảng thời gian cố định nhập tay.

### Làm sao biết scheduler chọn đúng máy?

Hệ thống lọc ràng buộc cứng trước, sau đó chấm điểm các ứng viên hợp lệ. Decision trace lưu ứng viên, điểm số, máy được chọn và phiên bản cấu hình.

### Thay đổi giá có làm sai báo giá cũ không?

Không. Báo giá lưu snapshot/phiên bản cấu hình đã dùng; cấu hình mới áp dụng cho quyết định mới và thay đổi được ghi audit.

### Dữ liệu thiết kế của khách được bảo vệ thế nào?

Các portal dùng phân quyền theo vai trò; xưởng chỉ truy cập dữ liệu cần cho job được giao. Hệ thống dùng JWT, URL file có thời hạn và ghi nhận các thao tác quan trọng.

## 11. Checklist chống sự cố khi thuyết trình

- Không upload file lần đầu ngay trước hội đồng; luôn có sẵn một model dự phòng.
- Không thay đổi cấu hình production trong demo; chỉ mở và giải thích.
- Không bấm fail QC hoặc refund trên dữ liệu duy nhất nếu chưa có bản ghi dự phòng.
- Giữ sẵn tab `/health` và Swagger để chứng minh backend đang hoạt động khi UI gặp lỗi mạng.
- Nếu một API chậm, chuyển sang dữ liệu seed ở bước kế tiếp và tiếp tục câu chuyện.
- Tắt thông báo hệ điều hành, trình quản lý mật khẩu và dữ liệu nhạy cảm trên màn hình.
- Zoom trình duyệt 90–100%, kiểm tra độ phân giải máy chiếu.
- Diễn tập một lần với đồng hồ; mục tiêu kết thúc ở phút 12 để còn thời gian cho câu hỏi.
