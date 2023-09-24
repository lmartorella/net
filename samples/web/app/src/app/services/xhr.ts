import { HttpErrorResponse } from "@angular/common/http";
import { Observable, catchError, of } from "rxjs";

export interface IModuleConfig {
    baseUrl?: string;
}

export const config: IModuleConfig = {
    baseUrl: ""
};

export const checkXhr = <T>(observable: Observable<T>): Promise<T> => {
    return new Promise((resolve, reject) => {
        observable.pipe(catchError((err: HttpErrorResponse) => {
            reject(new Error(err.statusText || err.error || err.message));
            return of({ } as T);
        })).subscribe(data => {
            if ((data as T & { error?: string })?.error) {
                reject(new Error((data as T & { error?: string }).error));
            } else if (!data) {
                reject(new Error("Server down"));
            } else {
                resolve(data);
            }
        });
    });
};
