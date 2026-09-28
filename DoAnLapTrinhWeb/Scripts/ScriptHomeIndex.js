document.addEventListener("DOMContentLoaded", function () {
    let stepDom = document.querySelector('.carousel-container');
    let listDom = document.querySelector('.carousel-container .list');
    let thumbnailDom = document.querySelector('.carousel-container .thumbnail');
    let nextDom = document.getElementById('next');
    let prevDom = document.getElementById('prev');

    let timeRunning = 500;
    let timeAutoNext = 7000;
    let runTimeOut;
    let runNextAuto = setTimeout(() => { nextDom.click(); }, timeAutoNext);

    nextDom.onclick = function () { showSlider('next'); }
    prevDom.onclick = function () { showSlider('prev'); }

    function showSlider(type) {
        let itemSlider = document.querySelectorAll('.carousel-container .list .item');
        let itemThumbnail = document.querySelectorAll('.carousel-container .thumbnail .item');

        if (type === 'next') {
            listDom.appendChild(itemSlider[0]);
            thumbnailDom.appendChild(itemThumbnail[0]);
            stepDom.classList.add('next');
        } else {
            listDom.prepend(itemSlider[itemSlider.length - 1]);
            thumbnailDom.prepend(itemThumbnail[itemThumbnail.length - 1]);
            stepDom.classList.add('prev');
        }

        clearTimeout(runTimeOut);
        runTimeOut = setTimeout(() => {
            stepDom.classList.remove('next');
            stepDom.classList.remove('prev');
        }, timeRunning);

        clearTimeout(runNextAuto);
        runNextAuto = setTimeout(() => { nextDom.click(); }, timeAutoNext);
    }
});