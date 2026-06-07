import { ApplicationRef, ComponentFactoryResolver, ComponentRef, Injectable, Injector } from '@angular/core';
import { GenericModalComponent } from '../generic-modal.component';

@Injectable({
  providedIn: 'root',
})
export class ModalService {
  output: { eventName: string, callbackFunc: any }[]
  constructor(
    private componentFactoryResolver: ComponentFactoryResolver,
    private appRef: ApplicationRef,
    private injector: Injector
  ) { }
  outputSetting(output: { eventName: string, callbackFunc: any }[]) {
    this.output = output;
  }
  open(component: any, data: any = {}, output: any = {}) {
    // Create a dynamic modal component
    const factory = this.componentFactoryResolver.resolveComponentFactory(GenericModalComponent);
    const componentRef: ComponentRef<GenericModalComponent> = factory.create(this.injector);

    // Pass the target component and data to the modal component
    componentRef.instance.component = component;
    componentRef.instance.data = data;

    // Provide the close method
    const closeModal = () => {
      this.appRef.detachView(componentRef.hostView);
      componentRef.destroy();
    };
    componentRef.instance.onClose = closeModal;

    // Attach the modal to the application
    this.appRef.attachView(componentRef.hostView);
    const domElem = (componentRef.hostView as any).rootNodes[0] as HTMLElement;
    document.body.appendChild(domElem);

    // Return the close method to allow external components to close the modal if needed
    return closeModal;
  }
}
