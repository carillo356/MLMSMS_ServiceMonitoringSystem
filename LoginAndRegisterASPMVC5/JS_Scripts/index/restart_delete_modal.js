//Restart and Delete Modal

// Action Button
const run = document.getElementById('btnRun');
const stop = document.getElementById('btnStop');
const restart = document.getElementById('btnRestart');
const del = document.getElementById('btnDelete');


run.addEventListener('click', () => {
    stop.style.display = "inline-block";
    restart.style.display = "inline-block";
    run.style.display = "none";
    del.style.display = "none";
});

stop.addEventListener('click', () => {
    stop.style.display = "none";
    restart.style.display = "none";
    run.style.display = "inline-block";
    del.style.display = "inline-block";
});

restart.addEventListener('click', () => {
    stop.style.display = "inline-block";
    restart.style.display = "inline-block";
    run.style.display = "none";
    del.style.display = "none";
});

// Show Alert Button when the restart button clicked and hide it.
function showModal(title, message) {
    var modal = document.getElementById('myModal');
    var modalTitle = document.getElementById('modal-title');
    var modalMessage = document.getElementById('modal-message');

    modal.style.display = "block";
    modalTitle.textContent = title;
    modalMessage.textContent = message;
}

function hideModal() {
    var modal = document.getElementById('myModal');
    modal.style.display = "none";
}
// Select the Yes and No buttons
var btnYes = document.getElementById('btnYes');
var btnNo = document.getElementById('btnNo');

// Add event listeners to the buttons
btnYes.addEventListener('click', btnYes_Click);
btnNo.addEventListener('click', btnNo_Click);

function btnYes_Click() {
    updateDateTime();
    hideModal();
}

function btnNo_Click() {
    hideModal();
}

// Show Alert Button when the delete button clicked and hide it.
function showdeleteModal(title, message) {
    var modal = document.getElementById('deleteModal');
    var modalTitle = document.getElementById('modal-title1');
    var modalMessage = document.getElementById('modal-message1');

    modal.style.display = "block";
    modalTitle.textContent = title;
    modalMessage.textContent = message;
}

function hidedeleteModal() {
    var modal = document.getElementById('deleteModal');
    modal.style.display = "none";
}
// Select the Yes and No buttons
var btnDe = document.getElementById('btnDel');
var btnCancel = document.getElementById('btnCancel');

// Add event listeners to the buttons
btnDe.addEventListener('click', btnDel_Click);
btnCancel.addEventListener('click', btnCancel_Click);

function btnDel_Click() {
    hidedeleteModal();
}

function btnCancel_Click() {
    hidedeleteModal();
}