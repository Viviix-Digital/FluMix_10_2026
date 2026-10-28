(function () {
  // Xóa popup cũ nếu còn
  const old = document.getElementById("flumix-fs-overlay");
  if (old) old.remove();

  const overlay = document.createElement("div");
  overlay.id = "flumix-fs-overlay";

  Object.assign(overlay.style, {
    position: "fixed",
    inset: "0",
    width: "100vw",
    height: "100vh",
    background: "rgba(0,0,0,0.5)",
    display: "flex",
    alignItems: "center",
    justifyContent: "center",
    zIndex: "99999",
  });

  const popup = document.createElement("div");

  Object.assign(popup.style, {
    background: "#fff",
    padding: "32px 24px",
    borderRadius: "8px",
    boxShadow: "0 2px 16px rgba(0,0,0,0.2)",
    textAlign: "center",
    minWidth: "320px",
    maxWidth: "90vw",
    fontFamily: "system-ui, sans-serif",
    color: "#111",
    boxSizing: "border-box",
  });

  // Nội dung thông báo
  const message = document.createElement("div");

  message.textContent =
    "Để trải nghiệm tốt hơn, vui lòng chọn chế độ màn hình ngang hoặc xoay ngang thiết bị.";

  Object.assign(message.style, {
    marginBottom: "24px",
    lineHeight: "1.5",
  });

  // Nút OK ở giữa
  const okButton = document.createElement("button");
  okButton.type = "button";
  okButton.textContent = "OK";

  Object.assign(okButton.style, {
    appearance: "none",
    border: "none",
    background: "#111",
    color: "#fff",
    padding: "12px 32px",
    borderRadius: "6px",
    fontSize: "16px",
    fontWeight: "600",
    cursor: "pointer",
    display: "block",
    margin: "0 auto",
    WebkitTapHighlightColor: "transparent",
  });

  // Hiển thị trạng thái xử lý
  const status = document.createElement("div");

  Object.assign(status.style, {
    marginTop: "16px",
    fontSize: "13px",
    color: "#555",
    minHeight: "1.2em",
  });

  async function requestFullscreen(el) {
    if (el.requestFullscreen) {
      return el.requestFullscreen();
    }

    if (el.webkitRequestFullscreen) {
      return el.webkitRequestFullscreen();
    }

    if (el.msRequestFullscreen) {
      return el.msRequestFullscreen();
    }

    throw new Error("Trình duyệt không hỗ trợ fullscreen.");
  }

  async function lockLandscape() {
    if (!screen.orientation || !screen.orientation.lock) {
      throw new Error("Trình duyệt không hỗ trợ khóa màn hình ngang.");
    }

    return screen.orientation.lock("landscape");
  }

  let busy = false;

  okButton.onclick = async () => {
    if (busy) return;
    busy = true;
    okButton.disabled = true;
    okButton.style.cursor = "default";
    okButton.style.opacity = "0.6";

    status.textContent = "Đang chuyển sang chế độ fullscreen...";

    try {
      await requestFullscreen(document.documentElement);

      status.textContent = "Fullscreen thành công. Đang yêu cầu xoay ngang...";
    } catch (e) {
      status.textContent =
        "Không thể bật fullscreen: " + (e && e.message ? e.message : e);

      busy = false;
      okButton.disabled = false;
      okButton.style.cursor = "pointer";
      okButton.style.opacity = "1";

      return;
    }

    try {
      await lockLandscape();

      status.textContent = "Đã bật fullscreen và khóa màn hình ngang.";
    } catch (e) {
      status.textContent =
        "Fullscreen đã bật, nhưng không thể tự xoay ngang. " +
        "Vui lòng xoay ngang thiết bị thủ công.";
    }

    setTimeout(() => {
      if (overlay.parentNode) {
        overlay.remove();
      }
    }, 1500);
  };

  overlay.addEventListener("click", (e) => {
    if (e.target === overlay) {
      e.stopPropagation();
    }
  });

  popup.appendChild(message);
  popup.appendChild(okButton);
  popup.appendChild(status);

  overlay.appendChild(popup);
  document.body.appendChild(overlay);
})();
