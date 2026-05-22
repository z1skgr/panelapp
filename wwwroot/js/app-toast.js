window.showToast = function (type, message) {

    const container = document.getElementById('appToastContainer');

    if (!container) return;

    const toast = document.createElement('div');

    toast.className = `app-toast ${type}`;

    let icon = 'bi-info-circle';

    if (type === 'success')
        icon = 'bi-check-circle';

    if (type === 'error')
        icon = 'bi-x-circle';

    if (type === 'warning')
        icon = 'bi-exclamation-triangle';

    toast.innerHTML = `
        <div class="app-toast-content">
            <i class="bi ${icon}"></i>
            <span>${message}</span>
        </div>
    `;

    container.appendChild(toast);

    requestAnimationFrame(() => {
        toast.classList.add('show');
    });

    setTimeout(() => {

        toast.classList.remove('show');

        setTimeout(() => {
            toast.remove();
        }, 200);

    }, 3200);
};