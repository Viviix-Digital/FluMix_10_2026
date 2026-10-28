mergeInto(LibraryManager.library, {
  FluMix_RequestFullscreenLandscape: function () {
    var canvas =
      document.querySelector("#unity-canvas") ||
      document.querySelector("canvas");
    var target = canvas || document.documentElement;

    function lockLandscape() {
      try {
        if (screen.orientation && typeof screen.orientation.lock === "function") {
          screen.orientation.lock("landscape").catch(function () {});
        }
      } catch (e) {}
    }

    try {
      var req =
        target.requestFullscreen ||
        target.webkitRequestFullscreen ||
        target.mozRequestFullScreen ||
        target.msRequestFullscreen;

      if (!req) {
        lockLandscape();
        return;
      }

      var result = req.call(target);
      if (result && typeof result.then === "function") {
        result.then(lockLandscape).catch(function () {
          lockLandscape();
        });
      } else {
        lockLandscape();
      }
    } catch (e) {
      lockLandscape();
    }
  },
});
