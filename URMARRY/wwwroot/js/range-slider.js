class RangeSlider {
    constructor(container) {
        this.container = container;

        this.fromSlider = container.querySelector('.fromSlider');
        this.toSlider   = container.querySelector('.toSlider');
        this.fromInput  = container.querySelector('.fromInput');
        this.toInput    = container.querySelector('.toInput');

        this.init();
    }

    init() {
        this.fillSlider();
        this.setToggleAccessible();

        this.fromSlider.addEventListener('input', () => this.controlFromSlider());
        this.toSlider.addEventListener('input', () => this.controlToSlider());
        this.fromInput.addEventListener('input', () => this.controlFromInput());
        this.toInput.addEventListener('input', () => this.controlToInput());
    }

    getParsed(a, b) {
        return [parseInt(a.value, 10), parseInt(b.value, 10)];
    }

    fillSlider() {
        const min = parseInt(this.toSlider.min);
        const max = parseInt(this.toSlider.max);
        const range = max - min;

        const fromPos = this.fromSlider.value - min;
        const toPos   = this.toSlider.value - min;

        this.toSlider.style.background = `
            linear-gradient(to right,
                #C6C6C6 0%,
                #C6C6C6 ${(fromPos / range) * 100}%,
                #a5be25 ${(fromPos / range) * 100}%,
                #a5be25 ${(toPos / range) * 100}%,
                #C6C6C6 ${(toPos / range) * 100}%,
                #C6C6C6 100%)`;
    }

    controlFromSlider() {
        if (+this.fromSlider.value > +this.toSlider.value) {
            this.fromSlider.value = this.toSlider.value;
        }
        this.fromInput.value = this.fromSlider.value;
        this.fillSlider();
    }

    controlToSlider() {
        if (+this.fromSlider.value <= +this.toSlider.value) {
            this.toInput.value = this.toSlider.value;
        } else {
            this.toSlider.value = this.fromSlider.value;
            this.toInput.value = this.fromSlider.value;
        }
        this.setToggleAccessible();
        this.fillSlider();
    }

    controlFromInput() {
        this.fromSlider.value =
            Math.min(+this.fromInput.value, +this.toInput.value);
        this.fillSlider();
    }

    controlToInput() {
        this.toSlider.value =
            Math.max(+this.fromInput.value, +this.toInput.value);
        this.setToggleAccessible();
        this.fillSlider();
    }

    setToggleAccessible() {
        this.toSlider.style.zIndex =
            Number(this.toSlider.value) <= 0 ? 2 : 0;
    }
}

document.addEventListener('DOMContentLoaded', () => {
    document
        .querySelectorAll('[data-range-slider]')
        .forEach(el => new RangeSlider(el));
});