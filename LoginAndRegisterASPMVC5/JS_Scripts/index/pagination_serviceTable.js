//$(document).ready(function () {
//    SynchServiceTB();
//});

//function ServicesAvailable() {
//    return GetServicesInController()
//        .then(function (servicesInController) {
//            Checkbox(servicesInController);
//        })
//        .catch(function (error) {
//            alert(error);
//        });
//}


//function SynchServiceTB() {
//    ServicesInMonitor()
//        .then(function () {
//            return ServicesAvailable();
//        })
//        .then(function () {
//            showPageServiceTB(1);
//        })
//        .catch(function (error) {
//            alert(error);
//        });
//}

//function getMonitoredServicesCount() {
//    return new Promise((resolve, reject) => {
//        $.ajax({
//            url: '/Home/GetMonitoredServicesCount',
//            type: 'GET',
//            dataType: 'json',
//            success: function (count) {
//                resolve(count);
//            },
//            error: function () {
//                reject("Failed to get the total number of monitored services.");
//            }
//        });
//    });
//}

//// Set up the event listener for options
//$('.optionsRow li').click(function () {
//    var optionText = $(this).find('.optionRow-text').text();
//    switch (optionText) {
//        case "All rows":
//            getMonitoredServicesCount().then(function (totalMonitoredServices) {
//                ITEMS_PER_PAGE = totalMonitoredServices;
//            }).catch(function (error) {
//                console.error('Failed to get the total number of monitored services.', error);
//            });
//            break;
//        default:
//            ITEMS_PER_PAGE = parseInt(optionText);
//            break;
//    }
//    SynchServiceTB();
// });

//    // define the number of items per page
//    let ITEMS_PER_PAGE = 5;

//    // get the table body element
//    const tableBody = document.querySelector('#serviceTable tbody');

//    // get the pagination links
//    const paginationLinks = document.querySelectorAll('.paginationService ul');

//    // define a function to show the items for the selected page
//    function showPageServiceTB(pageNumber) {
//        // calculate the start and end indexes of the items to show
//        const startIndex = (pageNumber - 1) * ITEMS_PER_PAGE;
//        const endIndex = startIndex + ITEMS_PER_PAGE;

//        // hide all rows in the table
//        const rows = tableBody.querySelectorAll('tr');
//        rows.forEach(row => {
//            row.style.display = 'none';
//        });

//        // show the rows for the current page
//        for (let i = startIndex; i < endIndex && i < rows.length; i++) {
//            rows[i].style.display = '';
//        }
//    }

//function ServicesInMonitor() {
//    return new Promise(function (resolve, reject) {
//        $.ajax({
//            url: '/Home/ServicesInMonitor',
//            type: 'GET',
//            dataType: 'json',
//            success: function (result) {
//                $("#serviceTable tbody").empty();

//                // Loop through the data and append each row to the table
//                result.forEach(function (Data) {
//                    var limit = 999;
//                    if (Data.ServiceStatus === "Running")
//                        statusText = '<span style="color:green">Running</span>'
//                    else if (Data.ServiceStatus === "Stopped")
//                        statusText = '<span style="color:red">Stopped</span>'
//                    else
//                        statusText = "";

//                    var row = "<tr data-toggle='modal' data-target='#service-modal'>";
//                    row += "<td onclick='handleRowClick(\"" + Data.ServiceName + "\", \"" + limit + "\")'>" + Data.ServiceName + "</td>";
//                    row += "<td onclick='handleRowClick(\"" + Data.ServiceName + "\", \"" + limit + "\")'>" + Data.LastStart + "</td>";
//                    row += "<td onclick='handleRowClick(\"" + Data.ServiceName + "\", \"" + limit + "\")'>" + Data.LastEventLog + "</td>";
//                    row += "<td onclick='handleRowClick(\"" + Data.ServiceName + "\", \"" + limit + "\")'>" + statusText + "</span></td>";
//                    row += "<td onclick='handleRowClick(\"" + Data.ServiceName + "\", \"" + limit + "\")'>" + Data.HostName + "</td>";
//                    row += "<td>";

//                    if (Data.ServiceStatus === "Stopped") {
//                        row += "<button class='action-button' style='margin-right: 5px;'; id='btnRun' onclick='handleServiceAction(\"" + Data.ServiceName + "\", \"start\")'>Run</button>";
//                    } else if (Data.ServiceStatus === "Running") {
//                        row += "<button class='action-button' style='display:none;' id='btnRun' onclick='handleServiceAction(\"" + Data.ServiceName + "\", \"start\")'>Run</button>";
//                        row += "<button class='action-button' id='btnStop' onclick='handleServiceAction(\"" + Data.ServiceName + "\", \"stop\")'>Stop</button>";
//                        row += "<button class='action-button ms-2' id='btnRestart' onclick='handleServiceAction(\"" + Data.ServiceName + "\", \"restart\")'>Restart</button>";
//                    } row += "<button class='action-button' id='btnDelete' onclick='RemoveAddedService(\"" + Data.ServiceName + "\", \"delete\")'>Delete</button>";
//                    row += "</td></tr>";
//                    $("#serviceTable tbody").append(row);
//                });
//                resolve();
//            },
//            error: function () {
//                reject("Failed to refresh services.");
//            }
//        });
//    });

