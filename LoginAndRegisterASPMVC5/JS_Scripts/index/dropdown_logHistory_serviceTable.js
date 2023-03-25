// Selecting Rows Dropdown for Log History
const optionMenu1 = document.querySelector(".select-menu"),
    select_btn1 = optionMenu1.querySelector(".select-btn1"),
    options1 = optionMenu1.querySelectorAll(".option"),
    sBtn_text1 = optionMenu1.querySelector(".sBtn-text");

select_btn1.addEventListener("click", () => optionMenu1.classList.toggle("active"));

options1.forEach(option => {
    option.addEventListener("click", () => {
        let selectedOption = option.querySelector(".option-text").innerText;
        sBtn_text1.innerText = selectedOption;

        optionMenu1.classList.remove("active");
    });
});

// Selecting Rows Dropdown for Service Table
const optionMenu2 = document.querySelector(".serviceRow"),
    select_btn2 = optionMenu2.querySelector(".btnRow"),
    options2 = optionMenu2.querySelectorAll(".optionRow"),
    sBtn_text2 = optionMenu2.querySelector(".sBtnRow");

select_btn2.addEventListener("click", () => optionMenu2.classList.toggle("active"));

options2.forEach(option => {
    option.addEventListener("click", () => {
        let selectedOption = option.querySelector(".optionRow-text").innerText;
        sBtn_text2.innerText = selectedOption;

        optionMenu2.classList.remove("active");
    });
});