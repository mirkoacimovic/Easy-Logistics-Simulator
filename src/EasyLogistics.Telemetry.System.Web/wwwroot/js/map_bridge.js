var map;
var markers = {};

window.mapBridge = {
    initMap: function (elementId) {
        // Center on Europe hubs
        map = L.map(elementId).setView([48.8566, 10.3522], 5);

        L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', {
            attribution: '&copy; OpenStreetMap contributors &copy; CARTO'
        }).addTo(map);
    },

    updateMarkers: function (trucks) {
        trucks.forEach(truck => {
            if (markers[truck.truckId]) {
                // Smoothly move existing marker
                markers[truck.truckId].setLatLng([truck.latitude, truck.longitude]);
                markers[truck.truckId].setTooltipContent(`Truck #${truck.truckId}<br>${truck.speed.toFixed(1)} km/h`);
            } else {
                // Create new marker
                markers[truck.truckId] = L.circleMarker([truck.latitude, truck.longitude], {
                    radius: 6,
                    fillColor: "#ff7800",
                    color: "#000",
                    weight: 1,
                    opacity: 1,
                    fillOpacity: 0.8
                }).addTo(map)
                    .bindTooltip(`Truck #${truck.truckId}`, { permanent: false, direction: 'top' });
            }
        });
    }
};
