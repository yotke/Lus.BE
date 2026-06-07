import { Component, Input, SimpleChanges } from '@angular/core';
import { ProjectTemplate } from 'src/app/Infrastructure/Classes & Models/Classes/project-template';
import { ProjectTime, TimesArray } from 'src/app/Infrastructure/Classes & Models/Classes/project-time';
import { ProjectTemplateService } from 'src/app/Infrastructure/Services/projectTemplateService/project-template.service';
import * as XLSX from 'xlsx-js-style';

@Component({
  selector: 'app-excel-export',
  templateUrl: './excel-export.component.html',
  styleUrls: ['./excel-export.component.scss']
})
export class ExcelExportComponent {
  isFirstTime: boolean = true;
  @Input('Project') CurrProject: ProjectTemplate | undefined = undefined;
  @Input('MonthlyProjects') MonthlyProjects: ProjectTemplate[] | undefined = undefined;
  @Input() hasCollision: boolean = false;
  @Input() data: any[] = [];
  @Input() timeData: any[] = [];
  totalSumData: any[] = [];
  signatureData: any[] = [];
  excelOutputData: any[] = [];
  excelSummiraizeOutputData: any[] = [];
  lineConnected: any[] = [];
  lineConnectedSum: any[] = [];
  rowIdxToConnect = 1;
  iteration: number = 0;
  pprevWorkDescription: any = null;
  borderStyleStrong: any = {
    top: { style: "thick", color: { rgb: "000000" } },
    bottom: { style: "thick", color: { rgb: "000000" } },
    left: { style: "thick", color: { rgb: "000000" } },
    right: { style: "thick", color: { rgb: "000000" } }
  };
  borderStyle: any = {
    top: { style: "thin", color: { rgb: "000000" } },
    bottom: { style: "thin", color: { rgb: "000000" } },
    left: { style: "thin", color: { rgb: "000000" } },
    right: { style: "thin", color: { rgb: "000000" } }
  };
  alignmentStyle: any = {
    horizontal: 'center',
    vertical: 'center',
    // wrapText: true
  }
  verticalBorderStyle = {
    left: { style: 'thick', color: { auto: 1 } },
    right: { style: 'thick', color: { auto: 1 } }
  };

  horizontalBorderStyle = {
    top: { style: 'thick', color: { auto: 1 } },
    bottom: { style: 'thick', color: { auto: 1 } }
  };

  strongRightBorderStyle = {
    right: { style: 'thick', color: { auto: 1 } }
  };
  strongTopBorderStyle = {
    top: { style: 'thick', color: { auto: 1 } }
  };

  doubleBorderStyle = {
    top: { style: "double", color: { rgb: "000000" } },
    bottom: { style: "double", color: { rgb: "000000" } },
    left: { style: "double", color: { rgb: "000000" } },
    right: { style: "double", color: { rgb: "000000" } }
  }
  doubleRightBorderStyle = {
    right: { style: 'double', color: { auto: 1 } }
  };
  doubleTopBorderStyle = {
    top: { style: 'double', color: { auto: 1 } }
  };
  doubleLeftBorderStyle = {
    left: { style: 'double', color: { auto: 1 } }
  };
  doubleBottomBorderStyle = {
    bottom: { style: 'double', color: { auto: 1 } }
  };

  constructor(private projectSvc: ProjectTemplateService) { }


  ngOnInit() {
    this.initExelOutputArray(this.MonthlyProjects);

  }


  async exportToExcel() {
    await this.projectSvc.currSystemDate$.subscribe(async date => {
      await this.projectSvc.GetMonthlyProjects(date ?? new Date()).subscribe(monthlyProjects => {
        this.MonthlyProjects = monthlyProjects;
    
        
        this.initExelOutputArray(this.MonthlyProjects);
        this.createExels()
      })

    })

  }
  createExels() {
    if (this.hasCollision) {
      return;
    }
    const ws1 = XLSX.utils.json_to_sheet([...this.excelOutputData]);
    ws1['!merges'] = this.lineConnected;
    this.addDoubleRightBorder(ws1);
    this.addDoubleLeftBorder(ws1);
    this.initSummiraizeExelOutput(this.MonthlyProjects);

    // Iterate through each row in ws1 and set the row height
    for (let rowIndex = 2; rowIndex <= this.excelOutputData.length; rowIndex++) {
      const rowNumber = rowIndex.toString();
      const rowHeight = 25; // Set your desired row height here (in points)

      // Check if the rows object exists, if not initialize it
      ws1['!rows'] = ws1['!rows'] || [];

      // Set the row height for each row
      ws1['!rows'][rowNumber as any] = { hpx: rowHeight }; // Cast rowNumber to any or use parseInt(rowNumber)
    }

    const ws2 = XLSX.utils.json_to_sheet([...this.excelSummiraizeOutputData]); // assuming you have another dataset for the new sheet
    ws2['!merges'] = this.lineConnectedSum;
    this.addDoubleRightBorder(ws2, true);
    this.addDoubleLeftBorder(ws2);
    const wb: XLSX.WorkBook = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws1, 'Sheet1');
    XLSX.utils.book_append_sheet(wb, ws2, 'Sheet2'); // append the second sheet

