# Đặt hàng, điều phối số lượng và thống kê đơn

Các quy tắc triển khai từ góp ý mentor ngày 19/11/2026.

## BR-ORDER-ADDRESS-001 — Thông tin đặt hàng

Đơn mới bắt buộc số nhà/đường, phường/xã, quận/huyện, tỉnh/thành, ngày giao mong muốn và đồng ý điều khoản. Mã bưu điện tùy chọn. Ngày mong muốn phải sau ngày hiện tại.

`RequestedDeliveryDate` là ngày khách đề nghị; `PromisedDeliveryDate` là ngày hệ thống đã kiểm tra và nhận cam kết. Khi đặt mới, hai ngày có thể bằng nhau. Luồng duyệt đổi ngày chỉ thay ngày cam kết, giữ nguyên mong muốn ban đầu. Đơn cũ giữ ngày mong muốn null; không suy diễn dữ liệu khách chưa khai báo.

Báo giá cho biết mốc sẵn sàng tại hub. Trước đặt đơn, hệ thống kiểm tra lại lịch máy, profile đã duyệt, vật liệu và vận chuyển lab → hub, cộng thời gian giao cuối `Delivery:LastMileDays` (mặc định 2 ngày, tối thiểu 1, tối đa 30). Đây là khoảng đệm vận hành chung, chưa phải tích hợp hãng vận chuyển/tính tuyến theo địa chỉ. Ngày yêu cầu sớm hơn khả năng hiện tại bị từ chối và trả ngày sớm nhất. Kiểm tra này không giữ chỗ máy; tại thời điểm thanh toán/phân công vẫn cần kiểm tra lại năng lực.

## BR-ORDER-ROUTE-001 — Phân luồng theo tổng số lượng

Tổng `Quantity` của tất cả dòng trong đơn **< 5**: `DispatchMode = Automatic`. Tổng **>= 5**: `Manual`. Mốc ban đầu được khai báo tại `OrderRoutingPolicy`, độc lập với ngưỡng tiền `AutoLaneThreshold`.

Báo giá chỉ tự duyệt nếu đồng thời dưới ngưỡng tiền và dưới ngưỡng số lượng. Báo giá lớn phải qua Ops. Duyệt báo giá không chứng minh đã thanh toán và không đồng nghĩa đã phân công sản xuất.

Đặt đơn luôn tạo `PaymentPending`, không tạo thanh toán giả kể cả khi còn cấu hình demo `Payment:AutoConfirmOnPlace`. Sau khi ghi nhận đủ cọc, `OrderConfirmedEvent` tạo các job. Chỉ đơn nhỏ được đưa vào assignment engine; engine kiểm tra máy/profile, tải, vật liệu và hạn trước khi gán. Nếu không gán được thì giữ job cho Ops. Đơn lớn chờ Ops phân công. Đơn cũ được migration giữ ở luồng Manual để không thay đổi cách xử lý hồi tố.

## BR-ORDER-SPLIT-001 — Xem trước và xác nhận chia lab

Ops chọn “Xem phương án chia xưởng” ở `/scheduling`. Chỉ job Pending/Reassigned, số lượng >= 2, đơn còn hiệu lực và đủ cọc mới được chia. Phải có ít nhất hai lab đủ năng lực. Nếu có ước tính slicer đã chốt, máy nhận phải cùng model và chia sẻ đúng phiên bản profile vật liệu đã duyệt; không tự thay bằng máy có thông số không tương đương.

Preview không ghi job; trả số lượng, lab, máy, giờ bắt đầu/kết thúc và token được bảo vệ phía server, hết hạn sau 5 phút. Các lô bắt đầu ít nhất 5 phút sau lúc tạo preview để Ops có thời gian kiểm tra.

Xác nhận kiểm tra lại đơn, trạng thái job, số lượng, hạn sản xuất, lịch máy, năng lực và vật liệu. Từng lab phải đủ thời gian vận chuyển về hub và giao khách. Tổng số lượng lô phải bằng job gốc; mỗi lô số lượng dương, mỗi lab một lô trong phương án. `Job.Split` hủy job gốc, tạo job con có `ParentJobId`, lưu quyết định và phân công trong cùng giao dịch Serializable. Ràng buộc chống trùng lịch máy là lớp bảo vệ cuối. Phương án lỗi thời/hết hạn/gửi lặp bị từ chối, không tạo thêm lô. Không sửa giá đơn hàng.

Một dòng hàng chỉ hoàn tất khi mọi lô đang có hiệu lực đạt nghiệm thu; job gốc đã hủy vì chia lô không chặn hoàn tất. Job thất bại được thay bằng in lại không khiến đơn bị đánh dấu thất bại vĩnh viễn. Khách xem tiến độ từng phần trong chi tiết đơn; không lộ tên xưởng hoặc thông tin máy.

## BR-ORDER-STATS-001 — Thống kê theo đơn

Trang `/ops/orders`, API `GET /api/v1/ops/orders`. Khoảng thời gian lọc **ngày tạo đơn**, theo giờ Việt Nam, bao gồm toàn bộ ngày kết thúc. Không phải thống kê số sự kiện giao hàng/hoàn tiền phát sinh trong kỳ.

- Tổng đơn: mỗi OrderId một lần, không nhân theo số job/lab/lần in lại.
- Thành công: trạng thái Delivered. Hủy: Cancelled.
- Có lịch sử sự cố: đơn bị trễ hoặc có job Failed, in lại, nhiều lần thử, bằng chứng QC bị từ chối.
- Sự cố chưa xử lý: đơn đang mở có job Failed chưa được thay thế bởi lần in lại còn hiệu lực. Không gọi đây là trạng thái Failed của Order vì mô hình Order không có trạng thái đó.
- Hoàn một phần: `0 < RefundedAmount < PaidAmount`.
- Hoàn toàn bộ tiền đã thu: `PaidAmount > 0 && RefundedAmount >= PaidAmount`; không nhầm tiền cọc với toàn bộ giá trị hợp đồng.

Các nhóm có thể giao nhau; đơn giao thành công sau in lại vẫn có lịch sử sự cố. Đơn có nhiều lô và một phần hoàn tiền vẫn tính một đơn có hoàn tiền. Số tiền dùng các trường thanh toán/hoàn tiền hiện có, không suy diễn từ giá báo hoặc yêu cầu hoàn tiền chưa thực hiện.

## Kiểm chứng

`dotnet test tests/PrintGrid.OrderWorkflowTests`: bộ kiểm thử độc lập cho đặt hàng, cọc, mốc 4/5 sản phẩm, tổng nhiều dòng, ngày giao, xác nhận chia hai lab, phương án cũ/hết hạn/gửi lặp, nghiệm thu đủ lô và báo cáo theo đơn.

`dotnet build src/PrintGrid.Api` và `npm --prefix frontend run build` kiểm tra backend/frontend. Migration `AddOrderDispatchAndRequestedDelivery` bổ sung hai trường vào `customer.orders`; `FreezeJobDeliveryBuffers` lưu khoảng đệm giao hàng trên job để in lại, chia lô và đổi ngày dùng đúng thời gian đã chốt. Job cũ giữ khoảng đệm 2 ngày và chặng cuối 0 ngày theo luồng cũ. Host hiện tự áp migration khi khởi động. Giao dịch đồng thời và migration cần được kiểm thử thêm trên PostgreSQL thật khi Docker khả dụng.
