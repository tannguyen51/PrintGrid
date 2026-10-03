# PrintGrid Tổng quan — Kịch bản thuyết trình (Tiếng Việt)

*Đi kèm `PrintGrid-Overview-Slides.html` (11 slide, phím mũi tên). Tổng ≈ 8–9 phút nói đều.
Chỉ dẫn sân khấu trong [ngoặc vuông]; **in đậm** = câu nhấn mạnh khi nói.*
*Bản đối ứng của `PrintGrid-Overview-Script.md` — slide vẫn là tiếng Anh, lời nói tiếng Việt.*

---

## Slide 1 · Trang chủ — 20s

> Xin chào hội đồng. Em là Tân, nhóm FA26SE249. Đây là **PrintGrid** — nền tảng điều phối và lập lịch in 3D phân tán cho một mạng lưới các xưởng in độc lập, dưới sự hướng dẫn của thầy Nguyễn Tấn Phúc.
> Trong mười phút tới: bối cảnh dự án, vấn đề cần giải quyết, và toàn bộ tập yêu cầu chức năng — phi chức năng trả lời cho từng vấn đề đó.

## Slide 2 · Bối cảnh — 60s

> Bắt đầu từ phía cung. Xung quanh chúng em — gồm cả maker lab của trường và **một xưởng đối tác đã xác nhận** — có rất nhiều xưởng in FFF, SLA nhỏ. Mỗi nơi sở hữu máy thật với giờ chết thật, nhưng điều phối công việc qua… tin nhắn/chat nhóm.
> Phía cầu là sinh viên, nhà nghiên cứu, hộ kinh doanh: những người muốn in một chi tiết **với ngày giao mà họ tin được**, và mức giá không đổi tùy việc xưởng nào trả lời tin nhắn.
> Luận đề của chúng em: coi toàn bộ mạng lưới là **một nhà máy ảo** — giá thống nhất, cam kết giao hàng dựa trên năng lực thật, và một chuẩn chất lượng duy nhất kiểm tại hub trung tâm.
> [chỉ vào sơ đồ] Đây là biên hệ thống: PrintGrid là khối đen ở giữa. Chín thực thể bên ngoài trao đổi thông tin với nó — khách hàng, quản lý xưởng, thợ in, nhân viên hub, quản trị vận hành, quản trị hệ thống, cùng hai hệ thống ngoài là cổng thanh toán và dịch vụ thông báo. Mỗi mũi tên qua biên mang một thông tin nghiệp vụ có tên, và tụi em đã ghi đủ hai mươi bốn luồng đó **trước khi** vẽ bất kỳ màn hình nào.

## Slide 3 · Vấn đề — 75s

> Vậy cái gì đang hỏng? Năm thất bại gọi tên được.
> **Một:** máy nằm không cách đơn hàng bị từ chối chỉ hai tòa nhà — năng lực phân mảnh và vô hình.
> **Hai:** ngày giao hàng là đoán mò. Báo giá hứa "ba đến năm ngày" từ một bảng tĩnh — không ai nhìn lịch máy thật. Một lời hứa không tính ra được là một lời hứa sẽ vỡ.
> **Ba:** chất lượng trôi dần mà không quy trách nhiệm được cho ai. Chi tiết lỗi thì không có chuẩn kiểm, không có phân định lỗi tại xưởng, tại hub hay tại file của khách — thành chuyện hai bên cãi nhau.
> **Bốn:** mọi cố định — xưởng từ chối, in hỏng ở 90% — đều đàm phán lại bằng tay, người với người. Cái giá là hàng giờ chat.
> **Và năm:** giá phụ thuộc vào việc khách hỏi xưởng nào. Điều đó giết niềm tin vào thương hiệu chung.
> Định nghĩa thành công của chúng em gói trong một câu: **một đơn tiền thật đi từ upload đến delivered không một thao tác điều phối thủ công nào, và sự cố giữa luồng được sửa trong dưới ba mươi giây.**

## Slide 4 · Bốn trụ cột — 60s

