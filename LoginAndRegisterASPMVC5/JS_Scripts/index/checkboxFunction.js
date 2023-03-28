// CheckBox Function
$(document).ready(function () {
    RealTimeCheckbox();
});
var _servicesInController;
function RealTimeCheckbox() {
    GetServicesInController()
        .then(function () {
            Checkbox(_servicesInController);
        })
        .catch(function (error) {
            alert(error);
        });
}

function GetServicesInController() {
    return new Promise(function (resolve, reject) {
        $.ajax({
            url: "/Home/GetServicesInController",
            type: 'GET',
            dataType: 'json',
            success: function (result) {
                _servicesInController = result;
                resolve();
            },
            error: function (xhr, textStatus, errorThrown) {
                reject('Error Geting service names');
            }
        });
    });
}

function Checkbox(_servicesInController) {
    if (_servicesInController.length > 0) {
        var checkboxes = '';
        $.each(_servicesInController, function (i, item) {
            var checkbox = `<li class="item"><div class="form-check">
                                    <input class="form-check-input" type="checkbox" value="${item}" id="service-${i}">
                                    <label class="form-check-label" for="service-${i}">${item}</label>
                                    </div></li>`;
            checkboxes += checkbox;

        });
        $('#serviceCheckboxes').html(/*checkedItems.join('') +*/ checkboxes);
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

//$('.btn-primary').on('click', function () {
//    $('input[type="checkbox"]:checked').each(function () {
//        AddService($(this).val());
//    });
//    $('#displayService-modal').modal('hide');
//});

$('.btn-primary').on('click', function () {
    var checkedCount = $('input[type="checkbox"]:checked').length;
    var servicesAdded = [];

    $('input[type="checkbox"]:checked').each(function () {
        var serviceName = $(this).val();
        AddService(serviceName);
        servicesAdded.push(serviceName);
    });

    if (servicesAdded.length > 0) {
        var toast = new bootstrap.Toast(document.getElementById('liveToast'));
        var toastMessage = "You have added " + checkedCount + " service(s)";
        document.querySelector('.toast-body').innerHTML = toastMessage;
        // set background color to green if service/s has been added.
        toast._element.classList.remove("text-bg-danger");
        toast._element.classList.add("bg-success");

        toast.show();

        setTimeout(function () {
            toast.dispose();
        }, 2000);
    }

    $('#displayService-modal').modal('hide');
});