mergeInto(LibraryManager.library, {
  FluMix_RequestFullscreenLandscape: function () {
    // Direct fullscreen + landscape lock (no overlay). Call from Login/Register click.

    function lockLandscape() {
      try {
        if (screen.orientation && typeof screen.orientation.lock === "function") {
          screen.orientation.lock("landscape").catch(function () {});
        }
      } catch (e) {}
    }

    try {
      var target = document.documentElement;
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
