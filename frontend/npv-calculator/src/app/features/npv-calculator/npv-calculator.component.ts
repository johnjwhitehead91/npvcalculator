// src/app/features/npv-calculator/npv-calculator.component.ts
import {Component, OnInit, OnDestroy} from '@angular/core';
import {FormBuilder, FormGroup, FormArray, Validators, ReactiveFormsModule} from '@angular/forms';
import {Subject, takeUntil, switchMap, catchError, of, Subscription} from 'rxjs';
import {CommonModule, DecimalPipe, DatePipe} from '@angular/common';
import * as XLSX from 'xlsx';
import jsPDF from 'jspdf';
import autoTable from 'jspdf-autotable';

// PrimeNG Imports
import {ButtonModule} from 'primeng/button';
import {InputNumberModule} from 'primeng/inputnumber';
import {CardModule} from 'primeng/card';
import {TableModule} from 'primeng/table';
import {ProgressBarModule} from 'primeng/progressbar';
import {MessageModule} from 'primeng/message';
import {MessagesModule} from 'primeng/messages';
import {DividerModule} from 'primeng/divider';
import {TagModule} from 'primeng/tag';
import {TooltipModule} from 'primeng/tooltip';
import {ChartModule} from 'primeng/chart';

// Services and Models
import {NpvApiService} from '../../services/npv-api.service';
import {NpvCalculationStatus} from '../../core/models/npv-calculation-status';
import {NpvCalculationResponse} from '../../core/models/npv-calculation-response';
import {NpvRequest} from '../../core/models/npv-request';
import {NpvChartComponent} from '../npv-chart/npv-chart.component';

@Component({
  selector: 'app-npv-calculator',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    DecimalPipe,
    ButtonModule,
    InputNumberModule,
    CardModule,
    TableModule,
    ProgressBarModule,
    MessageModule,
    MessagesModule,
    DividerModule,
    TagModule,
    TooltipModule,
    ChartModule,
    NpvChartComponent
  ],
  providers: [DatePipe],
  templateUrl: './npv-calculator.component.html',
  styleUrls: ['./npv-calculator.component.scss']
})
export class NpvCalculatorComponent implements OnInit, OnDestroy {
  npvForm: FormGroup;
  isCalculating = false;
  calculationId: string | null = null;
  calculationStatus: NpvCalculationStatus | null = null;
  calculationResults: NpvCalculationResponse | null = null;
  errorMessage: string | null = null;
  private destroy$ = new Subject<void>();
  private pollingSubscription?: Subscription;

  constructor(
    private fb: FormBuilder,
    private npvApiService: NpvApiService
  ) {
    this.npvForm = this.fb.group({
      initialInvestment: [-10000, [Validators.required]], // NEW: Initial investment field
      cashFlows: this.fb.array([], [Validators.required, Validators.minLength(1)]),
      lowerBoundDiscountRate: [1.0, [Validators.required, Validators.min(0.01), Validators.max(100)]],
      upperBoundDiscountRate: [15.0, [Validators.required, Validators.min(0.01), Validators.max(100)]],
      discountRateIncrement: [0.25, [Validators.required, Validators.min(0.01), Validators.max(10)]]
    });
  }

  ngOnInit(): void {
    this.addCashFlow();
    this.addCashFlow();
    this.addCashFlow();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.pollingSubscription?.unsubscribe();
  }

  get cashFlowsArray(): FormArray {
    return this.npvForm.get('cashFlows') as FormArray;
  }

  addCashFlow(): void {
    const cashFlowControl = this.fb.control(0, [Validators.required]);
    this.cashFlowsArray.push(cashFlowControl);
  }

  removeCashFlow(index: number): void {
    if (this.cashFlowsArray.length > 1) {
      this.cashFlowsArray.removeAt(index);
    }
  }

  onSubmit(): void {
    if (this.npvForm.valid) {
      this.isCalculating = true;
      this.errorMessage = null;
      this.calculationResults = null;
      this.calculationStatus = null;

      const request: NpvRequest = {
        initialInvestment: this.npvForm.value.initialInvestment, // NEW: Include initial investment
        cashFlows: this.npvForm.value.cashFlows,
        lowerBoundDiscountRate: this.npvForm.value.lowerBoundDiscountRate,
        upperBoundDiscountRate: this.npvForm.value.upperBoundDiscountRate,
        discountRateIncrement: this.npvForm.value.discountRateIncrement
      };

      this.npvApiService.calculateNpv(request)
        .pipe(
          takeUntil(this.destroy$),
          switchMap(response => {
            this.calculationId = response.calculationId;
            this.pollingSubscription = this.npvApiService.pollStatus(response.calculationId)
              .pipe(
                takeUntil(this.destroy$),
                catchError(error => {
                  this.errorMessage = 'Error starting calculation: ' + (error.error || error.message || 'Unknown error');
                  this.isCalculating = false;
                  return of(null);
                })
              )
              .subscribe(status => {
                if (status) {
                  this.calculationStatus = status;
                  if (status.status === 'Completed') {
                    this.loadResults();
                    this.pollingSubscription?.unsubscribe();
                  } else if (status.status === 'Failed') {
                    this.errorMessage = 'Calculation failed';
                    this.isCalculating = false;
                    this.pollingSubscription?.unsubscribe();
                  }
                }
              });
            return of(null);
          })
        )
        .subscribe();
    }
  }

