// src/app/features/npv-chart/npv-chart.component.ts
import {Component, Input, OnInit, OnChanges} from '@angular/core';
import {CommonModule} from '@angular/common';
import {ChartModule} from 'primeng/chart';
import {CardModule} from 'primeng/card';
import {NpvResult} from '../../core/models/npv-result';

@Component({
  selector: 'app-npv-chart',
  standalone: true,
  imports: [CommonModule, ChartModule, CardModule],
  templateUrl: './npv-chart.component.html',
  styleUrls: ['./npv-chart.component.scss']
})
export class NpvChartComponent implements OnInit, OnChanges {
  @Input() results: NpvResult[] = [];

  public chartData: any = {};
  public chartOptions: any = {};

  ngOnInit(): void {
    this.updateChart();
  }

  ngOnChanges(): void {
    this.updateChart();
  }

  private updateChart(): void {
    if (this.results && this.results.length > 0) {
      const labels = this.results.map(result => result.discountRate.toFixed(2));
      const data = this.results.map(result => result.npv);

      this.chartData = {
        labels: labels,
        datasets: [
          {
            label: 'NPV',
            data: data,
            borderColor: '#3b82f6',
            backgroundColor: 'rgba(59, 130, 246, 0.1)',
            fill: true,
            tension: 0.2
          }
        ]
      };

      this.chartOptions = {
        responsive: true,
        maintainAspectRatio: false,
        scales: {
          x: {
            title: {
              display: true,
              text: 'Discount Rate (%)'
            }
          },
          y: {
            title: {
              display: true,
              text: 'NPV ($)'
            },
            ticks: {
              callback: function (value: any) {
                return '$' + value.toLocaleString();
              }
            }
          }
        },
        plugins: {
          legend: {
            display: false
          }
        }
      };
    }
  }

}
