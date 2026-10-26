document.addEventListener('DOMContentLoaded', () => {
    const form = document.getElementById('manhours-form');
    const data = document.getElementById('badge-generator-data');
    const result = document.getElementById('result');
    const previewLink = document.getElementById('preview-link');
    const loadingIcon = document.getElementById('loading-icon');
    const previewError = document.getElementById('preview-error');
    let previewImg = document.getElementById('preview-img');

    form.addEventListener('submit', event => {
        event.preventDefault();
        let cloneUrl = document.getElementById('cloneUrl').value.trim();
        if (!cloneUrl) {
            const input = document.getElementById('cloneUrl');
            input.setCustomValidity(data.querySelector('[data-key="empty-url"]').textContent);
            input.reportValidity();
            return;
        }

        if (cloneUrl.endsWith('.git')) {
            cloneUrl = cloneUrl.substring(0, cloneUrl.length - 4);
        }
        if (cloneUrl.startsWith('https://')) {
            cloneUrl = cloneUrl.substring(8);
        }

        const statsLink = `${data.dataset.origin}/r/${cloneUrl}.html`;
        const imgLink = document.getElementById('directMode').checked
            ? `${data.dataset.origin}/r/${cloneUrl}.svg`
            : `https://img.shields.io/endpoint?url=${encodeURIComponent(`${data.dataset.origin}/r/${cloneUrl}.json`)}`;
        document.getElementById('markdown').value = `[![Man hours](${imgLink})](${statsLink})`;
        document.getElementById('html').value = `<a href="${statsLink}" target="_blank"><img alt="Man hours" src="${imgLink}" /></a>`;
        previewLink.href = statsLink;
        previewError.classList.add('d-none');
        loadingIcon.classList.remove('d-none');
        result.classList.remove('d-none');

        // Separate image instances prevent an earlier request from changing the latest preview.
        const nextImage = previewImg.cloneNode(false);
        nextImage.removeAttribute('src');
        nextImage.classList.add('d-none');
        previewImg.replaceWith(nextImage);
        previewImg = nextImage;
        nextImage.onload = () => {
            if (previewImg !== nextImage) return;
            loadingIcon.classList.add('d-none');
            nextImage.classList.remove('d-none');
        };
        nextImage.onerror = () => {
            if (previewImg !== nextImage) return;
            loadingIcon.classList.add('d-none');
            previewError.classList.remove('d-none');
        };
        nextImage.src = imgLink;
    });

    document.getElementById('cloneUrl').addEventListener('input', event => {
        event.target.setCustomValidity('');
    });
});
