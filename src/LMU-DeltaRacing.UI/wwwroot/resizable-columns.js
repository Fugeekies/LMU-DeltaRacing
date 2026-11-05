// Lets the user drag the right edge of a header cell to resize its column, in every table marked
// with data-resizable-columns. Widths are not remembered: a reload restores the layout.
// Everything is delegated from the document, so tables rendered later by Blazor work without any
// call from .NET, and no element is added that Blazor's diff would not know about.
(() => {
    "use strict";

    const edgeSize = 6;
    const minimumWidth = 40;
    let drag = null;

    const headerAt = (event) => {
        const header = event.target instanceof Element ? event.target.closest("th") : null;
        if (!header || !header.closest("table[data-resizable-columns] > thead")) {
            return null;
        }

        return header.getBoundingClientRect().right - event.clientX <= edgeSize ? header : null;
    };

    const headersOf = (table) => Array.from(table.tHead ? table.tHead.rows[0].cells : []);

    // Fixed layout gives every column the width of its header, so the current automatic widths
    // are copied first: the first drag must not move the other columns.
    const freeze = (table) => {
        if (table.classList.contains("columns-resized")) {
            return;
        }

        const headers = headersOf(table);
        const widths = headers.map((header) => header.getBoundingClientRect().width);
        headers.forEach((header, index) => {
            header.style.width = `${widths[index]}px`;
        });
        table.style.width = `${widths.reduce((total, width) => total + width, 0)}px`;
        table.classList.add("columns-resized");
    };

    const reset = (table) => {
        headersOf(table).forEach((header) => {
            header.style.width = "";
        });
        table.style.width = "";
        table.classList.remove("columns-resized");
    };

    document.addEventListener("pointermove", (event) => {
        if (drag) {
            const width = Math.max(minimumWidth, drag.startWidth + event.clientX - drag.startX);
            drag.header.style.width = `${width}px`;
            drag.table.style.width = `${drag.tableWidth + width - drag.startWidth}px`;
            return;
        }

        const target = event.target instanceof Element ? event.target.closest("table[data-resizable-columns] > thead th") : null;
        if (target) {
            target.classList.toggle("column-resize-edge", headerAt(event) === target);
        }
    });

    document.addEventListener("pointerout", (event) => {
        if (!drag && event.target instanceof Element && event.target.matches("th.column-resize-edge")) {
            event.target.classList.remove("column-resize-edge");
        }
    });

    document.addEventListener("pointerdown", (event) => {
        const header = event.button === 0 ? headerAt(event) : null;
        if (!header) {
            return;
        }

        const table = header.closest("table");
        freeze(table);
        drag = {
            header,
            table,
            startX: event.clientX,
            startWidth: header.getBoundingClientRect().width,
            tableWidth: table.getBoundingClientRect().width
        };
        document.documentElement.classList.add("column-resizing");
        event.preventDefault();
    });

    const stop = () => {
        if (drag) {
            drag = null;
            document.documentElement.classList.remove("column-resizing");
        }
    };

    document.addEventListener("pointerup", stop);
    document.addEventListener("pointercancel", stop);

    // A double click on an edge gives the table back its automatic widths.
    document.addEventListener("dblclick", (event) => {
        const header = headerAt(event);
        if (header) {
            reset(header.closest("table"));
            event.preventDefault();
        }
    });
})();
