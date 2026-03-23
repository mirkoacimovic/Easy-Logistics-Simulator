var fuelChartInstance = null;

window.initChart = (labels, values) => {
    const ctx = document.getElementById('fuelChart');
    if (!ctx) return;

    if (fuelChartInstance) {
        fuelChartInstance.destroy();
    }

    fuelChartInstance = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [{
                label: 'Fuel Consumed (L)',
                data: values,
                backgroundColor: 'rgba(220, 53, 69, 0.5)', // Red for fuel
                borderColor: 'rgba(220, 53, 69, 1)',
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            animation: false,
            scales: { y: { beginAtZero: true } }
        }
    });
};

window.updateChart = (labels, values) => {
    if (fuelChartInstance) {
        fuelChartInstance.data.labels = labels;
        fuelChartInstance.data.datasets[0].data = values;
        fuelChartInstance.update('none'); // 'none' prevents glitchy animations on fast updates
    }
};