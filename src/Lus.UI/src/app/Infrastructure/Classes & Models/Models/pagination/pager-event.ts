export class PagerEvent {
    func: Function | null;
    page: number | null;
    pageSize: number | null;

    constructor(f?: Function, p?: number, s?: number) {
        this.func = f ?? null;
        this.page = p ?? null;
        this.pageSize = s ?? null;
    }
}