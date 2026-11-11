Gói ngôn ngữ tiếng Việt cho AION2 Global (Steam / PURPLE), đồng bộ với bản game ngày **11/11**, gồm 153.008 mục văn bản.

Bổ sung bản dịch cho 89 mục mới, rà soát 65 mục thay đổi và đồng bộ 1.073 mục có nguồn trùng khớp. Nội dung mới chưa dịch giữ tiếng Anh.

## Cách sử dụng

1. Đóng game.
2. Tải và chạy `AION2_VIE.exe` bên dưới.
3. Kiểm tra đường dẫn game rồi bấm “Cài Đặt”, sau đó mở game.

Công cụ tự động kiểm tra phiên bản mới khi khởi động. Nếu có bản mới, bạn có thể cập nhật ngay trong công cụ chỉ bằng một lần bấm (sau khi tải sẽ xác minh bằng SHA-256 đi kèm Release, rồi thay thế và khởi động lại).

Muốn trở lại tiếng Anh, bấm “Khôi Phục Tiếng Anh”. Sau khi game cập nhật, cần cài lại một lần.

## Lưu ý

- **SmartScreen chặn (exe chưa ký số sẽ gặp)**: khi thấy “Microsoft Defender SmartScreen đã ngăn một ứng dụng chưa được nhận diện”, bấm “**More info**” → “**Run anyway**”.
  Nếu không muốn thông báo xuất hiện lại, có thể bỏ chặn trước (chọn một cách):
  - Bấm chuột phải vào `AION2_VIE.exe` → “Properties” → “General” → tích “**Unblock**” ở cuối → “OK”.
  - Hoặc mở PowerShell và nhập: `Unblock-File .\AION2_VIE.exe`
  - Hoặc tải bằng dòng lệnh (`Invoke-WebRequest` không thêm dấu đến từ Internet):
    `powershell -c "iwr https://github.com/yuezhang128/Steam-AION2-Vietnamese/releases/latest/download/AION2_VIE.exe -OutFile AION2_VIE.exe; .\AION2_VIE.exe"`
- Một số antivirus cũng có thể nhận diện nhầm (xem nguyên nhân tại [Nhận diện nhầm của antivirus](https://github.com/yuezhang128/Steam-AION2-Vietnamese#nhận-diện-nhầm-của-antivirus)).
- Có thể đối chiếu file bằng `AION2_VIE.exe.sha256` bên dưới: `certutil -hashfile AION2_VIE.exe SHA256`.
- Khi tích chọn “Cài kèm font tiếng Việt”, lần đầu sẽ tải font khoảng 334 KB.
- Đây là gói ngôn ngữ không chính thức do người chơi tạo. Bạn tự chịu trách nhiệm khi sử dụng.

Xem hướng dẫn chi tiết trong [README](https://github.com/yuezhang128/Steam-AION2-Vietnamese#readme).
