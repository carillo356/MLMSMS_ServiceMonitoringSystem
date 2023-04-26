$(document).ready(function () {
    SynchServiceTB();
});

    let ITEMS_PER_PAGE = 5;
    let logHistoryCount;
    let serviceTableCount;

    function ServicesAvailable() {
        return GetServicesInController()
            .then(function (servicesInController) {
                Checkbox(servicesInController);
            })
            .catch(function (error) {
                alert(error);
            });
    }

    function SynchServiceTB() {
        ServicesInMonitor()
            .then(function () {
                return ServicesAvailable();
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
                if (currentPage < totalPages || currentPage/totalPages === 1) {
                    showPageServiceTB(currentPage);
                }
                else if (currentPage > totalPages) {
                    document.getElementById('current-page').textContent = totalPages;
                    showPageServiceTB(totalPages);
                }
            })
            .catch(function (error) {
                alert(error);
            });
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
                success: function (result) {
                    $("#serviceTable tbody").empty();

                    // Loop through the data and append each row to the table
                    result.forEach(function (Data) {
                        var limit = 5;
                        if (Data.ServiceStatus === "Running")
                            statusText = '<span style="color:green">Running</span>'
                        else if (Data.ServiceStatus === "Stopped")
                            statusText = '<span style="color:red">Stopped</span>'
                        else
                            statusText = "";

                        var row = "<tr data-toggle='modal' data-target='#service-modal'>";
                        row += "<td onclick='handleRowClick(\"" + Data.ServiceName + "\", \"" + limit + "\")'>" + Data.ServiceName + "</td>";
                        row += "<td onclick='handleRowClick(\"" + Data.ServiceName + "\", \"" + limit + "\")'>" + Data.LastStart + "</td>";
                        row += "<td onclick='handleRowClick(\"" + Data.ServiceName + "\", \"" + limit + "\")'>" + Data.LastEventLog + "</td>";
                        row += "<td onclick='handleRowClick(\"" + Data.ServiceName + "\", \"" + limit + "\")'>" + statusText + "</span></td>";
                        row += "<td onclick='handleRowClick(\"" + Data.ServiceName + "\", \"" + limit + "\")'>" + Data.HostName + "</td>";
                        row += "<td onclick='handleRowClick(\"" + Data.ServiceName + "\", \"" + limit + "\")'>" + Data.LogBy + "</td>";
                        row += "<td>";

                        if (Data.ServiceStatus === "Stopped") {
                            row += "<button class='action-button' style='margin-right: 5px;'; id='btnRun' onclick='handleServiceAction(\"" + Data.ServiceName + "\", \"start\")' data-bs-toggle='tooltip' data-bs-placement='top' title='Run Service'>";
                            row += "<i class='bi bi-caret-right-fill'></i>";
                            row += "</button>";
                        } else if (Data.ServiceStatus === "Running") {
                            row += "<button class='action-button' style='display:none;' id='btnRun' onclick='handleServiceAction(\"" + Data.ServiceName + "\", \"start\")'>Run</button>";
                            row += "<button class='action-button' id='btnStop' onclick='handleServiceAction(\"" + Data.ServiceName + "\", \"stop\")' data-bs-toggle='tooltip' data-bs-placement='top' title='Stop Service'>";
                            row += "<i class='bi bi-stop-fill'></i>";
                            row += "</button>";
                            row += "<button class='action-button mx-2' id='btnRestart' onclick='handleServiceAction(\"" + Data.ServiceName + "\", \"restart\")' data-bs-toggle='tooltip' data-bs-placement='top' title='Restart Service'>"
                            row += "<i class='bi bi-arrow-repeat'></i>";
                            row += "</button>";
                        }
                        row += "<button class='action-button' id='btnDelete' onclick='RemoveAddedService(\"" + Data.ServiceName + "\", \"delete\")' data-bs-toggle='tooltip' data-bs-placement='top' title='Delete Service'>";
                        row += "<i class='bi bi-trash3-fill'></i>";
                        row += "</button>";
                        row += "</td></tr>";
                        $("#serviceTable tbody").append(row);
                    });
                    serviceTableCount = result.length;
                    resolve();
                },
                error: function () {
                    reject("Failed to refresh services.");
                }
            });
        });
    }

    function GetServicesInController() {
        var servicesInTable = [];
        $('#servicesTable tbody tr').each(function () {
            servicesInTable.push($(this).find('td:first').text());
        });

        return new Promise(function (resolve, reject) {
            $.ajax({
                url: "/Home/GetServicesInController",
                type: 'POST',
                dataType: 'json',
                contentType: 'application/json',
                data: JSON.stringify(servicesInTable),
                success: function (result) {
                    resolve(result);
                },
                error: function (xhr, textStatus, errorThrown) {
                    reject('Error getting service names');
                }
            });
        });
    }

    function Checkbox(servicesInController) {
        if (servicesInController.length > 0) {
            var checkboxes = '';
            $.each(servicesInController, function (i, item) {
                if (!$('#servicesTable td:contains(' + item + ')').length) { // Check if the service is already in the table
                    var checkbox = `<li class="item"><div class="form-check">
                    <input class="form-check-input" type="checkbox" value="${item}" id="service-${i}">
                        <label class="form-check-label" for="service-${i}">${item}</label>
                </div></li>`;
                    checkboxes += checkbox;
                }
            });
            $('#serviceCheckboxes').html(checkboxes);
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

    var activeServiceName;

    function handleRowClick(serviceName, limit) {
        logHistory(serviceName, limit);
    }

    function handleServiceAction(serviceName, action) {
        ServiceActions(serviceName, action);
    }

// Manage Services
function ServiceActions(serviceName, command) {
    /* Show the confirmation box here */
    var modal = document.getElementById("alert-modal");
    var modalTitle = document.getElementById("modal-title");
    var modalMessage = document.getElementById("modal-message");
    var btnYes = document.getElementById("btnYes");
    var btnNo = document.getElementById("btnNo");

    modal.style.display = "block";
    modalTitle.innerHTML = "<i class='bi bi-exclamation-triangle-fill text-warning'></i> System Message";
    modalMessage.innerHTML = "Are you sure you want to " + command.toUpperCase() + " '" + serviceName + "'?";
    btnYes.onclick = function () {
        /* Handle the command here */
        $.ajax({
            url: '/Home/ServiceAction',
            type: 'POST',
            data: { serviceName: serviceName, command: command },
            success: function () {
                $("#serviceTable tbody").empty();

                var commandText = command.toUpperCase();
                if (command === "stop") {
                    commandText = "STOPPED";
                } else if (command === "start") {
                    commandText = "STARTED";
                } else if (command === "restart") {
                    commandText = "RESTARTED";
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
                var toastMessage = "You " + commandText + " the service " + " ' " + serviceName + " ' " + ".";
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
                    toastWrapper.removeChild(toastElement);
                }, 4000);
            },
            error: function () {
                alert('Failed to ' + command + ' ' + serviceName);
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


    // Pagination for Log History 
    let indexPageLogHistory = 1;
    let paginationLogHistory = document.querySelector(".paginationLogs ul");
    let totalPagesLogHistory;

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
    function logHistory(serviceName, limit) {
        //Viewlogs, Gets all the records of a service
        $.ajax({
            url: "/Home/GetServiceLogsTB",
            type: "POST",
            data: { serviceName: serviceName },
            success: function (data) {
                // Get the table body element
                var tbody = $('#popupTable tbody');
                tbody.empty();

                // Create a string containing all the rows
                var rows = '';
                data.forEach(function (service) {
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
                logHistoryCount = data.length;
                generatePageNumbersLogHistory();
            },
            error: function () {
                alert("Failed to retrieve records for " + serviceName);
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
                error: function () {
                    reject("Failed to get the total number of monitored services.");
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