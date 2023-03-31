//Pagination for Users Table

// define the number of items per page
$(document).ready(function () {
    Synch();
});

function Synch() {
    RealTimeUsersTable()
        .then(function () {
            showPage(1);
        })
        .catch(function (error) {
            alert(error);
        });
}

// Set up the event listener for options
$('.options_AdminRow li').click(function () {
    var optionText = $(this).find('.option_Admin-text').text();
    switch (optionText) {
        case "All rows":
            getTotalUsersCount().then(function (totalUsers) {
                ITEMS_PER_PAGE = totalUsers;

            }).catch(function (error) {
                console.error('Failed to get the total number of users.', error);
            });
            break;
        default:
            ITEMS_PER_PAGE = parseInt(optionText);
            break;
    }
    Synch(); // show the first page of the updated table
});



// get the table body element
const tableBody = document.querySelector('#usersTable tbody');

// get the pagination links
const paginationLinks = document.querySelectorAll('.pagination li');

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
    generatePageNumbers();
}

function RealTimeUsersTable() {
    return new Promise(function (resolve, reject) {
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

                    row += "<button onclick='editUser(\"" + Data.IdUser + "\", \"" + Data.FirstName + "\", \"" + Data.LastName + "\", \"" + Data.Email + "\", \"" + Data.IsAdmin + "\", \"update\")' class='user-button' data-bs-toggle='modal' data-bs-target='#editUsers-modal'>";
                    row += "<i class='bi bi-pencil-square'></i>";
                    row += "</button>";

                    row += "<button onclick='DeleteUser(\"" + Data.IdUser + "\", \"" + Data.FirstName + "\", \"" + Data.LastName + "\", \"delete\")' class='user-button' id='btnSet' data-bs-toggle='tooltip' data-bs-placement='top' title='Delete'>";
                    row += "<i class='bi bi-trash3-fill'></i>";
                    row += "</button>";

                    row += "</td></tr>";
                    $("#usersTable tbody").append(row);
                });
                resolve();
            },
            error: function () {
                reject("Failed to refresh users.");
            }
        });
    });
}

function setNotification(IdUser, FirstName, LastName, command) {
    $.ajax({
        type: 'POST',
        url: '/Home/UpdateEmailNotification',
        data: { IdUser: IdUser },
        success: function () {
            $(document).ready(function () {
                RealTimeUsersTable().then(function () {
                    showPage(indexPage);
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
      

//let currentPage = 1;
//const itemsPerPage = 5;
//const totalPages = Math.ceil(totalItems / itemsPerPage);

//function renderPagination() {
//    let paginationHTML = '';
//    if (currentPage === 1) {
//        paginationHTML += '<li class="page-item disabled"><a class="page-link" href="#">Prev</a></li>';
//    } else {
//        paginationHTML += '<li class="page-item"><a class="page-link" href="#" onclick="prevPage()">Prev</a></li>';
//    }

//    for (let i = 1; i <= totalPages; i++) {
//        if (i === currentPage) {
//            paginationHTML += '<li class="page-item active"><a class="page-link" href="#" onclick="goToPage(' + i + ')">' + i + '</a></li>';
//        } else {
//            paginationHTML += '<li class="page-item"><a class="page-link" href="#" onclick="goToPage(' + i + ')">' + i + '</a></li>';
//        }
//    }

//    if (currentPage === totalPages) {
//        paginationHTML += '<li class="page-item disabled"><a class="page-link" href="#">Next</a></li>';
//    } else {
//        paginationHTML += '<li class="page-item"><a class="page-link" href="#" onclick="nextPage()">Next</a></li>';
//    }

//    document.getElementById('pagination').innerHTML = paginationHTML;
//}

//function goToPage(page) {
//    currentPage = page;
//    renderPagination();
//    showPage(page);
//}

//function nextPage() {
//    if (currentPage < totalPages) {
//        currentPage++;
//        renderPagination();
//        showPage(currentPage);
//    }
//}

//function prevPage() {
//    if (currentPage > 1) {
//        currentPage--;
//        renderPagination();
//        showPage(currentPage);
//    }
//}

//renderPagination();
//showPage(currentPage);

//const pagination = document.querySelector('.pagination');
//const links = pagination.querySelectorAll('.page-link');

//function updatePagination(start) {
//    for (let i = 0; i < links.length; i++) {
//        if (i === 0 || i === links.length - 1 || (i >= start && i < start + 5)) {
//            links[i].style.display = 'inline-block';
//        } else {
//            links[i].style.display = 'none';
//        }
//    }
//}

//updatePagination(1);

//pagination.addEventListener('click', event => {
//    event.preventDefault();
//    if (event.target.classList.contains('page-link')) {
//        const current = pagination.querySelector('.active');
//        if (event.target.innerText === 'Next') {
//            if (current.nextElementSibling) {
//                current.nextElementSibling.classList.add('active');
//                current.classList.remove('active');
//                updatePagination(parseInt(current.nextElementSibling.innerText) - 1);
//            }
//        } else if (event.target.innerText === 'Previous') {
//            if (current.previousElementSibling) {
//                current.previousElementSibling.classList.add('active');
//                current.classList.remove('active');
//                updatePagination(parseInt(current.previousElementSibling.innerText) - 1);
//            }
//        } else {
//            current.classList.remove('active');
//            event.target.parentElement.classList.add('active');
//            updatePagination(parseInt(event.target.innerText) - 1);
//        }
//    }
//});


//const paginationLinks = document.querySelectorAll('.pagination .page-item a');
//const previousLink = document.querySelector('#previous-link');
//const nextLink = document.querySelector('#next-link');
//let currentPage = 1;

//// initialize pagination links
//paginationLinks.forEach((link, index) => {
//    if (index > 0 && index < 6) {
//        link.textContent = index + 1;
//    } else {
//        link.parentElement.style.display = 'none';
//    }
//});

//previousLink.addEventListener('click', () => {
//    if (currentPage > 1) {
//        currentPage--;
//        updatePaginationLinks();
//    }
//});

//nextLink.addEventListener('click', () => {
//    currentPage++;
//    updatePaginationLinks();
//});

//function updatePaginationLinks() {
//    // update page numbers
//    paginationLinks.forEach((link, index) => {
//        if (index > 0 && index < 6) {
//            link.textContent = currentPage + (index - 1);
//        }
//    });

//    // show/hide links based on current page
//    if (currentPage === 1) {
//        previousLink.style.display = 'none';
//        nextLink.style.display = '';
//    } else if (currentPage === 6) {
//        previousLink.style.display = '';
//        nextLink.style.display = 'none';
//    } else {
//        previousLink.style.display = '';
//        nextLink.style.display = '';
//    }

//    // mark current page as active
//    paginationLinks.forEach(link => {
//        if (link.textContent === currentPage.toString()) {
//            link.parentElement.classList.add('active');
//        } else {
//            link.parentElement.classList.remove('active');
//        }
//    });
//}

//// initialize pagination on page load
//updatePaginationLinks();
