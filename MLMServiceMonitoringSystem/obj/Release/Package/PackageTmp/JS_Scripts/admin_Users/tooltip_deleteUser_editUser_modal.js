// Tooltip, DeleteUser & EditUser
var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'))
var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
    return new bootstrap.Tooltip(tooltipTriggerEl)
})

function DeleteUser(IdUser, FirstName, LastName, command) {
    /*Show the confirmation box here*/
    var modal = document.getElementById("deleteUsers-modal");
    var modalTitle = document.getElementById("modal-title");
    var modalMessage = document.getElementById("modal-message");
    var btnYes = document.getElementById("btnYes");
    var btnNo = document.getElementById("btnNo");

    modal.style.display = "block";
    modalTitle.innerHTML = "<i class='bi bi-exclamation-triangle-fill text-warning'></i> System Message";
    modalMessage.innerHTML = "Are you sure you want to " + command.toUpperCase() + " User" + " '" + FirstName + " " + LastName + "'?";
    btnYes.onclick = function () {
        $.ajax({
            type: 'POST',
            url: '/Home/DeleteUser',
            data: { IdUser: IdUser },
            success: function () {
                Synch();

                var commandText = command.toUpperCase();
                if (command === "delete") {
                    commandText = "DELETED";
                }

                var toast = new bootstrap.Toast(document.getElementById('liveToast'));
                var toastMessage = "You " + commandText + " the user  " + "' " + FirstName + " " + LastName + " '" + ".";
                document.querySelector('.toast-body').innerHTML = toastMessage;

                if (commandText === "DELETED") {
                    // set background color to red if commandText is Stopped
                    toast._element.classList.remove("bg-success");
                    toast._element.classList.add("bg-danger");
                }

                toast.show();

                setTimeout(function () {
                    toast.dispose();
                }, 4000);
            },
            error: function (xhr, status, error) {
                alert(error);
            }
        });
        modal.style.display = "none";
    }
    btnNo.onclick = function () {
        modal.style.display = "none";
    }
}

function editUser(IdUser, firstName, lastName, email, isAdmin) {
    // Set the values of the input fields in the modal
    $('#idUser').val(IdUser);
    $('#firstName').val(firstName);
    $('#lastName').val(lastName);
    $('#email').val(email);

    // Check the appropriate checkbox button
    $('#isAdmin').prop('checked', isAdmin === "true");
    $('#isAdmin').prop('unchecked', isAdmin === "false");

}

// Show Modal
var myModal = new bootstrap.Modal(document.getElementById('signup-modal'));
myModal.show();