(function () {
    function f({ a } = { a: 1 }, c = f({ a: 1 }, 2)) {
        if (!c)
            return 1;
    }
    f();
})();