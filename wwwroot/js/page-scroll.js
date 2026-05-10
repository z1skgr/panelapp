document.addEventListener("DOMContentLoaded", function () {
    const params = new URLSearchParams(window.location.search);
    const scrollTo = params.get("scrollTo");

    if (!scrollTo) {
        return;
    }

    const element = document.getElementById(scrollTo);

    if (element) {
        element.scrollIntoView({
            behavior: "smooth",
            block: "center"
        });
    }
});