  private loadResults(): void {
    if (this.calculationId) {
      this.npvApiService.getResults(this.calculationId)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (results) => {
            this.calculationResults = results;
            this.isCalculating = false;
          },
          error: (error) => {
            this.errorMessage = 'Error loading results: ' + (error.error || error.message || 'Unknown error');
            this.isCalculating = false;
          }
        });
    }
  }

  resetForm(): void {
    this.npvForm.reset({
      initialInvestment: -10000, // NEW: Reset initial investment
      lowerBoundDiscountRate: 1.0,
      upperBoundDiscountRate: 15.0,
      discountRateIncrement: 0.25
    });

    // Clear cash flows and add default ones
    while (this.cashFlowsArray.length > 0) {
      this.cashFlowsArray.removeAt(0);
    }
    this.addCashFlow();
    this.addCashFlow();
    this.addCashFlow();

    this.isCalculating = false;
    this.calculationId = null;
    this.calculationStatus = null;
    this.calculationResults = null;
    this.errorMessage = null;
  }

  getProgressPercentage(): number {
    if (!this.calculationStatus || this.calculationStatus.totalCalculations === 0) {
      return 0;
    }
    return (this.calculationStatus.progress / this.calculationStatus.totalCalculations) * 100;
  }

  // Helper methods for template
  getResultsCount(): number {
    return this.calculationResults?.results?.length || 0;
  }

  getMinDiscountRate(): number {
    const results = this.calculationResults?.results;
    return results && results.length > 0 ? results[0].discountRate : 0;
  }

  getMaxDiscountRate(): number {
    const results = this.calculationResults?.results;
    return results && results.length > 0 ? results[results.length - 1].discountRate : 0;
  }

  getCalculatedAt(): string {
    return this.calculationResults?.calculatedAt || '';
  }

  getBreakEvenRate(): string {
    if (!this.calculationResults?.results || this.calculationResults.results.length === 0) return 'N/A';

    const results = this.calculationResults.results;

    // Check if all NPVs are positive (IRR > upper bound)
    const allPositive = results.every(result => result.npv > 0);
    if (allPositive) {
      const maxRate = Math.max(...results.map(r => r.discountRate));
      return `> ${maxRate.toFixed(2)}%`;
    }

    // Check if all NPVs are negative (IRR < lower bound)
    const allNegative = results.every(result => result.npv < 0);
    if (allNegative) {
      const minRate = Math.min(...results.map(r => r.discountRate));
      return `< ${minRate.toFixed(2)}%`;
    }

    // Find where NPV crosses from positive to negative
    const breakEvenIndex = results.findIndex((result, index) =>
      result.npv < 0 && (index === 0 || results[index - 1].npv >= 0)
    );

    if (breakEvenIndex > 0) {
      const prevResult = results[breakEvenIndex - 1];
      const currResult = results[breakEvenIndex];

      // Linear interpolation for more accurate break-even
      const slope = (currResult.npv - prevResult.npv) / (currResult.discountRate - prevResult.discountRate);
      const breakEvenRate = prevResult.discountRate - (prevResult.npv / slope);
      return `~${breakEvenRate.toFixed(2)}%`;
    }

    return 'N/A';
  }

  getInvestmentDecision(): string {
    if (!this.calculationResults?.results || this.calculationResults.results.length === 0) return 'No data';

    const results = this.calculationResults.results;
    const positiveNPVCount = results.filter(r => r.npv > 0).length;
    const totalCount = results.length;

    if (positiveNPVCount > totalCount / 2) {
      return 'Generally Favorable';
    } else if (positiveNPVCount > 0) {
      return 'Depends on Cost of Capital';
    } else {
      return 'Not Recommended';
    }
  }

  getHighestNPV(): number {
    if (!this.calculationResults?.results || this.calculationResults.results.length === 0) return 0;
    return Math.max(...this.calculationResults.results.map(r => r.npv));
  }

  getLowestNPV(): number {
    if (!this.calculationResults?.results || this.calculationResults.results.length === 0) return 0;
    return Math.min(...this.calculationResults.results.map(r => r.npv));
  }

  // Export methods
  exportToExcel(): void {
    if (!this.calculationResults?.results) return;

    // Create summary sheet data
    const summaryData = [
      ['Metric', 'Value'],
      ['Initial Investment', `$${this.npvForm.value.initialInvestment?.toLocaleString()}`],
      ['Break-Even Rate (IRR)', this.getBreakEvenRate()],
      ['Investment Decision', this.getInvestmentDecision()],
      ['Highest NPV', `$${this.getHighestNPV().toLocaleString()}`],
      ['Lowest NPV', `$${this.getLowestNPV().toLocaleString()}`],
      ['Total Scenarios', this.getResultsCount().toString()],
      ['Rate Range', `${this.getMinDiscountRate().toFixed(2)}% - ${this.getMaxDiscountRate().toFixed(2)}%`]
    ];

    // Create detailed results data
    const detailedData = this.calculationResults.results.map(result => ({
      'Discount Rate (%)': result.discountRate.toFixed(2),
      'NPV (USD)': result.npv.toFixed(2),
      'Decision': result.npv >= 0 ? 'Accept' : 'Reject'
    }));

    const workbook = XLSX.utils.book_new();

    // Add summary sheet
    const summaryWorksheet = XLSX.utils.aoa_to_sheet(summaryData);
    XLSX.utils.book_append_sheet(workbook, summaryWorksheet, 'Summary');

    // Add detailed results sheet
    const detailedWorksheet = XLSX.utils.json_to_sheet(detailedData);
    XLSX.utils.book_append_sheet(workbook, detailedWorksheet, 'Detailed Results');

    const fileName = `NPV_Analysis_${new Date().toISOString().split('T')[0]}.xlsx`;
    XLSX.writeFile(workbook, fileName);
  }

  exportToPDF(): void {
    if (!this.calculationResults?.results) return;

    const doc = new jsPDF();

    // Add title
    doc.setFontSize(20);
    doc.setFont('helvetica', 'bold');
    doc.text('NPV Analysis Report', 105, 20, {align: 'center'});

    // Add date
    doc.setFontSize(10);
    doc.setFont('helvetica', 'normal');
    doc.text(`Generated on: ${new Date().toLocaleDateString()}`, 105, 30, {align: 'center'});

    // Add summary section
    doc.setFontSize(14);
    doc.setFont('helvetica', 'bold');
    doc.text('Investment Summary', 20, 50);

    doc.setFontSize(10);
    doc.setFont('helvetica', 'normal');

    const summaryData = [
      ['Initial Investment', `$${this.npvForm.value.initialInvestment?.toLocaleString()}`],
      ['Break-Even Rate (IRR)', this.getBreakEvenRate()],
      ['Investment Decision', this.getInvestmentDecision()],
      ['Highest NPV', `$${this.getHighestNPV().toLocaleString()}`],
      ['Lowest NPV', `$${this.getLowestNPV().toLocaleString()}`],
      ['Total Scenarios', this.getResultsCount().toString()],
      ['Rate Range', `${this.getMinDiscountRate().toFixed(2)}% - ${this.getMaxDiscountRate().toFixed(2)}%`]
    ];

    autoTable(doc, {
      startY: 60,
      head: [['Metric', 'Value']],
      body: summaryData,
      theme: 'grid',
      headStyles: {fillColor: [41, 128, 185]},
      margin: {left: 20, right: 20}
    });

    // Add detailed results table
    doc.setFontSize(14);
    doc.setFont('helvetica', 'bold');
    doc.text('Detailed Analysis', 20, (doc as any).lastAutoTable.finalY + 20);

    const tableData = this.calculationResults.results.map(result => [
      `${result.discountRate.toFixed(2)}%`,
      `$${result.npv.toLocaleString()}`,
      result.npv >= 0 ? 'Accept' : 'Reject'
    ]);

    autoTable(doc, {
      startY: (doc as any).lastAutoTable.finalY + 30,
      head: [['Discount Rate', 'NPV (USD)', 'Investment Decision']],
      body: tableData,
      theme: 'striped',
      headStyles: {fillColor: [52, 152, 219]},
      margin: {left: 20, right: 20},
      styles: {fontSize: 8},
      columnStyles: {
        1: {halign: 'right'},
        2: {halign: 'center'}
      },
      didParseCell: function (data) {
        if (data.column.index === 1 || data.column.index === 2) {
          const npvValue = parseFloat(tableData[data.row.index][1].replace(/[$,]/g, ''));
          if (npvValue >= 0) {
            data.cell.styles.textColor = [0, 128, 0]; // Green
          } else {
            data.cell.styles.textColor = [255, 0, 0]; // Red
          }
        }
      }
    });

    // Save the PDF
    const fileName = `NPV_Analysis_${new Date().toISOString().split('T')[0]}.pdf`;
    doc.save(fileName);
  }
}
