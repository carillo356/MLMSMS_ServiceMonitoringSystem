//Pagination for Non-Admin in Users Table


// define the number of items per page
const ITEMS_PER_PAGE = 5;

// get the table body element
const tableBody = document.querySelector('#usersTable tbody');

// get the pagination links
const paginationLinks = document.querySelectorAll('.pagination ul');

// define a function to show the items for the selected page
function showPage(pageNumber) {
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

function RealTimeUsersTable() {
    /*Refresh the table with the updated data*/
    $.ajax({
        url: "/Home/RealTimeUsersTable",
        type: "GET",
        dataType: 'json',
        success: function (result) {
            $("#usersTable tbody").empty();
            // Loop through the data and append each row to the table
            result.forEach(function (Data) {
                const Role = (Data.IsAdmin === true) ? '<span><i class="bi bi-person-gear"></i></span>' : '<span><i class="bi bi-person"></i></span>';

                var row = "<tr data-toggle='modal' data-target='#service-modal'>";
                row += "<td>" + Data.FirstName + "</td>";
                row += "<td>" + Data.LastName + "</td>";
                row += "<td>" + Data.Email + "</td>";
                row += "<td>" + Role + "</td>";
                row += "<td>";
                if (Data.Email_Notification == true) {
                    row += "<button onclick='setNotification(\"" + Data.IdUser + "\", \"" + Data.FirstName + "\", \"" + Data.LastName + "\", \"off\")' class='user-button btn-green' id='btnSet' data-bs-toggle='tooltip' data-bs-placement='top' title='Set Email On'>";
                    row += "<i class='bi bi-envelope-check-fill'></i>";
                    row += "</button>";
                } else {
                    row += "<button onclick='setNotification(\"" + Data.IdUser + "\", \"" + Data.FirstName + "\", \"" + Data.LastName + "\", \"on\")' class='user-button btn-red' id='btnSet' data-bs-toggle='tooltip' data-bs-placement='top' title='Set Email Off'>";
                    row += "<i class='bi bi-envelope-x-fill'></i>";
                    row += "</button>";
                }
                row += "</td></tr>";
                $("#usersTable tbody").append(row);
            });
        },
        error: function () {
            alert("Failed to refresh users.");
        }
    });
}

// show the first page by default
$(document).ready(function () {
    RealTimeUsersTable().then(function () {
        showPage(pageNumber);
    });
});

function setNotification(IdUser, FirstName, LastName, command) {
    $.ajax({
        type: 'POST',
        url: '/Home/UpdateEmailNotification',
        data: { IdUser: IdUser },
        success: function () {
            $(document).ready(function () {
                RealTimeUsersTable().then(function () {
                    showPage(pageNumber);
                });
            });

            if (command === "on") {
                commandText = "RECEIVE";
            } else if (command === "off") {
                commandText = "NOT RECEIVE";
            }

            var toast = new bootstrap.Toast(document.getElementById('liveToast'));
            var toastMessage = "User " + "' " + FirstName + " " + LastName + " '" + " WILL " + commandText + " email notifications.";
            document.querySelector('.toast-body').innerHTML = toastMessage;

            if (commandText === "NOT RECEIVE") {
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
        },
        error: function (xhr, status, error) {
            alert(error);
        }
    });
}

var pageNumber = 1;
// handle the click event of the pagination links
paginationLinks.forEach(link => {
    link.addEventListener('click', event => {
        event.preventDefault();
        // get the selected page number from the link's text
        pageNumber = parseInt(link.innerText);

        // show the items for the selected page
        showPage(pageNumber);

        // mark the selected page as active
        paginationLinks.forEach(link => {
            link.parentElement.classList.remove('active');
        });
        link.parentElement.classList.add('active');
    });
});
