import { Overlay, OverlayRef } from '@angular/cdk/overlay';
import { ComponentPortal } from '@angular/cdk/portal';
import { Injectable } from '@angular/core';
import { defer, NEVER } from 'rxjs';
import { finalize, share } from 'rxjs/operators';
import { LoaderComponent } from '../loader.component';

@Injectable({
  providedIn: 'root'
})
export class LoaderService {
  private overlayRef: OverlayRef | undefined = undefined;

  constructor(private overlay: Overlay) { }
  public readonly spinner$ = defer(() => {
    this.show();
    return NEVER.pipe(
      finalize(() => {
        this.hide();

      })
    );
  }).pipe(share());
  public show(): void {
    // Hack avoiding `ExpressionChangedAfterItHasBeenCheckedError` error
    Promise.resolve(null).then(() => {
      this.overlayRef = this.overlay.create({
        positionStrategy: this.overlay
          .position()
          .global()
          .centerHorizontally()
          .centerVertically(),
        hasBackdrop: true,
      });
      this.overlayRef.attach(new ComponentPortal(LoaderComponent));
    });
  }

  public hide(): void {
    this.overlayRef?.detach();
    this.overlayRef = undefined;
  }
}