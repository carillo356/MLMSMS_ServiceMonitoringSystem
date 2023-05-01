// Tooltips for setNotification
var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'))
var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
    return new bootstrap.Tooltip(tooltipTriggerEl)
})

$(document).ready(function () {
    RealTimeUsersTable();
});

const togglePassword = document.getElementById("togglePassword");
const password = document.getElementById("password");

togglePassword.addEventListener("change", function () {
    if (togglePassword.checked) {
        password.type = "text";
    } else {
        password.type = "password";
    }
});

// Show Modal
var myModal = new bootstrap.Modal(document.getElementById('signup-modal'));
myModal.show();