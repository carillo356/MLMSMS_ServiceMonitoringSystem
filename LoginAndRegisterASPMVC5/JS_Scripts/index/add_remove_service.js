// Add and Remove Service
function AddService(serviceName) {
    $.ajax({
        url: "/Home/AddService",
        type: "POST",
        data: { serviceName: serviceName },
        success: function () {

            var $servicesTable = $('#servicesTable');
            var $tableRows = $servicesTable.find('tbody tr');

            // Add the new service(s) to the top of the table in the order they were selected
            var $newRows = $('input[type="checkbox"]:checked').map(function () {
                return `<tr><td>${$(this).val()}</td></tr>`;
            }).get().reverse();

            $servicesTable.find('tbody').prepend($newRows);

            // Refresh the table
            RealTimeTable();
            RealTimeCheckbox();
        },
        error: function (xhr, textStatus, errorThrown) {
            alert('Error adding service');
        }
    });
}

function RemoveAddedService1(serviceName) {
    /*Show the confirmation box here*/
    $.ajax({
        url: "/Home/RemoveAddedService",
        type: "POST",
        data: { serviceName: serviceName },
        success: function () {
            $("#serviceTable tbody").empty();

            RealTimeTable();
            RealTimeCheckbox();
        },
        error: function () {
            alert("Failed to delete " + serviceName);
        }
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
            url: "/Home/RemoveAddedService",
            type: "POST",
            data: { serviceName: serviceName, command: command },
            success: function () {
                $("#serviceTable tbody").empty();
                RealTimeTable();
                RealTimeCheckbox();

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
        });
        modal.style.display = "none";
    }
    btnNo.onclick = function () {
        modal.style.display = "none";
    }
}