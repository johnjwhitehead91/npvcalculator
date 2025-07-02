import {ComponentFixture, TestBed} from '@angular/core/testing';

import {NpvCalculatorComponent} from './npv-calculator.component';

describe('NpvCalculatorComponent', () => {
  let component: NpvCalculatorComponent;
  let fixture: ComponentFixture<NpvCalculatorComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NpvCalculatorComponent]
    })
      .compileComponents();

    fixture = TestBed.createComponent(NpvCalculatorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
