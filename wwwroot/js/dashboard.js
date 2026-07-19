// User dropdown toggle
const userBtn = document.getElementById('userDropdownBtn');
const userDrop = document.getElementById('userDropdown');
if (userBtn && userDrop) {
    userBtn.addEventListener('click', function(e) {
        e.stopPropagation();
        userDrop.classList.toggle('open');
    });
    document.addEventListener('click', function() {
        userDrop.classList.remove('open');
    });
}

// Mobile sidebar toggle
const menuToggle = document.getElementById('menuToggle');
const sidebar = document.getElementById('sidebar');
if (menuToggle && sidebar) {
    menuToggle.addEventListener('click', function() {
        sidebar.classList.toggle('open');
    });
}

// Desktop sidebar toggle
const sidebarToggle = document.getElementById('sidebarToggle');
const mainWrapper = document.getElementById('mainWrapper');
const sidebarLogo = document.getElementById('sidebarLogo');

if (sidebarToggle && sidebar && mainWrapper) {
    sidebarToggle.addEventListener('click', function() {
        sidebar.classList.toggle('collapsed');
        mainWrapper.classList.toggle('sidebar-collapsed');
        
        // Hide logo when collapsed
        if (sidebar.classList.contains('collapsed')) {
            if (sidebarLogo) sidebarLogo.style.display = 'none';
        } else {
            if (sidebarLogo) sidebarLogo.style.display = 'block';
        }
    });
}

// Auto-dismiss toast after 4s
const toast = document.getElementById('toastAlert');
if (toast) { setTimeout(() => { toast.style.opacity = '0'; toast.style.transform = 'translateX(100px)'; toast.style.transition = '.3s'; setTimeout(() => toast.remove(), 300); }, 4000); }
