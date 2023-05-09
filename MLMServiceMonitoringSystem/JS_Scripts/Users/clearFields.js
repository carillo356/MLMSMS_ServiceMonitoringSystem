﻿// Clear all field when clicking the Cancel Button for Add users
document.getElementById('cancelButton').addEventListener('click', function () {
    document.getElementById('add-user-form').reset();

    document.getElementById('addUserButton').addEventListener('click', function () {
        setTimeout(function () {
            document.getElementById('add-user-form').reset();
        }, 500);
    });
});

// Clear all fields when clicking the Cancel Button for Edit users
$(function () {
    // Function to clear all input fields
    function clearFields() {
        $('#edit-user-form')[0].reset();
        $('#validation-summary-edit').html('');
    }

    // Attach the clearFields function to the Cancel button click event
    $('#cancel-button').click(function () {
        clearFields();
    });
});

// Clear all fields when clicking the Cancel Button for Update Password
$(function () {
    // Function to clear all input fields
    function clearFields() {
        $('#change-pass-form input[type="password"]').val('');
        $('#validation-summary-edit').html('');
    }

    // Attach the clearFields function to the Cancel button click event
    $('#cancelButton').click(function () {
        clearFields();
        $('#error-container-changePass').html('');
        $('#password2').val('');
    });
});




