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
                        bodyName: systemName + " " + scan.BodyName + "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;(" + (scan.BioSignalCount < 0 ? "Not scanned" : `${scan.BioSignalCount} signal${(scan.BioSignalCount > 1 ? "s" : "")}`) + ')',
/*                        systemName: systemName,*/
                        distance: distance,
                        scanName: scan.Name,
                        value: scan.Value.toLocaleString() + (scan.MaxValue == null ? "" : "<br/>- " + scan.MaxValue.toLocaleString()),
                        scandist: scan.Distance,
                        seen: (scan.Seen == 0) ? "" : "🏳️",
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
    const groupColumn = 0;
    if (!dataTable) {
        dataTable = $('#scans-table').DataTable({
            data: filteredRows,
            createdRow: function (row, data) {
                $(row).attr('class', data.rowClass);
            },
            columnDefs: [{ visible: false, targets: 0 }, { visible: false, targets: 1 }],
            
            columns: [
                { data: "bodyName", title: "Body" },
                /*{ data: "systemName", title: "System" },*/
                { data: "distance", title: "Distance" },
                { data: "scanName", title: "Planet / Species name" },
                { data: "seen", title: "Distance / Seen" },
                { data: "value", title: "Planet class / Value" },
                { data: "scandist", title: "Atmosphere / Min dist" },
                /*{ data: "planetClass", title: "Planet Class" },
                { data: "atmosphere", title: "Atmosphere" }*/
            ],
            order: [[1, 'asc']],
            pageLength: 25,
            lengthMenu: [10, 25, 50, 100],
            autoWidth: false,
            responsive: true,
            drawCallback: function (settings) {
                var api = this.api();
                var rows = api.rows({ page: 'current' }).nodes();
                var last = null;

                api.column(groupColumn, { page: 'current' })
                    .data()
                    .each(function (group, i) {
                        if (last !== group) {
                            var rowData = api.row(rows[i]).data();

                            // Par exemple, accéder à Distance (ou rowData.distance)
                            var distance = rowData?.distance ?? '';
                            var planetClass = rowData?.planetClass ?? '';
                            var atmosphere = rowData?.atmosphere ?? '';

                            $(rows)
                                .eq(i)
                                .before(
                                    `<tr class="group">
                                        <td><strong>${group}</strong></td>
                                        <td>${distance} LY</td>
                                        <td>${planetClass}</td>
                                        <td>${atmosphere}</td>
                                    </tr>`
                                );

                            last = group;
                        }
                    });
            }
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
