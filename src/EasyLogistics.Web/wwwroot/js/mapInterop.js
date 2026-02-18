// js/mapInterop.js

let map;
let markers = {};

/**
 * Initializes the Leaflet map.
 * Called from Blazor: OnAfterRenderAsync
 */
window.initFleetMap = (lat, lng) => {
    console.log("Initializing Map at:", lat, lng);

    // If map exists, destroy it to allow a clean re-init on navigation
    if (map !== undefined && map !== null) {
        map.remove();
        markers = {};
    }

    const container = document.getElementById('map-container');
    if (!container) {
        console.error("Map container not found!");
        return;
    }

    // Initialize Leaflet map
    map = L.map('map-container').setView([lat, lng], 12);

    // Use OpenStreetMap tiles
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '© OpenStreetMap contributors'
    }).addTo(map);
};

/**
 * Updates or creates markers for the fleet.
 * Called from Blazor: HandleFleetUpdate
 */
window.updateMarkers = (trucks) => {
    if (!map) return;

    trucks.forEach(truck => {
        // Safety check for empty positions
        if (!truck.position || !truck.position.includes(',')) return;

        // Convert "Lat, Lng" string back to numeric array [Lat, Lng]
        const parts = truck.position.split(',');
        const lat = parseFloat(parts[0].trim());
        const lng = parseFloat(parts[1].trim());

        // Skip if coordinates aren't valid numbers
        if (isNaN(lat) || isNaN(lng)) return;

        const coords = [lat, lng];

        if (!markers[truck.id]) {
            // Create a new marker if it doesn't exist
            markers[truck.id] = L.marker(coords).addTo(map)
                .bindTooltip(`Truck ${truck.id} - ${truck.speedDisplay}`, {
                    permanent: false,
                    direction: 'top'
                });
        } else {
            // Move existing marker smoothly
            markers[truck.id].setLatLng(coords);
            markers[truck.id].setTooltipContent(`Truck ${truck.id} - ${truck.speedDisplay}`);
        }

        // Visual speeding indicator (Red hue shift for speeding trucks)
        const markerElement = markers[truck.id].getElement();
        if (markerElement) {
            if (truck.status === "Speeding") {
                markerElement.style.filter = "hue-rotate(150deg) brightness(1.2) saturate(2)";
            } else {
                markerElement.style.filter = "none";
            }
        }
    });

    // Cleanup: If a truck is no longer in the list, remove its marker
    const currentIds = trucks.map(t => t.id.toString());
    Object.keys(markers).forEach(id => {
        if (!currentIds.includes(id)) {
            map.removeLayer(markers[id]);
            delete markers[id];
        }
    });
};