> Bốn cơ chế trả lời năm vấn đề vừa nêu.
> **Giá thống nhất toàn mạng:** một công thức duy nhất trên bộ tham số có phiên bản. Và phiên bản bị **đóng băng** lên báo giá — hôm nay đổi giá, báo giá hôm qua vẫn tái lập chính xác từng đồng. Đó là cam kết kiểm chứng được, không phải quy định trên giấy.
> **[chỉ] Đây là đóng góp cốt lõi của đề tài: lập lịch suy đoán ngay tại thời điểm báo giá.** Khi khách xin báo giá, engine chạy một lượt *đặt lịch thử* trên timeline thật của mọi máy, và trả về ngày khả thi sớm nhất — kèm phụ phí nếu khách cần gấp. Lời hứa giao hàng được **tính ra**, không phải tra bảng.
> **Chất lượng qua hub:** mọi chi tiết đi ngang một hub trung tâm — đối soát lô, checklist gắn với grade, phân loại lỗi kèm quy trách nhiệm, và in lại thừa hưởng deadline gốc.
> **Tái lập lịch theo sự kiện:** từ chối, in hỏng, chết máy, lỗi QC — mỗi sự cố kích hoạt sửa phần chưa chạy của lịch trong ngân sách ba mươi giây cứng, luôn có kế hoạch dự phòng được cam kết.
> Một điểm đáng chú ý: khách không bao giờ biết xưởng nào in chi tiết của mình, và các xưởng không thấy giá của nhau. Sự đồng nhất được **thiết kế trong kiến trúc**, không phải trong nội quy.

## Slide 5 · Bản đồ FR — 40s

> Giờ là kiểm kê yêu cầu. Bốn mươi chín yêu cầu chức năng trong sáu phân hệ: mười ba phía khách hàng, tám phía xưởng, sáu phía hub, mười phía scheduling engine — nhóm có dấu sao — bảy phân tích vận hành, và năm quản trị nền tảng. Sáu trong số đó là kết quả Review 1 của thầy cách đây hai tuần — đúng tinh thần phản biện để hồ sơ lớn dần có kiểm soát.
> Hai điều chúng em kiên quyết: thứ nhất, mọi yêu cầu đều **truy vết** được về một dòng trong phiếu đề tài đã duyệt — bốn mươi chín trên bốn mươi chín trong ma trận truy vết. Thứ hai, mọi yêu cầu đều **ưu tiên theo MoSCoW**, trong đó Must đúng bằng luồng vàng cộng các nhánh lỗi. Preview 3D và thư viện model là Could — cắt được, và tụi em nói trước điều đó.

## Slides 6–8 · Toàn bộ danh sách FR — 90s

> [đừng đọc bảng — ba slide này tồn tại để chứng minh sự đầy đủ; nói đè lên:]
> Đây là cả bốn mươi chín hạng mục. Thay vì đọc hết, em xin chỉ vào những chỗ danh sách **không chịu nói chung chung**.
> **Slide 6** — hành trình khách hàng kết thúc bằng **tiền thật**: mã QR SEPay, và đơn chỉ chốt khi webhook đã xác minh *và* API xác nhận của cổng thanh toán cùng khớp. Redirect trình duyệt không phải bằng chứng — giả định bảo mật đó được thiết kế từ ngày đầu.
> **Slide 7** — hàng có dấu sao là lập lịch suy đoán một lần nữa, và bên cạnh nó là các hàng rào: lọc khả thi cứng trước mọi gán việc, nhật ký quyết định tái dựng được từng lựa chọn, và hub chặn đóng gói cho tới khi mọi item qua kiểm.
> **Slide 8** — vận hành có quyền can thiệp kèm lý do và audit, hoàn tiền kèm đối soát hằng ngày; còn quản trị có một năng lực mang tính thương mại: đổi mọi hệ số giá, trọng số, giới hạn **nóng, trong một phút, không cần khởi động lại** — trong khi báo giá cũ vẫn giữ phiên bản đóng băng của nó.

## Slide 9 · NFR nửa đầu — 50s

> Yêu cầu chức năng nói *làm cái gì*; yêu cầu phi chức năng quyết định *người ta có tin hay không*. Mười bảy cam kết cốt lõi, mỗi cái mang một con số và một phép đo — đây là tám cái đầu.
> Báo giá: ước lượng sơ bộ dưới năm giây, báo giá chốt dưới sáu mươi giây p95 — đo bằng k6 trên staging.
> Tái lập lịch: ba mươi giây cứng, mười lăm giây trung bình; và nếu thuật toán không kịp cải thiện, một **lịch dự phòng vẫn được cam kết** — hệ thống không bao giờ bỏ trống kế hoạch.
> Khả năng mở rộng: năm mươi xưởng, ba trăm máy, một nghìn đơn mỗi tháng mà không đổi kiến trúc.
> Và ba tính chất dung sai bằng không: không mất đơn đã chốt, không đặt trùng lịch máy — bất khả thi *theo cấu trúc*, buộc bằng ràng buộc ở tầng database — và không gán việc cho máy không đủ khả năng vật lý in chi tiết đó.
> Thêm quy tắc niềm tin: ngày giao đã chốt chỉ đổi theo **ba điều kiện được duyệt**, mỗi lần đổi đều báo và xin ý kiến khách hàng.

