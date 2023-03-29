// Pagination for Service Table


// define the number of items per page
const ITEMS_PER_PAGE = 5;

// get the table body element
const tableBody = document.querySelector('#serviceTable tbody');

// get the pagination links
const paginationLinks = document.querySelectorAll('.paginationService ul');

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

// handle the click event of the pagination links
paginationLinks.forEach(link => {
    link.addEventListener('click', event => {
        event.preventDefault();
        // get the selected page number from the link's text
        const pageNumber = parseInt(link.innerText);

        // show the items for the selected page
        showPage(pageNumber);

        // mark the selected page as active
        paginationLinks.forEach(link => {
            link.parentElement.classList.remove('active');
        });
        link.parentElement.classList.add('active');
    });
});

