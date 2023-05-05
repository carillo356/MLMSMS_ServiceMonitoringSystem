// Clear all field when clicking the Cancel Button for Add users
document.getElementById('cancelButton').addEventListener('click', function () {
    document.getElementById('add-user-form').reset();
});

document.getElementById('addUserButton').addEventListener('click', function () {
        document.getElementById('add-user-form').reset();
        $('#validation-summary').html(''); // Clear validation summary
});

// Clear all fields when clicking the Cancel Button for Edit users
$(function () {
    // Function to clear all input fields
    function clearFields() {
        $('#edit-user-form input[type="text"], #edit-user-form input[type="email"], #edit-user-form input[type="password"], #edit-user-form input[type="radio"]').val('');
    }

    // Attach the clearFields function to the Cancel button click event
    $('#cancel-button').click(function () {
        clearFields();
    });
});
