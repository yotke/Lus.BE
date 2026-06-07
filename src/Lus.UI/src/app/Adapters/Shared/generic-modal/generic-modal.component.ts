import {
  Component, ComponentFactoryResolver, ComponentRef, EventEmitter, Input, OnDestroy, OnInit, Output, ViewChild, ViewContainerRef
} from '@angular/core';

@Component({
  selector: 'app-generic-modal',
  templateUrl: './generic-modal.component.html',
  styleUrls: ['./generic-modal.component.scss'],
})
export class GenericModalComponent implements OnInit, OnDestroy {
  @Input() component: any;
  @Input() title: string;
  @Input() data: any;
  @ViewChild('dynamicComponentContainer', { read: ViewContainerRef, static: true }) container!: ViewContainerRef;
  private componentRef!: ComponentRef<any>;

  // Automatically handle all dynamic outputs using a single EventEmitter
  @Output() eventHandler = new EventEmitter<{ eventName: string, data: any }>();

  @Input() onClose: () => void;

  constructor(private componentFactoryResolver: ComponentFactoryResolver) { }

  ngOnInit(): void {
    if (this.component) {
      const componentFactory = this.componentFactoryResolver.resolveComponentFactory(this.component);
      this.componentRef = this.container.createComponent(componentFactory);

      // Pass data to the component instance
      if (this.data) {
        Object.assign(this.componentRef.instance, this.data);
      }

      // Automatically bind outputs without explicitly declaring them
      this.bindOutputsDynamically();
    }
  }

  private bindOutputsDynamically() {
    const instance = this.componentRef.instance;

    for (const key in instance) {
      if (instance[key] instanceof EventEmitter) {
        // Subscribe and forward events to the parent using a single EventEmitter
        instance[key].subscribe((data: any) => {
          this.eventHandler.emit({ eventName: key, data });
        });
      }
    }
  }

  closeModal() {
    if (this.onClose) {
      this.onClose();
    }
  }

  ngOnDestroy(): void {
    if (this.componentRef) {
      this.componentRef.destroy();
    }
  }
}
