import {ComponentFixture, TestBed} from '@angular/core/testing';

import {NpvChartComponent} from './npv-chart.component';

describe('NpvChartComponent', () => {
  let component: NpvChartComponent;
  let fixture: ComponentFixture<NpvChartComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NpvChartComponent]
    })
      .compileComponents();

    fixture = TestBed.createComponent(NpvChartComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