//}

//        var indexPage = 1;
//        let pagination = document.querySelector('.paginationService ul');

//        function generatePageNumbers() {
//            pagination.innerHTML = '';
//        for (let i = indexPage; i < indexPage + 5; i++) {
//            pagination.innerHTML += `<li class="page-item"><a class="page-link" data-page="${i}">${i}</a></li>`;
//        }
//        pagination.innerHTML += `<li class="page-item" id="next-link"><a class="page-link">Next</a></li>`;

//        if (indexPage > 1) {
//            pagination.innerHTML = `<li class="page-item" id="previous-link"><a class="page-link">Previous</a></li>` + pagination.innerHTML;
//        }

//        // Event listener for "Next" link
//        document.getElementById("next-link").addEventListener("click", function () {
//            indexPage = indexPage + 1; // Set current page to next page number
//        showPageServiceTB(indexPage);
//        document.getElementById('current-page').textContent = indexPage;
//        document.getElementById('total-pages').textContent = indexPage + 4;
//        generatePageNumbers(); // Generate new page numbers
//        });

//        // Event listener for "Previous" link
//        document.getElementById("previous-link").addEventListener("click", function () {
//            if (indexPage > 1) { // Check if current page is greater than 1
//            indexPage = indexPage - 1;
//        showPageServiceTB(indexPage);
//        document.getElementById('current-page').textContent = indexPage;
//        document.getElementById('total-pages').textContent = indexPage + 4;
//        generatePageNumbers(); // Generate new page numbers
//            }
//        });
//    }
//        let page = 1;
//        $('.paginationService ul').on('click', '.page-link', function () {
//            let page = parseInt($(this).data('page'));
//        document.getElementById('current-page').textContent = page;
//        indexPage = page;
//        document.getElementById('total-pages').textContent = indexPage + 4;
//        showPageServiceTB(page);
//    });

//        document.getElementById('current-page').textContent = page;
//        document.getElementById('total-pages').textContent = indexPage + 4;
//        generatePageNumbers();

//        function GetServicesInController() {
//        var servicesInTable = [];
//        $('#servicesTable tbody tr').each(function () {
//            servicesInTable.push($(this).find('td:first').text());
//        });

//        return new Promise(function (resolve, reject) {
//            $.ajax({
//                url: "/Home/GetServicesInController",
//                type: 'POST',
//                dataType: 'json',
//                contentType: 'application/json',
//                data: JSON.stringify(servicesInTable),
//                success: function (result) {
//                    resolve(result);
//                },
//                error: function (xhr, textStatus, errorThrown) {
//                    reject('Error getting service names');
//                }
//            });
//        });
//    }

//        function Checkbox(servicesInController) {
//        if (servicesInController.length > 0) {
//            var checkboxes = '';
//        $.each(servicesInController, function (i, item) {
//                if (!$('#servicesTable td:contains(' + item + ')').length) { // Check if the service is already in the table
//                    var checkbox = `<li class="item"><div class="form-check">
//            <input class="form-check-input" type="checkbox" value="${item}" id="service-${i}">
//                <label class="form-check-label" for="service-${i}">${item}</label>
//        </div></li>`;
//                    checkboxes += checkbox;
//                }
//            });
//            $('#serviceCheckboxes').html(checkboxes);
//        }
//    }

//    $('#searchService').on('input', function () {
//        var searchText = $(this).val().toLowerCase();
//        $('.form-check-label').each(function () {
//            var label = $(this).text().toLowerCase();
//            $(this).closest('.item').toggle(label.includes(searchText));
//        });
//    });

//    $('input[name="ServiceName"]').on('input', function () {
//        var filter = $(this).val().toUpperCase();
//        $('#serviceCheckboxes li').each(function () {

//            var label = $(this).find('label').text().toUpperCase();
//            $(this).toggle(label.includes(filter));
//        });
//    });

//    /* Add and Remove Service*/
//    function AddService(serviceName) {
//        $.ajax({
//            url: "/Home/AddService",
//            type: "POST",
//            data: { serviceName: serviceName },
//            success: function () {
//                var $servicesTable = $('#servicesTable');
//                $servicesTable.find('tbody').prepend($newRows);
//            },
//            error: function (xhr, textStatus, errorThrown) {
//                alert('Error adding service');
//            }
//        }).then(function () {
//            ServicesInMonitor();
//            ServicesAvailable();
//        });
//    }


