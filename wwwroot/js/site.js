// Landing page: pricing toggle
const pricingToggle = document.getElementById('pricingToggle');
if (pricingToggle) {
    pricingToggle.addEventListener('change', function() {
        document.querySelectorAll('.price-monthly').forEach(el => el.classList.toggle('hidden', this.checked));
        document.querySelectorAll('.price-yearly').forEach(el => el.classList.toggle('hidden', !this.checked));
    });
}

// Landing page: mobile nav
const mobileBtn = document.getElementById('mobileMenuBtn');
const mobileMenu = document.getElementById('mobileMenu');
if (mobileBtn && mobileMenu) {
    mobileBtn.addEventListener('click', function() { mobileMenu.classList.toggle('open'); });
}

// Demo account autofill
function fillDemo(email, pass) {
    const emailEl = document.getElementById('Email') || document.querySelector('[name=Email]');
    const passEl = document.getElementById('Password') || document.querySelector('[name=Password]');
    if (emailEl) emailEl.value = email;
    if (passEl) passEl.value = pass;
}

// Password visibility toggle
document.querySelectorAll('.input-toggle').forEach(btn => {
    btn.addEventListener('click', function() {
        const input = this.closest('.input-wrapper').querySelector('input');
        if (!input) return;
        input.type = input.type === 'password' ? 'text' : 'password';
        this.querySelector('i').classList.toggle('fa-eye');
        this.querySelector('i').classList.toggle('fa-eye-slash');
    });
});
