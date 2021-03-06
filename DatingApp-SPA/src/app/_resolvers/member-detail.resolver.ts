import {Injectable} from '@angular/core';
import {User} from '../_models/user';
import {Resolve, Router, ActivatedRouteSnapshot} from '@angular/router';
import { AlertifyService } from '../_services/alertify.service';
import { UserService } from '../_services/user.service';
import { Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';

@Injectable()
export class MemberDetailResolver implements Resolve<User> {

    constructor(private userServices: UserService, private router: Router, private alertify: AlertifyService) {}

    resolve(route: ActivatedRouteSnapshot): Observable<User> {
        return this.userServices.getUser(+route.params['id']).pipe(
            catchError(error => {
                this.alertify.error(error);
                this.router.navigate(['/members']);
                return of(null);
                })
        );
    }
}