//    function RemoveAddedService1(serviceName) {
//        /*Show the confirmation box here*/
//        $.ajax({
//            url: "/Home/DeleteAddedService",
//            type: "POST",
//            data: { serviceName: serviceName },
//            success: function () {
//                ServicesInMonitor();
//            },
//            error: function (xhr, textStatus, errorThrown) {
//                alert("Failed to delete " + serviceName);
//            }
//        }).then(function () {
//            ServicesAvailable();
//        });
//    }

//    function RemoveAddedService(serviceName, command) {
//        /*Show the confirmation box here*/
//        var modal = document.getElementById("alert-modal");
//        var modalTitle = document.getElementById("modal-title");
//        var modalMessage = document.getElementById("modal-message");
//        var btnYes = document.getElementById("btnYes");
//        var btnNo = document.getElementById("btnNo");

//        modal.style.display = "block";
//        modalTitle.innerHTML = "<i class='bi bi-exclamation-triangle-fill text-warning'></i> System Message";
//        modalMessage.innerHTML = "Are you sure you want to " + command.toUpperCase() + " '" + serviceName + "'?";
//        btnYes.onclick = function () {
//            $.ajax({
//                url: "/Home/DeleteAddedService",
//                type: "POST",
//                data: { serviceName: serviceName, command: command },
//                success: function () {
//                    ServicesInMonitor();
//                    var commandText = command.toUpperCase();
//                    if (command === "delete") {
//                        commandText = "DELETED";
//                    }

//                    var toast = new bootstrap.Toast(document.getElementById('liveToast'));
//                    var toastMessage = "You " + commandText + " the service " + "' " + serviceName + " '" + ".";
//                    document.querySelector('.toast-body').innerHTML = toastMessage;

//                    if (commandText === "DELETED") {
//                        // set background color to red if commandText is Deleted
//                        toast._element.classList.remove("bg-success");
//                        toast._element.classList.add("bg-danger");
//                    }

//                    toast.show();

//                    setTimeout(function () {
//                        toast.dispose();
//                    }, 4000);
//                },
//                error: function () {
//                    alert("Failed to delete " + serviceName);
//                }
//            }).then(function () {
//                ServicesAvailable();
//            });
//            modal.style.display = "none";
//        }
//        btnNo.onclick = function () {
//            modal.style.display = "none";
//        }
//    }









////<script>
////    var indexLogPage = 1;
////    let paginationLog = document.querySelector('.paginationLogs ul');

////    function generateLogPageNumbers() {
////        paginationLog.innerHTML = '';
////    for (let i = indexLogPage; i < indexLogPage + 5; i++) {
////        paginationLog.innerHTML += `<li class="page-item"><a class="page-link" data-page="${i}">${i}</a></li>`;
////        }
////    paginationLog.innerHTML += `<li class="page-item" id="next-link"><a class="page-link">Next</a></li>`;

////        if (indexLogPage > 1) {
////        paginationLog.innerHTML = `<li class="page-item" id="previous-link"><a class="page-link">Previous</a></li>` + paginationLog.innerHTML;
////        }

////    // Event listener for "Next" link
////    document.getElementById("next-link").addEventListener("click", function () {
////        indexLogPage = indexLogPage + 1; // Set current page to next page number
////    logsshowPage(indexLogPage);
////    document.getElementById('current-logPage').textContent = indexLogPage;
////    document.getElementById('total-logPages').textContent = indexLogPage + 4;
////    generateLogPageNumbers(); // Generate new page numbers
////        });

////    // Event listener for "Previous" link
////    document.getElementById("previous-link").addEventListener("click", function () {
////            if (indexLogPage > 1) { // Check if current page is greater than 1
////        indexLogPage = indexLogPage - 1;
////    logsshowPage(indexLogPage);
////    document.getElementById('current-logPage').textContent = indexLogPage;
////    document.getElementById('total-logPages').textContent = indexLogPage + 4;
////    generateLogPageNumbers(); // Generate new page numbers
////            }
////        });
////    }
////    let logPage = 1;
////    $('.paginationLogs ul').on('click', '.page-link', function () {
////        let logPage = parseInt($(this).data('logPage'));
////    document.getElementById('current-logPage').textContent = logPage;
////    indexLogPage = logPage;
////    document.getElementById('total-logPages').textContent = indexLogPage + 4;
////    logsshowPage(logPage);
////    });

////    document.getElementById('current-logPage').textContent = logPage;
////    document.getElementById('total-logPages').textContent = indexLogPage + 4;
////    generateLogPageNumbers();

////</script>

////$(document).ready(function () {
//    //    // handle the click event of the pagination links
//    //    paginationLinks.forEach(link => {
//    //        link.addEventListener('click', event => {
//    //            event.preventDefault();
//    //            // get the selected page number from the link's text
//    //            const pageNumber = parseInt(link.innerText);

//    //            // show the items for the selected page
//    //            showPage(pageNumber);

//    //            // mark the selected page as active
//    //            paginationLinks.forEach(link => {
//    //                link.parentElement.classList.remove('active');
//    //            });
//    //            link.parentElement.classList.add('active');
//    //        });
//    //    });
//    //});