    XLSX.writeFile(wb, 'data.xlsx');
  }
  initSummiraizeExelOutput(MonthlyProjects: ProjectTemplate[] | undefined) {
    console.log(MonthlyProjects);
    console.log('this.hasCollision', this.hasCollision);
    this.iteration = 0;
    this.lineConnectedSum = [];
    this.rowIdxToConnect = 1;
    this.excelSummiraizeOutputData = [];

    this.initTimeDataFromTimeArraySummriaze(MonthlyProjects);
    this.excelSummiraizeOutputData.push(...this.timeData)

  }

  initExelOutputArray(MonthlyProjects: ProjectTemplate[] | undefined) {
    console.log(MonthlyProjects);
    console.log('this.hasCollision', this.hasCollision);
    this.iteration = 0;
    this.lineConnected = [];
    this.rowIdxToConnect = 1;
    this.excelOutputData = [];

    MonthlyProjects?.forEach(currProject => {
      this.sortProjectTimes(currProject);
      this.initDataFromCurrentProject(currProject);
      this.initTimeDataFromTimeArray(currProject); // Modify the ws worksheet with your time data
      this.initSumTimeData(currProject);
      this.initSignatureData(currProject);
      this.excelOutputData.push(...this.data, ...this.timeData, ...this.totalSumData, ...this.signatureData)
      this.iteration++;
    })
  }
  addDoubleRightBorder(ws: XLSX.WorkSheet, is_e: boolean = false) {
    for (let rowIndex = 2; rowIndex <= (is_e ? this.excelSummiraizeOutputData.length : this.excelOutputData.length); rowIndex++) {
      let cellAddress = is_e ? `E${rowIndex}` : `G${rowIndex}`;

      // Check if the cell already exists in your worksheet. If not, create it.
      if (!ws[cellAddress]) {
        ws[cellAddress] = { t: 'z' };  // Initialize as a stub cell
      }

      // If the cell doesn't have a style property, create it. Then, set the right border to the strong style.
      if (!ws[cellAddress].s) {
        ws[cellAddress].s = { border: {} };
      }
      ws[cellAddress].s.border = { ...ws[cellAddress].s.border, ...this.doubleRightBorderStyle };
    }
  }
  addDoubleLeftBorder(ws: XLSX.WorkSheet) {
    for (let rowIndex = 2; rowIndex <= this.excelOutputData.length; rowIndex++) {
      let cellAddress = `A${rowIndex}`;  // Adjusted to column "A"

      // Check if the cell already exists in your worksheet. If not, create it.
      if (!ws[cellAddress]) {
        ws[cellAddress] = { t: 'z' };  // Initialize as a stub cell
      }

      // If the cell doesn't have a style property, create it. Then, set the left border to the double style.
      if (!ws[cellAddress].s) {
        ws[cellAddress].s = { border: {} };
      }
      ws[cellAddress].s.border = { ...ws[cellAddress].s.border, ...this.doubleLeftBorderStyle };
    }
  }
  createLineConnected(row_i: number, col_i: number, col_j: number) {
    return {
      s: { r: this.rowIdxToConnect + row_i, c: col_i }, e: { r: this.rowIdxToConnect + row_i, c: col_j }
    }
  }

  initDataFromCurrentProject(currProject: ProjectTemplate | undefined) {
    this.rowIdxToConnect = 1 + this.excelOutputData.length;
    this.lineConnected.push(
      this.createLineConnected(0, 0, 6),
      this.createLineConnected(3, 0, 6),
      this.createLineConnected(5, 2, 4)
    )
    this.data = [];
    if (currProject) {

      const BigHeadlineRow = ["", "", "", "", "", "", `${currProject.EmployeeSectionName}`].map(cell => {
        return {
          v: cell, t: "s", s:
          {
            font: { bold: true, name: 'David', sz: 24 },
            alignment: this.alignmentStyle,
            border: this.doubleTopBorderStyle
          }
        }; // regular style for other cells
      }).reverse();

      // ${currProject.EmployeeSectionName} -
      const SmallHeadLine = ["", "", "", "", "", "", `דוח ביצוע שעות עבודה בחודש ${this.changeCurrDate(currProject.CurrentDate)} - פרויקט ${currProject.Name} - ${currProject.SectionName}`].map(cell => {
        return {
          v: cell, t: "s", s:
          {
            font: { bold: true, name: 'David', sz: 16 },
            alignment: this.alignmentStyle,
            border: this.doubleBottomBorderStyle
          }
        }; // regular style for other cells
      }).reverse();

      this.data.push(BigHeadlineRow);
      this.data.push([]); // empty row
      this.data.push([]); // empty row
      this.data.push(SmallHeadLine)



      const regularRow1 = [`${currProject.ConstrctorTitle}`, `${currProject.ConstrctorAddress}`, " ", "", "", `פלאפון: ${currProject.ConstrctorPhone}`, `עוסק מורשה מס': ${currProject.ConstrctorEntrepreneurNumber}`].map((cell, index) => {
        let style: any = {
          font: { bold: true, name: 'David', sz: 14 },
          alignment: this.alignmentStyle,
          border: this.borderStyle
        };

        // if (index === 4) {
        //   style.fill = { fgColor: { rgb: "FFFAD02E" } };
        //   style.font.color = { rgb: "FFFF0000" };  // Red font color added here for the specific cell
        // }

        return {
          v: cell || "",
          t: "s",
          s: style
        };
      }).reverse();
      this.lineConnected
      const regularRow2 = [`עירית ${currProject.ProjectLocation}`, `${this.changeCurrDate(currProject.CurrentDate)}`, " ", "", ` מ.פרויקט ${currProject.ProjectManager}`, `מספר חשבון`, `חשבונית מס': ${currProject.AccountNumber}`].map((cell, index) => {
        let style: any = {
          font: { bold: true, name: 'David', sz: 14 },
          alignment: this.alignmentStyle,
          border: this.borderStyle
        };

        if (index === 4) {
          style.fill = { fgColor: { rgb: "FFFAD02E" } };
          style.font.color = { rgb: "FFFF0000" };  // Red font color added here for the specific cell
        }

        return {
          v: cell || "",
          t: "s",
          s: style
        };
      }).reverse();

      const regularRow3 = [`המתכנן`, `${currProject.ConstrctorName}`, " ", " ", " ", `נושא הפרויקט`, `${currProject.ProjectSubject}`].map(cell => {
        return {
          v: cell, t: "s", s:
          {
            font: { bold: true, name: 'David', sz: 14 },
            alignment: this.alignmentStyle,
            border: this.borderStyle
          }
        }; // regular style for other cells
      }).reverse();

      const regularRow4 = [`מספר החוזה`, `${currProject.WorkContractNumber}`, " ", " ", " ", `שם העובד`, `${currProject.WorkerName}`].map(cell => {
        return {
          v: cell, t: "s", s:
          {
            font: { bold: true, name: 'David', sz: 14 },
            alignment: this.alignmentStyle,
            border: this.borderStyle
          }
        }; // regular style for other cells
      }).reverse();

      const regularRow5 = [`תוקף התחלת חוזה`, `${this.changeDateToStringDate(currProject.StartContractDate)}`, " ", " ", " ", `רמת תעריף`, `${currProject.WorkKindRate}`].map(cell => {
        return {
          v: cell, t: "s", s:
          {
            font: { bold: true, name: 'David', sz: 14 },
            alignment: this.alignmentStyle,
            border: this.borderStyle
          }
        }; // regular style for other cells
      }).reverse();

      const regularRow6 = [`תוקף סוף חוזה`, `${this.changeDateToStringDate(currProject.EndContractDate)}`, " ", " ", " ", `היחידה המעסיקה`, `${currProject.WorkContractNumber}`].map(cell => {
        return {
          v: cell, t: "s", s:
          {
            font: { bold: true, name: 'David', sz: 14 },
            alignment: this.alignmentStyle,
            border: this.borderStyle
          }
        }; // regular style for other cells
      }).reverse();




      this.data.push(
        regularRow1,
        regularRow2,
        regularRow3,
        regularRow4,
        regularRow5,
        regularRow6,
      )
    }
    this.data.push([]); // empty row
  }
  initTimeDataFromTimeArraySummriaze(MonthlyProjects: ProjectTemplate[] | undefined) {
    this.timeData = [];
    let bigHeadLine = ["", "", "", "", `סיכום שעות עבודה לחודש ${this.changeCurrDate(this.MonthlyProjects ? this.MonthlyProjects[0].CurrentDate : new Date())}`].map(cell => {
      return {
        v: cell, t: "s", s:
        {
          font: { bold: true, name: 'David', sz: 20 },
          alignment: this.alignmentStyle,
          border: this.doubleBorderStyle
        }
      }; // regular style for other cells

    }).reverse();
    this.lineConnectedSum.push(
      {
        s: { r: this.rowIdxToConnect, c: 0 },
        e: { r: this.rowIdxToConnect, c: 4 }
      }, // merge cells for date
    )
    this.timeData.push(bigHeadLine);
    this.timeData.push([]);
    this.timeData.push([]);
    this.rowIdxToConnect += 3;
    let smallHeadLine = ["", "", "", "", 'ש"ע אהובה אליה דרכים'].map(cell => {
      return {
        v: cell, t: "s", s:
        {
          font: { bold: true, name: 'David', sz: 16 },
          alignment: this.alignmentStyle,
          border: this.doubleBorderStyle
        }
      }; // regular style for other cells

    }).reverse();
    this.lineConnectedSum.push(
      {
        s: { r: this.rowIdxToConnect, c: 0 },
        e: { r: this.rowIdxToConnect, c: 4 }
      }, // merge cells for date
    )
    this.timeData.push(smallHeadLine);
    this.rowIdxToConnect += 2;


    const headLineRow = ['תאריך',
      'יום השבוע',
      'שעת התחלה',
      'שעת סיום',
      'שם הפרויקט',
    ].map(cell => {
      return {
        v: cell, t: "s", s:
        {
          font: { bold: true, name: 'David', sz: 14 },
          alignment: this.alignmentStyle,
          border: this.borderStyle
        }
      }; // regular style for other cells

    }).reverse();
    this.timeData.push(headLineRow);
    let colors = [
      "FFDDEEDD", // subtle green
      "FFEEDDFF", // subtle purple
      "FFFFEEEE", // subtle red/pink
      "FFFFF4DD", // subtle orange
      "FFEEDDDD", // subtle rose
      "FFDDEEEE", // subtle cyan
      "FFEEDDEE", // subtle magenta
      "FFEEEEEE", // subtle gray
      "FFDDEEDD", // subtle mint (repeat due to limited palette)
      "FFEEEEDD"  // subtle yellow
    ];
    if (MonthlyProjects) {
      MonthlyProjects?.forEach((currProject, projectIdx) => {
        this.sortProjectTimes(currProject);
        currProject.ProjectTimes.forEach((projectTime, index) => {
          let dataArray = this.convertDateRowWorkToExcelLineToSummiraizeData(currProject, projectTime, index, colors[projectIdx]);
          this.timeData.push(...dataArray);
        });
      });
    }


    // console.log(this.timeData);

  }


  initTimeDataFromTimeArray(currProject: ProjectTemplate | undefined) {
    this.rowIdxToConnect += 12;

    if (currProject) {

      this.timeData = [];
      const headLineRow = ['תאריך',
        'יום השבוע',
        'שעת התחלה',
        'שעת סיום',
        'סהכ שעות עבודה',
        'מקום העבודה',
        'תאור העבודה',
      ].map(cell => {
        return {
          v: cell, t: "s", s:
          {
            font: { bold: true, name: 'David', sz: 14 },
            alignment: this.alignmentStyle,
            border: this.borderStyle
          }
        }; // regular style for other cells

      }).reverse();
      this.timeData.push(headLineRow);
      let prevDescription: any = null
      let dataArray = null;
      currProject.ProjectTimes.forEach((projectTime, index) => {
        if (prevDescription != projectTime.WorkDescription && projectTime.WorkDescription != null && projectTime.WorkDescription != '') {
          dataArray = this.convertDateRowWorkToExcelLine(currProject, projectTime, index, true);
        }
        else {
          dataArray = this.convertDateRowWorkToExcelLine(currProject, projectTime, index, false);
        }

        prevDescription = projectTime.WorkDescription;
        this.timeData.push(...dataArray)

      });
    }


    // console.log(this.timeData);

  }
  initSumTimeData(currProject: ProjectTemplate | undefined) {

    this.totalSumData = [];
    if (currProject) {

      let totHours = this.calculateTotWorkHours(currProject);
      const regularRow1 = [`סך הכל שעות עבודה`, totHours, "", "", "", "", ""].map(cell => {
        if (cell) {
          return {
            v: cell, t: "s", s:
            {
              font: { bold: true, name: 'David', sz: 14 },
              alignment: this.alignmentStyle,
              border: this.borderStyle
            }
          }; // regular style for other cells
        }
        else {
          return {
            v: cell, t: "s", s:
            {
              font: { bold: true, name: 'David', sz: 14 },
              alignment: this.alignmentStyle,
            }
          }
        }
      }).reverse();
      // let cashPerHour = 204.59;
      let cashPerHour = Number(currProject.WorkRate == '' || currProject.WorkRate == null ? '204.59' : currProject.WorkRate);
      console.log(currProject.WorkRate);

      const regularRow2 = [`מחיר לשעת עבודה`, cashPerHour, "", "", "", "", ""].map(cell => {
        if (cell) {
          return {
            v: cell, t: "s", s:
            {
              font: { bold: true, name: 'David', sz: 14 },
              alignment: this.alignmentStyle,
              border: this.borderStyle
            }
          }; // regular style for other cells
        }
        else {
          return {
            v: cell, t: "s", s:
            {
              font: { bold: true, name: 'David', sz: 14 },
              alignment: this.alignmentStyle,
            }
          }
        }
      }).reverse();
      let totCalcPay = cashPerHour * totHours;
      const regularRow3 = [`סה"כ לחשבון`, totCalcPay.toFixed(2), "", "", "", "", ""].map(cell => {
        if (cell) {
          return {
            v: cell, t: "s", s:
            {
              font: { bold: true, name: 'David', sz: 14 },
              alignment: this.alignmentStyle,
              border: this.borderStyle
            }
          }; // regular style for other cells
        }
        else {
          return {
            v: cell, t: "s", s:
            {
              font: { bold: true, name: 'David', sz: 14 },
              alignment: this.alignmentStyle,
            }
          }
        }
      }).reverse();
      let totPayMinusOnePercent = totCalcPay - totCalcPay * 1 / 100;
      const regularRow4 = [`פלוטים -1%`, totPayMinusOnePercent.toFixed(2), "", "", "", "", ""].map(cell => {
        if (cell) {
          return {
            v: cell, t: "s", s:
            {
              font: { bold: true, name: 'David', sz: 14 },
              alignment: this.alignmentStyle,
              border: this.borderStyle
            }
          }; // regular style for other cells
        }
        else {
          return {
            v: cell, t: "s", s:
            {
              font: { bold: true, name: 'David', sz: 14 },
              alignment: this.alignmentStyle,
            }
          }
        }
      }).reverse();
      let taxRate = 18;
      let totValuedTax = totPayMinusOnePercent * taxRate / 100;
      const regularRow5 = [`מע"מ ${taxRate}%`, totValuedTax.toFixed(2), "", "", "", "", ""].map(cell => {
        if (cell) {
          return {
            v: cell, t: "s", s:
            {
              font: { bold: true, name: 'David', sz: 14 },
              alignment: this.alignmentStyle,
              border: this.borderStyle
            }
          }; // regular style for other cells
        }
        else {
          return {
            v: cell, t: "s", s:
            {
              font: { bold: true, name: 'David', sz: 14 },
              alignment: this.alignmentStyle,
            }
          }
        }
      }).reverse();
      let totalAccountPay = totPayMinusOnePercent + totValuedTax;
      const regularRow6 = [`סה"כ לחשבון`, totalAccountPay.toFixed(2), "", "", "", "", ""].map(cell => {
        if (cell) {
          return {
            v: cell, t: "s", s:
            {
              font: { bold: true, name: 'David', sz: 14 },
              alignment: this.alignmentStyle,
              border: this.borderStyle
            }
          }; // regular style for other cells
        }
        else {
          return {
            v: cell, t: "s", s:
            {
              font: { bold: true, name: 'David', sz: 14 },
              alignment: this.alignmentStyle,
            }
          }
        }
      }).reverse();

      this.totalSumData.push([]); // empty row

      this.totalSumData.push(
        regularRow1,
        regularRow2,
        regularRow3,
        regularRow4,
        regularRow5,
        regularRow6,
      )

      this.totalSumData.push([]); // empty row

    }
  }

  initSignatureData(currProject: ProjectTemplate | undefined) {
    this.rowIdxToConnect += 8;
    this.signatureData = [];
    if (currProject) {

      const SmallHeadLine = ["", "", "", "", "", "", `הצהרת העובד - הנני מצהיר בזה, כי כל הפרטים לגבי ביצוע שעות העבודה בדו"ח זה הינם נכונים ותואמים את הרישומים של משרדנו`].map(cell => {
        return {
          v: cell, t: "s", s:
          {
            font: { bold: true, name: 'David', sz: 14 },
            alignment: this.alignmentStyle,
            border: this.doubleTopBorderStyle
          }
        }; // regular style for other cells
      }).reverse();
      this.lineConnected.push(
        {
          s: { r: this.rowIdxToConnect, c: 0 }, e: { r: this.rowIdxToConnect, c: 6 }
        },
      )
      this.rowIdxToConnect++;
      this.signatureData.push(SmallHeadLine);

      const signatureRow = [`חתימה`, "", "__________________________________", "", `תאריך:`, "", "__________________________________"].map(cell => {
        return {
          v: cell, t: "s", s:
          {
            font: { bold: true, name: 'David', sz: 14 },
            alignment: this.alignmentStyle,
            // border: this.borderStyle
          }
        }; // regular style for other cells

      }).reverse();
      this.lineConnected.push(
        {
          s: { r: this.rowIdxToConnect, c: 4 }, e: { r: this.rowIdxToConnect, c: 5 }
        },
        {
          s: { r: this.rowIdxToConnect, c: 0 }, e: { r: this.rowIdxToConnect, c: 1 }
        }
      )
      this.rowIdxToConnect++;
      this.signatureData.push(signatureRow);

      const WorkerRow = [`שם העובד:`, "", "", "", "__________________________________", "", ""].map(cell => {
        return {
          v: cell, t: "s", s:
          {
            font: { bold: true, name: 'David', sz: 14 },
            alignment: this.alignmentStyle,
            // border: this.borderStyle
          }
        }; // regular style for other cells

      }).reverse();
      this.lineConnected.push(
        {
          s: { r: this.rowIdxToConnect, c: 2 }, e: { r: this.rowIdxToConnect, c: 5 }
        }
      )
      this.rowIdxToConnect++;
      this.signatureData.push(WorkerRow);

      const approveHeadlineRow = [``, "", "אישור המתכנן", "", "", "", "אישור העיריה"].map(cell => {
        return {
          v: cell, t: "s", s:
          {
            font: { bold: true, name: 'David', sz: 14 },
            alignment: this.alignmentStyle,
            // border: this.borderStyle
          }
        }; // regular style for other cells

      }).reverse();
      this.lineConnected.push(
        {
          s: { r: this.rowIdxToConnect, c: 4 }, e: { r: this.rowIdxToConnect, c: 6 }
        },
        {
          s: { r: this.rowIdxToConnect, c: 0 }, e: { r: this.rowIdxToConnect, c: 2 }
        }
      )
      this.rowIdxToConnect++;
      this.signatureData.push(approveHeadlineRow);

      const approveSmallHeadlineRow = [``, "לגבי סווג רמת התעריף ומספר שעות העובד:", "", "", "שם", "", "__________________________________"].map(cell => {
        return {
          v: cell, t: "s", s:
          {
            font: { bold: true, name: 'David', sz: 14 },
            alignment: this.alignmentStyle,
            // border: this.borderStyle
          }
        }; // regular style for other cells

      }).reverse();
      this.lineConnected.push(
        {
          s: { r: this.rowIdxToConnect, c: 5 }, e: { r: this.rowIdxToConnect, c: 6 }
        },
        {
          s: { r: this.rowIdxToConnect, c: 0 }, e: { r: this.rowIdxToConnect, c: 1 }
        }
      )
      this.rowIdxToConnect++;
      this.signatureData.push(approveSmallHeadlineRow);


      const approveNameRow = [`שם:`, "", "__________________________________", "", "תפקיד", "", "__________________________________"].map(cell => {
        return {
          v: cell, t: "s", s:
          {
            font: { bold: true, name: 'David', sz: 14 },
            alignment: this.alignmentStyle,
            // border: this.borderStyle
          }
        }; // regular style for other cells

      }).reverse();
      this.lineConnected.push(
        {
          s: { r: this.rowIdxToConnect, c: 4 }, e: { r: this.rowIdxToConnect, c: 5 }
        },
        {
          s: { r: this.rowIdxToConnect, c: 0 }, e: { r: this.rowIdxToConnect, c: 1 }
        }
      )
      this.rowIdxToConnect++;
      this.signatureData.push(approveNameRow);

      const approveSignatureRow = [`חתימה:`, "", "__________________________________", "", "חתימה:", "", "__________________________________"].map(cell => {
        return {
          v: cell, t: "s", s:
          {
            font: { bold: true, name: 'David', sz: 14 },
            alignment: this.alignmentStyle,
            // border: this.borderStyle
          }
        }; // regular style for other cells

      }).reverse();
      this.lineConnected.push(
        {
          s: { r: this.rowIdxToConnect, c: 4 }, e: { r: this.rowIdxToConnect, c: 5 }
        },
        {
          s: { r: this.rowIdxToConnect, c: 0 }, e: { r: this.rowIdxToConnect, c: 1 }
        }
      )
      this.rowIdxToConnect++;
      this.signatureData.push(approveSignatureRow);

      const approveDateRow = [``, "", "במידה והמתכנן מעסיק יותר מעובד אחד עליו להגיש גם דף ריכוז לכל העובדים", "", "", "תאריך:", "__________________________________"].map(cell => {
        return {
          v: cell, t: "s", s:
          {
            font: { bold: true, name: 'David', sz: 14 },
            alignment: this.alignmentStyle,
            border: this.doubleBottomBorderStyle
          }
        }; // regular style for other cells

      }).reverse();
      this.lineConnected.push(
        {
          s: { r: this.rowIdxToConnect, c: 4 }, e: { r: this.rowIdxToConnect, c: 6 }
        },

      )
      this.rowIdxToConnect++;
      this.signatureData.push(approveDateRow);

      this.signatureData.push([])
      this.signatureData.push([])
      this.signatureData.push([])
      this.signatureData.push([])
      this.signatureData.push([])
    }
  }


  private calculateTotWorkHours(currProject: ProjectTemplate) {
    let totHours = 0;
    currProject.ProjectTimes.forEach(ProjectTime => {
      ProjectTime.TimesArray.WorkingTimes.forEach(workRow => {
        totHours += this.calculateHoursDifference(workRow.StartTime, workRow.EndTime)
      })
    });
    return totHours;
  }

  private calculateHoursDifference(startTime: string, endTime: string) {
    // Parse start and end times
    let [startHour, startMinute] = startTime.split(':').map(Number);
    let [endHour, endMinute] = endTime.split(':').map(Number);

    // Convert everything to minutes
    let startInMinutes = startHour * 60 + startMinute;
    let endInMinutes = endHour * 60 + endMinute;

    // Calculate difference in minutes
    let differenceInMinutes;

    if (endInMinutes >= startInMinutes) {
      differenceInMinutes = endInMinutes - startInMinutes;
    } else {
      // This handles cases where end time is on the next day
      differenceInMinutes = (24 * 60 - startInMinutes) + endInMinutes;
    }

    // Convert difference back to decimal hours
    let hours = differenceInMinutes / 60;

    return hours;
  }

  private convertDateRowWorkToExcelLineToSummiraizeData(currProject: ProjectTemplate, projectTime: ProjectTime, isFirstTime: number, colors: string) {
    projectTime.DayIndicator = this.findIsraeliDay(new Date(projectTime.WorkDate));
    let data = projectTime.TimesArray.WorkingTimes.map((TimeRow, index: any) => {
      // if (index === projectTime.TimesArray.WorkingTimes.length - 1) { // If it's the last index
      if (index === 0) { // If it's the last index
        return {
          date: this.changeCurrDateDay(projectTime.WorkDate),
          dayIndicator: projectTime.DayIndicator,
          start: TimeRow.StartTime,
          end: TimeRow.EndTime,
          projectName: isFirstTime == 0 ? `פרויקט - ${currProject.Name} - ${currProject.SectionName}` : null
        };
      } else {
        return {
          date: null,  // null indicates we want to merge this cell with the one above
          dayIndicator: null,
          start: TimeRow.StartTime,
          end: TimeRow.EndTime,
          projectName: null
        };
      }
    });

    if (isFirstTime == 0) {
      if (currProject && currProject.ProjectTimes) {
        const totalLength = currProject.ProjectTimes.reduce((acc, projectTime) => {
          if (projectTime.TimesArray.WorkingTimes) {
            return acc + projectTime.TimesArray.WorkingTimes.length;
          }
          return acc;
        }, 0);
        this.lineConnectedSum.push(
          {
            s: { r: this.rowIdxToConnect, c: 0 },
            e: { r: this.rowIdxToConnect + totalLength - 1, c: 0 }
          }, // merge cells for date
        )
      }
    }
    this.lineConnectedSum.push(
      {
        s: { r: this.rowIdxToConnect, c: 3 },
        e: { r: this.rowIdxToConnect + projectTime.TimesArray.WorkingTimes.length - 1, c: 3 }
      },
      {
        s: { r: this.rowIdxToConnect, c: 4 },
        e: { r: this.rowIdxToConnect + projectTime.TimesArray.WorkingTimes.length - 1, c: 4 }
      },
    );


    this.rowIdxToConnect = this.rowIdxToConnect + projectTime.TimesArray.WorkingTimes.length

    let dataArray = data.map(d => {
      return [d.date, d.dayIndicator, d.start, d.end, d.projectName].map((cell, index) => {
        let style: any = {
          font: { bold: true, name: 'David', sz: 14 },
          alignment: this.alignmentStyle,
          border: this.borderStyle
        };

        if (index === 4) {
          style.fill = { fgColor: { rgb: colors } };
        }

        return {
          v: cell || "",
          t: "s",
          s: style
        };
      }).reverse();
    });
    return dataArray;
  }

  private sortProjectTimes(currProject: ProjectTemplate) {
    currProject.ProjectTimes.sort((a, b) => new Date(a.WorkDate).getTime() - new Date(b.WorkDate).getTime());
  }

  private convertDateRowWorkToExcelLine(currProject: ProjectTemplate, projectTime: ProjectTime, isFirstTime: number, isDifferent: boolean) {
    projectTime.DayIndicator = this.findIsraeliDay(new Date(projectTime.WorkDate));

    let data = projectTime.TimesArray.WorkingTimes.map((TimeRow, index: any) => {
      // if (index === projectTime.TimesArray.WorkingTimes.length - 1) { // If it's the last index

      if (index === 0) { // If it's the last index
        return {
          date: this.changeCurrDateDay(projectTime.WorkDate),
          dayIndicator: projectTime.DayIndicator,
          start: TimeRow.StartTime,
          end: TimeRow.EndTime,
          location: TimeRow.WorkLocation,
          totalHours: this.getTotalHours(projectTime.TimesArray).toString(),
          description: isFirstTime == 0 || isDifferent ? projectTime.WorkDescription : null
        };
      } else {
        return {
          date: null,  // null indicates we want to merge this cell with the one above
          dayIndicator: null,
          start: TimeRow.StartTime,
          end: TimeRow.EndTime,
          location: TimeRow.WorkLocation,
          totalHours: null,
          description: null
        };

      }

    });
    if (isFirstTime == 0) {
      if (currProject && currProject.ProjectTimes) {
        if (this.findDifferentProjectDescriptions(currProject).length < 3) {

          const totalLength = currProject.ProjectTimes.reduce((acc, projectTime) => {
            if (projectTime.TimesArray.WorkingTimes) {
              return acc + projectTime.TimesArray.WorkingTimes.length;
            }
            return acc;
          }, 0);
          this.lineConnected.push(
            {
              s: { r: this.rowIdxToConnect, c: 0 },
              e: { r: this.rowIdxToConnect + totalLength - 1, c: 0 }
            }, // merge cells for description
          )
        }
        else {
          let cumulativeSums = this.findDifferentProjectDescriptions(currProject);
          console.log(cumulativeSums);

          for (let i = 1; i < cumulativeSums.length; i++) {
            console.log(this.rowIdxToConnect + cumulativeSums[i - 1], this.rowIdxToConnect + cumulativeSums[i]);

            this.lineConnected.push(
              {
                s: { r: this.rowIdxToConnect + cumulativeSums[i - 1], c: 0 },
                e: { r: this.rowIdxToConnect + cumulativeSums[i] - 1, c: 0 }
              }, // merge cells for description
            );
          }
        }
      }
    }

    this.lineConnected.push(
      {
        s: { r: this.rowIdxToConnect, c: 2 },
        e: { r: this.rowIdxToConnect + projectTime.TimesArray.WorkingTimes.length - 1, c: 2 }
      },
      {
        s: { r: this.rowIdxToConnect, c: 5 },
        e: { r: this.rowIdxToConnect + projectTime.TimesArray.WorkingTimes.length - 1, c: 5 }
      }, // merge cells for dayIndicator
      {
        s: { r: this.rowIdxToConnect, c: 6 },
        e: { r: this.rowIdxToConnect + projectTime.TimesArray.WorkingTimes.length - 1, c: 6 }
      } // merge cells for dayIndicator
    );


    this.rowIdxToConnect = this.rowIdxToConnect + projectTime.TimesArray.WorkingTimes.length

    let dataArray = data.map(d => {
      return [d.date, d.dayIndicator, d.start, d.end, d.totalHours, d.location, d.description].map(cell => {
        return {
          v: cell || "",  // Default to an empty string if the cell is falsy
          t: "s",
          s: {
            font: { bold: true, name: 'David', sz: 14 },
            alignment: this.alignmentStyle,
            border: this.borderStyle
          }
        }
      }).reverse();
    }) // regular style for other cells
    return dataArray;
  }

  private findDifferentProjectDescriptions(currProject: ProjectTemplate): number[] {
    let prevDescription: any = null;
    let rowsDescriptionArray = [];
    let currSumRows = 0;

    currProject.ProjectTimes.forEach((projectTime) => {
      if (projectTime.WorkDescription == prevDescription || !projectTime.WorkDescription) {
        if (projectTime.TimesArray.WorkingTimes) {
          currSumRows += projectTime.TimesArray.WorkingTimes.length;
        }
      } else {
        // Push the cumulative sum for the previous group
        rowsDescriptionArray.push(currSumRows);

        // Accumulate the sum for the current group
        currSumRows = projectTime.TimesArray.WorkingTimes ? projectTime.TimesArray.WorkingTimes.length : 0;
      }

      // Update the previous work description for the next iteration
      prevDescription = projectTime.WorkDescription;
    });

    // Push the final cumulative sum for the last group
    rowsDescriptionArray.push(currSumRows);

    // Create a new array (sumsArray) where each cell contains the sum of all cells before it and the value of the current cell
    let sumsArray: number[] = [];
    let cumulativeSum = 0;

    rowsDescriptionArray.forEach((value) => {
      cumulativeSum += value;
      sumsArray.push(cumulativeSum);
    });

    // Now, sumsArray contains the desired sums for each cell
    return sumsArray;
  }
  private findIsraeliDay(date: Date): string {
    // Using 'he-IL' locale for Hebrew, Israel
    return date.toLocaleString('he-IL', { weekday: 'long' });
  }

  private getTotalHours(timesArray: TimesArray): number {
    let totalHours = 0;

    timesArray.WorkingTimes.forEach(timeRow => {
      console.log('timeRow.StartTime',timeRow.StartTime);
      console.log('timeRow.EndTime',timeRow.EndTime);
      
      const start = this.convertTo24HourFormat(timeRow.StartTime);
      const end = this.convertTo24HourFormat(timeRow.EndTime);
      console.log('end',end);
      console.log('start',start);
      
      const startDate = new Date(`1970-01-01T${start}:00`);
      const endDate = new Date(`1970-01-01T${end}:00`);

      let durationHours = (endDate.getTime() - startDate.getTime()) / (1000 * 60 * 60);
      totalHours += durationHours;
    });

    // Round totalHours to the nearest 0.5
    const roundedHours = Math.round(totalHours * 2) / 2;

    console.log("Rounded Hours:", roundedHours);  // Debugging line

    return roundedHours;
}

