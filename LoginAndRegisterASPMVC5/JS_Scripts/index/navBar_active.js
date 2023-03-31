// Nav Bar Active
    // Get the current URL path
    var currentPath = window.location.pathname;

    // Find the link in the navigation bar that matches the current URL path and add the "active" class to it
    $('.navbar[href="' + currentPath + '"]').addClass('active');

    // Handle click events on the navigation links
    $('.navbar').click(function () {
        // Remove the "active" class from all links in the navigation bar
        $('.navbar').removeClass('active');

        // Add the "active" class to the clicked link
        $(this).addClass('active');
    });
