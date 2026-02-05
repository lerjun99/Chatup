(function () {
    // startFalling(containerId, shapesArray, spawnIntervalMs)
    window.startFalling = function (containerId, shapes, spawnInterval) {
        const container = document.getElementById(containerId);
        if (!container) return;
        if (!window._fallingIntervals) window._fallingIntervals = {};
        if (window._fallingIntervals[containerId]) {
            clearInterval(window._fallingIntervals[containerId]);
        }

        function spawn() {
            const wrap = document.createElement('div');
            wrap.className = 'fall-wrap';

            const el = document.createElement('span');
            el.className = 'fall';
            el.textContent = shapes && shapes.length ? shapes[Math.floor(Math.random() * shapes.length)] : '*';

            // random start position and size
            wrap.style.left = (Math.random() * 100) + 'vw';
            el.style.fontSize = (12 + Math.random() * 28) + 'px';

            // random durations (fall + drift)
            const fallDur = 3 + Math.random() * 6;      // 3s - 9s
            const driftDur = 2 + Math.random() * 4;     // 2s - 6s

            el.style.animationDuration = fallDur + 's';
            el.style.animationDelay = (Math.random() * 1.5) + 's';
            wrap.style.animationDuration = driftDur + 's';
            wrap.style.animationDelay = (Math.random() * 1.5) + 's';

            // build DOM
            wrap.appendChild(el);
            container.appendChild(wrap);

            // cleanup after animation
            setTimeout(() => {
                if (wrap.parentNode) wrap.parentNode.removeChild(wrap);
            }, (fallDur + 3) * 1000);
        }

        // spawn immediately then repeatedly
        spawn();
        const id = setInterval(spawn, spawnInterval || 300);
        window._fallingIntervals[containerId] = id;
    };

    window.stopFalling = function (containerId) {
        if (window._fallingIntervals && window._fallingIntervals[containerId]) {
            clearInterval(window._fallingIntervals[containerId]);
            delete window._fallingIntervals[containerId];
        }
    };
})();
