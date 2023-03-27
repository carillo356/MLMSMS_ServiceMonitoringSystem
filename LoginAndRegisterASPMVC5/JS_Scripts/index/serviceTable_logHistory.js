
// Service Table and Log History
$(document).ready(function () {
    RealTimeTable();
});

var activeServiceName;



function handleRowClick(serviceName, limit) {
    logHistory(serviceName, limit);
}

function handleServiceAction(serviceName, action) {
    ServiceActions(serviceName, action);
}

//Manage Services
function ServiceActions(serviceName, command) {
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
        /* Handle the command here*/
        $.ajax({
            url: '/Home/ServiceAction',
            type: 'POST',
            data: { serviceName: serviceName, command: command },
            success: function () {
                $("#serviceTable tbody").empty();
                RealTimeTable();

                var commandText = command.toUpperCase();
                if (command === "stop") {
                    commandText = "STOPPED";
                } else if (command === "start") {
                    commandText = "STARTED";
                } else if (command === "restart") {
                    commandText = "RESTARTED";
                }

                var toast = new bootstrap.Toast(document.getElementById('liveToast'));
                var toastMessage = "You " + commandText + " the service " + " ' " + serviceName + " ' " + ".";
                document.querySelector('.toast-body').innerHTML = toastMessage;
                toast.show();

                setTimeout(function () {
                    toast.dispose();
                }, 4000);
            },
            error: function () {
                alert('Failed to ' + command + ' ' + serviceName);
            }
        });
        modal.style.display = "none";
    }
    btnNo.onclick = function () {
        modal.style.display = "none";
    }
}