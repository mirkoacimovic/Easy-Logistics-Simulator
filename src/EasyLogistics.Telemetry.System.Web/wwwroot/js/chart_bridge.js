// Global reference to the chart instance to allow updates
var fuelChartInstance = null;

window.initChart = (labels, values) => {
    const ctx = document.getElementById('fuelChart');
    if (!ctx) {
        console.error("Canvas element 'fuelChart' not found!");
        return;
    }

    // Destroy existing chart if it exists (prevents memory leaks/ghosting)
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
                backgroundColor: 'rgba(13, 110, 253, 0.5)',
                borderColor: 'rgba(13, 110, 253, 1)',
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            animation: { duration: 0 }, // Disable for real-time performance
            scales: {
                y: { beginAtZero: true }
            }
        }
    });
};

window.updateChart = (labels, values) => {
    if (fuelChartInstance) {
        fuelChartInstance.data.labels = labels;
        fuelChartInstance.data.datasets[0].data = values;
        fuelChartInstance.update();
    }
};