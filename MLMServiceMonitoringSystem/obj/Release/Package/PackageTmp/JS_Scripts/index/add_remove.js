/* Add and Remove Service*/
function AddService(serviceName, resolve, reject) {
    $.ajax({
        url: "/Home/AddService",
        type: "POST",
        data: { serviceName: serviceName },
        dataType: 'json',
        success: function (response) {
            if (response.success) {
                var $servicesTable = $('#servicesTable');
                $servicesTable.find('tbody').prepend($newRows);
                resolve();
            }
            else {
                var errorContainer = document.getElementById("error-container");
                errorContainer.innerHTML = `
                        <div class="alert alert-danger alert-dismissible fade show" role="alert">
                          <strong><i class="fa fa-exclamation"></i> Error!</strong> <span>${response.errorMessage}</span>
                          <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
                        </div>`;
            }
        },
        error: function (xhr) {
            var errorMessage = xhr.responseText;

            var errorContainer = document.getElementById("error-container");
            errorContainer.innerHTML = `
                    <div class="alert alert-danger alert-dismissible fade show" role="alert">
                        <strong><i class="fa fa-exclamation"></i> Error!</strong> <span>${errorMessage}</span>
                        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
                    </div>`;
            reject();
        }
    }).then(function () {
        SynchServiceTB();
    })
}

function RemoveAddedService(serviceName, command) {
    // Show the confirmation box here
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
            success: function (response) {
                if (response.success) {
                    ServicesInMonitor();

                    var commandText = command.toUpperCase();
                    if (command === "delete") {
                        commandText = "DELETED";
                    }

                    var toastElement = document.createElement('div');
                    toastElement.setAttribute('class', 'toast hide toast-stack');
                    toastElement.setAttribute('role', 'alert');
                    toastElement.setAttribute('aria-live', 'assertive');
                    toastElement.setAttribute('aria-atomic', 'true');

                    var toastBody = document.createElement('div');
                    toastBody.setAttribute('class', 'toast-body');
                    toastElement.appendChild(toastBody);

                    var toastWrapper = document.getElementById('toast-wrapper');
                    toastWrapper.appendChild(toastElement);

                    var toast = new bootstrap.Toast(toastElement);
                    var toastMessage = "You " + commandText + " the service " + "' " + serviceName + " '" + ".";
                    toastBody.innerHTML = toastMessage;

                    if (commandText === "DELETED") {
                        // set background color to red if commandText is Deleted
                        toast._element.classList.remove("bg-success");
                        toast._element.classList.add("bg-danger");
                    }

                    toast.show();

                    setTimeout(function () {
                        toast.dispose();
                    }, 4000);
                } else {
                    var errorContainer = document.getElementById("error-container");
                    errorContainer.innerHTML = `
                        <div class="alert alert-danger alert-dismissible fade show" role="alert">
                          <strong><i class="fa fa-exclamation"></i> Error!</strong> <span>${response.errorMessage}</span>
                          <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
                        </div>`;
                }
            },
            error: function (xhr) {
                var errorMessage = xhr.responseText;

                var errorContainer = document.getElementById("error-container");
                errorContainer.innerHTML = `
                    <div class="alert alert-danger alert-dismissible fade show" role="alert">
                        <strong><i class="fa fa-exclamation"></i> Error!</strong> <span>${errorMessage}</span>
                        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
                    </div>`;
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
