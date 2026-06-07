import { HttpContextToken } from "@angular/common/http";

export class CustomHttpContextTokens {
    static BYPASS_LOG = new HttpContextToken(() => false);
    static BYPASS_SPINNER = new HttpContextToken(() => false);
}
