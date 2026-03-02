window.mapInterop = {
    _map: null,
    _markers: new Map(),
    _firstLoadDone: false,

    initialize: function (lat, lng) {
        if (this._map) { this._map.remove(); this._markers.clear(); }

        this._map = L.map('map', {
            zoomControl: false,
            attributionControl: false,
            preferCanvas: true
        }).setView([lat, lng], 5);

        L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png').addTo(this._map);
        setTimeout(() => { this._map.invalidateSize(); }, 400);
    },

    updateTrucks: function (trucks) {
        if (!this._map || !trucks) return;
        const bounds = [];

        trucks.forEach(t => {
            const id = t.truckId;
            const color = t.speed > 5 ? '%2328a745' : '%23ffc107';
            const truckSvg = `data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 640 512'><path fill='${color}' d='M624 352h-16V243.9c0-12.7-5.1-24.9-14.1-33.9L494 110.1c-9-9-21.2-14.1-33.9-14.1H416V48c0-26.5-21.5-48-48-48H48C21.5 0 0 21.5 0 48v320c0 26.5 21.5 48 48 48h16c0 53 43 96 96 96s96-43 96-96h128c0 53 43 96 96 96s96-43 96-96h48c8.8 0 16-7.2 16-16v-32c0-8.8-7.2-16-16-16zM160 464c-26.5 0-48-21.5-48-48s21.5-48 48-48 48 21.5 48 48-21.5 48-48 48zm320 0c-26.5 0-48-21.5-48-48s21.5-48 48-48 48 21.5 48 48-21.5 48-48 48zm80-208H416V144h44.1l99.9 99.9V256z'/></svg>`;

            const tooltipHtml = `
                <div style="padding: 5px; min-width: 120px;">
                    <b style="color: #0d6efd">TRK-${String(id).padStart(3, '0')}</b><br/>
                    <small>${t.routeName}</small><br/>
                    <span style="font-family: monospace">${t.speedDisplay}</span>
                </div>`;

            if (this._markers.has(id)) {
                const m = this._markers.get(id);
                m.setLatLng([t.latitude, t.longitude]).setIcon(L.icon({ iconUrl: truckSvg, iconSize: [24, 24] }));
                m.setTooltipContent(tooltipHtml);
            } else {
                const m = L.marker([t.latitude, t.longitude], {
                    icon: L.icon({ iconUrl: truckSvg, iconSize: [24, 24] })
                }).addTo(this._map).bindTooltip(tooltipHtml, { sticky: true });
                this._markers.set(id, m);
            }
            bounds.push([t.latitude, t.longitude]);
        });

        if (!this._firstLoadDone && bounds.length > 0) {
            this._map.fitBounds(bounds, { padding: [50, 50] });
            this._firstLoadDone = true;
        }
    }
};