# FluMix — Tài liệu JSON API cơ bản

## Tổng quan

| Mục đích              | Method | Endpoint                    |
| --------------------- | ------ | --------------------------- |
| Đăng nhập             | `POST` | `/api/auth/login`           |
| Lấy danh sách câu hỏi | `GET`  | `/api/questions`            |
| Nộp điểm cuối ván     | `POST` | `/api/leaderboard`          |
| Đọc bảng xếp hạng     | `GET`  | `/api/leaderboard?limit=10` |

Base URL:

```text
https://{host}/api
Content-Type: application/json
```

Dữ liệu gửi và nhận đều dùng định dạng JSON. Điểm trong ván được tính trong quá trình chơi, API chỉ nhận tổng điểm khi ván kết thúc.

## 1. Đăng nhập

Sử dụng khi người chơi xác nhận thông tin tại màn đăng nhập.

```http
POST /api/auth/login
```

Request:

```json
{
  "username": "chienbinh",
  "password": "123456"
}
```

Quy ước dữ liệu:

- `username`: tên đăng nhập, được loại bỏ khoảng trắng ở hai đầu trước khi gửi.
- `password`: mật khẩu, giữ nguyên nội dung nhập vào, kể cả chuỗi rỗng.

Thành công `200`:

```json
{
  "username": "chienbinh",
  "displayName": "CHIẾN BINH 01"
}
```

- `displayName` là tên hiển thị và được dùng làm tên khi nộp điểm.
- Response không chứa `password`.

Thất bại `401` khi tên đăng nhập hoặc mật khẩu không đúng. Hai trường hợp này dùng chung một kết quả trả về.

## 2. Lấy danh sách câu hỏi

Sử dụng khi bắt đầu phần câu hỏi, trước khi câu hỏi được hiển thị.

```http
GET /api/questions
```

Thành công `200`:

```json
{
  "questions": [
    {
      "id": "q1",
      "prompt": "Cúm mùa là bệnh do nguyên nhân nào gây ra?",
      "options": ["Vi khuẩn", "Virus cúm", "Thay đổi thời tiết", "Dị ứng"],
      "correctIndex": 1,
      "explanation": "Cúm mùa do virus cúm gây ra, không phải vi khuẩn hay dị ứng."
    }
  ]
}
```

Quy ước dữ liệu:

- `id`: mã câu hỏi.
- `prompt`: nội dung câu hỏi.
- `options`: danh sách đáp án.
- `correctIndex`: vị trí đáp án đúng trong `options`, bắt đầu từ `0`.
- `explanation`: nội dung giải thích sau khi trả lời.

Danh sách câu hỏi được giữ lại để dùng cho các câu tiếp theo, không gọi API riêng cho từng câu. Khi danh sách đã dùng hết, thứ tự câu hỏi bắt đầu lại từ câu đầu tiên.

## 3. Nộp điểm cuối ván

Sử dụng khi ván chơi kết thúc. Mỗi ván chỉ nộp tổng điểm một lần.

```http
POST /api/leaderboard
```

Request:

```json
{
  "name": "CHIẾN BINH 01",
  "score": 1200
}
```

Quy ước dữ liệu:

- `name`: dùng `displayName` nhận được khi đăng nhập. Nếu chưa có thông tin đăng nhập, giá trị mặc định là `"CHIẾN BINH 01"`.
- `score`: tổng điểm của ván, có thể là số âm.

Thành công `200`:

```json
{
  "rank": 4
}
```

- `rank` là thứ hạng của điểm vừa nộp, bắt đầu từ `1`.
- Thứ hạng được tính trên toàn bộ điểm đã nộp, trước khi danh sách hiển thị được giới hạn ở 10 vị trí đầu. Ví dụ: thứ hạng `11` vẫn được trả về là `11`.

### Cách tính điểm trong ván

| Hành động                                   | Điểm |
| ------------------------------------------- | ---: |
| Bắn đúng loại virus                         | +100 |
| Bắn sai loại virus hoặc để virus chạm thành |  -50 |
| Trả lời đúng câu hỏi                        | +200 |
| Trả lời sai hoặc hết thời gian trả lời      | -100 |

API không nhận điểm của từng hành động riêng lẻ, chỉ nhận tổng điểm cuối ván.

## 4. Đọc bảng xếp hạng

Sử dụng khi mở màn bảng xếp hạng.

```http
GET /api/leaderboard?limit=10
```

Thành công `200`:

```json
{
  "entries": [
    { "name": "CHIẾN BINH 01", "score": 10000 },
    { "name": "CHIẾN BINH 02", "score": 9500 }
  ]
}
```

Quy ước dữ liệu:

- `entries` được sắp xếp theo `score` giảm dần.
- Khi nhiều người có cùng điểm, thứ tự trong response là thứ tự được giữ nguyên khi hiển thị.
- Số lượng kết quả tối đa theo tham số `limit` là `10`.

## 5. Dữ liệu JSON mẫu

| API                     | File dữ liệu       | Trường dữ liệu chính |
| ----------------------- | ------------------ | -------------------- |
| `POST /api/auth/login`  | `players.json`     | `players`            |
| `GET /api/questions`    | `questions.json`   | `questions`          |
| `POST /api/leaderboard` | `leaderboard.json` | `entries`            |
| `GET /api/leaderboard`  | `leaderboard.json` | `entries`            |

Tài khoản mẫu:

| Trường        | Giá trị         |
| ------------- | --------------- |
| Tên đăng nhập | `chienbinh`     |
| Mật khẩu      | `123456`        |
| Tên hiển thị  | `CHIẾN BINH 01` |
