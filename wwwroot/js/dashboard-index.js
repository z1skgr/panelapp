document.addEventListener('DOMContentLoaded', function () {
    let currentStatsGroup = 0;
    const statItems = document.querySelectorAll('.dashboard-stat-item');

    const totalStatsGroups = Math.max(
        ...Array.from(statItems).map(x => Number(x.dataset.statGroup || 0))
    ) + 1;


    const prevStatsBtn = document.getElementById('prevStatsBtn');
    const nextStatsBtn = document.getElementById('nextStatsBtn');
    const statsDots = document.getElementById('statsDots');

    function buildStatsDots() {
        if (!statsDots) return;

        statsDots.innerHTML = '';

        for (let i = 0; i < totalStatsGroups; i++) {
            const dot = document.createElement('button');
            dot.type = 'button';
            dot.className = 'dashboard-stat-dot';
            dot.dataset.group = i;

            dot.addEventListener('click', function () {
                currentStatsGroup = i;
                renderStatsGroup();
            });

            statsDots.appendChild(dot);
        }
    }

    function renderStatsGroup() {
        statItems.forEach(item => {
            const itemGroup = parseInt(item.dataset.statGroup || '0');

            item.classList.add('d-none');
            item.classList.remove('dashboard-stat-enter');

            if (itemGroup === currentStatsGroup) {
                item.classList.remove('d-none');

                window.setTimeout(() => {
                    item.classList.add('dashboard-stat-enter');
                }, 10);
            }
        });

        document.querySelectorAll('.dashboard-stat-dot').forEach(dot => {
            const dotGroup = parseInt(dot.dataset.group || '0');
            dot.classList.toggle('active', dotGroup === currentStatsGroup);
        });
    }

    if (prevStatsBtn) {
        prevStatsBtn.addEventListener('click', function () {
            currentStatsGroup = (currentStatsGroup - 1 + totalStatsGroups) % totalStatsGroups;
            renderStatsGroup();
        });
    }

    if (nextStatsBtn) {
        nextStatsBtn.addEventListener('click', function () {
            currentStatsGroup = (currentStatsGroup + 1) % totalStatsGroups;
            renderStatsGroup();
        });
    }

    buildStatsDots();
    renderStatsGroup();

    const recentPanelsFilter = document.getElementById('recentPanelsFilter');
    const recentRows = document.querySelectorAll('.recent-panel-row');
    const recentEmptyFilter = document.getElementById('recentPanelsEmptyFilter');

    if (recentPanelsFilter) {
        recentPanelsFilter.addEventListener('change', function () {
            const selectedStatus = this.value;
            let visibleRows = 0;

            recentRows.forEach(row => {
                const rowStatus = row.dataset.status;

                if (selectedStatus === 'all' || rowStatus === selectedStatus) {
                    row.classList.remove('d-none');
                    visibleRows++;
                } else {
                    row.classList.add('d-none');
                }
            });

            if (recentEmptyFilter) {
                recentEmptyFilter.classList.toggle('d-none', visibleRows > 0);
            }
        });
    }

    const allChartData = window.dashboardChartData || {
        panels: [],
        offers: []
    };
    const chartCanvas = document.getElementById('panelsChart');
    const emptyState = document.getElementById('chartEmptyState');
    const chartSummary = document.getElementById('chartSummary');
    const chartTopSelect = document.getElementById('chartTopSelect');
    const chartModeInputs = document.querySelectorAll('input[name="chartMode"]');

    const chartEntityToggle = document.getElementById('chartEntityToggle');
    const chartTitle = document.getElementById('chartTitle');
    const chartSubtitle = document.getElementById('chartSubtitle');

    if (!chartCanvas) {
        if (emptyState) {
            emptyState.classList.remove('d-none');
        }

        return;
    }

    let chartInstance = null;

    function isDarkMode() {
        return document.body.classList.contains('dark-mode');
    }

    function getSelectedMode() {
        const selected = document.querySelector('input[name="chartMode"]:checked');
        return selected ? selected.value : 'stacked';
    }

    function getTopN() {
        const value = parseInt(chartTopSelect?.value || '8');
        return isNaN(value) ? 8 : value;
    }

    function buildChartPayload(sourceData) {
        const topN = getTopN();
        const mode = getSelectedMode();
        const sliced = (sourceData || []).slice(0, topN);

        return {
            mode,
            labels: sliced.map(x => x.customer),
            underConstruction: sliced.map(x => x.underConstruction),
            completed: sliced.map(x => x.completed),
            cancelled: sliced.map(x => x.cancelled),
            totals: {
                underConstruction: sliced.reduce((sum, x) => sum + x.underConstruction, 0),
                completed: sliced.reduce((sum, x) => sum + x.completed, 0),
                cancelled: sliced.reduce((sum, x) => sum + x.cancelled, 0)
            }
        };
    }

    function renderSummary(data) {
        if (!chartSummary) return;

        chartSummary.innerHTML = `
            <span class="chart-pill chart-pill-warning">
                <i class="bi bi-tools me-1"></i>Υπό Κατασκευή: ${data.totals.underConstruction}
            </span>
            <span class="chart-pill chart-pill-success">
                <i class="bi bi-check-circle me-1"></i>Ολοκληρωμένοι: ${data.totals.completed}
            </span>
            <span class="chart-pill chart-pill-secondary">
                <i class="bi bi-x-circle me-1"></i>Ακυρωμένοι: ${data.totals.cancelled}
            </span>
        `;
    }

    function renderChart() {
        const sourceData =
            activeChartEntity === "offers"
                ? allChartData.offers
                : allChartData.panels;
        const data = buildChartPayload(sourceData);
        const dark = isDarkMode();


        if (activeChartEntity === "offers") {

            chartTitle.textContent = "Προσφορές ανά Πελάτη";

            chartSubtitle.textContent =
                "Σύγκριση προσφορών ανά πελάτη και κατάσταση";
        }
        else {

            chartTitle.textContent = "Πίνακες ανά Πελάτη";

            chartSubtitle.textContent =
                "Σύγκριση πινάκων ανά πελάτη και κατάσταση";
        }

        if (!data.labels.length) {
            chartCanvas.style.display = 'none';

            if (emptyState) {
                emptyState.classList.remove('d-none');
            }

            return;
        }

        chartCanvas.style.display = '';

        if (emptyState) {
            emptyState.classList.add('d-none');
        }

        renderSummary(data);

        if (chartInstance) {
            chartInstance.destroy();
        }

        const isStacked = data.mode === 'stacked';
        const ctx = chartCanvas.getContext('2d');

        function makeGradient(colorTop, colorBottom) {
            const gradient = ctx.createLinearGradient(0, 0, 0, chartCanvas.height || 360);
            gradient.addColorStop(0, colorTop);
            gradient.addColorStop(1, colorBottom);
            return gradient;
        }

        const underGradient = dark
            ? makeGradient('rgba(251, 191, 36, 0.95)', 'rgba(245, 158, 11, 0.38)')
            : makeGradient('rgba(245, 158, 11, 0.95)', 'rgba(245, 158, 11, 0.30)');

        const completedGradient = dark
            ? makeGradient('rgba(74, 222, 128, 0.95)', 'rgba(34, 197, 94, 0.36)')
            : makeGradient('rgba(34, 197, 94, 0.95)', 'rgba(34, 197, 94, 0.30)');

        const cancelledGradient = dark
            ? makeGradient('rgba(203, 213, 225, 0.90)', 'rgba(148, 163, 184, 0.30)')
            : makeGradient('rgba(148, 163, 184, 0.90)', 'rgba(148, 163, 184, 0.26)');

        const firstLabel =
            activeChartEntity === "offers"
                ? "Draft"
                : "Υπό Κατασκευή";

        const secondLabel =
            activeChartEntity === "offers"
                ? "Accepted"
                : "Ολοκληρωμένοι";

        const thirdLabel =
            activeChartEntity === "offers"
                ? "Cancelled"
                : "Ακυρωμένοι";

        chartInstance = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: data.labels,
                datasets: [
                    {
                        label: firstLabel,
                        data: data.underConstruction,
                        backgroundColor: underGradient,
                        borderColor: dark ? '#fbbf24' : '#d97706',
                        borderWidth: 1,
                        borderRadius: 4,
                        borderSkipped: false,
                        stack: isStacked ? 'panels' : undefined,
                        categoryPercentage: 0.55,
                        barPercentage: 0.75,
                        maxBarThickness: 28,
                        animations: {
                            y: {
                                from: 500
                            }
                        }
                    },
                    {
                        label: secondLabel,
                        data: data.completed,
                        backgroundColor: completedGradient,
                        borderColor: dark ? '#4ade80' : '#16a34a',
                        borderWidth: 1,
                        borderRadius: 4,
                        borderSkipped: false,
                        stack: isStacked ? 'panels' : undefined,
                        categoryPercentage: 0.55,
                        barPercentage: 0.75,
                        maxBarThickness: 28,
                        animations: {
                            y: {
                                from: 500
                            }
                        }
                    },
                    {
                        label: thirdLabel,
                        data: data.cancelled,
                        backgroundColor: cancelledGradient,
                        borderColor: dark ? '#cbd5e1' : '#64748b',
                        borderWidth: 1,
                        borderRadius: 4,
                        borderSkipped: false,
                        stack: isStacked ? 'panels' : undefined,
                        categoryPercentage: 0.55,
                        barPercentage: 0.75,
                        maxBarThickness: 28,
                        animations: {
                            y: {
                                from: 500
                            }
                        }
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                layout: {
                    padding: {
                        top: 10,
                        right: 10,
                        bottom: 0,
                        left: 10
                    }
                },
                interaction: {
                    mode: 'index',
                    intersect: false
                },
                plugins: {
                    legend: {
                        position: 'top',
                        labels: {
                            usePointStyle: true,
                            pointStyle: 'circle',
                            color: dark ? '#e2e8f0' : '#334155',
                            padding: 18,
                            font: {
                                size: 12,
                                weight: '600'
                            }
                        }
                    },
                    tooltip: {
                        backgroundColor: dark ? 'rgba(15, 23, 42, 0.96)' : 'rgba(15, 23, 42, 0.92)',
                        titleColor: '#fff',
                        bodyColor: '#fff',
                        padding: 13,
                        cornerRadius: 14,
                        displayColors: true,
                        callbacks: {
                            footer: function (tooltipItems) {
                                const total = tooltipItems.reduce((sum, item) => sum + item.raw, 0);
                                return `Σύνολο: ${total} ${activeChartEntity === "offers"
                                        ? "προσφορές"
                                        : "πίνακες"
                                    }`;
                            },
                            label: function (context) {
                                return `${context.dataset.label}: ${context.raw}`;
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        stacked: isStacked,
                        grid: {
                            display: false,
                            drawBorder: false
                        },
                        ticks: {
                            color: dark ? '#cbd5e1' : '#475569',
                            font: {
                                size: 12,
                                weight: '600'
                            },
                            maxRotation: 0,
                            minRotation: 0
                        }
                    },
                    y: {
                        stacked: isStacked,
                        beginAtZero: true,
                        grace: '10%',
                        ticks: {
                            precision: 0,
                            stepSize: 1,
                            color: dark ? '#cbd5e1' : '#64748b'
                        },
                        grid: {
                            color: dark ? 'rgba(148, 163, 184, 0.14)' : 'rgba(148, 163, 184, 0.18)',
                            drawBorder: false
                        }
                    }
                },
                animation: {
                    duration: 700,
                    easing: 'easeOutQuart',
                    delay: function (context) {
                        if (context.type !== 'data') return 0;

                        return context.dataIndex * 35;
                    }
                }
            }
        });
    }



    let activeChartEntity = "panels";

    chartEntityToggle?.addEventListener("click", () => {

        activeChartEntity =
            activeChartEntity === "panels"
                ? "offers"
                : "panels";

        chartEntityToggle.dataset.entity = activeChartEntity;

        chartEntityToggle.innerHTML =
            activeChartEntity === "panels"
                ? '<i class="bi bi-arrow-repeat me-1"></i> Προβολή Προσφορών'
                : '<i class="bi bi-arrow-repeat me-1"></i> Προβολή Πινάκων';

        chartCanvas.animate(
            [
                {
                    opacity: 0.6,
                    transform: 'scale(.98)'
                },
                {
                    opacity: 1,
                    transform: 'scale(1)'
                }
            ],
            {
                duration: 300,
                easing: 'ease-out'
            }
        );

        renderChart();
    });

    function getChartColors() {
        return {
            first: '#f59e0b',
            second: '#22c55e',
            third: '#94a3b8'
        };
    }

    if (chartTopSelect) {
        chartTopSelect.addEventListener('change', renderChart);
    }

    chartModeInputs.forEach(input => {
        input.addEventListener('change', renderChart);
    });

    const themeObserver = new MutationObserver(function () {
        renderChart();
    });

    themeObserver.observe(document.body, {
        attributes: true,
        attributeFilter: ['class']
    });
    renderChart();
});