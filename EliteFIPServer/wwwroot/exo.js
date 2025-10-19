let dataTable;
let allRows = []; // conserve tous les scans

async function loadScans() {
    try {
        const res = await fetch('/exo');
        const data = await res.json();
        allRows = []; // réinitialisation

        data.Systems.forEach(system => {
            const systemName = system.SystemName;
            const distance = system.Distance;

            Object.values(system.Scans).forEach(scanArray => {
                scanArray.forEach(scan => {
                    // Classe CSS distance
                    let distanceClass = '';
                    if (scan.Distance <= 200) distanceClass = 'low-distance';
                    else if (scan.Distance <= 500) distanceClass = 'medium-distance';
                    else distanceClass = 'high-distance';

                    // Classe CSS valeur
                    let valueClass = scan.Value >= 5_000_000 ? 'high-value' : '';

                    allRows.push({
                        rowClass: `${distanceClass} ${valueClass}`,
                        systemName: systemName,
                        distance: distance,
                        bodyName: scan.BodyName,
                        scanName: scan.Name,
                        value: scan.Value.toLocaleString() + (scan.MaxValue == null ? "" : "<br/>- " + scan.MaxValue.toLocaleString()),
                        scandist: scan.Distance,
                        seen: (scan.Seen == 0) ? "No" : "Yes",
                        planetClass: scan.PlanetClass,
                        atmosphere: scan.AtmosphereType,
                        raw: scan
                    });
                });
            });
        });

        applyFilters();

    } catch (err) {
        console.error('Erreur en chargeant les scans:', err);
    }
}

function applyFilters() {
    const maxDistance = parseFloat(document.getElementById('maxDistance').value) || Infinity;
    const minValue = parseFloat(document.getElementById('minValue').value) || 0;

    const filteredRows = allRows.filter(r => r.distance <= maxDistance && r.raw.Value >= minValue);

    if (!dataTable) {
        dataTable = $('#scans-table').DataTable({
            data: filteredRows,
            createdRow: function (row, data) {
                $(row).attr('class', data.rowClass);
            },
            columns: [
                { data: "systemName", title: "System" },
                { data: "distance", title: "Distance" },
                { data: "bodyName", title: "Body" },
                { data: "scanName", title: "Scan Name" },
                { data: "value", title: "Value" },
                { data: "scandist", title: "Scan Dist" },
                { data: "seen", title: "Seen" },
                { data: "planetClass", title: "Planet Class" },
                { data: "atmosphere", title: "Atmosphere" }
            ],
            order: [[1, 'asc']],
            pageLength: 25,
            lengthMenu: [10, 25, 50, 100],
            autoWidth: false,
            responsive: true
        });
    } else {
        dataTable.clear();
        dataTable.rows.add(filteredRows);
        dataTable.draw();
    }
}

// Filtre sur clic
document.getElementById('applyFilters').addEventListener('click', applyFilters);
// Filtre sur Enter
document.getElementById('maxDistance').addEventListener('keyup', e => { if (e.key === 'Enter') applyFilters(); });
document.getElementById('minValue').addEventListener('keyup', e => { if (e.key === 'Enter') applyFilters(); });

// Première charge
loadScans();

// Rafraîchissement automatique toutes les 5 secondes
setInterval(loadScans, 5000);
