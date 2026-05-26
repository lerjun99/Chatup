function renderCalendar(events, dotNetRef) {

    var el = document.getElementById('calendar');

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

    calendar.render();
}