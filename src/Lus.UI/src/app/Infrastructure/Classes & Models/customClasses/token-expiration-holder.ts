export class TokenExpirationHolder {
    constructor(expiration?: number, expirationdate?: Date, lastping?: Date) {
        this.Expiration = expiration ?? null;
        this.ExpirationDate = expirationdate ?? null;
        this.LastPing = lastping ?? null;
    }

    Expiration: number | null;
    ExpirationDate: Date | null;
    LastPing: Date | null;
}
