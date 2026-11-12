function renderCalendar(events, dotNetRef) {

    var el = document.getElementById('calendar');
    if (!el) return;

    // Re-rendering must not stack a second calendar on the same element
    if (el._fcInstance) {
        el._fcInstance.destroy();
    }

    var calendar = new FullCalendar.Calendar(el, {

        initialView: 'dayGridMonth',
        height: "auto",
        editable: true,
        selectable: true,
        events: events,

        // CLICK DATE → CREATE TICKET
        dateClick: function (info) {
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync(
                    'OnDateClick',
                    info.dateStr
                );
            }
        },

        // DRAG & DROP EVENT
        eventDrop: function (info) {
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync(
                    'OnEventDrop',
                    info.event.id,
                    info.event.start.toISOString()
                );
            }
        },

        // MONTH CHANGE
        datesSet: function (info) {
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync(
                    'OnMonthChanged',
                    info.start.toISOString()
                );
            }
        }
    });

    el._fcInstance = calendar;
    calendar.render();
}

// Read-only calendar for the dashboard. Reports the month actually in view
// (view.currentStart) rather than the first visible grid cell.
function renderDashboardCalendar(elementId, events, dotNetRef) {

    var el = document.getElementById(elementId);
    if (!el || typeof FullCalendar === 'undefined') return false;

    if (el._fcInstance) {
        el._fcInstance.destroy();
    }

    var calendar = new FullCalendar.Calendar(el, {
        initialView: 'dayGridMonth',
        height: 'auto',
        fixedWeekCount: false,
        dayMaxEvents: 2,
        headerToolbar: { left: 'title', center: '', right: 'today prev,next' },
        events: events,

        datesSet: function (info) {
            if (dotNetRef) {
                var start = info.view.currentStart;
                var month = start.getFullYear() + '-' +
                    String(start.getMonth() + 1).padStart(2, '0') + '-01';
                dotNetRef.invokeMethodAsync('OnVisibleMonthChanged', month);
            }
        }
    });

    el._fcInstance = calendar;
    calendar.render();
    return true;
}

function destroyDashboardCalendar(elementId) {
    var el = document.getElementById(elementId);
    if (el && el._fcInstance) {
        el._fcInstance.destroy();
        el._fcInstance = null;
    }
}
