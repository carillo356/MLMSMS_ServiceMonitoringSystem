// Selecting Rows Dropdown for Users (Non-Admin) Table
const optionMenu2 = document.querySelector(".nonAdminRow"),
    select_btn2 = optionMenu2.querySelector(".btn_nonAdminRow"),
    options2 = optionMenu2.querySelectorAll(".option_nonAdmin"),
    sBtn_text2 = optionMenu2.querySelector(".sBtn_nonAdminRow");

select_btn2.addEventListener("click", () => optionMenu2.classList.toggle("active"));

options2.forEach(option => {
    option.addEventListener("click", () => {
        let selectedOption = option.querySelector(".option_nonAdmin-text").innerText;
        sBtn_text2.innerText = selectedOption;

        optionMenu2.classList.remove("active");
    });
});