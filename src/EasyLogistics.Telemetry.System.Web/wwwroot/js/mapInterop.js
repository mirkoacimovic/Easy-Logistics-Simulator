var map;
var markers = {};

window.mapInterop = {
    initialize: function (lat, lng) {
        // CLEANUP: Destroy the old map if it exists to free up the #map div
        if (map) {
            map.off();
            map.remove();
            map = null;
            markers = {};
            this._firstLoadDone = false;
        }

        const el = document.getElementById('map');
        if (!el) return;

        map = L.map('map', {
            zoomControl: true,
            preferCanvas: true
        }).setView([lat, lng], 13);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '© OpenStreetMap'
        }).addTo(map);

        console.log("🚚 Map Ready.");
    },

    updateTrucks: function (trucks) {
        if (!map || !trucks) return;

        trucks.forEach(t => {
            const content = `<div style='padding:10px'><b>Truck #${t.truckId}</b><br/>Speed: ${t.speed} km/h</div>`;
            if (markers[t.truckId]) {
                markers[t.truckId].setLatLng([t.latitude, t.longitude]).setPopupContent(content);
            } else {
                markers[t.truckId] = L.marker([t.latitude, t.longitude]).addTo(map).bindPopup(content);
            }
        });

        if (Object.keys(markers).length > 0 && !this._firstLoadDone) {
            const group = new L.featureGroup(Object.values(markers));
            map.fitBounds(group.getBounds().pad(0.1));
            this._firstLoadDone = true;
        }
    }
};