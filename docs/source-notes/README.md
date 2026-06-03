# Source Notes

Hai file nguồn chính hiện nằm ở thư mục cha:

- `PLAN_IMAGE_PDF_OPTIMIZER_APP.md`: product plan gốc.
- `FE_UI_PLAN_IMAGE_PDF_OPTIMIZER_APP.md`: UI/FE plan do FE đề xuất.

Bộ file `00-05` là bản đã gom, tách và chuẩn hoá cho **Feature 01: Image -> AVIF -> PDF Optimizer**.

Scope MVP đã chốt:

- Chỉ làm AVIF, không làm WebP trong MVP.
- Không làm target size/auto optimize trong MVP; nếu cần sẽ mở ở V2.
- FFmpeg phải được bundle sẵn khi đóng gói app cho user, không yêu cầu user cấu hình path.
- Bộ `C:\Users\INEC-EDITOR-2\Desktop\Chi Trieu\Sao ke GD` là golden test set bắt buộc.

File `00_APP_SUITE_MASTER_PLAN.md` là master plan cấp app suite, dùng để tránh hiểu nhầm Feature 01 là toàn bộ core app.

File `00_SUITE_UI_UX_SHELL_PLAN.md` là UI/UX plan cấp app shell cho nhiều module.

File `06_WINUI_UI_DIRECTION.md` là định hướng bổ sung nếu chọn WinUI 3 / Windows App SDK thay cho WinForms MVP tối giản.

File `07_CORE_TECHNICAL_DIRECTION.md` là định hướng core kỹ thuật bổ sung cho app suite nhiều module.

File `08_WINUI_IMPLEMENTATION_HANDOFF.md` là checklist handoff bắt buộc trước khi giao agent khác code UI WinUI để tránh vỏ và ruột lệch nhau.

Khi có thay đổi mới từ Product hoặc FE, cập nhật file nguồn tương ứng trước, sau đó điều chỉnh lại các file:

- `00_MASTER_PLAN.md`
- `00_APP_SUITE_MASTER_PLAN.md`
- `00_SUITE_UI_UX_SHELL_PLAN.md`
- `01_PRODUCT_REQUIREMENTS.md`
- `02_UI_UX_IMPLEMENTATION_PLAN.md`
- `03_TECHNICAL_ARCHITECTURE.md`
- `04_IMPLEMENTATION_PHASES.md`
- `05_MVP_ACCEPTANCE_CHECKLIST.md`
- `06_WINUI_UI_DIRECTION.md`
- `07_CORE_TECHNICAL_DIRECTION.md`
- `08_WINUI_IMPLEMENTATION_HANDOFF.md`
