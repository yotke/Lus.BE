import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../Infrastructure/Services/Auth/auth.service';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
})
export class LoginComponent {
  loginForm: FormGroup;
  hidePassword = true;
  isSubmitting = false;
  errorMessage: string | null = null;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {
    this.loginForm = this.fb.group({
      email: [environment.production ? '' : environment.SignIn_Email, [Validators.required, Validators.email]],
      password: [environment.production ? '' : environment.SignIn_Password, [Validators.required]],
    });
  }

  get email() {
    return this.loginForm.get('email');
  }

  get password() {
    return this.loginForm.get('password');
  }

  onSubmit(): void {
    this.errorMessage = null;

    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    const { email, password } = this.loginForm.value;

    this.authService.loginWithPassword(email, password).subscribe({
      next: response => {
        this.isSubmitting = false;
        const isSuccess = response?.isSuccess ?? response?.IsSuccess;
        if (isSuccess) {
          this.router.navigate(['/Home']);
        } else {
          this.errorMessage = this.mapError(response);
        }
      },
      error: () => {
        this.isSubmitting = false;
        this.errorMessage = 'אירעה שגיאה בהתחברות. נסו שוב מאוחר יותר.';
      },
    });
  }

  private mapError(response: any): string {
    const exceptionId = response?.exceptionId ?? response?.ExceptionId;
    const serverMessage = response?.errorMessage ?? response?.ErrorMessage;

    switch (exceptionId) {
      case 10:
        return 'משתמש לא נמצא.';
      case 11:
        return 'המשתמש טרם אומת.';
      case 12:
        return 'שם משתמש או סיסמה שגויים.';
      case 13:
        return serverMessage || 'תוקף הסיסמה פג. יש לאפס סיסמה.';
      case 41:
        return 'אימות אבטחה נכשל. נסו שוב.';
      case 101: {
        const lockTime = response?.lockTimeLeft ?? response?.LockTimeLeft;
        return lockTime
          ? `החשבון ננעל. נסו שוב בעוד ${Math.ceil(lockTime)} דקות.`
          : 'החשבון ננעל זמנית עקב ניסיונות התחברות מרובים.';
      }
      default:
        return serverMessage || 'ההתחברות נכשלה. בדקו את הפרטים ונסו שוב.';
    }
  }
}
