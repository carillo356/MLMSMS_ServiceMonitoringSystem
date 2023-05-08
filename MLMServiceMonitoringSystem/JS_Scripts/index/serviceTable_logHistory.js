$(document).ready(function () {
    SynchServiceTB();
});

let ITEMS_PER_PAGE = 5;
let logHistoryCount;
let serviceTableCount;
let servicesNotMonitored;
var activeServiceName;

// Pagination for Log History 
let indexPageLogHistory = 1;
let paginationLogHistory = document.querySelector(".paginationLogs ul");
let totalPagesLogHistory;

function SynchServiceTB() {
    ServicesInMonitor()
        .then(function () {
            Checkbox();
        })
        .then(function () {
            generatePageNumbers();
        })
        .then(function () {
            // Get the current page value and set it to 1 if it's null
            let currentPage = parseInt(document.getElementById('current-page').textContent);
            let totalPages = parseInt(document.getElementById('total-pages').textContent);
            currentPage = currentPage ? currentPage : 1;
            totalPages = totalPages ? totalPages : 1;
            // Pass the current page value to the function
            if (currentPage < totalPages || currentPage / totalPages === 1) {
                showPageServiceTB(currentPage);
            }
            else if (currentPage > totalPages) {
                document.getElementById('current-page').textContent = totalPages;
                showPageServiceTB(totalPages);
            }
        })
}

// Set up the event listener for options
$('.optionsRow li').click(function () {
    let currentPage = parseInt(document.getElementById('current-page').textContent);
    currentPage = currentPage ? currentPage : 1;
    var optionText = $(this).find('.optionRow-text').text();
    if (optionText === "All rows") {
        ITEMS_PER_PAGE = serviceTableCount;
    } else {
        ITEMS_PER_PAGE = parseInt(optionText);
    }
    showPageServiceTB(currentPage);
})

