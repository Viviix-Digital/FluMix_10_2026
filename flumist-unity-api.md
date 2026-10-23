# FluMist — Tài liệu API cho team Unity

| | |
|---|---|
| Phiên bản | **v1.3** (23/10/2026) — **người chơi tự đăng ký trong game** ([4.1](#41-post-quizplayerregister--đăng-ký)) |
| Sự kiện | **FluMist 2026** — chạy online ngày **28/10/2026** |
| Base URL | `https://api.sukientuongtac.com/api` |
| `projectSlug` | **`flumist-2026`** |
| Dành cho | Team Unity làm game FluMist (bắn virus + trả lời câu hỏi) |

> **Đây là bản chốt.** Game gọi đúng API trong tài liệu này. README "FluMix — Tài liệu JSON API cơ bản" (bản API giả của team Unity) chỉ dùng để tham khảo — [Phụ lục A](#phụ-lục-a--đổi-từ-readme-flumix) ghi mỗi API trong README đổi thành API nào.
>
> Server chỉ **thêm** field mới, không đổi tên / xoá field cũ — game nên **bỏ qua field lạ** thay vì báo lỗi.

---

## Mục lục

0. [Môi trường test](#0-môi-trường-test)
1. [Tóm tắt nhanh](#1-tóm-tắt-nhanh)
2. [Luật điểm](#2-luật-điểm)
3. [Quy ước chung](#3-quy-ước-chung)
4. [Tài khoản](#4-tài-khoản)
5. [Câu hỏi](#5-câu-hỏi--get-quizprojectsflumist-2026questions)
6. [Ván chơi](#6-ván-chơi)
7. [Kết thúc ván + bảng xếp hạng](#7-kết-thúc-ván--bảng-xếp-hạng)
8. [Lỗi](#8-lỗi)
9. [Code mẫu C# (Unity)](#9-code-mẫu-c-unity)
10. [Danh sách test cho team Unity](#10-danh-sách-test-cho-team-unity)
- [Phụ lục A — đổi từ README FluMix](#phụ-lục-a--đổi-từ-readme-flumix)
- [Lịch sử thay đổi](#lịch-sử-thay-đổi)

---

## 0. Môi trường test

| | |
|---|---|
| Trạng thái | Team server báo khi API bản này sẵn sàng trên server |
| Base URL | `https://api.sukientuongtac.com/api` (cùng server chạy thật) |
| `projectSlug` | `flumist-2026` |
| Tài khoản người chơi test | **Tự đăng ký** bằng API đăng ký (4.1) — tạo bao nhiêu tài khoản test cũng được. Tài khoản test sẽ được **xoá trước ngày chạy** |
| Bộ câu hỏi | **10 câu mẫu** lấy từ `questions.json` của team Unity (q1…q10), mỗi câu **200 điểm**, có lời giải thích. Bộ chính thức của brand sẽ thay trước ngày chạy |

- **Không hard-code `codeId`** của câu hỏi: mã do server sinh, khác nhau giữa bộ mẫu và bộ chính thức. Luôn lấy từ API câu hỏi (mục 5).
- Trong 10 câu mẫu, **8 câu có đáp án đúng là B**. Nên **xáo thứ tự hiển thị** lựa chọn trong game (vẫn gửi đúng `key` gốc — mục 5).

---

## 1. Tóm tắt nhanh

1. Lần đầu người chơi **đăng ký tài khoản ngay trong game** (đăng ký xong là đã đăng nhập); lần sau **đăng nhập**. Phải có tài khoản mới chơi được. **Một tài khoản chỉ chơi trên một máy**: đăng nhập máy khác thì máy cũ bị đăng xuất.
2. Lấy **bộ câu hỏi** một lần (có sẵn **lời giải thích**, **không có đáp án**).
3. Bắt đầu **một ván** (`playId`).
4. Trong ván, game **tự cộng dồn điểm bắn virus**. Mỗi lần người chơi **trả lời xong một câu**, game gửi **`progress`** gồm câu trả lời + điểm virus cộng dồn từ lần gửi trước. **Server chấm câu hỏi** và trả đúng / sai, điểm câu, đáp án đúng.
5. Hết ván: gửi lần cuối có `isCompleted: true` (kèm điểm virus còn lại). Response trả **top + hạng của người chơi** để hiện màn kết quả.

| Mục đích | Method | Endpoint |
|---|---|---|
| Đăng ký | `POST` | `/quiz/player/register` |
| Đăng nhập | `POST` | `/quiz/player/login` |
| Làm mới token | `POST` | `/quiz/player/refresh` |
| Thông tin người chơi + tổng điểm | `GET` | `/quiz/me` |
| Lấy bộ câu hỏi | `GET` | `/quiz/projects/flumist-2026/questions` |
| Bắt đầu ván | `POST` | `/quiz/plays` |
| Gửi kết quả (mỗi câu) / kết thúc ván | `POST` | `/quiz/plays/{playId}/progress` |
| Bảng xếp hạng | `GET` | `/quiz/projects/flumist-2026/leaderboard?limit=10` |

---

## 2. Luật điểm

| Hành động trong game | Điểm | Ai tính | Gửi thế nào |
|---|---:|---|---|
| Bắn đúng loại virus | +100 | Game | Cộng dồn vào `extraPoints` |
| Bắn sai loại virus / virus chạm thành | −50 | Game | Cộng dồn vào `extraPoints` (**âm được**) |
| Trả lời đúng câu hỏi | +200 | **Server** (`points` của câu) | Gửi `answer` |
| Trả lời sai | −100 | **Server** | Gửi `answer` |
| Hết thời gian trả lời | −100 | **Server** | Gửi `answer: ""` |

- Điểm câu hỏi do **ban tổ chức cấu hình** trên server (`points` của từng câu, điểm trừ khi sai). Game **không tự cộng** điểm câu hỏi: lấy `answer.points` trong response để hiện hiệu ứng (+200 / −100).
- `extraPoints` = **tổng điểm virus từ lần gửi trước tới giờ**, một số nguyên âm / dương / 0. Ví dụ giữa hai câu bắn đúng 4 con, sai 2 con ⇒ `extraPoints = 4×100 − 2×50 = 300`.
- **Không có trần âm**: điểm ván và tổng điểm có thể âm.
- **Điểm HUD** trong ván = `play.score` của response gần nhất **+** điểm virus đang cộng dồn chưa gửi.
- Xếp hạng theo **tổng điểm mọi ván** của tài khoản.

---

## 3. Quy ước chung

### 3.1. Request

- Body JSON, header `Content-Type: application/json`.
- Sau khi đăng nhập, **mọi** request gửi header `Authorization: Bearer <accessToken>` (kể cả lấy câu hỏi và bảng xếp hạng).

### 3.2. Response

Mọi response (thành công và lỗi) cùng một dạng — **dữ liệu nằm trong `data`**:

```json
{ "statusCode": 200, "message": "OK", "data": { … } }
```

Lỗi: `data = null` (lỗi validate 400: `data` là mảng thông báo). Một số lỗi có thêm **`code`** — game xử lý theo `code`, **đừng so `message`**:

```json
{ "statusCode": 401, "message": "Tài khoản đã đăng nhập ở thiết bị khác", "data": null, "code": "SESSION_REPLACED" }
```

| `code` | HTTP | Ý nghĩa | Game nên |
|---|---|---|---|
| `SESSION_REPLACED` | 401 | Tài khoản vừa đăng nhập ở máy khác | Báo "Tài khoản đã đăng nhập ở thiết bị khác" → xoá token → về màn đăng nhập. **Không** refresh |
| `PLAY_FINISHED` | 409 | Gửi vào ván đã kết thúc | Bỏ lần gửi đó; bắt đầu ván mới nếu cần |
| `ANSWER_LIMIT` | 400 | Ván đã đủ số câu tối đa ban tổ chức đặt (mặc định **không giới hạn**) | Kết thúc ván: gửi `isCompleted` **không kèm** `answer` |
| `EXTRA_LIMIT` | 400 | Điểm virus vượt trần ban tổ chức đặt (mặc định **không giới hạn**) | Lỗi cấu hình — ghi log, báo team server |
| `EVENT_CONFLICT` | 409 | Gửi lại một `eventId` đã dùng nhưng **nội dung khác** | Lỗi của game (trùng `eventId`) — ghi log, báo team |

### 3.3. Token

| Token | Hạn | Hết hạn thì |
|---|---|---|
| `accessToken` | 1 ngày | Gọi refresh (4.3) |
| `refreshToken` | 30 ngày | Đăng nhập lại |

`401` **không** có `code` ⇒ refresh **một lần** rồi gửi lại request; vẫn `401` ⇒ về màn đăng nhập. `401` có `code = SESSION_REPLACED` ⇒ về màn đăng nhập **ngay**.

### 3.4. Giờ

Giờ Việt Nam, không kèm offset: `"2026-10-28T17:45:12.000"` — chỉ để hiển thị.

---

## 4. Tài khoản

Người chơi **tự đăng ký tài khoản trong game**, không có chơi khách. Game cần hai màn: **Đăng ký** (lần đầu) và **Đăng nhập** (lần sau, hoặc trên máy khác).

### 4.1. `POST /quiz/player/register` — đăng ký

```json
{ "projectSlug": "flumist-2026", "username": "chienbinh", "password": "123456", "fullName": "Chiến Binh 01" }
```

| Trường | Bắt buộc | Quy tắc |
|---|---|---|
| `username` | **có** | 3–64 ký tự, chỉ gồm chữ **không dấu**, số và `. _ - @ +` — **không khoảng trắng, không dấu tiếng Việt**. Không trùng trong sự kiện. Server **phân biệt hoa thường** ⇒ game nên **bỏ khoảng trắng hai đầu và chuyển về chữ thường** ở cả đăng ký lẫn đăng nhập |
| `password` | **có** | 6–128 ký tự |
| `fullName` | nên có | **Tên hiển thị trên bảng xếp hạng** (có dấu, có khoảng trắng, ≤ 255 ký tự). Không gửi ⇒ bảng xếp hạng hiện username che bớt (`chi***`) |
| `phone`, `email` | không | Tuỳ chọn. `email` phải đúng định dạng nếu gửi |

- Response **`201`** — giống đăng nhập (4.2): **đăng ký xong là đã đăng nhập**, lưu token, vào game luôn (không cần gọi login).
- Trùng username ⇒ `409` "Tên đăng nhập đã có người dùng" — báo người chơi chọn tên khác.
- Sai quy tắc ⇒ `400` — nên kiểm ngay ở game trước khi gửi (regex `^[a-z0-9._@+-]{3,64}$` sau khi chuyển chữ thường; mật khẩu ≥ 6 ký tự).
- Ban tổ chức tắt đăng ký ⇒ `403` "Sự kiện này không cho tự đăng ký" — hiện `message`.

### 4.2. `POST /quiz/player/login` — đăng nhập

```json
{ "projectSlug": "flumist-2026", "username": "chienbinh", "password": "123456" }
```

- `username`: bỏ khoảng trắng hai đầu + chuyển chữ thường, giống lúc đăng ký.
- `password`: **không được rỗng** — chặn ngay ở game (gửi rỗng ⇒ `400`).

Response `200`:

```json
{
  "statusCode": 200,
  "message": "OK",
  "data": {
    "player": { "uuid": "019a…", "username": "chienbinh", "fullName": "Chiến Binh 01",
                "phone": null, "email": null, "projectSlug": "flumist-2026" },
    "accessToken": "eyJ…",
    "refreshToken": "eyJ…"
  }
}
```

- `player.fullName` = **tên hiển thị** (README cũ: `displayName`), cũng là tên trên bảng xếp hạng. Không có thì bảng xếp hạng hiện username che bớt (`chi***`).
- Sai tên đăng nhập / mật khẩu ⇒ `401` "Sai tên đăng nhập hoặc mật khẩu". Tài khoản bị khoá ⇒ `401` "Tài khoản đã bị khoá".

**Một tài khoản một máy.** Mỗi lần đăng nhập tạo **phiên mới**:
- Máy đăng nhập trước đó nhận `401 SESSION_REPLACED` ở **request kế tiếp**. Server **không đẩy** thông báo; muốn máy cũ biết khi đang đứng yên ở menu thì gọi `GET /quiz/me` **30–60 giây/lần** (không gọi trong lúc chơi).
- Ván đang chơi dở của máy cũ **kết thúc dang dở**: điểm đã gửi **vẫn được tính**.
- Đăng ký cũng tạo phiên mới như đăng nhập.

Không có API quên / đổi mật khẩu cho người chơi: người chơi quên mật khẩu thì báo ban tổ chức đặt lại trên trang quản trị.

### 4.3. `POST /quiz/player/refresh`

```json
{ "refreshToken": "eyJ…" }
```

Response giống đăng nhập. **Lưu đè cả hai token.** Mỗi refresh token **chỉ dùng một lần**: dùng lại token cũ ⇒ `401` (đăng nhập lại). Bị đăng nhập máy khác ⇒ `401 SESSION_REPLACED`.

### 4.4. `GET /quiz/me`

```json
{
  "player": { "uuid": "…", "username": "chienbinh", "fullName": "Chiến Binh 01" },
  "project": { "slug": "flumist-2026", "name": "FluMist 2026", "status": "ACTIVE", "settings": { … } },
  "totalScore": 1250,
  "plays": 3,
  "playsToday": 1,
  "remaining": { "total": null, "today": null }
}
```

`totalScore` = tổng điểm **mọi ván** (có thể âm). `remaining.*` = số ván còn được chơi, `null` = không giới hạn.

### 4.5. Đăng xuất

Không có API đăng xuất: game xoá hai token là đủ.

---

## 5. Câu hỏi — `GET /quiz/projects/flumist-2026/questions`

Gọi **một lần** lúc vào game, giữ lại dùng cho cả ván (không gọi riêng từng câu). Response `200` (trong `data`):

```json
{
  "project": {
    "slug": "flumist-2026", "name": "FluMist 2026", "status": "ACTIVE",
    "settings": { "revealAnswer": true, "scoringMode": "FIXED",
                  "wrongAnswerPoints": -100, "allowRepeatAnswers": true, "maxAnswersPerPlay": null,
                  "maxExtraPerEvent": null, "maxExtraPerPlay": null,
                  "maxPlays": null, "maxPlaysPerDay": null, "…": "…" }
  },
  "questions": [
    {
      "codeId": "3WYXWDZK",
      "type": "SINGLE",
      "content": "Cúm mùa là bệnh do nguyên nhân nào gây ra?",
      "options": [
        { "key": "A", "label": "Vi khuẩn" },
        { "key": "B", "label": "Virus cúm" },
        { "key": "C", "label": "Thay đổi thời tiết" },
        { "key": "D", "label": "Dị ứng" }
      ],
      "blanks": 0,
      "points": 200,
      "imageUrl": null,
      "explanation": "Cúm mùa do virus cúm gây ra, không phải vi khuẩn hay dị ứng.",
      "orderNo": 1
    }
  ]
}
```

| Trường | Ý nghĩa |
|---|---|
| `codeId` | Mã câu hỏi (8 ký tự) — dùng khi gửi câu trả lời. README cũ: `id` |
| `content` | Nội dung câu hỏi. README cũ: `prompt` |
| `options[].key` / `label` | Phím (`A`, `B`, …) + nội dung lựa chọn. Game gửi **phím**, không gửi vị trí |
| `points` | Điểm khi trả lời đúng (FluMist: 200) |
| `explanation` | Lời giải thích, hiện sau khi người chơi trả lời. `null` = câu không có |
| `orderNo` | Thứ tự gợi ý |
| `project.settings.wrongAnswerPoints` | Điểm khi sai / hết giờ (FluMist: −100) — chỉ để hiển thị luật |

- **Không có đáp án đúng** (README cũ có `correctIndex`). Đúng / sai biết **sau khi gửi `progress`** (6.2): response có `answer.correct` và `answer.correctAnswer` (vd `"B"`) để game tô đáp án đúng.
- **Xáo lựa chọn**: game được đổi thứ tự hiển thị, nhưng gửi `key` **gốc** của lựa chọn người chơi bấm (vd hiển thị "Virus cúm" ở vị trí thứ 3 thì vẫn gửi `"B"`).
- Hết danh sách thì **quay lại câu đầu**: câu gặp lại trong cùng ván **vẫn được chấm và tính điểm**.
- `answer` gửi lên: `SINGLE` = `"B"` · hết giờ / bỏ qua = `""`.

---

## 6. Ván chơi

### 6.1. `POST /quiz/plays` — bắt đầu ván

```json
{ "clientPlayId": "6f1c0b9e2d0a4c559b1a1f0e5d2c7a11" }
```

`clientPlayId` (tuỳ chọn, 1–64 ký tự `A-Z a-z 0-9 _ -`): mã ván do game sinh. Gửi lại cùng mã ⇒ trả **ván cũ**, không tạo ván mới (an toàn khi retry).

Response `201` (trong `data`):

```json
{ "play": { "playId": "019a…", "clientPlayId": "6f1c…", "status": "PLAYING",
            "answerScore": 0, "bonusScore": 0, "extraScore": 0, "score": 0,
            "endReason": null, "startedAt": "…", "finishedAt": null },
  "resumed": false }
```

- Mỗi tài khoản có **tối đa một ván đang chơi**. Còn ván chưa kết thúc (vd app bị tắt giữa ván) ⇒ server **trả lại ván đó** (`resumed: true`) — điểm HUD lấy từ `play.score`. **Luôn dùng `playId` server trả về.**
- Hết lượt (nếu ban tổ chức đặt giới hạn) hoặc sự kiện đã đóng ⇒ `403`.

### 6.2. `POST /quiz/plays/{playId}/progress` — gửi khi trả lời xong một câu

**Một câu trả lời = một lần gửi**, kèm điểm virus cộng dồn. **Không gửi theo từng phát bắn.**

```json
{
  "eventId": "9b2f4c1e8a7d4e0f9c3b2a1d0e9f8c7b",
  "answer": { "codeId": "3WYXWDZK", "answer": "B", "timeMs": 5300 },
  "extraPoints": 300
}
```

| Trường | Bắt buộc | Ý nghĩa |
|---|---|---|
| `eventId` | **có** | Mã lần gửi, 1–64 ký tự `A-Z a-z 0-9 _ -`. **Sinh mới cho mỗi lần gửi** — khuyên dùng `Guid.NewGuid().ToString("N")` (không dùng số thứ tự: ván được trả lại sau khi mở lại app sẽ trùng). **Gửi lại (retry) phải giữ nguyên `eventId` và cả body** |
| `answer` | không | `codeId` + `answer` (+ `timeMs`: thời gian trả lời, ms, tuỳ chọn) |
| `extraPoints` | không | Điểm virus cộng dồn từ lần gửi trước (số nguyên, **âm được**). Không có thì bỏ trường hoặc gửi `0` |
| `isCompleted` | không | `true` ⇒ **kết thúc ván** sau khi ghi lần gửi này (mục 7) |

Response `200` (trong `data`) — trả lời đúng:

```json
{
  "eventId": "9b2f4c1e8a7d4e0f9c3b2a1d0e9f8c7b",
  "replayed": false,
  "answer": { "codeId": "3WYXWDZK", "correct": true, "points": 200, "basePoints": 200,
              "multiplier": 1, "duplicate": false, "correctAnswer": "B" },
  "extraPoints": 300,
  "completed": false,
  "play": { "playId": "019a…", "status": "PLAYING", "answerScore": 400, "extraScore": 550,
            "score": 950, "endReason": null, "…": "…" },
  "totalScore": 2150
}
```

Trả lời sai / hết giờ — chỉ khác phần `answer`:

```json
"answer": { "codeId": "3WYXWDZK", "correct": false, "points": -100, "basePoints": 200,
            "multiplier": 1, "duplicate": false, "correctAnswer": "B" }
```

- `answer.points` = điểm câu này (+200 / −100). `play.score` = điểm **ván này**. `totalScore` = **tổng mọi ván**.
- Mỗi lần gửi được ghi **ngay** vào tổng và bảng xếp hạng, kể cả khi ván chưa kết thúc.

Quy tắc:

1. **Retry an toàn.** Mạng chập chờn / timeout / lỗi 5xx ⇒ gửi lại **đúng body cũ** (cùng `eventId`). Server trả `200`, `replayed: true`, **không cộng thêm** điểm; response là **kết quả lúc ghi lần đầu**.
2. **Gửi tuần tự.** Chờ lần gửi trước nhận `200` rồi mới gửi lần sau. Lần gửi chưa nhận `200` (mất mạng lâu) thì **giữ lại và gửi lại đúng body đó trước** khi gửi câu tiếp theo — nếu không, điểm virus có thể bị tính hai lần.
3. **Chỉ trừ điểm virus đã gửi sau khi nhận `200`.** Lỗi thì giữ nguyên để gửi lần sau.
4. **`isCompleted` gửi cuối cùng.** Sau khi ván kết thúc, mọi lần gửi mới ⇒ `409 PLAY_FINISHED`, điểm đó **mất**.
5. Đăng nhập máy khác giữa ván ⇒ máy cũ nhận `401 SESSION_REPLACED`, ván cũ kết thúc dang dở (điểm đã gửi vẫn tính).

---

## 7. Kết thúc ván + bảng xếp hạng

### 7.1. Gửi `isCompleted: true`

Hết ván (thành bị phá, hết giờ ván…): gửi **lần cuối** với `isCompleted: true`, kèm điểm virus còn lại. Nếu ván kết thúc ngay khi vừa trả lời một câu thì gộp luôn `answer` vào lần gửi này.

```json
{ "eventId": "0c4d1e2f3a4b5c6d7e8f9a0b1c2d3e4f", "extraPoints": -50, "isCompleted": true }
```

Response có thêm **`leaderboard`**:

```json
{
  "completed": true,
  "play": { "status": "FINISHED", "endReason": "COMPLETED", "score": 900, "…": "…" },
  "totalScore": 2100,
  "leaderboard": {
    "asOf": "2026-10-28T20:15:03.120",
    "top": [
      { "rank": 1, "displayName": "Trần Thị Bình", "totalScore": 10000, "isMe": false },
      { "rank": 2, "displayName": "chi***", "totalScore": 9500, "isMe": false }
    ],
    "me": { "rank": 137, "displayName": "Chiến Binh 01", "totalScore": 2100, "isMe": true, "inTop": false }
  }
}
```

- `top`: tối đa **20** người. `me`: hạng của người đang chơi trong **toàn bộ** người chơi, kể cả khi ngoài top (`inTop: false`). README cũ trả `rank` ⇒ nay dùng **`leaderboard.me.rank`**.
- Xếp theo **tổng điểm mọi ván** (không phải từng ván). Bằng điểm ⇒ tên A→Z. **Không đồng hạng.** Tài khoản bị khoá không có trên bảng.
- Server không tính kịp bảng xếp hạng ⇒ `leaderboard: null` kèm `leaderboardError`. **Điểm vẫn đã lưu.** Game gọi 7.2 để lấy lại.
- Gửi lại đúng lần `isCompleted` đó (cùng `eventId`, cùng body) ⇒ `replayed: true`, cùng kết quả điểm, kèm bảng xếp hạng **mới nhất**.

### 7.2. `GET /quiz/projects/flumist-2026/leaderboard?limit=10`

Cần đăng nhập. Dùng khi mở màn bảng xếp hạng, hoặc khi `leaderboard: null`. `data` = phần `leaderboard` ở 7.1. `limit` từ 1 đến 100, mặc định 20. **Không gọi liên tục** trong lúc chơi.

---

## 8. Lỗi

| HTTP | Khi nào | Game nên |
|---|---|---|
| `400` | Body sai (thiếu `eventId`, `answer` sai kiểu, mật khẩu rỗng…) — `message` ghép các lỗi; `ANSWER_LIMIT`, `EXTRA_LIMIT` | Lỗi của game — ghi log `message`; với `ANSWER_LIMIT` thì kết thúc ván |
| `401` | Token hết hạn / sai; `SESSION_REPLACED`; sai mật khẩu; tài khoản bị khoá | Xem 3.3 |
| `403` | Hết lượt chơi; sự kiện đã đóng ("Sự kiện đã kết thúc"); ban tổ chức tắt đăng ký | Hiện `message`, về menu |
| `404` | Sai `projectSlug`; `playId` không phải của tài khoản này; `codeId` không có (vd bộ câu hỏi vừa đổi) | Lấy lại câu hỏi / bắt đầu ván mới |
| `409` | `PLAY_FINISHED`; `EVENT_CONFLICT`; trùng username khi đăng ký ("Tên đăng nhập đã có người dùng") | Xem 3.2; trùng username ⇒ báo chọn tên khác |
| `5xx` / mất mạng / timeout | Server hoặc mạng lỗi | Gửi lại **đúng body** (tối đa 3 lần, chờ tăng dần), sau đó giữ lại để gửi sau |

---

## 9. Code mẫu C# (Unity)

Dùng package **Newtonsoft Json** (`com.unity.nuget.newtonsoft-json`, Package Manager → *Add package by name*). Không dùng `JsonUtility` vì nó không phân biệt `null` với object rỗng (`leaderboard: null`, `answer: null`).

### 9.1. Model

```csharp
using System;

public class ApiResponse<T>
{
    public int statusCode;
    public string message;
    public T data;
    public string code;              // SESSION_REPLACED, PLAY_FINISHED, … hoặc null
}

public class PlayerDto { public string uuid, username, fullName, phone, email, projectSlug; }
public class AuthDto { public PlayerDto player; public string accessToken, refreshToken; }

public class OptionDto { public string key, label; }
public class QuestionDto
{
    public string codeId, type, content, imageUrl, explanation;
    public OptionDto[] options;
    public int points, orderNo, blanks;
}
public class SettingsDto
{
    public bool revealAnswer, allowRepeatAnswers;
    public string scoringMode;
    public int wrongAnswerPoints;
    public int? maxAnswersPerPlay, maxPlays, maxPlaysPerDay;
}
public class ProjectDto { public string slug, name, status; public SettingsDto settings; }
public class QuestionsDto { public ProjectDto project; public QuestionDto[] questions; }

public class PlayDto
{
    public string playId, clientPlayId, status, endReason, startedAt, finishedAt;
    public int answerScore, bonusScore, extraScore, score;
}
public class StartPlayDto { public PlayDto play; public bool resumed; }

public class AnswerResultDto
{
    public string codeId, correctAnswer;
    public bool correct, duplicate;
    public int points, basePoints, multiplier;
}
public class RankRowDto { public int rank, totalScore; public string displayName; public bool isMe, inTop; }
public class LeaderboardDto { public string asOf; public RankRowDto[] top; public RankRowDto me; }
public class ProgressResultDto
{
    public string eventId, leaderboardError;
    public bool replayed, completed;
    public AnswerResultDto answer;   // null nếu lần gửi không kèm câu trả lời
    public int extraPoints, totalScore;
    public PlayDto play;
    public LeaderboardDto leaderboard;   // chỉ có khi completed
}
```

### 9.2. Client gọi API

```csharp
using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public class FluMistApi : MonoBehaviour
{
    public string baseUrl = "https://api.sukientuongtac.com/api";
    public string projectSlug = "flumist-2026";
    public string accessToken, refreshToken;          // lưu PlayerPrefs sau khi đăng nhập

    /// Bị đăng xuất (đăng nhập máy khác / hết phiên) ⇒ game về màn đăng nhập.
    public event Action<string> LoggedOut;

    const int TimeoutSec = 10;
    const int MaxAttempts = 3;

    /// Username luôn bỏ khoảng trắng + chữ thường (server phân biệt hoa thường).
    static string NormUser(string u) => (u ?? "").Trim().ToLowerInvariant();
    public static bool ValidUsername(string u) => System.Text.RegularExpressions.Regex.IsMatch(NormUser(u), "^[a-z0-9._@+-]{3,64}$");

    /// Đăng ký xong là đã đăng nhập (201 + token). 409 = trùng username.
    public IEnumerator Register(string username, string password, string fullName, Action<AuthDto> onOk, Action<long, string, string> onError)
    {
        var body = JsonConvert.SerializeObject(new { projectSlug, username = NormUser(username), password, fullName = fullName?.Trim() });
        return Send<AuthDto>("POST", "/quiz/player/register", body, false, a => { SaveTokens(a); onOk(a); }, onError);
    }

    public IEnumerator Login(string username, string password, Action<AuthDto> onOk, Action<long, string, string> onError)
    {
        var body = JsonConvert.SerializeObject(new { projectSlug, username = NormUser(username), password });
        return Send<AuthDto>("POST", "/quiz/player/login", body, false, a => { SaveTokens(a); onOk(a); }, onError);
    }

    public IEnumerator Questions(Action<QuestionsDto> onOk, Action<long, string, string> onError) =>
        Send("GET", $"/quiz/projects/{projectSlug}/questions", null, true, onOk, onError);

    public IEnumerator StartPlay(string clientPlayId, Action<StartPlayDto> onOk, Action<long, string, string> onError) =>
        Send("POST", "/quiz/plays", JsonConvert.SerializeObject(new { clientPlayId }), true, onOk, onError);

    /// body đã serialize sẵn — gửi lại phải dùng ĐÚNG chuỗi này (cùng eventId).
    public IEnumerator Progress(string playId, string body, Action<ProgressResultDto> onOk, Action<long, string, string> onError) =>
        Send("POST", $"/quiz/plays/{playId}/progress", body, true, onOk, onError);

    public IEnumerator Leaderboard(int limit, Action<LeaderboardDto> onOk, Action<long, string, string> onError) =>
        Send("GET", $"/quiz/projects/{projectSlug}/leaderboard?limit={limit}", null, true, onOk, onError);

    public IEnumerator Me(Action<string> onOk, Action<long, string, string> onError) =>
        Send<object>("GET", "/quiz/me", null, true, _ => onOk("ok"), onError);

    void SaveTokens(AuthDto a) { accessToken = a.accessToken; refreshToken = a.refreshToken; }

    void Logout(string reason) { accessToken = refreshToken = null; LoggedOut?.Invoke(reason); }

    UnityWebRequest Build(string method, string path, string body, bool auth)
    {
        var req = new UnityWebRequest(baseUrl + path, method) { downloadHandler = new DownloadHandlerBuffer(), timeout = TimeoutSec };
        if (body != null)
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            req.SetRequestHeader("Content-Type", "application/json");
        }
        if (auth) req.SetRequestHeader("Authorization", "Bearer " + accessToken);
        return req;
    }

    static ApiResponse<T> Parse<T>(string text)
    {
        try { return JsonConvert.DeserializeObject<ApiResponse<T>>(text); } catch { return null; }
    }

    IEnumerator Refresh(Action<bool> done)
    {
        using (var req = Build("POST", "/quiz/player/refresh", JsonConvert.SerializeObject(new { refreshToken }), false))
        {
            yield return req.SendWebRequest();
            var res = Parse<AuthDto>(req.downloadHandler.text);
            if (req.responseCode == 200 && res?.data != null) { SaveTokens(res.data); done(true); }
            else done(false);
        }
    }

    /// onError(httpCode, code, message): httpCode 0 = mất mạng / timeout.
    IEnumerator Send<T>(string method, string path, string body, bool auth, Action<T> onOk, Action<long, string, string> onError)
    {
        bool refreshed = false;
        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            using (var req = Build(method, path, body, auth))
            {
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.ConnectionError)
                {
                    if (attempt < MaxAttempts) { yield return new WaitForSeconds(0.5f * attempt); continue; }
                    onError(0, null, "Mất kết nối server");
                    yield break;
                }

                var res = Parse<T>(req.downloadHandler.text);
                long http = req.responseCode;
                if (http >= 200 && http < 300 && res != null) { onOk(res.data); yield break; }

                if (http == 401 && auth)
                {
                    if (res?.code == "SESSION_REPLACED") { Logout("Tài khoản đã đăng nhập ở thiết bị khác"); onError(401, res.code, res.message); yield break; }
                    if (!refreshed)
                    {
                        refreshed = true;
                        bool ok = false;
                        yield return Refresh(r => ok = r);
                        if (ok) { attempt--; continue; }   // gửi lại với token mới
                    }
                    Logout("Phiên đăng nhập hết hạn");
                    onError(401, res?.code, res?.message);
                    yield break;
                }

                if (http >= 500 && attempt < MaxAttempts) { yield return new WaitForSeconds(0.5f * attempt); continue; }
                onError(http, res?.code, res?.message ?? $"Lỗi HTTP {http}");
                yield break;
            }
        }
    }
}
```

### 9.3. Một ván chơi

```csharp
using System;
using System.Collections;
using Newtonsoft.Json;
using UnityEngine;

public class FluMistRound : MonoBehaviour
{
    public FluMistApi api;
    public QuestionDto[] questions;      // từ api.Questions, lấy một lần

    string playId;
    int virus;                           // điểm virus cộng dồn CHƯA gửi
    int questionIndex;
    string pendingBody; int pendingVirus;  // lần gửi chưa nhận 200
    public int HudScore { get; private set; }

    // ── Bắn virus: chỉ cộng dồn, KHÔNG gọi server ──
    public void OnVirusShot(bool rightType) { virus += rightType ? 100 : -50; HudScore += rightType ? 100 : -50; }
    public void OnVirusReachedWall() { virus -= 50; HudScore -= 50; }

    public IEnumerator Begin(Action<string> onError)
    {
        StartPlayDto start = null;
        yield return api.StartPlay(Guid.NewGuid().ToString("N"), s => start = s, (h, c, m) => onError(m));
        if (start == null) yield break;
        playId = start.play.playId;
        HudScore = start.play.score;     // ván được trả lại (resumed) thì tiếp tục điểm cũ
        virus = 0;
    }

    public QuestionDto NextQuestion() => questions[questionIndex++ % questions.Length];   // hết thì quay lại

    /// key = phím gốc người chơi chọn ("B"), "" khi hết giờ.
    public IEnumerator Answer(QuestionDto q, string key, int timeMs, Action<ProgressResultDto> show)
    {
        yield return Flush(null);                       // còn lần gửi cũ chưa xong ⇒ gửi trước
        if (pendingBody != null) yield break;           // vẫn mất mạng — báo "Mất kết nối", điểm virus giữ lại cho câu sau

        Queue(new { eventId = NewEventId(), answer = new { codeId = q.codeId, answer = key ?? "", timeMs }, extraPoints = virus });
        yield return Flush(show);
    }

    public IEnumerator Finish(Action<ProgressResultDto> showResult)
    {
        yield return Flush(null);
        if (pendingBody != null) yield break;           // vẫn mất mạng — báo người chơi, gọi Finish lại sau
        Queue(new { eventId = NewEventId(), extraPoints = virus, isCompleted = true });
        yield return Flush(showResult);                 // showResult nhận leaderboard (null ⇒ gọi api.Leaderboard)
    }

    static string NewEventId() => Guid.NewGuid().ToString("N");

    void Queue(object body) { pendingBody = JsonConvert.SerializeObject(body); pendingVirus = virus; }

    IEnumerator Flush(Action<ProgressResultDto> onResult)
    {
        if (pendingBody == null) yield break;
        ProgressResultDto res = null; long http = 0; string code = null;
        yield return api.Progress(playId, pendingBody, r => res = r, (h, c, m) => { http = h; code = c; });

        if (res != null)
        {
            virus -= pendingVirus;                      // chỉ trừ phần đã gửi, sau khi nhận 200
            HudScore = res.play.score + virus;
            pendingBody = null;
            onResult?.Invoke(res);
        }
        else if (http == 0 || http >= 500)
        {
            // mất mạng: giữ pendingBody, lần sau gửi lại ĐÚNG body này
        }
        else
        {
            pendingBody = null;                         // 4xx: không gửi lại; điểm virus vẫn giữ
            Debug.LogWarning($"progress {http} {code}"); // PLAY_FINISHED ⇒ ván đã kết thúc; ANSWER_LIMIT ⇒ gọi Finish
        }
    }
}
```

Hiện kết quả một câu: `res.answer.correct`, `res.answer.points` (+200 / −100), tô `res.answer.correctAnswer`, rồi hiện `q.explanation`.

---

## 10. Danh sách test cho team Unity

| # | Kịch bản | Kết quả đúng |
|---|---|---|
| 1 | Đăng ký tài khoản mới (có `fullName`) | `201` + token, vào game luôn; bảng xếp hạng hiện đúng `fullName` |
| 1a | Đăng ký trùng username / username có dấu hoặc khoảng trắng / mật khẩu < 6 ký tự | `409` / game chặn trước khi gửi (server `400`) |
| 1b | Đăng nhập đúng / sai mật khẩu / để trống mật khẩu / gõ username chữ hoa | `200` + token / `401` / game chặn, không gửi / vẫn vào được (game chuyển chữ thường) |
| 2 | Lấy câu hỏi | 10 câu, mỗi câu có `explanation`, **không** có đáp án |
| 3 | Trả lời đúng | `correct: true`, `points: 200`, `correctAnswer` đúng phím |
| 4 | Trả lời sai | `correct: false`, `points: -100` |
| 5 | Hết giờ (`answer: ""`) | `points: -100` |
| 6 | Chơi quá 10 câu (quay lại câu 1) | Câu lặp vẫn tính điểm, `duplicate: false` |
| 7 | Bắn virus đúng / sai giữa hai câu, gửi kèm câu trả lời | `play.score` tăng / giảm đúng tổng virus + điểm câu |
| 8 | Xáo lựa chọn khi hiển thị | Server chấm đúng theo `key` gốc |
| 9 | Tắt mạng lúc gửi câu trả lời, bật lại | Gửi lại cùng `eventId` ⇒ `replayed: true` hoặc ghi lần đầu; **điểm không bị cộng hai lần** |
| 10 | Kết thúc ván (`isCompleted`) | Có `leaderboard.top` + `leaderboard.me.rank`; gửi tiếp ⇒ `409 PLAY_FINISHED` |
| 11 | Đăng nhập cùng tài khoản trên máy thứ hai | Máy một nhận `401 SESSION_REPLACED` ở request kế tiếp ⇒ về màn đăng nhập; ván dở của máy một vẫn giữ điểm |
| 12 | Tắt app giữa ván, mở lại, bắt đầu ván | `resumed: true`, cùng `playId`, điểm HUD tiếp tục |
| 13 | Tổng điểm âm (cố tình bắn sai, trả lời sai) | Vẫn có hạng trên bảng xếp hạng, đứng cuối |
| 14 | Mở màn bảng xếp hạng | `GET …/leaderboard?limit=10` ⇒ tối đa 10 dòng + `me` |

---

## Phụ lục A — đổi từ README FluMix

| README cũ | API server (bản chốt) | Khác biệt chính |
|---|---|---|
| `POST /api/auth/login` `{username, password}` → `{username, displayName}` | `POST /api/quiz/player/login` `{projectSlug, username, password}` → `data.player` + `accessToken` / `refreshToken` | Thêm `projectSlug`; có token; `displayName` ⇒ `player.fullName`; mật khẩu rỗng ⇒ chặn ở game |
| `GET /api/questions` → `{id, prompt, options[], correctIndex, explanation}` | `GET /api/quiz/projects/flumist-2026/questions` → `{codeId, content, options[{key,label}], points, explanation}` | **Không có `correctIndex`** — đúng / sai biết sau khi gửi `progress`; `id` ⇒ `codeId`; `prompt` ⇒ `content`; đáp án gửi theo `key` (`correctIndex` 0 ⇒ `A`, 1 ⇒ `B`…) |
| Game tự tính điểm cả ván (virus + câu hỏi) | Virus: game cộng dồn, gửi trong `extraPoints`. Câu hỏi: **server chấm** (+200 / −100) | Không tự cộng điểm câu hỏi |
| `POST /api/leaderboard` `{name, score}` cuối ván → `{rank}` | `POST /api/quiz/plays` khi vào ván → `POST /api/quiz/plays/{playId}/progress` mỗi câu → lần cuối `isCompleted: true` → `data.leaderboard.me.rank` | Không gửi tên + tổng điểm; tên lấy theo tài khoản; hạng tính trên **tổng mọi ván** |
| `GET /api/leaderboard?limit=10` → `{entries[{name, score}]}` | `GET /api/quiz/projects/flumist-2026/leaderboard?limit=10` → `data.top[{rank, displayName, totalScore}]` + `data.me` | Cần đăng nhập; có thêm hạng của chính người chơi |
| Không có đăng ký (tài khoản mẫu `players.json`) | `POST /api/quiz/player/register` `{projectSlug, username, password, fullName}` → giống đăng nhập | Người chơi tự đăng ký trong game; `fullName` = tên hiển thị |
| Chơi khi chưa đăng nhập (tên mặc định "CHIẾN BINH 01") | **Không có** — phải có tài khoản | |
| Response không bọc | Response bọc `{statusCode, message, data, code?}` | |

## Lịch sử thay đổi

| Phiên bản | Ngày | Thay đổi |
|---|---|---|
| v1.0 | 12/10/2026 | Bản đầu cho FluMist: một máy một phiên, `progress` + `isCompleted`, bảng xếp hạng top 20 + hạng cá nhân |
| v1.1 | 23/10/2026 | Sự kiện 28/10, slug `flumist-2026`. Luật điểm FluMist: câu sai / hết giờ **−100** (server), `extraPoints` **âm được** (virus −50), điểm virus cộng dồn gửi kèm câu trả lời. Câu hỏi có **`explanation`**; câu lặp lại trong ván **vẫn tính điểm**; mã lỗi `ANSWER_LIMIT`. Phụ lục A đổi từ README FluMix |
| v1.2 | 23/10/2026 | **Không đổi API.** Thêm môi trường test (bộ 10 câu mẫu), bảng endpoint, bảng lỗi, code mẫu C#, danh sách test. `eventId` khuyên dùng GUID (số thứ tự có thể trùng khi ván được trả lại). Giữ lại lần gửi chưa nhận 200 để gửi lại trước câu tiếp theo. Xáo thứ tự lựa chọn khi hiển thị, gửi `key` gốc |
| v1.3 | 23/10/2026 | **Không đổi API.** Người chơi **tự đăng ký trong game**: đăng ký thành mục 4.1 (quy tắc username / mật khẩu / `fullName`, chuyển username về chữ thường), đăng nhập 4.2; tài khoản test tự đăng ký. Code mẫu thêm `Register`, danh sách test thêm ca đăng ký |
