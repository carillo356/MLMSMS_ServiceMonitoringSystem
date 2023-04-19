/* Add and Remove Service*/
function AddService(serviceName) {
    $.ajax({
        url: "/Home/AddService",
        type: "POST",
        data: { serviceName: serviceName },
        success: function () {
            var $servicesTable = $('#servicesTable');
            $servicesTable.find('tbody').prepend($newRows);
        },
        error: function (xhr, textStatus, errorThrown) {
            alert('Error adding service');
        }
    }).then(function () {
        SynchServiceTB();
    }).catch(function (error) {
        console.error('Failed to get the total number of monitored services.', error);
    });
}

function RemoveAddedService1(serviceName) {
    /*Show the confirmation box here*/
    $.ajax({
        url: "/Home/DeleteAddedService",
        type: "POST",
        data: { serviceName: serviceName },
        success: function () {
            ServicesInMonitor();
        },
        error: function (xhr, textStatus, errorThrown) {
            alert("Failed to delete " + serviceName);
        }
    }).then(function () {
        SynchServiceTB();
    });
}

function RemoveAddedService(serviceName, command) {
    /*Show the confirmation box here*/
    var modal = document.getElementById("alert-modal");
    var modalTitle = document.getElementById("modal-title");
    var modalMessage = document.getElementById("modal-message");
    var btnYes = document.getElementById("btnYes");
    var btnNo = document.getElementById("btnNo");

    modal.style.display = "block";
    modalTitle.innerHTML = "<i class='bi bi-exclamation-triangle-fill text-warning'></i> System Message";
    modalMessage.innerHTML = "Are you sure you want to " + command.toUpperCase() + " '" + serviceName + "'?";
    btnYes.onclick = function () {
        $.ajax({
            url: "/Home/DeleteAddedService",
            type: "POST",
            data: { serviceName: serviceName, command: command },
            success: function () {
                var commandText = command.toUpperCase();
                if (command === "delete") {
                    commandText = "DELETED";
                }

                var toast = new bootstrap.Toast(document.getElementById('liveToast'));
                var toastMessage = "You " + commandText + " the service " + "' " + serviceName + " '" + ".";
                document.querySelector('.toast-body').innerHTML = toastMessage;

                if (commandText === "DELETED") {
                    // set background color to red if commandText is Deleted
                    toast._element.classList.remove("bg-success");
                    toast._element.classList.add("bg-danger");
                }

                toast.show();

                setTimeout(function () {
                    toast.dispose();
                }, 4000);
            },
            error: function () {
                alert("Failed to delete " + serviceName);
            }
        }).then(function () {
            SynchServiceTB();
        });
        modal.style.display = "none";
    }
    btnNo.onclick = function () {
        modal.style.display = "none";
    }
}