private convertTo24HourFormat(time: string): string {
  const lowerTime = time.toLowerCase().trim();

  // Check if the time string ends with "am" or "pm"
  if (lowerTime.endsWith('am') || lowerTime.endsWith('pm')) {
      // Extract period and the time portion without the period
      const period = lowerTime.slice(-2);
      const timeWithoutPeriod = lowerTime.slice(0, -2).trim();
      let [hourStr, minuteStr] = timeWithoutPeriod.split(":");
      const hourNum = parseInt(hourStr);
      const minuteNum = minuteStr ? parseInt(minuteStr) : 0;
      let hour = hourNum;
      let minute = minuteNum;

      // Convert to 24-hour format
      if (period === 'pm' && hour !== 12) {
          hour += 12;
      }
      if (period === 'am' && hour === 12) {
          hour = 0;
      }

      return `${hour.toString().padStart(2, '0')}:${minute.toString().padStart(2, '0')}`;
  } else {
      // Assume time is already in 24-hour format
      // Split the time into hours and minutes
      let [hourStr, minuteStr] = lowerTime.split(":");
      const hour = parseInt(hourStr);
      const minute = minuteStr ? parseInt(minuteStr) : 0;
      return `${hour.toString().padStart(2, '0')}:${minute.toString().padStart(2, '0')}`;
  }
}




  private changeCurrDate(CurrentDate: Date): string {
    const date = new Date(CurrentDate);
    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    return `${month}/${date.getFullYear()}`;
  }
  private changeCurrDateDay(CurrentDate: Date): string {
    const date = new Date(CurrentDate);
    const day = date.getDate().toString().padStart(2, '0');  // Extract the day
    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    return `${day}/${month}`;  // Format as day/month
  }
  private changeDateToStringDate(CurrentDate: Date): string {
    const date = new Date(CurrentDate);
    const day = date.getDate().toString().padStart(2, '0');
    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    return `${day}/${month}/${date.getFullYear()}`;
  }


}