function ServicesInMonitor() {
    return new Promise(function (resolve, reject) {
        $.ajax({
            url: '/Home/ServicesInMonitor',
            type: 'GET',
            dataType: 'json',
            success: function (response) {
                if (response.success) {
                    $("#serviceTable tbody").empty();
                    // Loop through the data and append each row to the table
                    response.servicesInMonitor.forEach(function (Data) {
                        var limit = 5;
                        if (Data.ServiceStatus === "Running")
                            statusText = '<span style="color:green">Running</span>'
                        else if (Data.ServiceStatus === "Stopped")
                            statusText = '<span style="color:red">Stopped</span>'
                        else
                            statusText = "";

                        if (Data.PendingCommand) {
                            statusText = '<span style="color:orange">Issued to ' + Data.PendingCommand + '</span>';
                        }

                        var row = "<tr data-toggle='modal' data-target='#service-modal'>";
                        row += "<td onclick='handleRowClick(\"" + Data.ServiceName + "\", \"" + limit + "\")'>" + Data.ServiceName + "</td>";
                        row += "<td onclick='handleRowClick(\"" + Data.ServiceName + "\", \"" + limit + "\")'>" + Data.LastStart + "</td>";
                        row += "<td onclick='handleRowClick(\"" + Data.ServiceName + "\", \"" + limit + "\")'>" + Data.LastEventLog + "</td>";
                        row += "<td onclick='handleRowClick(\"" + Data.ServiceName + "\", \"" + limit + "\")'>" + statusText + "</span></td>";
                        row += "<td onclick='handleRowClick(\"" + Data.ServiceName + "\", \"" + limit + "\")'>" + Data.HostName + "</td>";
                        row += "<td onclick='handleRowClick(\"" + Data.ServiceName + "\", \"" + limit + "\")'>" + Data.LogBy + "</td>";
                        row += "<td>";

                        if (!Data.PendingCommand) {
                            if (Data.ServiceStatus === "Stopped") {
                                row += "<button class='action-button' style='margin-right: 5px;'; id='btnRun' onclick='handleServiceAction(\"" + Data.ServiceName + "\", \"start\", \"" + Data.HostName + "\")' data-bs-toggle='tooltip' data-bs-placement='top' title='Run Service'>";
                                row += "<i class='bi bi-caret-right-fill'></i>";
                                row += "</button>";
                            }
                            else {
                                row += "<button class='action-button' style='display:none;' id='btnRun' onclick='handleServiceAction(\"" + Data.ServiceName + "\", \"start\", \"" + Data.HostName + "\")'>Run</button>";
                                row += "<button class='action-button' id='btnStop' onclick='handleServiceAction(\"" + Data.ServiceName + "\", \"stop\", \"" + Data.HostName + "\")' data-bs-toggle='tooltip' data-bs-placement='top' title='Stop Service'>";
                                row += "<i class='bi bi-stop-fill'></i>";
                                row += "</button>";
                                row += "<button class='action-button mx-2' id='btnRestart' onclick='handleServiceAction(\"" + Data.ServiceName + "\", \"restart\", \"" + Data.HostName + "\")' data-bs-toggle='tooltip' data-bs-placement='top' title='Restart Service'>"
                                row += "<i class='bi bi-arrow-repeat'></i>";
                                row += "</button>";
                            }
                            if (isAdminSession) {
                                row += "<button class='action-button' id='btnDelete' onclick='RemoveAddedService(\"" + Data.ServiceName + "\", \"delete\", \"" + Data.HostName + "\")' data-bs-toggle='tooltip' data-bs-placement='top' title='Delete Service'>";
                                row += "<i class='bi bi-trash3-fill'></i>";
                                row += "</button>";
                            }
                        }
                        else {
                            row += "<button class='action-button' id='btnCancelCommand' onclick='handlePendingCommandCancellation(\"" + Data.ServiceName + "\", \"" + Data.HostName + "\")' data-bs-toggle='tooltip' data-bs-placement='top' title='Cancel Issued Command'>";
                            row += "<i class='bi bi-x-lg'></i>";
                            row += "</button>";
                        }

                        row += "</td></tr>";
                        $("#serviceTable tbody").append(row);
                    });
                    serviceTableCount = response.servicesInMonitor.length;
                    servicesNotMonitored = response.servicesNotMonitored;

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
        });
    });
}

function handlePendingCommandCancellation(serviceName, hostName) {
    // Prepare the data to send to the server
    const requestData = {
        serviceName: serviceName,
        hostName: hostName
    };

    // Add your AJAX call to cancel the issued command here
    $.ajax({
        url: '/Home/CancelPendingCommand', // Update the URL to match your controller and action
        type: 'POST',
        dataType: 'json',
        data: requestData,
        success: function (response) {
            if (response.success) {

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
                var toastMessage = "Command issued for " + " ' " + serviceName + "' " + "has been cancelled.";
                toastBody.innerHTML = toastMessage;

                // set background color to red if commandText is Stopped
                toast._element.classList.remove("bg-success");
                toast._element.classList.add("bg-dark");

                toast.show();

                setTimeout(function () {
                    toast.dispose();
                }, 4000);

                ServicesInMonitor();


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
    });
}


//function Checkbox() {
//    if (servicesNotMonitored.length > 0) {
//        var checkboxes = '';
//        $.each(servicesNotMonitored, function (i, item) {
//            if (!$('#servicesTable td:contains(' + item + ')').length) { // Check if the service is already in the table
//                var checkbox = `<li class="item"><div class="form-check">
//                <input class="form-check-input" type="checkbox" value="${item}" id="service-${i}">
//                    <label class="form-check-label" for="service-${i}">${item}</label>
//            </div></li>`;
//                checkboxes += checkbox;
//            }
//        });
//        $('#serviceCheckboxes').html(checkboxes);
//    }
//}

function Checkbox() {
    if (servicesNotMonitored.length > 0) {
        // Populate the hostname dropdown and remove duplicates
        var hostnames = [...new Set(servicesNotMonitored.map(service => service.HostName))];
        var hostnameOptions = hostnames.map((hostname, i) => `<option value="${hostname}">${hostname}</option>`).join('');
        $('#hostnameDropdown').html(hostnameOptions);

        // Function to update the services checkboxes based on the selected hostname
        function updateServicesCheckboxes() {
            var selectedHostname = $('#hostnameDropdown').val();
            var filteredServices = servicesNotMonitored.filter(service => service.HostName === selectedHostname);

            var checkboxes = '';
            $.each(filteredServices, function (i, item) {
                if (!$('#servicesTable td:contains(' + item.ServiceName + ')').length) { // Check if the service is already in the table
                    var checkbox = `<li class="item"><div class="form-check">
                        <input class="form-check-input" type="checkbox" value="${item.ServiceName}" data-host-name="${item.HostName}" id="service-${i}">
                        <label class="form-check-label" for="service-${i}">${item.ServiceName}</label>
                    </div></li>`;
                    checkboxes += checkbox;
                }
            });
            $('#serviceCheckboxes').html(checkboxes);
        }

        // Update the services checkboxes when the selected hostname changes
        $('#hostnameDropdown').on('change', updateServicesCheckboxes);

        // Initialize the services checkboxes with the first hostname
        updateServicesCheckboxes();
    }
}


$('#searchService').on('input', function () {
    var searchText = $(this).val().toLowerCase();
    $('.form-check-label').each(function () {
        var label = $(this).text().toLowerCase();
        $(this).closest('.item').toggle(label.includes(searchText));
    });
});

$('input[name="ServiceName"]').on('input', function () {
    var filter = $(this).val().toUpperCase();
    $('#serviceCheckboxes li').each(function () {

        var label = $(this).find('label').text().toUpperCase();
        $(this).toggle(label.includes(filter));
    });
});

function handleRowClick(serviceName, limit) {
    logHistory(serviceName, limit);
}

function handleServiceAction(serviceName, command, hostName) {
    ServiceActions(serviceName, command, hostName);
}

//Manage Services
function ServiceActions(serviceName, command, hostName) {
    /*Show the confirmation box here*/
    var modal = document.getElementById("alert-modal");
    var modalTitle = document.getElementById("modal-title");
    var modalMessage = document.getElementById("modal-message");
    var modalNote = document.getElementById("modal-messageNote");
    var btnYes = document.getElementById("btnYes");
    var btnNo = document.getElementById("btnNo");

    modal.style.display = "block";
    modalTitle.innerHTML = "<i class='bi bi-exclamation-triangle-fill text-warning'></i> System Message";
    modalMessage.innerHTML = "Are you sure you want to " + command.toUpperCase() + " '" + serviceName + "'?";
    modalNote.innerHTML = "Note: This action may take a while, refresh your list after 5 minutes.";
    btnYes.onclick = function () {
        /* Handle the command here*/
        $.ajax({
            url: '/Home/ServiceAction',
            type: 'POST',
            dataType: 'json',
            data: { serviceName: serviceName, command: command, hostName: hostName },
            success: function (response) {
                if (response.success) {
                    $("#serviceTable tbody").empty();

                    var commandText = command.toUpperCase();
                    if (command === "stop") {
                        commandText = "STOP";
                    } else if (command === "start") {
                        commandText = "START";
                    } else if (command === "restart") {
                        commandText = "RESTART";
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
                    var toastMessage = "Command to " + commandText + " ' " + serviceName + "' " + "has been queued. This process may take a while, refresh your list after 5 minutes.";
                    /*var toastMessage = "You " + commandText + " the service " + " ' " + serviceName + " ' " + ".";*/
                    toastBody.innerHTML = toastMessage;

                    if (commandText === "STOPPED") {
                        // set background color to red if commandText is Stopped
                        toast._element.classList.remove("bg-success");
                        toast._element.classList.add("bg-danger");
                    } else {
                        // set background color to green for any other commandText
                        toast._element.classList.remove("bg-danger");
                        toast._element.classList.add("bg-success");
                    }

                    toast.show();

                    setTimeout(function () {
                        toast.dispose();
                    }, 4000);
                } else {
                    // Handle the error case
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


// Log History pagination
var itemsPerPageLogHistory = 5;
const logHistoryTableBody = document.querySelector('#popupTable tbody');
const logHistoryPaginationLinks = document.querySelectorAll('.paginationLogs li');

function showPageLogHistory(pageNumberLogHistory) {
    const startIndexLogHistory = (pageNumberLogHistory - 1) * itemsPerPageLogHistory;
    const endIndexLogHistory = startIndexLogHistory + itemsPerPageLogHistory;

    const rowsLogHistory = logHistoryTableBody.querySelectorAll('tr');
    rowsLogHistory.forEach(rowLogHistory => {
        rowLogHistory.style.display = 'none';
    });

    for (let i = startIndexLogHistory; i < endIndexLogHistory && i < rowsLogHistory.length; i++) {
        rowsLogHistory[i].style.display = '';
    }
}

$('.options_LogHistoryRow li').click(function () {
    var optionText = $(this).find('.option_LogHistory-text').text();

    if (optionText === "All rows") {
        itemsPerPageLogHistory = logHistoryCount;
    } else {
        itemsPerPageLogHistory = parseInt(optionText);
    }
    generatePageNumbersLogHistory();
    let currentPageLogs = parseInt(document.getElementById('current-page-log-history').textContent);
    let totalPagesLogs = parseInt(document.getElementById('total-pages-log-history').textContent);

    currentPageLogs = currentPageLogs ? currentPageLogs : 1;
    totalPagesLogs = totalPagesLogs ? totalPagesLogs : 1;

    // Pass the current page value to the function
    if (currentPageLogs < totalPagesLogs || currentPageLogs / totalPagesLogs === 1) {
        showPageLogHistory(currentPageLogs);
    }
    else if (currentPageLogs > totalPagesLogs) {
        document.getElementById('current-page-log-history').textContent = totalPagesLogs;
        showPageLogHistory(totalPagesLogs);
    }
});

// Log History Modal
function logHistory(serviceName) {
    //Viewlogs, Gets all the records of a service
    $.ajax({
        url: "/Home/GetServiceLogsTB",
        type: "POST",
        data: { serviceName: serviceName },
        success: function (response) {
            // Get the table body element
            if (response.success) {
                var tbody = $('#popupTable tbody');
                tbody.empty();

                // Create a string containing all the rows
                var rows = '';
                response.servicesLogsList.forEach(function (service) {
                    // Add a CSS class to the td element based on the value of ServiceStatus
                    var statusColorClass = service.ServiceStatus === 'Running' ? 'text-success' : 'text-danger';

                    rows += '<tr>' +
                        '<td>' + service.LastStart + '</td>' +
                        '<td>' + service.LastEventLog + '</td>' +
                        '<td class="status">' +
                        '<span class="' + statusColorClass + '">' + service.ServiceStatus + '</span>' +
                        '</td>' +
                        '<td>' + service.HostName + '</td>' +
                        '<td>' + service.LogBy + '</td>' +
                        '</tr>';
                });

                // Append the rows to the table in a single operation
                tbody.append(rows);
                activeServiceName = serviceName;

                $('#logHistory-modal .modal-servicelogs .service_name').text(serviceName);

                //// Show the popup dialog
                //ITEMS_PAR_PAGE_LOGS = limit;
                //logsshowPage(1);
                $('#logHistory-modal').css('display', 'block');
                showPageLogHistory(1);
                logHistoryCount = response.servicesLogsList.length;
                generatePageNumbersLogHistory();
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
        }
    });
}

// Get the modal element
var logHistoryModal = document.getElementById("logHistory-modal");

// Get the close button element
var closeButton = logHistoryModal.querySelector(".close");

// Add event listener to the close button
closeButton.addEventListener("click", function () {
    // Hide the modal when close button is clicked
    logHistoryModal.style.display = "none";
});

function generatePageNumbersLogHistory() {
    totalPagesLogHistory = Math.ceil(logHistoryTableBody.querySelectorAll('tr').length / itemsPerPageLogHistory);
    paginationLogHistory.innerHTML = '';

    let maxVisiblePages = 5;
    let startPage = indexPageLogHistory - Math.floor(maxVisiblePages / 2);
    let endPage = startPage + maxVisiblePages - 1;

    if (startPage < 1) {
        startPage = 1;
        endPage = Math.min(totalPagesLogHistory, startPage + maxVisiblePages - 1);
    }

    if (endPage > totalPagesLogHistory) {
        endPage = totalPagesLogHistory;
        startPage = Math.max(1, endPage - maxVisiblePages + 1);
    }

    if (indexPageLogHistory > 1) {
        paginationLogHistory.innerHTML = `<li class="page-item" id="previous-link-log-history"><button type="button" class="page-link">Previous</button></li>`;
    }

    for (let i = startPage; i <= endPage; i++) {
        // Check if the current iteration is the active page
        const isActive = i === indexPageLogHistory;

        paginationLogHistory.innerHTML += `<li class="page-item${isActive ? " active" : ""}"><button type="button" class="page-link" data-page="${i}">${i}</button></li>`;
    }

    if (indexPageLogHistory < totalPagesLogHistory) {
        paginationLogHistory.innerHTML += `<li class="page-item" id="next-link-log-history"><button type="button" class="page-link">Next</button></li>`;
    }

    if (indexPageLogHistory < totalPagesLogHistory) {
        document.getElementById("next-link-log-history").addEventListener("click", function () {
            indexPageLogHistory = indexPageLogHistory + 1;
            showPageLogHistory(indexPageLogHistory);
            generatePageNumbersLogHistory();
        });
    }

    if (indexPageLogHistory > 1) {
        document.getElementById("previous-link-log-history").addEventListener("click", function () {
            indexPageLogHistory = indexPageLogHistory - 1;
            showPageLogHistory(indexPageLogHistory);
            generatePageNumbersLogHistory();
        });
    }

    // Add click event listener to the page number elements
    document.querySelectorAll(".paginationLogs .page-link").forEach(function (pageLink) {
        const pageNumber = parseInt(pageLink.getAttribute("data-page"));
        if (pageNumber) {
            pageLink.addEventListener("click", function () {
                indexPageLogHistory = pageNumber;
                showPageLogHistory(indexPageLogHistory);
                generatePageNumbersLogHistory();
            });
        }
    });
    document.getElementById('current-page-log-history').textContent = indexPageLogHistory;
    document.getElementById('total-pages-log-history').textContent = totalPagesLogHistory;
}

let indexPage = 1;
let paginationService = document.querySelector(".paginationService ul");
let totalPages;

// get the table body element
const tableBody = document.querySelector('#serviceTable tbody');

// get the pagination links
const paginationLinks = document.querySelectorAll('.paginationService li');

// define a function to show the items for the selected page
function showPageServiceTB(pageNumber) {
    // calculate the start and end indexes of the items to show
    const startIndex = (pageNumber - 1) * ITEMS_PER_PAGE;
    const endIndex = startIndex + ITEMS_PER_PAGE;

    // hide all rows in the table
    const rows = tableBody.querySelectorAll('tr');
    rows.forEach(row => {
        row.style.display = 'none';
    });

    // show the rows for the current page
    for (let i = startIndex; i < endIndex && i < rows.length; i++) {
        rows[i].style.display = '';
    }
}

function getMonitoredServicesCount() {
    return new Promise((resolve, reject) => {
        $.ajax({
            url: '/Home/GetMonitoredServicesCount',
            type: 'GET',
            dataType: 'json',
            success: function (result) {
                resolve(result.totalMonitoredServices);
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
        });
    });
}

function generatePageNumbers() {
    totalPages = Math.ceil(serviceTableCount / ITEMS_PER_PAGE);
    paginationService.innerHTML = '';

    let maxVisiblePages = 5;
    let startPage = indexPage - Math.floor(maxVisiblePages / 2);
    let endPage = startPage + maxVisiblePages - 1;

    if (startPage < 1) {
        startPage = 1;
        endPage = Math.min(totalPages, startPage + maxVisiblePages - 1);
    }

    if (endPage > totalPages) {
        endPage = totalPages;
        startPage = Math.max(1, endPage - maxVisiblePages + 1);
    }

    if (indexPage > 1) {
        paginationService.innerHTML = `<li class="page-item" id="previous-link"><button type="button" class="page-link">Previous</button></li>`;
    }

    for (let i = startPage; i <= endPage; i++) {
        // Check if the current iteration is the active page
        const isActive = i === indexPage;

        paginationService.innerHTML += `<li class="page-item${isActive ? " active" : ""}"><button type="button" class="page-link" data-page="${i}">${i}</button></li>`;
    }

    if (indexPage < totalPages) {

        paginationService.innerHTML += `<li class="page-item" id="next-link"><button type="button" class="page-link">Next</button></li>`;
    }

    if (indexPage < totalPages) {
        document.getElementById("next-link").addEventListener("click", function () {
            indexPage = indexPage + 1;
            showPageServiceTB(indexPage);
            generatePageNumbers();
        });
    }

    if (indexPage > 1) {
        document.getElementById("previous-link").addEventListener("click", function () {
            indexPage = indexPage - 1;
            showPageServiceTB(indexPage);
            generatePageNumbers();
        });
    }

    // Add click event listener to the page number elements
    document.querySelectorAll(".paginationService .page-link").forEach(function (pageLink) {
        const pageNumber = parseInt(pageLink.getAttribute("data-page"));
        if (pageNumber) {
            pageLink.addEventListener("click", function () {
                indexPage = pageNumber;
                showPageServiceTB(indexPage);
                generatePageNumbers();
            });
        }
        document.getElementById('current-page').textContent = indexPage;
        document.getElementById('total-pages').textContent = totalPages;
    })
}