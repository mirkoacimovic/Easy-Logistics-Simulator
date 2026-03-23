//window.mapInterop = {
//    map: null,
//    markers: {},

//    initialize: function (lat, lon) {
//        console.info("📍 [MAP] Hard Resetting JS Context...");

//        // 1. FULL CLEANUP: Kill the old Leaflet instance
//        if (this.map) {
//            try {
//                this.map.off();
//                this.map.remove();
//            } catch (e) {
//                console.warn("📍 [MAP] Cleanup warning:", e);
//            }
//            this.map = null;
//        }

//        // 2. CACHE WIPE: Clear the markers object
//        // Without this, the script thinks markers exist on the new map when they don't.
//        this.markers = {};

//        // 3. DOM SYNC: Small timeout to ensure Blazor has rendered the #map div
//        setTimeout(() => {
//            const container = document.getElementById('map');
//            if (!container) {
//                console.error("📍 [MAP] DOM ERROR: #map div is missing!");
//                return;
//            }

//            // Create new instance
//            this.map = L.map('map', {
//                zoomControl: false,
//                attributionControl: true
//            }).setView([lat, lon], 13);

//            // 4. ORB FIX: crossOrigin: true prevents the browser from blocking tiles
//            L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', {
//                attribution: '&copy; OpenStreetMap contributors &copy; CARTO',
//                crossOrigin: true
//            }).addTo(this.map);

//            this.map.invalidateSize();
//            console.info("📍 [MAP] New instance locked and loaded.");
//        }, 200);
//    },

//    updateTrucks: function (trucks) {
//        // Guard clause: Don't try to draw if initialize() hasn't finished
//        if (!this.map || !trucks) {
//            console.warn("📍 [MAP] Update ignored: Map or data not ready.");
//            return;
//        }

//        console.info(`📍 [MAP] Drawing ${trucks.length} units.`);

//        trucks.forEach(truck => {
//            const id = truck.truckId.toString();
//            const pos = [truck.latitude, truck.longitude];

//            // If marker exists on THIS specific map instance, move it
//            if (this.markers[id]) {
//                this.markers[id].setLatLng(pos);
//            }
//            // Otherwise, create it for the first time on this map
//            else {
//                const marker = L.marker(pos, {
//                    icon: L.divIcon({
//                        className: 'truck-icon-container',
//                        html: `<div class="truck-marker-glow">🚛</div>`,
//                        iconSize: [30, 30]
//                    })
//                }).addTo(this.map);

//                this.markers[id] = marker;
//            }
//        });
//    }
//};

window.mapInterop = {
    map: null,
    markers: {},

    initialize: function (lat, lon) {
        if (this.map) {
            this.map.remove();
            this.markers = {};
        }

        this.map = L.map('map', {
            zoomControl: false,
            attributionControl: false
        }).setView([lat, lon], 13);

        L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', {
            maxZoom: 19
        }).addTo(this.map);

        console.info("📍 [MAP] Engine Initialized at Belgrade Center.");
    },

    updateTrucks: function (trucks) {
        if (!this.map || !trucks) return;

        trucks.forEach(truck => {
            const id = (truck.truckId || truck.TruckId).toString();
            const lat = truck.latitude || truck.Latitude;
            const lon = truck.longitude || truck.Longitude;
            const speed = truck.speed || truck.Speed;
            const driver = truck.driverName || truck.DriverName || "Unknown";
            const aiStatus = truck.aiStatus || truck.AiStatus || "NOMINAL";

            if (lat === undefined || lon === undefined) return;

            const pos = [lat, lon];
            const isSpeeding = speed > 85;

            if (this.markers[id]) {
                // Update Position (CSS handles the smooth slide)
                this.markers[id].setLatLng(pos);

                // Update Tooltip Content dynamically
                this.markers[id].getTooltip().setContent(
                    `<b>${driver}</b><br/>Speed: ${speed.toFixed(1)} km/h<br/>Status: ${aiStatus}`
                );

                // Update Icon Class if speeding
                const iconDiv = this.markers[id].getElement().querySelector('.truck-marker-glow');
                if (isSpeeding) iconDiv.classList.add('truck-speeding');
                else iconDiv.classList.remove('truck-speeding');

            } else {
                // Create New Marker
                const marker = L.marker(pos, {
                    icon: L.divIcon({
                        className: 'truck-icon-container',
                        html: `<div class="truck-marker-glow ${isSpeeding ? 'truck-speeding' : ''}">🚛</div>`,
                        iconSize: [30, 30]
                    })
                }).addTo(this.map);

                // Attach Tooltip
                marker.bindTooltip(`<b>${driver}</b><br/>Speed: ${speed.toFixed(1)} km/h`, {
                    permanent: false,
                    direction: 'top'
                });

                this.markers[id] = marker;
            }
        });
    }
};