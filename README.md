# FluMix — luồng gọi API backend

Client Unity không gọi URL trực tiếp từ màn hình. Mỗi màn gọi một method trên `AppServices`. Bản đang chạy dùng file JSON local. Backend HTTP thay đúng các method này, không đổi gameplay.

Điểm vào: `Assets/Scripts/FluMix/Data/AppServices.cs`.

| Việc | Interface | Bản local hiện tại |
| --- | --- | --- |
| Đăng nhập | `IPlayerRepository` | `LocalJsonPlayerRepository` |
| Ngân hàng câu hỏi | `IQuestionRepository` | `LocalJsonQuestionRepository` |
| Bảng xếp hạng | `ILeaderboardRepository` | `LocalJsonLeaderboardRepository` |

File local nằm ở `StreamingAssets/FluMix/`. Lần chạy đầu, game copy sang `Application.persistentDataPath/FluMix/` rồi đọc/ghi bản đó.

Base URL dưới đây là hợp đồng để gắn HTTP. Chưa có host thật trong project.

```text
https://{host}/api
Content-Type: application/json
```

Điểm trong trận do client tính. Backend chỉ nhận điểm cuối khi hết giờ.

## 1. Đăng nhập

Khi nào: người chơi bấm nút bắt đầu ở màn login.

Gọi: `LoginView.Submit` → `Players.TryLogin(username, password)`.

```http
POST /api/auth/login
```

```json
{
  "username": "chienbinh",
  "password": "123456"
}
```

Client `Trim()` username trước khi gửi. Password giữ nguyên, kể cả chuỗi rỗng.

Thành công `200`:

```json
{
  "username": "chienbinh",
  "displayName": "CHIẾN BINH 01"
}
```

Client lưu record này. `displayName` là tên gửi lên bảng xếp hạng. Màn tiếp theo là luật chơi. Chưa gọi API nào khác.

Thất bại `401` khi sai tài khoản hoặc sai mật khẩu. Client không phân biệt hai trường hợp: rung khung đăng nhập và ở lại màn login.

Không gửi password lại trong response. Backend lưu mật khẩu đã băm.

Quên mật khẩu trên UI chỉ hiện chữ “Mật khẩu lưu trên máy này”. Không gọi API.

## 2. Lấy câu hỏi

Khi nào: boss bắt đầu, trong `BossSequence`, trước khi hiện câu.

Gọi: `Questions.LoadAll()`.

```http
GET /api/questions
```

`200`:

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

`correctIndex` là chỉ số 0 của `options`.

Client giữ cả mảng và cache đến khi reload. Mỗi lần boss xuất hiện chỉ lấy câu tiếp theo trên máy (`QuestionPresenter.TakeNext`), không gọi lại API từng câu. Hết mảng thì quay lại câu đầu.

`FetchRemote()` có trên interface nhưng gameplay không gọi. HTTP sau này nhét vào `LoadAll()`, không gọi `FetchRemote()` từ trận đấu.

Bắn virus, cộng/trừ điểm, đổi loại virus không gọi API.

## 3. Nộp điểm cuối ván

Khi nào: hết giờ, hoặc boss kết thúc đúng lúc ván đã hết. `GameFlow.EndRound` gọi một lần.

Gọi: `Leaderboard.Submit(displayName, score)`.

```http
POST /api/leaderboard
```

```json
{
  "name": "CHIẾN BINH 01",
  "score": 1200
}
```

`name` là `displayName` lúc login. Chưa login thì client gửi `"CHIẾN BINH 01"`. `score` là tổng điểm trên HUD, có thể âm.

`200`:

```json
{
  "rank": 4
}
```

`rank` bắt đầu từ 1, tính trên toàn bộ điểm đã nộp, trước khi cắt top 10. Hạng 11 vẫn trả `11` dù bảng chỉ giữ 10 người.

Client hiện hạng này ở màn kết quả. Cờ `submitted` chặn nộp lần hai trong cùng ván.

Điểm client đang cộng:

| Việc | Điểm |
| --- | --- |
| Bắn đúng loại virus | +100 |
| Bắn sai loại, hoặc virus chạm thành | -50 |
| Trả lời đúng câu boss | +200 |
| Trả lời sai hoặc hết giờ câu hỏi | -100 |

Backend không nhận từng phát bắn. Chỉ nhận tổng ở cuối ván.

## 4. Đọc bảng xếp hạng

Khi nào: từ màn kết quả bấm sang bảng xếp hạng. `LeaderboardView` cũng đọc lại mỗi lần màn được bật.

Gọi: `Leaderboard.GetTop(10)`.

```http
GET /api/leaderboard?limit=10
```

`200`:

```json
{
  "entries": [
    { "name": "CHIẾN BINH 01", "score": 10000 },
    { "name": "CHIẾN BINH 02", "score": 9500 }
  ]
}
```

Sắp điểm giảm dần. Nhiều người cùng điểm thì giữ thứ tự backend trả. Client vẽ tối đa 10 dòng. Hạng 1–3 dùng huy hiệu, từ hạng 4 hiện số.

Bấm chơi lại không gọi API. Ván mới bắt đầu điểm 0 và chỉ nộp khi ván đó kết thúc.

## 5. Tải lại dữ liệu

Khi nào: debug F1, không phải luồng người chơi.

Gọi: `AppServices.ReloadFromPersistent()`.

Với HTTP, gọi lại đúng ba API ở trên và thay cache:

1. `GET /api/questions`
2. Không cần login lại nếu session còn.
3. `GET /api/leaderboard?limit=10`

## File JSON local tương ứng

| API | File |
| --- | --- |
| `POST /api/auth/login` | `players.json` → `players[]` |
| `GET /api/questions` | `questions.json` → `questions[]` |
| `POST` và `GET /api/leaderboard` | `leaderboard.json` → `entries[]`, giữ 10 dòng |

Tài khoản mẫu trong `players.json`: `chienbinh` / `123456`, tên hiển thị `CHIẾN BINH 01`.

## Gắn HTTP sau này

Giữ nguyên `IPlayerRepository`, `IQuestionRepository`, `ILeaderboardRepository`. Viết class HTTP mới và gán vào `AppServices` thay ba class `LocalJson*`. `LoginView`, `BossSequence`, `GameFlow`, `LeaderboardView` không đổi.

Các method hiện đồng bộ. HTTP nên chạy request xong rồi trả kết quả đúng kiểu hiện tại: `bool` + `PlayerRecord`, `QuestionData[]`, `LeaderboardEntry[]`, `int rank`.
