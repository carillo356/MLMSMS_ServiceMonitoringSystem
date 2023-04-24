//Selecting Rows Dropdown for Users (Admin) Table
const optionMenu2 = document.querySelector(".adminRow"),
    select_btn2 = optionMenu2.querySelector(".btn_AdminRow"),
    options2 = optionMenu2.querySelectorAll(".option_Admin"),
    sBtn_text2 = optionMenu2.querySelector(".sBtn_AdminRow");

select_btn2.addEventListener("click", () => optionMenu2.classList.toggle("active"));

options2.forEach(option => {
    option.addEventListener("click", () => {
        let selectedOption = option.querySelector(".option_Admin-text").innerText;
        sBtn_text2.innerText = selectedOption;

        optionMenu2.classList.remove("active");
    });
});

// Show/Hide Password with eye icon
$(".toggle-password").click(function () {
    var input = $($(this).attr("toggle"));
    if (input.attr("type") == "password") {
        input.attr("type", "text");
        $(this).removeClass("bi-eye-slash-fill").addClass("bi-eye-fill");
    } else {
        input.attr("type", "password");
        $(this).removeClass("bi-eye-fill").addClass("bi-eye-slash-fill");
    }
});