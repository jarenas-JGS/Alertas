document.getElementById('togglePwd')?.addEventListener('click', function () {
    const inp = document.getElementById('loginPassword');
    if (!inp) return;

    const icon = this.querySelector('i');

    if (inp.type === 'password') {
        inp.type = 'text';
        icon?.classList.remove('bi-eye');
        icon?.classList.add('bi-eye-slash');
    } else {
        inp.type = 'password';
        icon?.classList.remove('bi-eye-slash');
        icon?.classList.add('bi-eye');
    }
});