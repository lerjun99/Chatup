window.faceCamera = {
    stream: null,
    video: null,

    start: async function () {
        try {
            this.video = document.getElementById("video");
            if (!this.video) throw "Video element not found";

            // Request camera
            this.stream = await navigator.mediaDevices.getUserMedia({ video: true });
            this.video.srcObject = this.stream;

            // Wait for metadata to load
            await new Promise(resolve => {
                this.video.onloadedmetadata = resolve;
            });

            await this.video.play();
            console.log("Camera started successfully");
        } catch (err) {
            console.error("Camera start failed:", err);
            throw err;
        }
    },

    capture: function () {
        if (!this.video || this.video.videoWidth === 0 || this.video.videoHeight === 0) {
            throw "Video not ready";
        }

        // Create canvas same size as video
        const canvas = document.createElement("canvas");
        canvas.width = this.video.videoWidth;
        canvas.height = this.video.videoHeight;

        const ctx = canvas.getContext("2d");
        if (!ctx) throw "Failed to get 2D context";

        // Draw video frame into canvas
        ctx.drawImage(this.video, 0, 0, canvas.width, canvas.height);

        // Convert to PNG (safer for Base64) and strip the prefix
        const dataUrl = canvas.toDataURL("image/png"); // png avoids padding issues
        const base64 = dataUrl.split(",")[1]; // <-- REMOVE "data:image/png;base64,"

        console.log("Captured image length (Base64 only):", base64.length);
        return base64; // only the Base64 part
    },

    stop: function () {
        if (this.stream) {
            this.stream.getTracks().forEach(track => track.stop());
            this.stream = null;
            console.log("Camera stopped");
        }
    }
};
