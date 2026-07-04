// Theme toggle switcher script for Smart Eye Clinic Hospital Management System
document.addEventListener("DOMContentLoaded", function () {
    const themeToggleBtn = document.getElementById("themeToggleBtn");
    const themeToggleIcon = document.getElementById("themeToggleIcon");

    if (themeToggleBtn && themeToggleIcon) {
        // Check local storage for current user theme choice
        const currentTheme = localStorage.getItem("theme");
        if (currentTheme === "dark") {
            document.body.classList.add("dark-theme");
            themeToggleIcon.classList.replace("fa-regular", "fa-solid");
            themeToggleIcon.classList.replace("fa-moon", "fa-sun");
            themeToggleIcon.classList.add("text-warning");
        }

        themeToggleBtn.addEventListener("click", function () {
            document.body.classList.toggle("dark-theme");
            
            if (document.body.classList.contains("dark-theme")) {
                localStorage.setItem("theme", "dark");
                themeToggleIcon.classList.replace("fa-regular", "fa-solid");
                themeToggleIcon.classList.replace("fa-moon", "fa-sun");
                themeToggleIcon.classList.add("text-warning");
            } else {
                localStorage.setItem("theme", "light");
                themeToggleIcon.classList.replace("fa-solid", "fa-regular");
                themeToggleIcon.classList.replace("fa-sun", "fa-moon");
                themeToggleIcon.classList.remove("text-warning");
            }
        });
    }
});