## Slide 10 · NFR nửa sau — 50s

> Nửa còn lại. Bảo mật: file model là tài sản trí tuệ — chỉ truy cập được trong thời hạn job được gán, mọi lượt truy cập đều ghi log, không tải hàng loạt. Với thanh toán: không có dữ liệu thẻ nào chạm vào database hay log của chúng em.
> Dễ dùng cho cả hai phía: khách lần đầu hoàn thành một đơn trong dưới mười phút **không cần ai hướng dẫn** — test có điều phối viên, năm trên năm người đạt; còn ở xưởng, app thao tác cạnh máy in: ba lần chạm tới mọi cập nhật trạng thái, chịu được mất mạng hai phút — vì nó được dùng *ngay bên chiếc máy đang chạy*, không phải bên bàn làm việc.
> Kiểm toán được: năm mươi quyết định lấy mẫu bất kỳ phải tái dựng đầy đủ với tập ứng viên, điểm số và phiên bản cấu hình.
> Và các cam kết nghiên cứu — vì đây là capstone, không chỉ là product: mọi tuyên bố về scheduler được **đo với ít nhất ba baseline trên seed cố định, chạy lại cho kết quả giống hệt**; độ chính xác ước lượng in được theo dõi liên tục, MAPE mục tiêu dưới hai mươi lăm phần trăm và giảm dần sau mỗi chu kỳ hiệu chuẩn bằng dữ liệu in thật của xưởng đối tác.

## Slide 11 · Kết — 40s

> Chốt lại vòng lặp: một trăm linh tư phát biểu được trích xuất, một trăm linh ba quy tắc nghiệp vụ, bốn mươi chín yêu cầu chức năng, mười bảy yêu cầu phi chức năng đo được, ba mươi ba use case, bốn mươi bốn user story — một ma trận nối toàn bộ, và mọi luồng thông tin trên sơ đồ ngữ cảnh đều có ít nhất một use case xử lý.
> Ba từ cho những gì chúng em bảo vệ: **truy vết được, kiểm chứng bác bỏ được, và đang được xây** — hợp đồng API và event đã khóa, stack chung đã chạy, sprint luồng vàng đang giữa đường.
> Em cảm ơn — sẵn sàng nhận câu hỏi.

---

## Câu hỏi dự phòng & trả lời ngắn

| Hội đồng hỏi | Trả lời |
|---|---|
| Vì sao có hub thay vì mỗi xưởng tự ship? | Chuẩn QC đồng nhất, gộp đơn (một đơn khách có thể chia nhiều xưởng), và giấu danh tính xưởng — hub mới là thứ biến "một nhà máy ảo" thành thật thay vì thành cái chợ. |
| Đặt lịch thử lúc báo giá — không đắt lắm à? | Đắt nhưng có trần thiết kế: chỉ thử, không chốt; ≤60s p95 là NFR có bài đo; không tìm được lịch khả thi thì **từ chối báo giá** — failure mode trung thực. |
| Webhook đến hai lần hoặc không đến thì sao? | Chốt đơn cần webhook khớp chữ ký **và** API xác nhận gọi lại; idempotent theo mã giao dịch; URL return không bao giờ là bằng chứng; đối soát T-1 hằng ngày bắt trường hợp thiếu/trùng. |
| Ngân sách sửa lịch 30s đo bằng gì? | Trên harness giả lập (UC-027) cấu hình 50 xưởng/300 máy với fault injection, cộng test race cho ràng buộc không đặt trùng. |
| Vì sao giấu xưởng với khách? | Cam kết thương hiệu là giá và chất lượng đồng nhất; lộ xưởng là đưa trở lại mặc cả theo xưởng và đổ lỗi chéo — chính là vấn đề P3/P5. |
| Trễ tiến độ thì cắt gì trước? | Nhóm Could: preview 3D, thư viện model, Gantt — đã đánh dấu Should/Could công khai trong SRS, nên phạm vi trung trình từ trước khi ai hỏi. |
