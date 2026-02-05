window.activityTracker = {
    initialize: function (dotNetHelper) {
        let lastActivityTime = new Date();

        const update = () => {
            lastActivityTime = new Date();
            dotNetHelper.invokeMethodAsync("UpdateActivity");
        };

        ['mousemove', 'keydown', 'scroll', 'click'].forEach(evt => {
            document.addEventListener(evt, update);
        });

        // Optional: log for debugging
        console.log("✅ activityTracker initialized");
    }
};
