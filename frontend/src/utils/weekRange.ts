export interface WeekRange {
    start: Date;
    end: Date;
    days: Date[];
}

export function getWeekRange(anchor = new Date()): WeekRange {
    const start = new Date(
        anchor.getFullYear(),
        anchor.getMonth(),
        anchor.getDate()
    );
    const dayOffset = (start.getDay() + 6) % 7;
    start.setDate(start.getDate() - dayOffset);
    start.setHours(0, 0, 0, 0);

    const end = new Date(start);
    end.setDate(end.getDate() + 6);
    end.setHours(23, 59, 59, 999);

    const days = Array.from({ length: 7 }, (_, index) => {
        const day = new Date(start);
        day.setDate(day.getDate() + index);
        return day;
    });

    return { start, end, days };
}

export function shiftWeek(anchor: Date, amount: number): Date {
    const shifted = new Date(anchor);
    shifted.setDate(shifted.getDate() + amount * 7);
    return shifted;
}
